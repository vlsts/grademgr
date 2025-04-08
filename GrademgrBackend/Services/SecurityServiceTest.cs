using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace GrademgrBackend.Tests
{
    public class SecurityServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _jwtSettingsSection;

        public SecurityServiceTests()
        {
            // Setup mock configuration for JWT settings
            _mockConfiguration = new Mock<IConfiguration>();
            _jwtSettingsSection = new Mock<IConfigurationSection>();

            _jwtSettingsSection.Setup(s => s["Key"]).Returns("ThisIsAVeryLongSecretKeyUsedForTestingJWTTokensInOurApplication");
            _jwtSettingsSection.Setup(s => s["Issuer"]).Returns("GrademgrTest");
            _jwtSettingsSection.Setup(s => s["Audience"]).Returns("GrademgrTestUsers");

            _mockConfiguration
                .Setup(c => c.GetSection("JwtSettings"))
                .Returns(_jwtSettingsSection.Object);
        }

        [Fact]
        public void SanitizeString_RemovesDangerousCharacters()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);
            var inputWithSpecialChars = "Test<script>alert('xss')</script>User123";

            // Act
            var sanitized = securityService.SanitizeString(inputWithSpecialChars);

            // Assert
            Assert.Equal("Testscriptalert(xss)scriptUser123", sanitized);
            Assert.DoesNotContain("<", sanitized);
            Assert.DoesNotContain(">", sanitized);
            Assert.DoesNotContain("'", sanitized);
        }

        [Fact]
        public void SanitizeEmail_NormalizesEmailAddress()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);
            var dirtyEmail = " Test.User@Example.COM ";

            // Act
            var sanitized = securityService.SanitizeEmail(dirtyEmail);

            // Assert
            Assert.Equal("test.user@example.com", sanitized);
        }

        [Theory]
        [InlineData("5f8d0d55b54764b783944a9a", true)]  // Valid MongoDB ObjectId
        [InlineData("5f8d0d55b54764b783944a9", false)]   // Too short
        [InlineData("5f8d0d55b54764b783944a9az", false)] // Invalid characters
        [InlineData("", false)]                          // Empty string
        [InlineData(null, false)]                        // Null
        public void IsValidMongoId_ValidatesCorrectly(string id, bool expected)
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);

            // Act
            var result = securityService.IsValidMongoId(id);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@example.com", true)]
        [InlineData("user.name+tag@example.co.uk", true)]
        [InlineData("user@localhost", true)]
        [InlineData("user@127.0.0.1", true)]
        [InlineData("user.example.com", false)]
        [InlineData("user@", false)]
        [InlineData("@example.com", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidEmail_ValidatesCorrectly(string email, bool expected)
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);

            // Act
            var result = securityService.IsValidEmail(email);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("normal text", false)]
        [InlineData("$where: function() { return true; }", true)]
        [InlineData("{ $ne: 1 }", true)]
        [InlineData("db.eval('harmful code')", true)]
        [InlineData("{ field: { $regex: '((?.)*' } }", true)]
        [InlineData("{ field: { $gt: 5 } }", true)]
        public void ContainsSqlInjectionPattern_DetectsMongoDbInjection(string input, bool expected)
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);

            // Act
            var result = securityService.ContainsSqlInjectionPattern(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("<script>alert('xss')</script>", true)]
        [InlineData("javascript:alert('xss')", true)]
        [InlineData("onclick=alert('xss')", true)]
        [InlineData("normal text", false)]
        [InlineData("document.cookie", true)]
        [InlineData("window.location", true)]
        public void ContainsScriptingPattern_DetectsScriptingAttacks(string input, bool expected)
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);

            // Act
            var result = securityService.ContainsScriptingPattern(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void HashPassword_AndVerifyPassword_WorkCorrectly()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);
            var password = "SecurePassword123!";

            // Act
            var hash = securityService.HashPassword(password);
            var isValid = securityService.VerifyPassword(password, hash);
            var isInvalid = securityService.VerifyPassword("WrongPassword", hash);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEqual(password, hash); // Hash should not be the original password
            Assert.True(isValid); // Correct password should verify
            Assert.False(isInvalid); // Wrong password should not verify
        }

        [Fact]
        public async Task IsTokenValidAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);

            // Generate a valid token for testing
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettingsSection.Object["Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Valid for 1 day
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Issuer = _jwtSettingsSection.Object["Issuer"],
                Audience = _jwtSettingsSection.Object["Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var validToken = tokenHandler.WriteToken(token);

            // Act
            var result = await securityService.IsTokenValidAsync(validToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTokenValidAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);
            
            // Generate an expired token for testing
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettingsSection.Object["Key"]);
            
            // Create dates in the correct order but with expiration in the past
            var now = DateTime.UtcNow;
            var notBefore = now.AddDays(-2); // 2 days ago
            var expires = now.AddDays(-1);   // 1 day ago (expired, but still after notBefore)
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                { 
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                NotBefore = notBefore,
                Expires = expires,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Issuer = _jwtSettingsSection.Object["Issuer"],
                Audience = _jwtSettingsSection.Object["Audience"]
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var expiredToken = tokenHandler.WriteToken(token);
            
            // Act
            var result = await securityService.IsTokenValidAsync(expiredToken);
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsTokenValidAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var securityService = new SecurityService(_mockConfiguration.Object);
            string invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = await securityService.IsTokenValidAsync(invalidToken);

            // Assert
            Assert.False(result);
        }
    }
}
