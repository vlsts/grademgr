using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controller that handles authentication-related operations such as user registration and login.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    /// <summary>
    /// The service responsible for authentication operations.
    /// </summary>
    private readonly IAuthService _authService;
    
    /// <summary>
    /// The service responsible for security-related operations such as input sanitization.
    /// </summary>
    private readonly ISecurityService _securityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="securityService">The security service.</param>
    public AuthController(IAuthService authService, ISecurityService securityService)
    {
        _authService = authService;
        _securityService = securityService;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="model">The registration data.</param>
    /// <returns>A response containing the result of the registration operation.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    message = "Invalid input", 
                    errors = GetModelStateErrors() 
                });
            }

            // Sanitize inputs
            model.Username = _securityService.SanitizeString(model.Username);
            model.Email = _securityService.SanitizeEmail(model.Email);
            model.FullName = _securityService.SanitizeString(model.FullName);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(model.Username) || model.Username.Length < 3)
            {
                return BadRequest(new { message = "Username must be at least 3 characters long." });
            }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters long." });
            }

            if (string.IsNullOrWhiteSpace(model.Email) || !_securityService.IsValidEmail(model.Email))
            {
                return BadRequest(new { message = "Please provide a valid email address." });
            }

            // Check for security threats
            if (_securityService.ContainsScriptingPattern(model.Username) || 
                _securityService.ContainsScriptingPattern(model.Email) || 
                _securityService.ContainsScriptingPattern(model.FullName))
            {
                return BadRequest(new { message = "Invalid input detected." });
            }

            var result = await _authService.RegisterAsync(model);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user and generates an access token.
    /// </summary>
    /// <param name="model">The login credentials.</param>
    /// <returns>A response containing the authentication result and access token if successful.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    message = "Invalid input", 
                    errors = GetModelStateErrors() 
                });
            }

            // Sanitize inputs
            model.Email = _securityService.SanitizeEmail(model.Email);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(model.Email) || !_securityService.IsValidEmail(model.Email))
            {
                return BadRequest(new { message = "Please provide a valid email address." });
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Password is required." });
            }

            // Check for security threats
            if (_securityService.ContainsScriptingPattern(model.Email))
            {
                return BadRequest(new { message = "Invalid input detected." });
            }

            var result = await _authService.LoginAsync(model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Extracts error messages from the ModelState dictionary.
    /// </summary>
    /// <returns>A dictionary of field names and their associated error messages.</returns>
    private Dictionary<string, string[]> GetModelStateErrors()
    {
        return ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }
}
