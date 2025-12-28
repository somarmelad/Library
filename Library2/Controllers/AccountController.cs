using Library2.Data;
using Library2.Helpers; 
using Library2.Models;
using Library2.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library2.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool loginExists = await _context.Readers.AnyAsync(r => r.Login == model.Login) ||
                                    await _context.Employees.AnyAsync(e => e.Login == model.Login);

                if (loginExists)
                {
                    ModelState.AddModelError("Login", "Пользователь с таким логином уже зарегистрирован.");
                    return View(model);
                }

                if (model.BirthDate > DateTime.Now)
                {
                    ModelState.AddModelError("BirthDate", "Дата рождения не может быть в будущем.");
                    return View(model);
                }

                string passwordHash = PasswordHasher.HashPassword(model.Password);

                var newReader = new Reader
                {
                    Login = model.Login,
                    PasswordHash = passwordHash,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    MiddleName = model.MiddleName,
                    BirthDate = model.BirthDate,
                    Phone = model.Phone,
                    Role = "Reader"
                };

                _context.Readers.Add(newReader);
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, newReader.IdReader.ToString()),
                    new Claim(ClaimTypes.Name, newReader.Login),
                    new Claim(ClaimTypes.Role, "Reader"),
                    new Claim("UserType", "Reader")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Reader");
            }

            return View(model);
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Поиск пользователя
                var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Login == model.Login);
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Login == model.Login);

                string role = "";
                string passwordHash = "";
                int userId = 0;
                string userType = "";

                if (reader != null)
                {
                    passwordHash = reader.PasswordHash;
                    role = reader.Role;
                    userId = reader.IdReader;
                    userType = "Reader";
                }
                else if (employee != null)
                {
                    passwordHash = employee.PasswordHash;
                    role = employee.Role;
                    userId = employee.IdEmployee;
                    userType = "Librarian";
                }

                if (string.IsNullOrEmpty(role) || !PasswordHasher.VerifyPassword(model.Password, passwordHash))
                {
                    ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, model.Login),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("UserType", userType)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false
                };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                if (userType == "Librarian")
                {
                    return RedirectToAction("Index", "Librarian");
                }
                else if (userType == "Reader")
                {
                    return RedirectToAction("Index", "Reader");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(model);
        }

        // GET: Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}