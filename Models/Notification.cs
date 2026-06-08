using System.ComponentModel.DataAnnotations;

namespace Task_Manager.Models
{
    public enum NotificationType
    {
        Reminder = 1,
        DueSoon = 2,
        Completed = 3,
        Overdue = 4
    }

    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int TaskItemId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty; // ????

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false; // set = false ?

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual TaskItem TaskItem { get; set; } = null!;
    }
}
