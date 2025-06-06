using DigitalAssetAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalAssetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("pending-assets")]
        public async Task<IActionResult> GetPendingAssets()
        {
            var pendingAssets = await _context.Assets
                .Where(a => a.Status == "Pending")
                .Include(a => a.UploadedBy)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    uploadedBy = a.UploadedBy != null ? a.UploadedBy.Username : "Unknown",
                    status = a.Status
                })
                .ToListAsync();

            return Ok(pendingAssets);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    status = u.Status
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("block-user/{userId}")]
        public async Task<IActionResult> BlockUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { Message = $"User with id {userId} not found" });

            user.Status = "Blocked";
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"User {userId} blocked" });
        }

        [HttpPost("unblock-user/{userId}")]
        public async Task<IActionResult> UnblockUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { Message = $"User with id {userId} not found" });

            user.Status = "Active";
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"User {userId} unblocked" });
        }
    }
}
