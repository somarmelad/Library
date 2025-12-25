using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Library2.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Reservations/Reserve
        [Authorize(Roles = "Reader")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int BookId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int readerId))
            {
                return RedirectToAction("Login", "Account");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            bool isSuccess = false;
            string message = "";

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var book = await _context.Books.FirstOrDefaultAsync(b => b.IdBook == BookId);

                    if (book == null || book.Status != BookStatus.Available)
                    {
                        isSuccess = false;
                        message = "Книга уже занята или не существует.";
                        return;
                    }

                    book.Status = BookStatus.Reserved;

                    var newReservation = new Reservation
                    {
                        BookId = BookId,
                        ReaderId = readerId,
                        ReservationDate = DateTime.Now,
                        ExpirationDate = DateTime.Now.AddDays(3),
                        Status = "Ready"
                    };

                    _context.Reservations.Add(newReservation);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    isSuccess = true;
                    message = $"Экземпляр #{book.IdBook} успешно забронирован.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    isSuccess = false;
                    message = "Системная ошибка при бронировании.";
                    throw;
                }
            });

            if (isSuccess) TempData["Success"] = message;
            else TempData["Error"] = message;

            return RedirectToAction("Index", "Reader");
        }

        // GET: Reservations/Librarian
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Librarian()
        {
            var activeReservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.Reader)
                .Where(r => r.Status == "Ready")
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();

            return View(activeReservations);
        }

        // POST: Reservations/IssueBook
        [Authorize(Roles = "Librarian")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBook(int reservationId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.IdReservation == reservationId);

                if (reservation == null || reservation.Book == null)
                {
                    TempData["Error"] = "Бронирование не найдено.";
                    return RedirectToAction(nameof(Librarian));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    reservation.Book.Status = BookStatus.Issued;

                    var newLoan = new BookLoan
                    {
                        BookId = reservation.BookId,
                        ReaderId = reservation.ReaderId ?? 0,
                        EmployeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                        LoanDate = DateTime.Now
                    };

                    reservation.Status = "Issued";

                    _context.BookLoans.Add(newLoan);
                    _context.Update(reservation);
                    _context.Update(reservation.Book);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Книга выдана.";
                    return RedirectToAction(nameof(Librarian));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Ошибка при выдаче.";
                    return RedirectToAction(nameof(Librarian));
                }
            });
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

                    // 1. Обновляем статус книги
                    var book = await _context.Books.FirstOrDefaultAsync(b => b.IdBook == reservation.BookId);
                    if (book != null)
                    {
                        book.Status = BookStatus.Available;
                        _context.Update(book);
                    }

                    // 2. Обновляем статус брони
                    reservation.Status = "Cancelled"; // В View у вас проверка на "Cancelled", а в коде было "Canceled" (с одной 'l')
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
    }
}