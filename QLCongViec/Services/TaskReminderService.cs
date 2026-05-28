using Microsoft.EntityFrameworkCore;
using QLCongViec.Data;
using System.Net;

namespace QLCongViec.Services
{
    public class TaskReminderService : ITaskReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public TaskReminderService(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task SendDueSoonReminderAsync(int userId, string userEmail, string fullName)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var dueSoonTasks = await _context.TaskItems
                .Where(t =>
                    t.UserId == userId &&
                    t.Status != 2 &&
                    t.DueDate != null &&
                    t.DueDate >= today &&
                    t.DueDate <= tomorrow &&
                    t.IsReminderSent == false)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            if (!dueSoonTasks.Any())
            {
                return;
            }

            var taskRows = string.Join("", dueSoonTasks.Select(t =>
                $@"
                <tr>
                    <td style='padding:8px;border:1px solid #ddd;'>
                        {WebUtility.HtmlEncode(t.Title)}
                    </td>
                    <td style='padding:8px;border:1px solid #ddd;'>
                        {(t.DueDate.HasValue ? t.DueDate.Value.ToString("dd/MM/yyyy") : "Không có")}
                    </td>
                    <td style='padding:8px;border:1px solid #ddd;'>
                        {GetPriorityText(t.Priority)}
                    </td>
                    <td style='padding:8px;border:1px solid #ddd;'>
                        {GetStatusText(t.Status)}
                    </td>
                </tr>
                "
            ));

            string body = $@"
                <h3>Thông báo công việc sắp đến hạn</h3>

                <p>Xin chào <strong>{WebUtility.HtmlEncode(fullName)}</strong>,</p>

                <p>Bạn có một số công việc sắp đến hạn trong hệ thống <strong>QLCongViec</strong>.</p>

                <table style='border-collapse:collapse;width:100%;'>
                    <thead>
                        <tr style='background-color:#f2f2f2;'>
                            <th style='padding:8px;border:1px solid #ddd;'>Công việc</th>
                            <th style='padding:8px;border:1px solid #ddd;'>Hạn hoàn thành</th>
                            <th style='padding:8px;border:1px solid #ddd;'>Ưu tiên</th>
                            <th style='padding:8px;border:1px solid #ddd;'>Trạng thái</th>
                        </tr>
                    </thead>
                    <tbody>
                        {taskRows}
                    </tbody>
                </table>

                <p style='margin-top:16px;'>
                    Vui lòng đăng nhập hệ thống để cập nhật tiến độ công việc.
                </p>

                <p>Trân trọng,<br/>QLCongViec</p>
            ";

            await _emailService.SendEmailAsync(
                userEmail,
                "QLCongViec - Nhắc nhở công việc sắp đến hạn",
                body);

            foreach (var task in dueSoonTasks)
            {
                task.IsReminderSent = true;
            }

            await _context.SaveChangesAsync();
        }

        private string GetPriorityText(int priority)
        {
            return priority switch
            {
                0 => "Thấp",
                1 => "Trung bình",
                2 => "Cao",
                _ => "Không xác định"
            };
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Chưa làm",
                1 => "Đang làm",
                2 => "Hoàn thành",
                _ => "Không xác định"
            };
        }
    }
}