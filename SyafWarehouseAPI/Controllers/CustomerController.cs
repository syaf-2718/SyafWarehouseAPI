using Microsoft.AspNetCore.Mvc;
using SyafWarehouseAPI.DTO;
using SyafWarehouseAPI.Models;
using SyafWarehouseAPI.Services;

namespace SyafWarehouseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Path will be: api/customers
    public class CustomersController : ControllerBase
    {
        //private static readonly List<Customer> _customers = new();
        private readonly CustomerDataStore _dataStore;

        //Inject the CustomerDataStore 
        public CustomersController(CustomerDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        // 1. ENDPOINT: Register New Customer (Generates OTPs)
        // POST: api/customers/register
        [HttpPost("register")]
        public IActionResult RegisterCustomer([FromBody] Customer newCustomer)
        {
            // Input Validation checks
            if (string.IsNullOrWhiteSpace(newCustomer.ICNumber) || string.IsNullOrWhiteSpace(newCustomer.Name))
            {
                return BadRequest(new { message = "Name and IC Number are required." });
            }

            // Duplicate IC check rule
            bool icExists = _dataStore.Customers.Any(c => c.ICNumber.Replace("-", "").Trim() == newCustomer.ICNumber.Replace("-", "").Trim());
            if (icExists)
            {
                return Conflict(new
                {
                    subject = "Account already exist",
                    message = "There is account registered with the IC number. Please login to continue."
                });
            }

            // Generate Mock 6-Digit OTP numbers (e.g., 123456)
            var random = new Random();
            newCustomer.PhoneOtp = random.Next(100000, 999999).ToString();
            newCustomer.EmailOtp = random.Next(100000, 999999).ToString();

            // Assign mock auto-incremented ID and save to cache memory
            newCustomer.Id = _dataStore.Customers.Count + 1;
            _dataStore.Customers.Add(newCustomer);

            // In a production app, you would plug in SMS (Twilio) and Email (SMTP/SendGrid) services here
            return CreatedAtAction(nameof(CheckExistingCustomer), new { icNumber = newCustomer.ICNumber }, new
            {
                message = "Registration initialized. OTPs have been sent.",
                icNumber = newCustomer.ICNumber,
                debugPhoneOtp = newCustomer.PhoneOtp, 
                debugEmailOtp = newCustomer.EmailOtp  
            });
        }

        // 2. NEW ENDPOINT: Verify the Phone and Email OTPs
        // POST: api/customers/verify-otp
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ICNumber))
            {
                return BadRequest(new { message = "IC Number is required to look up account details." });
            }

            // Find the pending account record
            var customer = _dataStore.Customers.FirstOrDefault(c => c.ICNumber.Replace("-", "").Trim() == request.ICNumber.Replace("-", "").Trim());
            if (customer == null)
            {
                return NotFound(new { message = "No pending registration found for this IC number." });
            }

            // Check Phone OTP
            if (customer.PhoneOtp == request.PhoneOtpCode)
            {
                customer.IsPhoneVerified = true;
            }

            // Check Email OTP
            if (customer.EmailOtp == request.EmailOtpCode)
            {
                customer.IsEmailVerified = true;
            }

            // Evaluate Verification Status Results
            if (customer.IsFullyVerified)
            {
                return Ok(new
                {
                    status = "Success",
                    message = "Both Phone and Email have been successfully verified! Account activation complete.",
                    customerName = customer.Name
                });
            }

            return BadRequest(new
            {
                status = "Failed",
                message = "Verification incomplete or invalid code provided.",
                phoneStatus = customer.IsPhoneVerified ? "Verified" : "Invalid/Missing OTP",
                emailStatus = customer.IsEmailVerified ? "Verified" : "Invalid/Missing OTP"
            });
        }

        // 3. ENDPOINT: Check if IC already exists
        // GET: api/customers/check/{icNumber}
        [HttpGet("check/{icNumber}")]
        public IActionResult CheckExistingCustomer(string icNumber)
        {
            if (string.IsNullOrWhiteSpace(icNumber))
            {
                return BadRequest(new { message = "IC Number parameter is required." });
            }

            // Find matching customer by IC
            var existingCustomer = _dataStore.Customers.FirstOrDefault(c => c.ICNumber.Replace("-", "").Trim() == icNumber.Replace("-", "").Trim());

            if (existingCustomer != null)
            {
                return Ok(new
                {
                    exists = true,
                    subject = "Account already exist",
                    message = "There is account registered with the IC number. Please login to continue.",
                    customerName = existingCustomer.Name
                });
            }

            return Ok(new
            {
                exists = false,
                message = "This IC number is available for registration."
            });
        }

        // 4. ENDPOINT: Finalize Account Setup (Privacy Policy, PIN Matching, & Biometrics)
        // POST: api/customers/finalize-setup
        [HttpPost("finalize-setup")]
        public IActionResult FinalizeAccountSetup([FromBody] AccountSetupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ICNumber))
            {
                return BadRequest(new { message = "IC Number is required." });
            }

            // 1. Locate the customer profile
            var customer = _dataStore.Customers.FirstOrDefault(c => c.ICNumber.Replace("-", "").Trim() == request.ICNumber.Replace("-", "").Trim());
            if (customer == null)
            {
                return NotFound(new { message = "Account registration details not found." });
            }

            // 2. Safeguard: Must pass OTP validation first
            if (!customer.IsFullyVerified)
            {
                return BadRequest(new { message = "Access Denied. You must complete Phone and Email OTP verification first." });
            }

            // 3. Validation: Privacy Policy Agreement Requirement
            if (!request.AgreeToPrivacyPolicy)
            {
                return BadRequest(new { message = "Validation Failed: You must accept the Privacy Policy agreement to proceed." });
            }

            // 4. Validation: PIN format check (Must be exactly 6 numeric digits)
            if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length != 6 || !request.Pin.All(char.IsDigit))
            {
                return BadRequest(new { message = "Validation Failed: PIN must be exactly 6 numeric digits." });
            }

            // 5. Validation: PIN Matching Rules
            if (request.Pin != request.ConfirmPin)
            {
                return BadRequest(new { message = "Validation Failed: PIN and Confirm PIN do not match." });
            }

            // 6. Save Security Preferences and Activate Account
            customer.HasAcceptedPrivacyPolicy = true;
            customer.SecurePin = request.Pin; // In a production app, hash this value before saving!
            customer.IsBiometricEnabled = request.EnableBiometric;
            customer.IsAccountActive = true;

            // 7. Success Response: Issue auth token payload signaling frontend to navigate to the Home Page
            return Ok(new
            {
                status = "Success",
                message = "Account security setup complete! User authenticated.",
                navigateTo = "HomePage",
                authToken = Guid.NewGuid().ToString(), // Mock authentication token string
                userProfile = new
                {
                    name = customer.Name,
                    email = customer.Email,
                    biometricsActive = customer.IsBiometricEnabled
                }
            });
        }

        // 5. UNIFIED LOGIN ENDPOINT: Checks account status and routes to Login or Activation
        // POST: api/customers/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ICNumber))
            {
                return BadRequest(new { message = "IC Number is required." });
            }

            // 1. Look up the user in the system by IC
            var customer = _dataStore.Customers.FirstOrDefault(c => c.ICNumber.Replace("-", "").Trim() == request.ICNumber.Replace("-", "").Trim());

            if (customer == null)
            {
                return NotFound(new
                {
                    subject = "Account Not Found",
                    message = "No record found with this IC number. Please contact administration."
                });
            }

            // =========================================================================
            // CASE A: User exists but has NOT activated their security layout yet
            // =========================================================================
            if (!customer.IsAccountActive)
            {
                // Reset verification states for a clean session
                customer.IsPhoneVerified = false;
                customer.IsEmailVerified = false;

                // Auto-generate fresh 6-digit OTP codes
                var random = new Random();
                customer.PhoneOtp = random.Next(100000, 999999).ToString();
                customer.EmailOtp = random.Next(100000, 999999).ToString();

                // Instruct frontend to divert the user to the OTP activation path
                return Ok(new
                {
                    status = "ActivationRequired",
                    message = "Your profile exists but requires security activation. OTPs have been sent to your registered phone and email.",
                    navigateTo = "OtpVerificationPage",
                    icNumber = customer.ICNumber,
                    debugPhoneOtp = customer.PhoneOtp, // Provided for easy copy-pasting in Scalar testing
                    debugEmailOtp = customer.EmailOtp  // Provided for easy copy-pasting in Scalar testing
                });
            }

            // =========================================================================
            // CASE B: User is fully active -> Perform Standard PIN authentication
            // =========================================================================
            if (string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest(new { message = "Your account is active. Please provide your 6-digit PIN to login." });
            }

            if (customer.SecurePin != request.Pin)
            {
                return Unauthorized(new
                {
                    subject = "Invalid Credentials",
                    message = "The PIN you entered is incorrect. Please try again."
                });
            }

            // Authentication Success
            return Ok(new
            {
                status = "Success",
                message = "Login successful! Welcome back.",
                navigateTo = "HomePage",
                authToken = Guid.NewGuid().ToString(),
                userProfile = new
                {
                    name = customer.Name,
                    email = customer.Email,
                    biometricsActive = customer.IsBiometricEnabled
                }
            });
        }

    }
}
