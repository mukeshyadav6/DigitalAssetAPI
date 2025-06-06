using DigitalAssetAPI.Data;
using DigitalAssetAPI.DTOs;
using DigitalAssetAPI.Models;
using DigitalAssetAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DigitalAssetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public AssetController(AppDbContext context, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _env = env;
            _emailService = emailService;
        }

        // =========================== USER: Upload Asset ===========================
        [HttpPost("upload")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UploadAsset([FromForm] AssetUploadDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (user.Status == "Blocked")
                return Forbid("Your account is blocked. You cannot upload assets.");

            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Path.GetFileName(request.File.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var asset = new Asset
            {
                Title = request.Title,
                FilePath = filePath,
                Status = "Pending",
                UploadedById = userId
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            // Notify admin via email
            var adminEmail = "mukeshyadavv13@gmail.com";
            var subject = "New Asset Uploaded - Approval Required";
            var body = $"<p>User <strong>{user.Username}</strong> has uploaded a new asset titled <strong>{asset.Title}</strong>.</p><p>Please review and take action.</p>";

            await _emailService.SendEmailAsync(adminEmail, subject, body);

            return Ok(new { Message = "Asset uploaded successfully", UserId = userId });
        }

        // =========================== USER: My Assets ===========================
        [HttpGet("my-assets")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetUserAssets()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (user.Status == "Blocked")
                return Forbid("Your account is blocked. You cannot view your assets.");

            var assets = await _context.Assets
                .Where(a => a.UploadedById == userId)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Status,
                    a.FilePath
                })
                .ToListAsync();

            return Ok(assets);
        }

        // =========================== ADMIN: Approve / Reject Asset ===========================
        [HttpPost("review/{assetId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewAsset(int assetId, [FromQuery] string status)
        {
            var asset = await _context.Assets
                .Include(a => a.UploadedBy)
                .FirstOrDefaultAsync(a => a.Id == assetId);

            if (asset == null)
                return NotFound("Asset not found.");

            if (status != "Approved" && status != "Rejected")
                return BadRequest("Invalid status. Use 'Approved' or 'Rejected'.");

            asset.Status = status;
            await _context.SaveChangesAsync();

            // Notify user via email
            var subject = $"Your asset \"{asset.Title}\" has been {status}";
            var body = $"<p>Hello {asset.UploadedBy?.Username ?? "User"},</p><p>Your asset titled <strong>{asset.Title}</strong> has been <strong>{status}</strong> by the admin.</p>";

            if (!string.IsNullOrEmpty(asset.UploadedBy?.Email))
            {
                await _emailService.SendEmailAsync(asset.UploadedBy.Email, subject, body);
            }

            return Ok(new { message = $"Asset {status} successfully and user notified (if email available)." });
        }
    }
}
