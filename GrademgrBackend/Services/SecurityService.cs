using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BC = BCrypt.Net.BCrypt;

public class SecurityService : ISecurityService
{
    private readonly IConfiguration _configuration;

    public SecurityService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Input Sanitization
    public string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove potentially dangerous characters
        input = Regex.Replace(input, @"[^\w\s@.,;:()[\]{}!?$&#+-]", "");
        
        return input.Trim();
    }

    public string SanitizeEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return email;
            
        // Basic email sanitization
        email = Regex.Replace(email, @"[^\w@.-]", "");
        
        return email.Trim().ToLowerInvariant();
    }

    public string SanitizeMongoId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;
            
        // MongoDB ObjectId is a 24-character hex string
        if (IsValidMongoId(id))
            return id;
            
        return null; // Invalid ObjectId format
    }
    
    // Validation
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
    
    public bool IsValidMongoId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
            
        return Regex.IsMatch(id, @"^[0-9a-fA-F]{24}$");
    }
    
    // Security Checks
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
    
    // Password Handling
    public string HashPassword(string password)
    {
        return BC.HashPassword(password);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BC.Verify(password, hash);
    }
    
    // Token Validation
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