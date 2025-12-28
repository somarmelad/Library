using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
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

            var booksQuery = _context.Books
                 .AsNoTracking()
                 .Include(b => b.Publisher)
                 .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                 .OrderBy(b => b.Title)
                 .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                booksQuery = booksQuery.Where(b =>
                    b.Title.ToLower().Contains(lowerSearch) ||
                    b.BookAuthors.Any(ba => ba.Author.LastName.ToLower().Contains(lowerSearch)));
            }

            var booksList = await booksQuery.ToListAsync();

            var userLogin = User.Identity?.Name;
            var readerId = await GetReaderIdAsync(userLogin);

            var myReservedIds = new List<int>();
            if (readerId.HasValue)
            {
                myReservedIds = await _context.Reservations
                    .Where(r => r.ReaderId == readerId.Value && r.Status == "Ready")
                    .Select(r => r.BookId)
                    .ToListAsync();
            }

            ViewBag.MyReservedIds = myReservedIds;
            return View(booksList);
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

            ViewBag.AvailableCount = await _context.Books
        .CountAsync(b => b.Title == book.Title && b.Status == BookStatus.Available);

            var userLogin = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            var readerId = await GetReaderIdAsync(userLogin);

            ViewBag.IsReservedByMe = false;
            if (readerId.HasValue)
            {
                ViewBag.IsReservedByMe = await _context.Reservations
                    .AnyAsync(r => r.ReaderId == readerId.Value
                                 && r.Book.Title == book.Title
                                 && r.Status == "Ready");
            }

            return View(book);
        }

        // POST: Reader/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int bookId, string Title)
        {
            var userLogin = User.Identity?.Name;
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Login == userLogin);
            if (reader == null) return NotFound("Профиль читателя не найден");

            // Ищем первый свободный экземпляр
            var availableBook = await _context.Books
                .FirstOrDefaultAsync(b => b.Title == Title && b.Status == BookStatus.Available);

            if (availableBook == null)
            {
                TempData["Error"] = "Свободных экземпляров нет.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                availableBook.Status = BookStatus.Reserved;
                _context.Update(availableBook);

                var reservation = new Reservation
                {
                    BookId = availableBook.IdBook,
                    ReaderId = reader.IdReader,
                    ReservationDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddDays(3),
                    Status = "Ready" 
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Книга забронирована и ждет вас! Номер экземпляра: #{availableBook.IdBook}.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Ошибка при бронировании.";
            }

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

        [Authorize(Roles = "Reader")]
[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id) 
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int readerId)) return Challenge();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var reservation = await _context.Reservations
                        .FirstOrDefaultAsync(r => r.IdReservation == id && r.ReaderId == readerId);

                    if (reservation == null)
                    {
                        TempData["Error"] = "Бронирование не найдено.";
                        return RedirectToAction("Index", "Reader");
                    }

                    if (reservation.Status == "Completed")
                    {
                        TempData["Error"] = "Книга уже выдана вам на руки.";
                        return RedirectToAction("Index", "Reader");
                    }

                    var book = await _context.Books.FirstOrDefaultAsync(b => b.IdBook == reservation.BookId);
                    if (book != null)
                    {
                        book.Status = BookStatus.Available;
                        _context.Update(book);
                    }

                    reservation.Status = "Cancelled";
                    _context.Update(reservation);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Бронирование успешно отменено.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Ошибка при отмене бронирования.";
                }

                return RedirectToAction("Index", "Reader");
            });
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