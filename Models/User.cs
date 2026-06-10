using System.ComponentModel.DataAnnotations;

namespace Task_Manager.Models
{
    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }


    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public bool IsAdmin { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
