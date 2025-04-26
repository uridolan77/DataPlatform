using System.Threading.Tasks;
using GenericDataPlatform.API.Models.Auth;

namespace GenericDataPlatform.API.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<User> GetUserByIdAsync(string id);
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> ValidateTokenAsync(string token);
    }
}
