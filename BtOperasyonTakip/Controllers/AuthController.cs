using BtOperasyonTakip.Data;
using BtOperasyonTakip.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BtOperasyonTakip.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Operasyon"))
                    return RedirectToAction("Index", "Dashboard");
                if (User.IsInRole("Saha"))
                    return RedirectToAction("Index", "Ticket");

                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            var hash = HashPassword(password);

            var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.PasswordHash == hash);
            if (user == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "Saha")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            if (user.Role == "Operasyon")
                return RedirectToAction("Index", "Dashboard");
            else
                return RedirectToAction("Index", "Ticket");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string fullName, string username, string email, string password, string role)
        {
            if (_context.Users.Any(u => u.UserName == username))
            {

                ViewBag.Error = "Bu kullanıcı adı zaten mevcut.";
                return View();
            }

            else if(_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Bu e-mail adı zaten mevcut.";
                return View();
            }
            
            var user = new User
                {
                    FullName = fullName,
                    UserName = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    CreatedAt = DateTime.Now,
                    Role = string.IsNullOrWhiteSpace(role) ? "Saha" : role
                };

            _context.Users.Add(user);
            _context.SaveChanges();

            await SignInUser(user);

            if (user.Role == "Operasyon")
                return RedirectToAction("Index", "Home");
            else
                return RedirectToAction("Index", "Ticket");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "Saha")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }
    }
}
