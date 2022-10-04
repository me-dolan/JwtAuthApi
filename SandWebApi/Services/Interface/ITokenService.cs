using SandWebApi.Models;
using SandWebApi.Models.Requests;
using SandWebApi.Models.Responses;

namespace SandWebApi.Services.Interface
{
    public interface ITokenService
    {
        Task<Tuple<string,string>> GenerateTokenAsync(int userId);
        Task<bool> RemoveRefreshTokenAsync(User user);
        Task<ValidateRefreshTokenResponse> ValidateRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest);
    }
}
