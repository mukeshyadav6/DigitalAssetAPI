// AssetUploadDto.cs
namespace DigitalAssetAPI.DTOs
{
    public class AssetUploadDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public string? Category { get; set; }
        public IFormFile? File { get; set; }
    }
}
