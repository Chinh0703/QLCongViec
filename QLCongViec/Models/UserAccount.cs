using System.ComponentModel.DataAnnotations;

namespace QLCongViec.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}