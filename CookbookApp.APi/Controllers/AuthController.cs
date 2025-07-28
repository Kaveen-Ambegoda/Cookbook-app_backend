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
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CookbookAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly CookbookDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(CookbookDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            try
            {
                if (login == null || string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.Password))
                    return BadRequest("Email and password are required");

                var user = _context.Users.SingleOrDefault(x => x.Email == login.Email);
                if (user == null)
                    return Unauthorized("Invalid credentials");

                if (!BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                    return Unauthorized("Invalid credentials");

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
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: creds
                );

                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    refreshToken = refreshToken,
                    expiresAt = DateTime.UtcNow.AddMinutes(30),
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, "An error occurred during login");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel register)
        {
            try
            {
                if (register == null || string.IsNullOrEmpty(register.Email) ||
                    string.IsNullOrEmpty(register.Password) || string.IsNullOrEmpty(register.Username))
                    return BadRequest("All fields are required");

                if (register.Password.Length < 6)
                    return BadRequest("Password must be at least 6 characters long");

                var existingUser = _context.Users.Any(u => u.Email == register.Email);
                if (existingUser)
                {
                    return Conflict("User already exists");
                }

                var existingUsername = _context.Users.Any(u => u.Username == register.Username);
                if (existingUsername)
                {
                    return Conflict("Username already taken");
                }

                var token = GenerateVerificationToken();

                var user = new User
                {
                    Username = register.Username,
                    Email = register.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(register.Password),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    EmailVerificationToken = token,
                    EmailVerificationTokenExpiryTime = DateTime.UtcNow.AddDays(1),
                    IsEmailConfirmed = false,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SendVerificationEmailAsync(user.Email, token);

                return Ok(new { Message = "User registered successfully. Please check your email for verification." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                return StatusCode(500, $"Registration failed: {ex.Message} | Inner: {innerMessage}");
            }
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] TokenModel tokenModel)
        {
            try
            {
                if (tokenModel == null || string.IsNullOrEmpty(tokenModel.Token) || string.IsNullOrEmpty(tokenModel.RefreshToken))
                    return BadRequest("Invalid request - tokens are required");

                var principal = GetPrincipalFromExpiredToken(tokenModel.Token);
                if (principal == null)
                    return BadRequest("Invalid access token");

                var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Invalid token claims");

                var user = _context.Users.SingleOrDefault(u => u.Id.ToString() == userId);

                if (user == null)
                    return Unauthorized("User not found");

                if (string.IsNullOrEmpty(user.RefreshToken) || user.RefreshToken != tokenModel.RefreshToken)
                    return Unauthorized("Invalid refresh token");

                if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                    return Unauthorized("Refresh token has expired");

                var newJwtToken = GenerateJwtToken(principal.Claims.ToList());
                var newRefreshToken = GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(newJwtToken),
                    refreshToken = newRefreshToken,
                    expiresAt = DateTime.UtcNow.AddMinutes(30)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh error: {ex.Message}");
                return StatusCode(500, "An error occurred during token refresh");
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.RefreshToken))
                    return BadRequest("Refresh token is required");

                var user = _context.Users.SingleOrDefault(u => u.RefreshToken == model.RefreshToken);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    _context.SaveChanges();
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                return StatusCode(500, "An error occurred during logout");
            }
        }

        [HttpPost("revoke-all-tokens")]
        [Authorize]
        public IActionResult RevokeAllTokens()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Invalid user context");

                var user = _context.Users.SingleOrDefault(u => u.Id.ToString() == userId);
                if (user == null)
                    return NotFound("User not found");

                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                _context.SaveChanges();

                return Ok(new { message = "All tokens revoked successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Revoke tokens error: {ex.Message}");
                return StatusCode(500, "An error occurred while revoking tokens");
            }
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
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
                expires: DateTime.UtcNow.AddMinutes(30),
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
                ValidateLifetime = false, // We ignore expiration here for refresh
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ClockSkew = TimeSpan.Zero // Reduce clock skew to zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }

        private string GenerateVerificationToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private async Task SendVerificationEmailAsync(string toEmail, string token)
        {
            try
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
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create this account, please ignore this email.</p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                if ((int)response.StatusCode >= 400)
                {
                    Console.WriteLine($"Failed to send email: {response.StatusCode}");
                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"SendGrid Response: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending error: {ex.Message}");
                // Don't throw - registration should still succeed even if email fails
            }
        }

        [HttpGet("verify-email")]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                Console.WriteLine($"=== EMAIL VERIFICATION START ===");
                Console.WriteLine($"Received token: {token}");
                Console.WriteLine($"Token length: {token?.Length}");
                Console.WriteLine($"Request method: {Request.Method}");
                Console.WriteLine($"Request URL: {Request.Path}{Request.QueryString}");

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("ERROR: Token is null or empty");
                    return BadRequest(new { message = "Token is required" });
                }

                Console.WriteLine("Checking database connection...");
                var totalUsers = _context.Users.Count();
                Console.WriteLine($"Total users in database: {totalUsers}");

                var user = _context.Users.SingleOrDefault(u => u.EmailVerificationToken == token);
                Console.WriteLine($"User found with token: {user != null}");

                if (user != null)
                {
                    Console.WriteLine($"User details:");
                    Console.WriteLine($"  - ID: {user.Id}");
                    Console.WriteLine($"  - Email: {user.Email}");
                    Console.WriteLine($"  - Username: {user.Username}");
                    Console.WriteLine($"  - IsEmailConfirmed: {user.IsEmailConfirmed}");
                    Console.WriteLine($"  - Token in DB: {user.EmailVerificationToken}");
                    Console.WriteLine($"  - Token expiry: {user.EmailVerificationTokenExpiryTime}");
                    Console.WriteLine($"  - Current UTC: {DateTime.UtcNow}");
                }

                if (user == null)
                {
                    Console.WriteLine("ERROR: No user found with provided token");

                    var usersWithTokens = _context.Users
                        .Where(u => !string.IsNullOrEmpty(u.EmailVerificationToken))
                        .Select(u => new { u.Email, u.EmailVerificationToken })
                        .ToList();

                    Console.WriteLine($"Users with verification tokens: {usersWithTokens.Count}");
                    foreach (var u in usersWithTokens)
                    {
                        Console.WriteLine($"  - {u.Email}: {u.EmailVerificationToken}");
                    }

                    return NotFound(new { message = "Invalid verification token" });
                }

                if (user.EmailVerificationTokenExpiryTime < DateTime.UtcNow)
                {
                    Console.WriteLine("ERROR: Token has expired");
                    Console.WriteLine($"  Token expired at: {user.EmailVerificationTokenExpiryTime}");
                    Console.WriteLine($"  Current time: {DateTime.UtcNow}");
                    return BadRequest(new { message = "Verification token has expired" });
                }

                if (user.IsEmailConfirmed)
                {
                    Console.WriteLine("WARNING: Email already verified");
                    return Ok(new { message = "Email already verified" });
                }

                Console.WriteLine("Updating user record...");
                user.IsEmailConfirmed = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiryTime = null;
                user.EmailVerifiedAt = DateTime.UtcNow;

                var changes = await _context.SaveChangesAsync();
                Console.WriteLine($"Database changes saved: {changes} records affected");

                Console.WriteLine("SUCCESS: Email verification completed");
                Console.WriteLine($"=== EMAIL VERIFICATION END ===");

                return Ok(new { message = "Email verified successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in email verification:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, new { message = "An error occurred during verification", error = ex.Message });
            }
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Email))
                    return BadRequest("Email is required");

                var user = _context.Users.SingleOrDefault(u => u.Email == model.Email);
                if (user == null)
                    return NotFound("User not found");

                if (user.IsEmailConfirmed)
                    return BadRequest("Email is already verified");

                var token = GenerateVerificationToken();
                user.EmailVerificationToken = token;
                user.EmailVerificationTokenExpiryTime = DateTime.UtcNow.AddDays(1);

                _context.SaveChanges();
                await SendVerificationEmailAsync(user.Email, token);

                return Ok(new { message = "Verification email sent successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Resend verification error: {ex.Message}");
                return StatusCode(500, "An error occurred while resending verification email");
            }
        }

        [HttpPost("google-login")]
        public IActionResult GoogleLogin([FromBody] GoogleLoginModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Username))
                    return BadRequest("Invalid Google login data.");

                var user = _context.Users.SingleOrDefault(u => u.Email == model.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        PasswordHash = "",
                        Role = "User",
                        CreatedAt = DateTime.UtcNow,
                        IsEmailConfirmed = true,
                        EmailVerifiedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                }

                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

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
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    refreshToken = refreshToken,
                    expiresAt = DateTime.UtcNow.AddMinutes(30),
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google login error: {ex.Message}");
                return StatusCode(500, "An error occurred during Google login");
            }
        }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.AccessToken))
                    return BadRequest("Missing Facebook access token");

                var fbClient = new HttpClient();
                var fbResponse = await fbClient.GetAsync($"https://graph.facebook.com/me?fields=id,name,email&access_token={model.AccessToken}");

                if (!fbResponse.IsSuccessStatusCode)
                    return BadRequest("Invalid Facebook token");

                var fbContent = await fbResponse.Content.ReadAsStringAsync();
                var fbData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(fbContent);

                if (fbData == null || !fbData.ContainsKey("email") || !fbData.ContainsKey("name"))
                    return BadRequest("Facebook account missing email or name");

                var email = fbData["email"];
                var username = fbData["name"];

                var user = _context.Users.SingleOrDefault(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = "",
                        Role = "User",
                        CreatedAt = DateTime.UtcNow,
                        IsEmailConfirmed = true,
                        EmailVerifiedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                }

                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var jwt = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: creds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwt),
                    refreshToken = refreshToken,
                    expiresAt = DateTime.UtcNow.AddMinutes(30),
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Facebook login error: {ex.Message}");
                return StatusCode(500, "An error occurred during Facebook login");
            }
        }
    }

    // Additional model classes needed for new endpoints
    public class LogoutModel
    {
        public string RefreshToken { get; set; }
    }

    public class ResendVerificationModel
    {
        public string Email { get; set; }
    }
}