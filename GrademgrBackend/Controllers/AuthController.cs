using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISecurityService _securityService;

    public AuthController(IAuthService authService, ISecurityService securityService)
    {
        _authService = authService;
        _securityService = securityService;
    }

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