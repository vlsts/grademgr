public class RegisterRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
    public UserRole Role { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; }
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
