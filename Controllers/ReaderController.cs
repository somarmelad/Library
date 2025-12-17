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

            var alreadyReserved = await _context.Reservations
                .AnyAsync(r => r.BookId == BookId && r.ReaderId == readerId.Value && (r.Status == "Pending" || r.Status == "Ready"));

            if (alreadyReserved)
            {
                TempData["Error"] = "Вы уже забронировали эту книгу.";
                return RedirectToAction(nameof(Index));
            }

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
    }
}