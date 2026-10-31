namespace SyafWarehouseAPI.DTO
{
    public class AccountSetupRequest
    {
        public string ICNumber { get; set; } = string.Empty;
        public bool AgreeToPrivacyPolicy { get; set; }
        public string Pin { get; set; } = string.Empty;
        public string ConfirmPin { get; set; } = string.Empty;
        public bool EnableBiometric { get; set; }
    }
}
