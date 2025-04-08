using System;
using System.Threading.Tasks;

public interface ISecurityService
{
    // Input Sanitization
    string SanitizeString(string input);
    string SanitizeEmail(string email);
    string SanitizeMongoId(string id);
    
    // Validation
    bool IsValidEmail(string email);
    bool IsValidMongoId(string id);
    
    // Security Checks
    bool ContainsSqlInjectionPattern(string input);
    bool ContainsScriptingPattern(string input);
    
    // Password Handling
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    
    // Token Validation
    Task<bool> IsTokenValidAsync(string token);
}