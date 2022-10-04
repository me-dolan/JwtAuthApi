using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SandWebApi.Models.Requests;
using SandWebApi.Models.Responses;
using SandWebApi.Services.Interface;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SandWebApi.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public UserController(IUserService userService, ITokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new TokenResponse
                {
                    Error = "ошибка",
                    ErrorCode = "L01"
                });
            }

            var loginResponse = await _userService.LoginAsync(loginRequest);

            if (!loginResponse.IsSuccess)
            {
                return Unauthorized(new
                {
                    loginResponse.ErrorCode,
                    loginResponse.Error
                });
            }

            return Ok(loginResponse);
        }


        // регистрация без авторизации, токен даётся при логине
        [HttpPost]
        [Route("sigup")]
        public async Task<IActionResult> Signup(SignupRequest signupRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new TokenResponse
                {
                    Error = "ошибка",
                    ErrorCode = "s01"
                });
            }
            
            var signupResponse = await _userService.SignupAsync(signupRequest);

            if (!signupResponse.IsSuccess)
            {
                return UnprocessableEntity(signupResponse);
            }

            return Ok(signupResponse.Email);
        }

        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest refreshTokenRequest)
        {
            if (refreshTokenRequest == null || string.IsNullOrEmpty(refreshTokenRequest.RefreshToken) || refreshTokenRequest.UserId == 0)
            {
                return BadRequest(new TokenResponse
                {
                    Error = "ошибка",
                    ErrorCode = "L01"
                });
            }

            var validateRefreshTokenResponse = await _tokenService.ValidateRefreshTokenAsync(refreshTokenRequest);

            if (!validateRefreshTokenResponse.IsSuccess)
            {
                return BadRequest(validateRefreshTokenResponse);
            }

            var tokenResponse = await _tokenService.GenerateTokenAsync(validateRefreshTokenResponse.UserId);

            return Ok(new TokenResponse { AccesToken = tokenResponse.Item1, RefreshToken = tokenResponse.Item2 });
        }

        
        
        
        [Authorize]
        [HttpGet]
        [Route("info")]
        public async Task<IActionResult> GetInfo()
        {

            ClaimsPrincipal currentUser = this.User;

            var currentUserId = Convert.ToInt32(currentUser.FindFirst(ClaimTypes.NameIdentifier).Value);

            var userResponse = await _userService.GetInfoAsync(currentUserId);

            if (!userResponse.IsSuccess)
            {
                return UnprocessableEntity(userResponse);
            }

            return Ok(userResponse);
        }
    }
}
