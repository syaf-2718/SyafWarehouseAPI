namespace SyafWarehouseAPI.DTO
{
    public class OtpVerificationRequest
    {
        public string ICNumber { get; set; } = string.Empty;
        public string PhoneOtpCode { get; set; } = string.Empty;
        public string EmailOtpCode { get; set; } = string.Empty;
    }
}
