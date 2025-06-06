using DigitalAssetAPI.DTOs;
using DigitalAssetAPI.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DigitalAssetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and Password required");

            byte[] passwordHash, passwordSalt;
            PasswordHelper.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_RegisterUser", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@Username", request.Username);
            command.Parameters.AddWithValue("@Email", request.Email);  // ✅ Add this line
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@PasswordSalt", passwordSalt);
            command.Parameters.AddWithValue("@Role", request.Role ?? "User");

            await connection.OpenAsync();
            try
            {
                var result = await command.ExecuteScalarAsync();
                if (result == null)
                    return BadRequest("User registration failed");

                return Ok(new { Message = "User registered successfully", UserId = result });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                    return Conflict("Username already exists");
                throw;
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and Password required");

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetUserByUsername", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Username", request.Username);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows)
                return Unauthorized("Invalid username or password");

            await reader.ReadAsync();

            var userId = reader.GetInt32(reader.GetOrdinal("Id"));
            var username = reader.GetString(reader.GetOrdinal("Username"));
            var role = reader.GetString(reader.GetOrdinal("Role"));
            var storedHash = (byte[])reader["PasswordHash"];
            var storedSalt = (byte[])reader["PasswordSalt"];

            if (!PasswordHelper.VerifyPasswordHash(request.Password, storedHash, storedSalt))
                return Unauthorized("Invalid username or password");

            var token = CreateJwtToken(userId, username, role);

            var response = new UserResponseDto
            {
                Id = userId,
                Username = username,
                Role = role,
                Token = token
            };

            return Ok(response);
        }

        private string CreateJwtToken(int userId, string username, string role)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    
}
