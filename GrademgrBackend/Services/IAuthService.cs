using System.Threading.Tasks;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest model);
    Task<LoginResponse> LoginAsync(LoginRequest model);
}