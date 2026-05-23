using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLCongViec.Data;
using QLCongViec.Models;

namespace QLCongViec.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;

            var userTasks = _context.TaskItems
                .Where(t => t.UserId == userId.Value);

            var model = new StatisticsViewModel
            {
                TotalTasks = await userTasks.CountAsync(),

                NotStartedTasks = await userTasks
                    .CountAsync(t => t.Status == 0),

                InProgressTasks = await userTasks
                    .CountAsync(t => t.Status == 1),

                CompletedTasks = await userTasks
                    .CountAsync(t => t.Status == 2),

                LowPriorityTasks = await userTasks
                    .CountAsync(t => t.Priority == 0),

                MediumPriorityTasks = await userTasks
                    .CountAsync(t => t.Priority == 1),

                HighPriorityTasks = await userTasks
                    .CountAsync(t => t.Priority == 2),

                OverdueTasks = await userTasks
                    .CountAsync(t => t.DueDate != null && t.DueDate < today && t.Status != 2)
            };

            return View(model);
        }
    }
}