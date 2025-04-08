using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using BC = BCrypt.Net.BCrypt;

namespace GrademgrBackend.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockJwtSection;
        private readonly DatabaseContext _dbContext;
        private readonly AuthService _authService;
        
        public AuthServiceTests()
        {
            // Setup configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            _mockJwtSection = new Mock<IConfigurationSection>();
            
            _mockJwtSection.Setup(x => x["Key"]).Returns("ThisIsAVerySecureTestKeyWith32Chars!");
            _mockJwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            _mockJwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            
            _mockConfiguration
                .Setup(x => x.GetSection("JwtSettings"))
                .Returns(_mockJwtSection.Object);
                
            // Setup in-memory database instead of mocking DbContext
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _dbContext = new DatabaseContext(options);
            
            // Create service instance with real in-memory database
            _authService = new AuthService(_dbContext, _mockConfiguration.Object);
        }

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithNewUser_ShouldRegisterSuccessfully()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "Password123!",
                FullName = "New User",
                Role = UserRole.Student
            };

            // Act
            var result = await _authService.RegisterAsync(registerRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Registration successful", result.Message);
            
            // Verify user was added to the database
            var addedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            Assert.NotNull(addedUser);
            Assert.Equal(registerRequest.Username, addedUser.Username);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUser_ShouldReturnError()
        {
            // Arrange - Add a user to the database first
            var existingUser = new User
            {
                Id = "existingId",
                Username = "existinguser",
                Email = "existing@example.com",
                Password = BC.HashPassword("Password123!"),
                FullName = "Existing User",
                Role = UserRole.Student,
                CreatedAt = DateTime.UtcNow
            };
            
            await _dbContext.Users.AddAsync(existingUser);
            await _dbContext.SaveChangesAsync();
            
            var registerRequest = new RegisterRequest
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = "Password123!",
                FullName = "Existing User",
                Role = UserRole.Student
            };

            // Act
            var result = await _authService.RegisterAsync(registerRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User with this email or username already exists", result.Message);
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange - Add a user to the database first
            string password = "Password123!";
            var user = new User
            {
                Id = "123456789",
                Username = "testuser",
                Email = "test@example.com",
                Password = BC.HashPassword(password),
                Role = UserRole.Student,
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            };
            
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = password
            };

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal(UserRole.Student.ToString(), result.Role);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldThrowException()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _authService.LoginAsync(loginRequest));
            Assert.Equal("Invalid email or password", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldThrowException()
        {
            // Arrange - Add a user to the database first
            var user = new User
            {
                Id = "123456789",
                Username = "testuser",
                Email = "test@example.com",
                Password = BC.HashPassword("CorrectPassword123!"),
                Role = UserRole.Student,
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow
            };
            
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword!"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _authService.LoginAsync(loginRequest));
            Assert.Equal("Invalid email or password", exception.Message);
        }

        #endregion
    }
}
