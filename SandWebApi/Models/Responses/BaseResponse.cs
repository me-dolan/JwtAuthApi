using System.Text.Json.Serialization;

namespace SandWebApi.Models.Responses
{
    public abstract class BaseResponse
    {
        [JsonIgnore]
        public bool IsSuccess { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Error { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }
    }
}
