using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLCongViec.Data;
using QLCongViec.Models;
using QLCongViec.Services;
using System.Security.Cryptography;
using System.Text;

namespace QLCongViec.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ITaskReminderService _taskReminderService;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService,
            ITaskReminderService taskReminderService)
        {
            _context = context;
            _emailService = emailService;
            _taskReminderService = taskReminderService;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var emailExists = await _context.UserAccounts
                    .AnyAsync(u => u.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }

                string code = GenerateVerificationCode();

                HttpContext.Session.SetString("PendingFullName", model.FullName);
                HttpContext.Session.SetString("PendingEmail", model.Email);
                HttpContext.Session.SetString("PendingPasswordHash", HashPassword(model.Password));
                HttpContext.Session.SetString("RegisterVerifyCode", code);
                HttpContext.Session.SetString("RegisterCodeExpireAt", DateTime.Now.AddMinutes(5).ToString("O"));

                string body = $@"
                    <h3>Xác thực đăng ký tài khoản QLCongViec</h3>
                    <p>Mã xác thực của bạn là:</p>
                    <h2>{code}</h2>
                    <p>Mã này có hiệu lực trong 5 phút.</p>
                ";

                await _emailService.SendEmailAsync(
                    model.Email,
                    "Mã xác thực đăng ký QLCongViec",
                    body);

                return RedirectToAction("VerifyRegisterEmail");
            }

            return View(model);
        }

        public IActionResult VerifyRegisterEmail()
        {
            var email = HttpContext.Session.GetString("PendingEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegisterEmail(VerificationCodeViewModel model)
        {
            var email = HttpContext.Session.GetString("PendingEmail");
            var fullName = HttpContext.Session.GetString("PendingFullName");
            var passwordHash = HttpContext.Session.GetString("PendingPasswordHash");
            var savedCode = HttpContext.Session.GetString("RegisterVerifyCode");
            var expireAtString = HttpContext.Session.GetString("RegisterCodeExpireAt");

            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(fullName) ||
                string.IsNullOrEmpty(passwordHash) ||
                string.IsNullOrEmpty(savedCode) ||
                string.IsNullOrEmpty(expireAtString))
            {
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;

            if (!DateTime.TryParse(expireAtString, out DateTime expireAt))
            {
                ModelState.AddModelError("", "Mã xác thực không hợp lệ");
                return View(model);
            }

            if (DateTime.Now > expireAt)
            {
                ModelState.AddModelError("", "Mã xác thực đã hết hạn. Vui lòng đăng ký lại.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                if (model.Code != savedCode)
                {
                    ModelState.AddModelError("Code", "Mã xác thực không đúng");
                    return View(model);
                }

                var user = new UserAccount
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = passwordHash,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                ClearRegisterSession();

                return RedirectToAction("Login");
            }

            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string passwordHash = HashPassword(model.Password);

                var user = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == passwordHash);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                    return View(model);
                }

                if (!user.IsEmailConfirmed)
                {
                    ModelState.AddModelError("", "Email chưa được xác thực");
                    return View(model);
                }

                string code = GenerateVerificationCode();

                HttpContext.Session.SetInt32("PendingLoginUserId", user.Id);
                HttpContext.Session.SetString("LoginVerifyCode", code);
                HttpContext.Session.SetString("LoginCodeExpireAt", DateTime.Now.AddMinutes(5).ToString("O"));

                string body = $@"
                    <h3>Xác thực đăng nhập QLCongViec</h3>
                    <p>Mã đăng nhập của bạn là:</p>
                    <h2>{code}</h2>
                    <p>Mã này có hiệu lực trong 5 phút.</p>
                ";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Mã xác thực đăng nhập QLCongViec",
                    body);

                return RedirectToAction("VerifyLoginEmail");
            }

            return View(model);
        }

        public IActionResult VerifyLoginEmail()
        {
            var pendingUserId = HttpContext.Session.GetInt32("PendingLoginUserId");

            if (pendingUserId == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLoginEmail(VerificationCodeViewModel model)
        {
            var pendingUserId = HttpContext.Session.GetInt32("PendingLoginUserId");
            var savedCode = HttpContext.Session.GetString("LoginVerifyCode");
            var expireAtString = HttpContext.Session.GetString("LoginCodeExpireAt");

            if (pendingUserId == null ||
                string.IsNullOrEmpty(savedCode) ||
                string.IsNullOrEmpty(expireAtString))
            {
                return RedirectToAction("Login");
            }

            if (!DateTime.TryParse(expireAtString, out DateTime expireAt))
            {
                ModelState.AddModelError("", "Mã xác thực không hợp lệ");
                return View(model);
            }

            if (DateTime.Now > expireAt)
            {
                ModelState.AddModelError("", "Mã xác thực đã hết hạn. Vui lòng đăng nhập lại.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                if (model.Code != savedCode)
                {
                    ModelState.AddModelError("Code", "Mã xác thực không đúng");
                    return View(model);
                }

                var user = await _context.UserAccounts
                    .FirstOrDefaultAsync(u => u.Id == pendingUserId.Value);

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);

                ClearLoginSession();

                await _taskReminderService.SendDueSoonReminderAsync(
                    user.Id,
                    user.Email,
                    user.FullName);

                return RedirectToAction("Index", "TaskItems");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LogoutConfirmed()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        private string GenerateVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);

            return Convert.ToBase64String(hashBytes);
        }

        private void ClearRegisterSession()
        {
            HttpContext.Session.Remove("PendingFullName");
            HttpContext.Session.Remove("PendingEmail");
            HttpContext.Session.Remove("PendingPasswordHash");
            HttpContext.Session.Remove("RegisterVerifyCode");
            HttpContext.Session.Remove("RegisterCodeExpireAt");
        }

        private void ClearLoginSession()
        {
            HttpContext.Session.Remove("PendingLoginUserId");
            HttpContext.Session.Remove("LoginVerifyCode");
            HttpContext.Session.Remove("LoginCodeExpireAt");
        }
    }
}