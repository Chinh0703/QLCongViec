using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLCongViec.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề công việc")]
        [StringLength(100)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hạn hoàn thành")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; }

        [Display(Name = "Mức độ ưu tiên")]
        public int Priority { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public UserAccount? UserAccount { get; set; }

        [Display(Name = "Đã gửi nhắc hạn")]
        public bool IsReminderSent { get; set; } = false;
    }
}