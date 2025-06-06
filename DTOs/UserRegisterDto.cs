    namespace DigitalAssetAPI.DTOs
    {
        public class UserRegisterDto
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string Email { get; set; }    // <-- user ka Gmail ID
            public string? Role { get; set; }  // Usually "User" or "Admin"
        }
    }   
