using SyafWarehouseAPI.Models;

namespace SyafWarehouseAPI.Services
{
    public class CustomerDataStore
    {
        // Keeps the mock state safe and accessible globally across requests
        public List<Customer> Customers { get; } = new()
        {
            new Customer
            {
                Id = 1,
                Name = "Syaf Jamal",
                ICNumber = "950101-14-1234",
                PhoneNumber = "+60123456789",
                Email = "syaf@example.com",
                IsAccountActive = false
            },
            new Customer
            {
                Id = 2,
                Name = "Admin User",
                ICNumber = "880808-14-8888",
                PhoneNumber = "+60198765432",
                Email = "admin@example.com",
                SecurePin = "112233",
                IsAccountActive = true
            }
        };
    }
}
