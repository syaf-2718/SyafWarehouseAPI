# 📖 Customer Identity & Security Architecture Documentation

This document serves as the developer engineering reference for the **SyafWarehouseAPI** unified account activation and secure credential onboarding lifecycle.

## 🛠️ Architectural Strategy: Strategy A (Unified Flow)
The backend leverages an **orchestrated single-entry point** via the `/login` pipeline. Instead of forcing separate frontend UI routes for registration checks and first-time setups, the architecture evaluates the account status dynamically:
1. **Unactivated Profiles** (`IsAccountActive == false`) automatically trigger a secure state reset, generate verification tokens, and dispatch multi-factor OTP models.
2. **Active Profiles** (`IsAccountActive == true`) bypass registration loops to execute direct secure PIN authentication routines.

---

## 📂 Core Solution Architecture Blueprint

```text
SyafWarehouseAPI/
│
├── 📂 Controllers/          
│   └── CustomersController.cs    <-- Manages REST HTTP routing & validation logic
│
├── 📂 DTOs/ (Data Transfer Objects)
│   ├── LoginRequest.cs           <-- Input structure matching IC / PIN
│   ├── OtpVerificationRequest.cs <-- Expected fields for multi-factor checks
│   └── AccountSetupRequest.cs    <-- Payload layout for privacy, PIN, and biometrics
│
├── 📂 Models/               
│   └── Customer.cs               <-- Core data object model blueprint
│
└── 📂 Services/             
    └── CustomerDataStore.cs      <-- In-memory state machine repository service (Singleton)
```

---

## 🚦 Endpoints & Lifecycle Matrix

### 1. Identity Check
* **Route:** `GET /api/customers/check/{icNumber}`
* **Purpose:** Queries the directory registry to verify if an identity profile already exists.

### 2. The Unified Entrance (Login / Initialize Activation)
* **Route:** `POST /api/customers/login`
* **Purpose:** If `pin` is omitted or `IsAccountActive` evaluates to false, it generates numeric tokens and directs the client application state to `OtpVerificationPage`. If the profile is active, it runs direct authentication logic.

### 3. Multi-Factor Token Verification
* **Route:** `POST /api/customers/verify-otp`
* **Purpose:** Validates the email and phone network validation keys sequentially. If both match, it marks the account state flags as verified and directs the client layout state to `PinSetupPage`.

### 4. Account Security Finalization
* **Route:** `POST /api/customers/finalize-setup`
* **Purpose:** Enforces data privacy compliance, validates that the credential consists of exactly 6 numeric characters, checks entry uniformity, and sets the active token layout targeting the app `HomePage`.

---

## 🧪 Interactive Testing Playbook (Scalar / Swagger)

### 🟢 1. Happy Path Sequence (Success Flow)

Follow these steps in sequential order using the pre-loaded testing profile (`950101-14-1234`):

#### Step 1: Initial Identity Verification
* **Method & URL:** `GET` `http://localhost:5062/api/Customers/check/950101-14-1234`
* **Expected Code:** `200 OK`
* **Expected Output Payload:**
```json
{
  "exists": true,
  "subject": "Account already exist",
  "message": "There is account registered with the IC number. Please login to continue.",
  "customerName": "Syaf Jamal"
}
```

#### Step 2: First-Time Access Entry
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/login`
* **Payload Body:**
```json
{ "icNumber": "950101-14-1234" }
```
* **Expected Code:** `200 OK`
* **Expected Output Payload:** *Note down the generated 6-digit debug codes below.*
```json
{
  "status": "ActivationRequired",
  "message": "Your profile exists but requires security activation. OTPs have been sent to your registered phone and email.",
  "navigateTo": "OtpVerificationPage",
  "icNumber": "950101-14-1234",
  "debugPhoneOtp": "XXXXXX",
  "debugEmailOtp": "XXXXXX"
}
```

#### Step 3: Identity Verification
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/verify-otp`
* **Payload Body:** *(Provide the exact generated codes from Step 2)*
```json
{
  "icNumber": "950101-14-1234",
  "phoneOtpCode": "XXXXXX", 
  "emailOtpCode": "XXXXXX" 
}
```
* **Expected Code:** `200 OK`
* **Expected Output Payload:**
```json
{
  "status": "Success",
  "message": "Both Phone and Email have been successfully verified! Proceed to security setup.",
  "navigateTo": "PinSetupPage",
  "customerName": "Syaf Jamal"
}
```

#### Step 4: Access Configuration Completion
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/finalize-setup`
* **Payload Body:**
```json
{
  "icNumber": "950101-14-1234",
  "agreeToPrivacyPolicy": true,
  "pin": "654321",
  "confirmPin": "654321",
  "enableBiometric": true
}
```
* **Expected Code:** `200 OK`
* **Expected Output Payload:**
```json
{
  "status": "Success",
  "message": "Account security setup complete! User authenticated.",
  "navigateTo": "HomePage",
  "authToken": "d3b07384-d113-4956-a5cc-96e053d2d854"
}
```

#### Step 5: Regular Returning Login
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/login`
* **Payload Body:**
```json
{
  "icNumber": "950101-14-1234",
  "pin": "654321"
}
```
* **Expected Code:** `200 OK` (Bypasses verification layers directly to the home workspace layout).

---

### 🔴 2. Negative Path Edge-Cases (Validation Safeguards)

These tests confirm that the validation engines accurately block malicious or faulty data transmissions.

#### Scenario A: Requesting Absent System Records
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/login`
* **Payload Body:** `{ "icNumber": "000000-00-0000" }`
* **Expected Code:** `404 Not Found`
* **Payload Result:** Contains `"subject": "Account Not Found"`.

#### Scenario B: Invalid Multi-Factor Credentials
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/verify-otp`
* **Payload Body:** `{ "icNumber": "950101-14-1234", "phoneOtpCode": "000000", "emailOtpCode": "000000" }`
* **Expected Code:** `400 Bad Request`
* **Payload Result:** Returns `"phoneStatus": "Invalid/Missing OTP"`.

#### Scenario C: Privacy Policy Compliance Failure
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/finalize-setup`
* **Payload Body:** `{ "icNumber": "950101-14-1234", "agreeToPrivacyPolicy": false, "pin": "123456", "confirmPin": "123456" }`
* **Expected Code:** `400 Bad Request`
* **Payload Result:** Returns *Validation Failed: You must accept the Privacy Policy.*

#### Scenario D: Mismatched Core Passcode Setup
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/finalize-setup`
* **Payload Body:** `{ "icNumber": "950101-14-1234", "agreeToPrivacyPolicy": true, "pin": "123456", "confirmPin": "654321" }`
* **Expected Code:** `400 Bad Request`
* **Payload Result:** Returns *Validation Failed: PIN and Confirm PIN do not match.*

#### Scenario E: Malformed Security PIN Structure
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/finalize-setup`
* **Payload Body:** `{ "icNumber": "950101-14-1234", "agreeToPrivacyPolicy": true, "pin": "123ABC", "confirmPin": "123ABC" }`
* **Expected Code:** `400 Bad Request`
* **Payload Result:** Returns *Validation Failed: PIN must be exactly 6 numeric digits.*

#### Scenario F: Incorrect PIN Authentication Attempt
* **Method & URL:** `POST` `http://localhost:5062/api/Customers/login`
* **Payload Body:** `{ "icNumber": "880808-14-8888", "pin": "000000" }`
* **Expected Code:** `401 Unauthorized`
* **Payload Result:** Returns `"subject": "Invalid Credentials"`.
