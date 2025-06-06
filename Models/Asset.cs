using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalAssetAPI.Models
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? FilePath { get; set; }  // <-- Yeh line add karo

        public string? Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [ForeignKey("User")]
        public int UploadedById { get; set; }

        public User? UploadedBy { get; set; }
    }
}
