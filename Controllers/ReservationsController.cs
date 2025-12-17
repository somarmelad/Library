using Microsoft.AspNetCore.Mvc;
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;

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
                TempData["Error"] = "Ошибка аутентификации. Пожалуйста, войдите в систему.";
                return RedirectToAction("Login", "Account");
            }

            // Важно: загружаем книгу
            var book = await _context.Books
                .Include(b => b.BookLoans)
                .FirstOrDefaultAsync(b => b.IdBook == BookId);

            if (book == null)
            {
                TempData["Error"] = "Книга не найдена.";
                return RedirectToAction("Index", "Reader");
            }

            // Проверка на дубликат бронирования
            bool alreadyReserved = await _context.Reservations.AnyAsync(
                r => r.BookId == BookId &&
                     r.ReaderId == readerId &&
                     (r.Status == "Pending" || r.Status == "Ready")
            );

            if (alreadyReserved)
            {
                TempData["Error"] = $"Книга '{book.Title}' уже забронирована вами.";
                return RedirectToAction("Index", "Reader");
            }

            // Считаем доступность
            int activeLoansCount = book.BookLoans?.Count(bl => bl.ReturnDate == null) ?? 0;
            int activeReservationsCount = await _context.Reservations
                .CountAsync(r => r.BookId == BookId && (r.Status == "Pending" || r.Status == "Ready"));

            if (book.Quantity <= (activeLoansCount + activeReservationsCount))
            {
                TempData["Error"] = $"Книга '{book.Title}' сейчас недоступна для бронирования.";
                return RedirectToAction("Index", "Reader");
            }

            var newReservation = new Reservation
            {
                BookId = BookId,
                ReaderId = readerId,
                ReservationDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(3),
                Status = "Pending"
            };

            _context.Reservations.Add(newReservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Книга '{book.Title}' забронирована до {newReservation.ExpirationDate:dd.MM.yyyy}.";
            return RedirectToAction("Index", "Reader");
        }

        // GET: Reservations/Librarian
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> Librarian()
        {
            var activeReservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.Reader)
                .Where(r => r.Status == "Pending" || r.Status == "Ready")
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();

            ViewBag.Readers = await _context.Readers.OrderBy(r => r.LastName).ToListAsync();
            return View(activeReservations);
        }

        // POST: Reservations/IssueBook
        [Authorize(Roles = "Librarian")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBook(int reservationId, int readerId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.IdReservation == reservationId);

            if (reservation == null)
            {
                TempData["Error"] = "Бронирование не найдено.";
                return RedirectToAction(nameof(Librarian));
            }

            var reader = await _context.Readers.FindAsync(readerId);
            if (reader == null)
            {
                TempData["Error"] = "Читатель не найден.";
                return RedirectToAction(nameof(Librarian));
            }

            var newLoan = new BookLoan
            {
                BookId = reservation.BookId,
                ReaderId = readerId,
                EmployeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                LoanDate = DateTime.Now
            };

            _context.BookLoans.Add(newLoan);

            reservation.Status = "Completed";
            _context.Reservations.Update(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Книга '{reservation.Book?.Title}' успешно выдана.";
            return RedirectToAction(nameof(Librarian));
        }

        [Authorize(Roles = "Librarian")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int reservationId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.IdReservation == reservationId);

            if (reservation != null)
            {
                reservation.Status = "Canceled";
                _context.Reservations.Update(reservation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Бронирование отменено.";
            }
            return RedirectToAction(nameof(Librarian));
        }
    }
}