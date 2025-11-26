using Library2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Проверяем подключение к БД
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    ViewBag.DbStatus = "Подключение к MySQL успешно!";

                    // Пробуем получить список книг
                    var booksCount = await _context.Books.CountAsync();
                    ViewBag.BooksCount = booksCount;

                    var readersCount = await _context.Readers.CountAsync();
                    ViewBag.ReadersCount = readersCount;

                    var loansCount = await _context.BookLoans.CountAsync();
                    ViewBag.LoansCount = loansCount;
                }
                else
                {
                    ViewBag.DbStatus = "Не удалось подключиться к MySQL";
                }
            }
            catch (Exception ex)
            {
                ViewBag.DbStatus = $"Ошибка подключения: {ex.Message}";
                _logger.LogError(ex, "Ошибка подключения к БД");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Простой тестовый метод
        public IActionResult Test()
        {
            return Content("HomeController работает!");
        }
    }
}