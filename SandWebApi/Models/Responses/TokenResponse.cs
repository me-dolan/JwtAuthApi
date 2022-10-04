using System.Text.Json.Serialization;

namespace SandWebApi.Models.Responses
{
    public class TokenResponse : BaseResponse
    {
        public string? AccesToken { get; set; }
        public string? RefreshToken { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int UserId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Name { get; set; }
    }
}
