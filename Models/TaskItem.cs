using System.ComponentModel.DataAnnotations;

namespace Task_Manager.Models
{
    public enum TaskPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TaskStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }


    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime DueDate { get; set; }


        // Si on veut stocker le nom texte (ex: "High") plutôt
        // que l'entier, on ajoute dans le DbContext :  .HasConversion<string>()
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;


        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }


        // Navigation properties
        public User User { get; set; } = null!;

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
