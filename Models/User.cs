namespace DigitalAssetAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        public string? Email { get; set; }

        public string Username { get; set; } = string.Empty;

        public byte[]? PasswordHash { get; set; }

        public byte[]? PasswordSalt { get; set; }

        public string Role { get; set; } = "User"; // User or Admin

        public string Status { get; set; } = "Active"; // For admin to block/unblock

        public ICollection<Asset>? UploadedAssets { get; set; }

        public bool IsBlocked { get; set; } = false;

    }
}
