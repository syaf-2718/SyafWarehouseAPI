namespace SyafWarehouseAPI.DTO
{
    public class LoginRequest
    {
        public string ICNumber { get; set; } = string.Empty;

        // This is optional for initial verification but required if the account is already active
        public string Pin { get; set; } = string.Empty;
    }
}
