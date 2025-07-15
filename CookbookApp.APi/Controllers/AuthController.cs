using BCrypt.Net;
using CookbookApp.APi.Data;
using CookbookApp.APi.Models;
using CookbookAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CookbookAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CookbookDbContext _context; // Fixed declaration
        private readonly IConfiguration _configuration;

        public AuthController(CookbookDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            var user = _context.Users.SingleOrDefault(x => x.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
            {
                return Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); // refresh token valid for 7 days
            _context.SaveChanges();

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = refreshToken
            });
        }
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel register)
        {
            try
            {
                var existingUser = _context.Users.Any(u => u.Email == register.Email);
                if (existingUser)
                {
                    return Conflict("User already exists");
                }

                var user = new User
                {
                    Username = register.Username,
                    Email = register.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password),
                    Role = "User",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                return Ok(new { Message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                return StatusCode(500, $"Registration failed: {ex.Message} | Inner: {innerMessage}");
            }

        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] TokenModel tokenModel)
        {
            if (tokenModel == null)
                return BadRequest("Invalid Request");

            var principal = GetPrincipalFromExpiredToken(tokenModel.Token);
            if (principal == null)
                return BadRequest("Invalid token");

            var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var user = _context.Users.SingleOrDefault(u => u.Id.ToString() == userId);


            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return Unauthorized("Invalid refresh token");


            var newToken = GenerateJwtToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            _context.SaveChanges();

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(newToken),
                refreshToken = newRefreshToken
            }
                );



        }




        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private JwtSecurityToken GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // <-- We ignore expiration here!
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtToken || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }
                            
    }
}
