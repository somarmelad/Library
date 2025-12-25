using Library2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims; 

namespace Library2.Controllers
{
   
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Home/Index
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View();
            }

           
            if (User.IsInRole("Librarian"))
            {
                return RedirectToAction("Index", "Librarian");
            }

            
            if (User.IsInRole("Reader"))
            {
                return RedirectToAction("Index", "Reader");
            }

            return View();
        }


        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> DbStatusCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    ViewBag.DbStatus = "Подключение к MySQL успешно!";
                    ViewBag.BooksCount = await _context.Books.CountAsync();
                    ViewBag.ReadersCount = await _context.Readers.CountAsync();
                    ViewBag.LoansCount = await _context.BookLoans.CountAsync();
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

        
        public IActionResult Test()
        {
            return Content("HomeController работает!");
        }
    }
}