namespace QLCongViec.Services
{
    public interface ITaskReminderService
    {
        Task SendDueSoonReminderAsync(int userId, string userEmail, string fullName);
    }
}