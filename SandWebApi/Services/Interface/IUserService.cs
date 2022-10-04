using SandWebApi.Models.Requests;
using SandWebApi.Models.Responses;


namespace SandWebApi.Services.Interface
{
    public interface IUserService
    {
        Task<TokenResponse> LoginAsync(LoginRequest loginRequest);
        Task<SignupResponse> SignupAsync(SignupRequest signupRequest);

        Task<UserResonse> GetInfoAsync(int userId);
    }
}
