using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Task_Manager.Models
{
    // Réponse du endpoint POST /token.
    public class Token
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
