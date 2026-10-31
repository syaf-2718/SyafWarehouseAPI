namespace SyafWarehouseAPI.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ICNumber { get; set; } = string.Empty; // National ID
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string PhoneOtp { get; set; } = string.Empty;
        public string EmailOtp { get; set; } = string.Empty;
        public bool IsPhoneVerified { get; set; } = false;
        public bool IsEmailVerified { get; set; } = false;
        public bool IsFullyVerified => IsPhoneVerified && IsEmailVerified;

        public bool HasAcceptedPrivacyPolicy { get; set; } = false;
        public string SecurePin { get; set; } = string.Empty; // Stores the finalized 6-digit PIN
        public bool IsBiometricEnabled { get; set; } = false;
        public bool IsAccountActive { get; set; } = false;    // Flips to true once PIN setup is complete
    }
}
