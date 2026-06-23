using System.ComponentModel.DataAnnotations;

namespace Task_Manager.Models
{
    public class RegisterRequest 
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Le format de l'email est invalide.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
        public string Password { get; set; } = string.Empty;

        // public string ConfirmedPassword { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Le format de l'email est invalide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        public string Password { get; set; } = string.Empty;
    }

    public class PromoteUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public UserStatus? NewStatus { get; set; }   // nullable → non modifié si absent
        public bool? IsActive { get; set; }          // nullable → non modifié si absent
    }


    public class DeleteUserRequest
    {
        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Le format de l'email est invalide.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ReadUserRequest
    {
        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Le format de l'email est invalide.")]
        public string Email { get; set; } = string.Empty;
    }


    public class RegisterResponse 
    {
        public string? Message { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public UserStatus UserStatus { get; set; } = UserStatus.Standard;
    }


    public class UserResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserStatus UserStatus { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum UserStatus
    {
        Standard,
        Admin,
        SuperAdmin,
    }

    public enum UpdateUserResult
    {
        Success,
        UserNotFound,
        Forbidden,        // tente de se modifier soi-même
        NoChanges         // aucun champ fourni dans la requête
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

        public UserStatus UserStatus { get; set; } = UserStatus.Standard;

        //public string? RefreshToken { get; set; }

        //public DateTime? RefreshTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
