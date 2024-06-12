using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using System.Text;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Пользователь с таким логином уже существует.");
                    return View(model);
                }

                var salt = CreateSalt();
                var passwordHash = HashPassword(model.Password, salt);

                var user = new User
                {
                    Login = model.Login,
                    Password = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(salt),
                    FullName = model.FullName
                };

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Success");
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);

                if (user != null && VerifyPassword(model.Password, Convert.FromBase64String(user.Password), Convert.FromBase64String(user.PasswordSalt)))
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    return RedirectToAction("UserProfile", new { userId = user.Id });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
                }
            }

            return View(model);
        }

        private byte[] HashPassword(string password, byte[] salt)
        {
            using (var blake2b = new HMACBlake2B(256))
            {
                blake2b.Key = salt;
                return blake2b.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            var hashToVerify = HashPassword(password, salt);
            return hash.SequenceEqual(hashToVerify);
        }

        private byte[] CreateSalt()
        {
            var buffer = new byte[16]; 
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return buffer;
        }

        public async Task<IActionResult> UserProfile(int userId, string senderLogin, DateTime? dateFrom, DateTime? dateTo, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            IQueryable<Message> query = _context.Messages
                .Where(m => m.ToUserId == userId)
                .Include(m => m.FromUser);

            if (!string.IsNullOrEmpty(senderLogin))
            {
                query = query.Where(m => m.FromUser.Login.Contains(senderLogin));
            }
            if (dateFrom.HasValue)
            {
                var utcDateFrom = DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc);
                query = query.Where(m => m.SendDate >= utcDateFrom);
            }
            if (dateTo.HasValue)
            {
                var utcDateTo = DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc);
                query = query.Where(m => m.SendDate <= utcDateTo);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var messages = await query.OrderBy(m => m.SendDate).ToListAsync();

            ViewBag.Messages = messages;
            return View(user);
        }






        [HttpGet]
        public async Task<IActionResult> SendMessage()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            // Убедитесь, что список логинов устанавливается даже если он пустой
            var users = await _context.Users
                                      .OrderBy(u => u.Login)
                                      .Select(u => u.Login)
                                      .ToListAsync() ?? new List<string>();

            ViewData["AllUserLogins"] = users;
            ViewData["UserId"] = userId.Value;
            return View();
        }






        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                int? userId = HttpContext.Session.GetInt32("UserId"); // Получение ID пользователя из сессии
                if (!userId.HasValue)
                {
                    // Пользователь не авторизован
                    return RedirectToAction("Login");
                }

                var recipient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == model.RecipientLogin);
                if (recipient == null)
                {
                    ModelState.AddModelError("RecipientLogin", "Пользователь с таким логином не найден.");
                    return View(model);
                }

                var message = new Message
                {
                    FromUserId = userId.Value,
                    ToUserId = recipient.Id,
                    Title = model.MessageTitle,
                    Text = model.MessageText,
                    SendDate = DateTime.UtcNow,
                    Status = "Новое"
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                ViewBag.Message = "Сообщение успешно отправлено.";
                return View("SendMessageSuccess");
            }

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null && message.Status != "прочитанное")
            {
                message.Status = "прочитанное";
                await _context.SaveChangesAsync();
            }

            return Ok();
        }




        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            var users = await _context.Users
                .Where(u => u.Login.Contains(term))
                .Select(u => new { label = u.Login })
                .Take(5)
                .ToListAsync();

            return Json(users);
        }


        public IActionResult Success()
        {
            return View();
        }

    }
}
