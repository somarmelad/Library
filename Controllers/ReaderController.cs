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

        // Поиск ID читателя по логину из Claims
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

            // Получаем логин текущего пользователя (Identity обычно хранит его в ClaimTypes.Name)
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

            // Считаем активные бронирования (через словарь для скорости)
            var reservationsDict = await _context.Reservations
                .Where(r => r.Status == "Pending" || r.Status == "Ready")
                .GroupBy(r => r.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BookId, x => x.Count);

            // Получаем список того, что забронировал именно этот читатель
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

        // POST: Reader/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int BookId)
        {
            var userLogin = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            var readerId = await GetReaderIdAsync(userLogin);

            if (!readerId.HasValue)
            {
                TempData["Error"] = "Профиль читателя не найден.";
                return RedirectToAction(nameof(Index));
            }

            var book = await _context.Books
                .Include(b => b.BookLoans.Where(bl => bl.ReturnDate == null))
                .FirstOrDefaultAsync(b => b.IdBook == BookId);

            if (book == null) return NotFound();

            // Проверка: а не забронировал ли он уже?
            var alreadyReserved = await _context.Reservations
                .AnyAsync(r => r.BookId == BookId && r.ReaderId == readerId.Value && (r.Status == "Pending" || r.Status == "Ready"));

            if (alreadyReserved)
            {
                TempData["Error"] = "Вы уже забронировали эту книгу.";
                return RedirectToAction(nameof(Index));
            }

            // Считаем остаток
            int loaned = book.BookLoans.Count();
            int reserved = await _context.Reservations
                .CountAsync(r => r.BookId == BookId && (r.Status == "Pending" || r.Status == "Ready"));

            if (book.Quantity - loaned - reserved > 0)
            {
                var res = new Reservation
                {
                    BookId = BookId,
                    ReaderId = readerId.Value,
                    ReservationDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddDays(3),
                    Status = "Pending"
                };
                _context.Reservations.Add(res);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Книга забронирована.";
            }
            else
            {
                TempData["Error"] = "Нет свободных экземпляров.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}