using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents a request to register a new user in the system.
/// Contains all required information for user creation.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the username. Must be at least 3 characters long.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the email address. Must be a valid email format.
    /// Used as unique identifier for authentication.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the password. Must be at least 6 characters long.
    /// Will be hashed before storage.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the user role (Student or Teacher).
    /// Determines permissions within the system.
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }
}

/// <summary>
/// Represents a request to authenticate a user in the system.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the email address used for authentication.
    /// Must be a valid email format.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// Will be verified against the stored hash.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}

/// <summary>
/// Represents the response to a registration request.
/// Contains information about the success or failure of the registration.
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the registration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a message providing details about the registration result.
    /// Contains error information if registration failed.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// Represents the response to a successful login request.
/// Contains the authentication token and user role information.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Gets or sets the JWT authentication token.
    /// Used for authorizing subsequent API requests.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the user's role in the system.
    /// Used for client-side permission handling.
    /// </summary>
    public string Role { get; set; }
}
