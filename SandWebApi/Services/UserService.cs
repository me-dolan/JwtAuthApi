using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SandWebApi.Data;
using SandWebApi.Helpers;
using SandWebApi.Models;
using SandWebApi.Models.Requests;
using SandWebApi.Models.Responses;
using SandWebApi.Services.Interface;
using System.Security.Cryptography;

namespace SandWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public UserService(ApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<UserResonse> GetInfoAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return new UserResonse
                {
                    IsSuccess = false,
                    Error = "Пользователь не найден",
                    ErrorCode = "I001"
                };
            }

            return new UserResonse
            {
                IsSuccess = true,
                Email = user.Email,
                Name = user.Name,
            };
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest loginRequest)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == loginRequest.Email);

            if(user == null)
            {
                return new TokenResponse
                {
                    IsSuccess = false,
                    Error = "мейл не найден",
                    ErrorCode = "L02"
                };
            }
            var passwordHash = PasswordHelper.HashUsingPbkdf2(loginRequest.Password, Convert.FromBase64String(user.PasswordSalt));

            if(user.Password != passwordHash)
            {
                return new TokenResponse
                {
                    IsSuccess = false,
                    Error = "невалидный пароль",
                    ErrorCode = "L03"
                };
            }

            var token = await Task.Run(() => _tokenService.GenerateTokenAsync(user.Id));

            return new TokenResponse
            {
                IsSuccess = true,
                AccesToken = token.Item1,
                RefreshToken = token.Item2,
                UserId = user.Id,
                Name = user.Name
            };
        }

        public async Task<SignupResponse> SignupAsync(SignupRequest signupRequest)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == signupRequest.Email);

            if (user != null)
            {
                return new SignupResponse
                {
                    IsSuccess = false,
                    Error = "Пользователь с таким email уже существует",
                    ErrorCode = "S02"
                };
            }

            if (signupRequest.Password != signupRequest.ConfirmPassword)
            {
                return new SignupResponse
                {
                    IsSuccess = false,
                    Error = "Пароли не совпадают",
                    ErrorCode = "S03"
                };
            }

            if (signupRequest.Password.Length <= 7)
            {
                return new SignupResponse
                {
                    IsSuccess = false,
                    Error = "Пароль слишком короткий",
                    ErrorCode = "S04"
                };
            }

            var salt = PasswordHelper.GetSecureSalt();
            var passwordHash = PasswordHelper.HashUsingPbkdf2(signupRequest.Password, salt);

            var newUser = new User
            {
                Email = signupRequest.Email,
                Password = passwordHash,
                PasswordSalt = Convert.ToBase64String(salt),
                Name = signupRequest.Name
            };

            await _context.Users.AddAsync(newUser);

            var saveResponse = await _context.SaveChangesAsync();

            if(saveResponse >= 0)
            {
                return new SignupResponse
                {
                    IsSuccess = true,
                    Email = newUser.Email
                };
            }

            return new SignupResponse
            {
                IsSuccess = false,
                Error = "Не удалось сохранить пользователя",
                ErrorCode = "S05"
            };
        }
    }
}
