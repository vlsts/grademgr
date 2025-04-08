using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BC = BCrypt.Net.BCrypt;

/// <summary>
/// Service providing security-related functionality including input sanitization, validation,
/// security checks, password handling, and token validation.
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration containing security settings.</param>
    public SecurityService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Sanitizes a string by removing potentially dangerous characters.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <returns>A sanitized version of the input string.</returns>
    public string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove potentially dangerous characters
        input = Regex.Replace(input, @"[^\w\s@.,;:()[\]{}!?$&#+-]", "");
        
        return input.Trim();
    }

    /// <summary>
    /// Sanitizes an email address by removing invalid characters and normalizing format.
    /// </summary>
    /// <param name="email">The email address to sanitize.</param>
    /// <returns>A sanitized and normalized version of the email address.</returns>
    public string SanitizeEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email;
            
        // Basic email sanitization
        email = Regex.Replace(email, @"[^\w@.-]", "");
        
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Sanitizes a MongoDB ObjectId by validating its format.
    /// </summary>
    /// <param name="id">The ObjectId string to sanitize.</param>
    /// <returns>The validated ObjectId if valid, otherwise null.</returns>
    public string SanitizeMongoId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;
            
        // MongoDB ObjectId is a 24-character hex string
        if (IsValidMongoId(id))
            return id;
            
        return null; // Invalid ObjectId format
    }
    
    /// <summary>
    /// Validates if a string is a properly formatted email address.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email is valid; otherwise, false.</returns>
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Validates if a string is a properly formatted MongoDB ObjectId.
    /// </summary>
    /// <param name="id">The ObjectId string to validate.</param>
    /// <returns>True if the id is a valid MongoDB ObjectId; otherwise, false.</returns>
    public bool IsValidMongoId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
            
        return Regex.IsMatch(id, @"^[0-9a-fA-F]{24}$");
    }
    
    /// <summary>
    /// Checks if a string contains potential MongoDB injection patterns.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the input contains injection patterns; otherwise, false.</returns>
    public bool ContainsSqlInjectionPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // MongoDB specific injection patterns
        var patterns = new[]
        {
            @"\$where\s*:.*javascript",         // $where operator with JavaScript
            @"\$regex\s*:.*\(\(\?\!\.\)\.\)\*", // Regex DoS pattern
            @"\$\{.*\}",                        // JavaScript template literal injection
            @"\$[a-zA-Z0-9]*\s*:",              // MongoDB operators ($ne, $gt, etc.)
            @"db\.eval\(.*\)",                  // MongoDB eval() command
            @"__proto__",                       // Prototype pollution
            @"constructor\."                    // Constructor access
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// Checks if a string contains client-side scripting patterns that could be used for XSS attacks.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the input contains scripting patterns; otherwise, false.</returns>
    public bool ContainsScriptingPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var patterns = new[]
        {
            @"<script.*?>",                     // Script tags
            @"javascript:",                     // JavaScript protocol
            @"on\w+\s*=",                       // Event handlers (onclick, onload, etc.)
            @"eval\s*\(",                       // eval()
            @"document\.",                      // document object access
            @"window\.",                        // window object access
            @"alert\s*\(",                      // alert()
            @"function\s*\("                    // function declarations
        };

        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// Generates a secure hash of a password using bcrypt.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>A secure hash of the password.</returns>
    public string HashPassword(string password)
    {
        return BC.HashPassword(password);
    }
    
    /// <summary>
    /// Verifies that a plain text password matches a previously hashed password.
    /// </summary>
    /// <param name="password">The plain text password to check.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public bool VerifyPassword(string password, string hash)
    {
        return BC.Verify(password, hash);
    }
    
    /// <summary>
    /// Validates a JWT token for authenticity and expiration.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>True if the token is valid and not expired; otherwise, false.</returns>
    public async Task<bool> IsTokenValidAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
            
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            // This will throw an exception if the token is invalid
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal != null;
        }
        catch
        {
            return false;
        }
    }
}
