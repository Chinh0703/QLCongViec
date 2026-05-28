using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLCongViec.Data;
using QLCongViec.Models;
using QLCongViec.Services;

namespace QLCongViec.Controllers
{
    public class TaskItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskReminderService _taskReminderService;

        public TaskItemsController(
            ApplicationDbContext context,
            ITaskReminderService taskReminderService)
        {
            _context = context;
            _taskReminderService = taskReminderService;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private bool IsLoggedIn()
        {
            return GetCurrentUserId() != null;
        }

        public async Task<IActionResult> Index(string? searchString, int? status, int? priority)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = HttpContext.Session.GetString("Email");
            var fullName = HttpContext.Session.GetString("FullName");

            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(fullName))
            {
                await _taskReminderService.SendDueSoonReminderAsync(
                    userId.Value,
                    userEmail,
                    fullName);
            }
            var tasks = _context.TaskItems
                .Where(t => t.UserId == userId.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                tasks = tasks.Where(t =>
                    t.Title.Contains(searchString) ||
                    (t.Description != null && t.Description.Contains(searchString)));
            }

            if (status.HasValue)
            {
                tasks = tasks.Where(t => t.Status == status.Value);
            }

            if (priority.HasValue)
            {
                tasks = tasks.Where(t => t.Priority == priority.Value);
            }

            ViewBag.SearchString = searchString;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(await tasks
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId.Value);

            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem taskItem)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                taskItem.CreatedAt = DateTime.Now;
                taskItem.UserId = userId.Value;
                taskItem.IsReminderSent = false;

                _context.Add(taskItem);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(taskItem);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem taskItem)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != taskItem.Id)
            {
                return NotFound();
            }

            var oldTask = await _context.TaskItems
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (oldTask == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    taskItem.UserId = userId.Value;
                    taskItem.CreatedAt = oldTask.CreatedAt;

                    if (taskItem.Status == 2)
                    {
                        taskItem.IsReminderSent = true;
                    }
                    else
                    {
                        taskItem.IsReminderSent = false;
                    }

                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskItemExists(taskItem.Id, userId.Value))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(taskItem);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var taskItem = await _context.TaskItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId.Value);

            if (taskItem == null)
            {
                return NotFound();
            }

            return View(taskItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var taskItem = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (taskItem != null)
            {
                _context.TaskItems.Remove(taskItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TaskItemExists(int id, int userId)
        {
            return _context.TaskItems.Any(e => e.Id == id && e.UserId == userId);
        }
    }
}