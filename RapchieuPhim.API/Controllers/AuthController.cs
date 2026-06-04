using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RapchieuPhim.API.DTOs.Auth;
using RapchieuPhim.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RapchieuPhim.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CinemaManagementContext _context;
        private readonly IConfiguration _config;

        public AuthController(CinemaManagementContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/Auth/Register
        // Fields: FullName, Email, Password, ConfirmPassword, DateOfBirth, Gender, Phone, OtpCode
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate ConfirmPassword matches Password
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match." });

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return Conflict(new { Message = "Email is already registered." });

            // Check if phone already exists
            if (await _context.Users.AnyAsync(u => u.Phone == request.Phone))
                return Conflict(new { Message = "Phone number is already registered." });

            // Get Customer role
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
                return StatusCode(500, new { Message = "Customer role not found. Please seed the ROLES table." });

            var user = new User
            {
                FullName     = request.FullName,
                Email        = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Phone        = request.Phone,
                Gender       = request.Gender,
                DateOfBirth  = request.DateOfBirth,
                RoleId       = customerRole.RoleId,
                RewardPoint  = 0,
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful.", UserId = user.UserId });
        }

        // POST: api/Auth/Login
        // Fields: Email, Password
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
                return Unauthorized(new { Message = "Invalid email or password." });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { Message = "Invalid email or password." });

            var token    = GenerateJwtToken(user);
            var expireAt = DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

            return Ok(new AuthResponse
            {
                Token     = token,
                ExpiresAt = expireAt,
                User = new UserInfo
                {
                    UserId          = user.UserId,
                    FullName        = user.FullName,
                    Email           = user.Email,
                    Phone           = user.Phone,
                    AvatarUrl       = user.AvatarUrl,
                    MembershipLevel = user.MembershipLevel,
                    RewardPoint     = user.RewardPoint,
                    RoleId          = user.RoleId,
                    RoleName        = user.Role.RoleName
                }
            });
        }

        // POST: api/Auth/LoginGoogle
        // FE sends the idToken obtained from Google Sign-In SDK
        [HttpPost("LoginGoogle")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequest request)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                // Verify the ID token with Google – throws if invalid or expired
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { Message = "Invalid Google token." });
            }

            // Find existing user by email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null)
            {
                // Auto-register on first Google login
                var customerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Customer");
                if (customerRole == null)
                    return StatusCode(500, new { Message = "Customer role not found." });

                user = new User
                {
                    FullName     = payload.Name,
                    Email        = payload.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // random hash – Google users don't use password login
                    AvatarUrl    = payload.Picture,
                    RoleId       = customerRole.RoleId,
                    RewardPoint  = 0,
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Reload with Role navigation
                user = await _context.Users
                    .Include(u => u.Role)
                    .FirstAsync(u => u.UserId == user.UserId);
            }
            else if (!user.IsActive)
            {
                return Unauthorized(new { Message = "Account is disabled." });
            }

            var token    = GenerateJwtToken(user);
            var expireAt = DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

            return Ok(new AuthResponse
            {
                Token     = token,
                ExpiresAt = expireAt,
                User = new UserInfo
                {
                    UserId          = user.UserId,
                    FullName        = user.FullName,
                    Email           = user.Email,
                    Phone           = user.Phone,
                    AvatarUrl       = user.AvatarUrl,
                    MembershipLevel = user.MembershipLevel,
                    RewardPoint     = user.RewardPoint,
                    RoleId          = user.RoleId,
                    RoleName        = user.Role.RoleName
                }
            });
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private string GenerateJwtToken(User user)
        {
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name,               user.FullName),
                new Claim(ClaimTypes.Role,               user.Role.RoleName),
                new Claim("RoleId",                      user.RoleId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             _config["Jwt:Issuer"],
                audience:           _config["Jwt:Audience"],
                claims:             claims,
                expires:            expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
