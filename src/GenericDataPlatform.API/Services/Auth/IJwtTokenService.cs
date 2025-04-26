using System.Security.Claims;
using GenericDataPlatform.API.Models.Auth;

namespace GenericDataPlatform.API.Services.Auth
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
        bool ValidateToken(string token);
        ClaimsPrincipal GetPrincipalFromToken(string token);
        string GetUserIdFromToken(string token);
    }
}
