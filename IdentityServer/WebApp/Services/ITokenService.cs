using IdentityModel.Client;

namespace WebApp.Services
{
    public interface ITokenService
    {
        Task<TokenResponse> GetTokenAsync(string scope);
    }
}
