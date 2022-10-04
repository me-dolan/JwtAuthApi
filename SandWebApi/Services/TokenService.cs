using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SandWebApi.Data;
using SandWebApi.Helpers;
using SandWebApi.Models;
using SandWebApi.Models.Requests;
using SandWebApi.Models.Responses;
using SandWebApi.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SandWebApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public TokenService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Tuple<string, string>> GenerateTokenAsync(int userId)
        {
            
            var accesToken = await GenerateAccesToken(userId);
            var refreshToken = await GenerateRefreshToken();

            var userRecord = await _context.Users.Include(o => o.RefreshToken).FirstOrDefaultAsync(e => e.Id == userId);

            if (userRecord == null)
            {
                return null;
            }

            var salt = PasswordHelper.GetSecureSalt();

            var refreshTokenHash = PasswordHelper.HashUsingPbkdf2(refreshToken, salt);

            if (userRecord.RefreshToken != null && userRecord.RefreshToken.Any())
            {
                await RemoveRefreshTokenAsync(userRecord);
            }

            userRecord.RefreshToken?.Add(new Token
            {
                ExpiryDate = DateTime.UtcNow.AddDays(14),
                Ts = DateTime.UtcNow,
                UserId = userId,
                TokenHash = refreshTokenHash,
                TokenSalt = Convert.ToBase64String(salt)
            });

            
            await _context.SaveChangesAsync();
            Tuple<string, string> token = new(accesToken, refreshToken);
            return token;
        }

        public async Task<bool> RemoveRefreshTokenAsync(User user)
        {
            var userRecord = await _context.Users.Include(o => o.RefreshToken).FirstOrDefaultAsync(e => e.Id == user.Id);
            
            if(userRecord == null)
            {
                return false;
            }

            if(userRecord.RefreshToken != null && userRecord.RefreshToken.Any())
            {
                var currentRefreshToken = userRecord.RefreshToken.First();

                _context.Tokens.Remove(currentRefreshToken);
            }

            await _context.SaveChangesAsync();

            return false;
        }

        public async Task<ValidateRefreshTokenResponse> ValidateRefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
        {
            var refreshToken = await _context.Tokens.FirstOrDefaultAsync(o => o.Id == refreshTokenRequest.UserId);

            var response = new ValidateRefreshTokenResponse();
            if (refreshToken == null)
            {
                response.IsSuccess = false;
                response.Error = "Недействительный сеанс или пользователь уже вышел из системы";
                response.ErrorCode = "invalid_grant";
                return response;
            }

            var refreshTokenToValidateHash = PasswordHelper
                .HashUsingPbkdf2(refreshTokenRequest.RefreshToken, Convert.FromBase64String(refreshToken.TokenSalt));


            if(refreshToken.TokenHash != refreshTokenToValidateHash)
            {
                response.IsSuccess = false;
                response.Error = "Невалидный рефреш токен";
                response.ErrorCode = "invalid_gratnt";
                return response;
            }
            
            if (refreshToken.ExpiryDate < DateTime.Now)
            {
                response.IsSuccess = false;
                response.Error = "Срок действия рефреш токена истёк";
                response.ErrorCode = "invalid_gratnt";
                return response;
            }

            response.IsSuccess = true;
            response.UserId = refreshToken.UserId;
            return response;
        }


        public async Task<string> GenerateAccesToken(int userId)
        {

            var tokenHandler = new JwtSecurityTokenHandler();

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature);

            var claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            });

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                Expires = DateTime.Now.AddMinutes(15),
                SigningCredentials = signingCredentials
            };

            var accesToken = tokenHandler.CreateToken(tokenDescription);

            return await Task.Run(() => tokenHandler.WriteToken(accesToken));
        }

        public async Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                await Task.Run(() => rng.GetBytes(randomNumber));
            }
            var refreshToken = Convert.ToBase64String(randomNumber);
            return refreshToken;
        }
    }
}
