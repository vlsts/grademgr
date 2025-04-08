using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
    public string Role { get; set; }
}
