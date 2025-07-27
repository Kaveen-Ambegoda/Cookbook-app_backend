using BCrypt.Net;
using CookbookApp.APi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using CookbookApp.APi.Models;

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
            if (user == null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized();

            if (!user.IsEmailConfirmed)
                return Unauthorized("Please verify your email before logging in.");


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
        public async Task<IActionResult> Register([FromBody] RegisterModel register)
        {
            try
            {
                var existingUser = _context.Users.Any(u => u.Email == register.Email);
                if (existingUser)
                {
                    return Conflict("User already exists");
                }

                var token = GenerateVerificationToken();

                var user = new User
                {
                    Username = register.Username,
                    Email = register.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password),
                    Role = "User",
                    CreatedAt = DateTime.Now,
                    EmailVerificationToken = token,
                    EmailVerificationTokenExpiryTime = DateTime.UtcNow.AddDays(1), // Token valid for 1 day
                    IsEmailConfirmed = false, // Initially set to false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                await Task.Delay(500);
                await SendVerificationEmailAsync(user.Email, token);

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

            var newJwtToken = GenerateJwtToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            _context.SaveChanges();

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                refreshToken = newRefreshToken
            });
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
        private string GenerateVerificationToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private async Task SendVerificationEmailAsync(string toEmail, string token)
        {
            var verifyUrl = $"http://localhost:8080/verify-email?token={token}";

            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var to = new EmailAddress(toEmail);
            var subject = "Verify your email - CookBook App";
            var htmlContent = $@"
        <p>Hi there,</p>
        <p>Thanks for registering on CookBook. Please confirm your email address by clicking the link below:</p>
        <p><a href='{verifyUrl}'>Verify Email</a></p>
        <p>This link will expire in 24 hours.</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                Console.WriteLine($"Failed to send email: {response.StatusCode}");
            }
        }


        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is required");

            var user = _context.Users.SingleOrDefault(u => u.EmailVerificationToken == token);

            if (user == null)
                return NotFound("Invalid token");

            if (user.EmailVerificationTokenExpiryTime < DateTime.UtcNow)
                return BadRequest("Token has expired");

            user.IsEmailConfirmed = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully!" });
        }

    }

}
