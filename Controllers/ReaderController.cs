using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Library2.Controllers
{
    [Authorize]
    public class ReaderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReaderController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetReaderIdAsync(string login)
        {
            if (string.IsNullOrEmpty(login)) return null;

            var reader = await _context.Readers
                .FirstOrDefaultAsync(r => r.Login == login);
            return reader?.IdReader;
        }

        // GET: Reader/Index
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var userLogin = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);

            var booksQuery = _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Include(b => b.BookLoans.Where(bl => bl.ReturnDate == null))
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                booksQuery = booksQuery.Where(b =>
                    b.Title.ToLower().Contains(lowerSearch) ||
                    (b.Annotation != null && b.Annotation.ToLower().Contains(lowerSearch)) ||
                    b.BookAuthors.Any(ba =>
                        ba.Author.FirstName.ToLower().Contains(lowerSearch) ||
                        ba.Author.LastName.ToLower().Contains(lowerSearch)
                    )
                );
            }

            var booksList = await booksQuery.OrderBy(b => b.Title).ToListAsync();

            var reservationsDict = await _context.Reservations
                .Where(r => r.Status == "Pending" || r.Status == "Ready")
                .GroupBy(r => r.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BookId, x => x.Count);

            var readerId = await GetReaderIdAsync(userLogin);
            var myReservedIds = new List<int>();

            if (readerId.HasValue)
            {
                myReservedIds = await _context.Reservations
                    .Where(r => r.ReaderId == readerId.Value && (r.Status == "Pending" || r.Status == "Ready"))
                    .Select(r => r.BookId)
                    .ToListAsync();
            }

            ViewBag.ActiveReservations = reservationsDict;
            ViewBag.MyReservedIds = myReservedIds;

            return View("Index", booksList);
        }

        // GET: Reader/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .Include(b => b.BookLoans)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            int loanedCount = book.BookLoans?.Count(bl => bl.ReturnDate == null) ?? 0;
            int reservedCount = await _context.Reservations
                .CountAsync(r => r.BookId == id && (r.Status == "Pending" || r.Status == "Ready"));

            ViewBag.AvailableCount = book.Quantity - loanedCount - reservedCount;

            var userLogin = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            var readerId = await GetReaderIdAsync(userLogin);

            ViewBag.IsReservedByMe = false;
            if (readerId.HasValue)
            {
                ViewBag.IsReservedByMe = await _context.Reservations
                    .AnyAsync(r => r.BookId == id && r.ReaderId == readerId.Value && (r.Status == "Pending" || r.Status == "Ready"));
            }

            return View(book);
        }

        // POST: Reader/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int bookId)
        {
            // Получаем логин текущего пользователя (читателя)
            var userLogin = User.Identity?.Name;

            // Находим ID читателя в базе (используя твой метод поиска по логину)
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Login == userLogin);

            if (reader == null) return NotFound("Профиль читателя не найден");

            // Создаем новую запись бронирования
            var reservation = new Reservation
            {
                BookId = bookId,
                ReaderId = reader.IdReader,
                ReservationDate = DateTime.Now,

                // УСТАНОВКА СРОКА: Текущая дата + 3 дня
                ExpirationDate = DateTime.Now.AddDays(3),

                Status = "Pending"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Книга забронирована! Пожалуйста, заберите её в течение 3-х дней.";

            return RedirectToAction("MyBooks");
        }


        // GET: Reader/MyBooks
        public async Task<IActionResult> MyBooks()
        {
            var userLogin = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            var readerId = await GetReaderIdAsync(userLogin);
            if (!readerId.HasValue) return NotFound();

            var loans = await _context.BookLoans
                .Include(l => l.Book)
                .Where(l => l.ReaderId == readerId.Value)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();

            var reservations = await _context.Reservations
                .Include(r => r.Book)
                .Where(r => r.ReaderId == readerId.Value)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            ViewBag.Loans = loans;
            ViewBag.Reservations = reservations;

            return View("MyBooks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null) return NotFound();

            var userLogin = User.Identity?.Name;
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Login == userLogin);

            if (reader == null || reservation.ReaderId != reader.IdReader)
            {
                TempData["Error"] = "У вас нет прав для отмены этой брони.";
                return RedirectToAction(nameof(MyBooks));
            }

            if (reservation.Status == "Pending" || reservation.Status == "Ready")
            {
                reservation.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Бронирование успешно отменено.";
            }
            else
            {
                TempData["Error"] = "Эту бронь уже нельзя отменить.";
            }

            return RedirectToAction(nameof(MyBooks));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendLoan(int id)
        {   
            var loan = await _context.BookLoans
                .FirstOrDefaultAsync(l => l.IdBookLoan == id && l.ReturnDate == null);

            if (loan == null)
            {
                TempData["Error"] = "Выдача не найдена или книга уже возвращена.";
                return RedirectToAction(nameof(MyBooks));
            }

            var userLogin = User.Identity?.Name;
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Login == userLogin);

            if (reader == null || loan.ReaderId != reader.IdReader)
            {
                TempData["Error"] = "Ошибка доступа.";
                return RedirectToAction(nameof(MyBooks));
            }


            loan.LoanDate = DateTime.Now; 
            await _context.SaveChangesAsync();

            TempData["Success"] = "Срок пользования книгой продлен на 14 дней!";
            return RedirectToAction(nameof(MyBooks));
        }
    }
}