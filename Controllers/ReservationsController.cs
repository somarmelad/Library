using Microsoft.AspNetCore.Mvc;
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
        // Метод, вызываемый с главной страницы при нажатии "Забронировать"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int BookId)
        {
            var book = await _context.Books
                .Include(b => b.BookLoans)
                .FirstOrDefaultAsync(b => b.IdBook == BookId);

            if (book == null)
            {
                TempData["Error"] = "Книга не найдена.";
                return RedirectToAction("Index", "Books");
            }

            // Проверка доступности
            int availableCount = book.Quantity - (book.BookLoans?.Count(bl => bl.ReturnDate == null) ?? 0);

            if (availableCount <= 0)
            {
                TempData["Error"] = $"Книга '{book.Title}' сейчас отсутствует. Бронирование невозможно.";
                return RedirectToAction("Index", "Books");
            }

            // ⚠️ В реальном приложении здесь должна быть логика получения ID читателя (пользователя)
            // Для упрощения, пока перенаправим на страницу выбора читателя/подтверждения.
            // Вместо этого, создадим пока бронь без ReaderId, а библиотекарь сам его выберет.

            var newReservation = new Reservation
            {
                BookId = BookId,
                ReservationDate = DateTime.Now,
                // Бронь активна 3 дня
                ExpirationDate = DateTime.Now.AddDays(3),
                Status = "Pending"
            };

            _context.Reservations.Add(newReservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Книга '{book.Title}' успешно забронирована! Бронь действительна до {newReservation.ExpirationDate:dd.MM.yyyy}.";
            return RedirectToAction("Index", "Books");
        }

        // GET: Reservations/Librarian (Панель библиотекаря)
        // Здесь библиотекарь видит бронирования и проводит выдачу.
        public async Task<IActionResult> Librarian()
        {
            var activeReservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.Reader) // Если ReaderId заполнен
                .Where(r => r.Status == "Pending" || r.Status == "Ready")
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();

            // Передаем также список всех читателей для формы выдачи
            ViewBag.Readers = await _context.Readers.OrderBy(r => r.LastName).ToListAsync();

            return View(activeReservations);
        }

        // POST: Reservations/IssueBook (Выдача забронированной книги)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBook(int reservationId, int readerId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);

            if (reservation == null)
            {
                TempData["Error"] = "Бронирование не найдено.";
                return RedirectToAction(nameof(Librarian));
            }

            // 1. Проверяем, существует ли читатель
            var reader = await _context.Readers.FindAsync(readerId);
            if (reader == null)
            {
                TempData["Error"] = "Выбранный читатель не существует.";
                return RedirectToAction(nameof(Librarian));
            }

            // 2. Создаем новую запись о выдаче (BookLoan)
            var newLoan = new BookLoan // Предполагая, что у вас есть модель BookLoan
            {
                BookId = reservation.BookId,
                ReaderId = readerId,
                // Предполагается, что в реальной системе вы получите ID текущего сотрудника
                EmployeeId = 1, // ID сотрудника (замените на реальную логику аутентификации)
                LoanDate = DateTime.Now,
                // ReturnDate = null (активная выдача)
            };

            _context.BookLoans.Add(newLoan);

            // 3. Обновляем статус бронирования
            reservation.Status = "Completed";
            reservation.ReaderId = readerId; // Фиксируем, кому выдана книга

            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Книга '{reservation.Book.Title}' успешно выдана читателю {reader.LastName}.";
            return RedirectToAction(nameof(Librarian));
        }

        // POST: Reservations/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
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
