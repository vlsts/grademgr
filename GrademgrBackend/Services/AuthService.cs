using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using BC = BCrypt.Net.BCrypt;

/// <summary>
/// Service handling user authentication, including registration and login functionality.
/// </summary>
public class AuthService : IAuthService
{
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="context">The database context for accessing user data.</param>
    /// <param name="configuration">Application configuration containing JWT settings.</param>
    public AuthService(DatabaseContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="model">Registration information containing user details.</param>
    /// <returns>A response indicating success or failure of the registration process.</returns>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest model)
    {
        if (await _context.Users.AnyAsync(u => u.Username == model.Username || u.Email == model.Email))
        {
            return new RegisterResponse
            {
                Success = false,
                Message = "User with this email or username already exists"
            };
        }

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            Role = model.Role,
            Password = BC.HashPassword(model.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful"
        };
    }

    /// <summary>
    /// Authenticates a user and generates a JWT token for authorized access.
    /// </summary>
    /// <param name="model">Login credentials containing email and password.</param>
    /// <returns>A response containing the authentication token and user role.</returns>
    /// <exception cref="Exception">Thrown when login credentials are invalid.</exception>
    public async Task<LoginResponse> LoginAsync(LoginRequest model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BC.Verify(model.Password, user.Password))
        {
            throw new Exception("Invalid email or password");
        }

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            Role = user.Role.ToString()
        };
    }

    /// <summary>
    /// Generates a JWT authentication token for a user.
    /// </summary>
    /// <param name="user">The user for whom to generate the token.</param>
    /// <returns>A JWT token string that can be used for authentication.</returns>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
