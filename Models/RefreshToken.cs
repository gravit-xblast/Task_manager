using System.ComponentModel.DataAnnotations;

namespace Task_Manager.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }


        [Required]
        public string Token { get; set; } = string.Empty;


        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsRevoked { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
