using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Для SelectList
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library2.Controllers
{
    public class BookLoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookLoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BookLoans
        public async Task<IActionResult> Index()
        {
            var loans = await _context.BookLoans
                .Include(bl => bl.Book)
                .Include(bl => bl.Reader)
                .Include(bl => bl.Employee)
                .ToListAsync();
            return View(loans);
        }

        // GET: BookLoans/Create
        public async Task<IActionResult> Create()
        {
            // ИСПРАВЬ: Async ToList и SelectList (предполагая поля IdBook/Title для Book, IdReader/FullName для Reader, IdEmployee/Name для Employee)
            ViewBag.Books = new SelectList(
                await _context.Books.Where(b => b.Quantity > 0).ToListAsync(),
                "IdBook", "Title");

            ViewBag.Readers = new SelectList(
                await _context.Readers.ToListAsync(),
                "IdReader", "FullName"); // Замени "FullName" на реальное поле, если другое

            ViewBag.Employees = new SelectList(
                await _context.Employees.ToListAsync(),
                "IdEmployee", "Name"); // Замени "Name" на реальное поле

            return View(new BookLoan());
        }

        // POST: BookLoans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookLoan bookLoan)
        {
            if (ModelState.IsValid)
            {
                // Проверяем и уменьшаем количество
                var book = await _context.Books.FindAsync(bookLoan.BookId);
                if (book == null || book.Quantity <= 0)
                {
                    ModelState.AddModelError("BookId", "Книга недоступна или не существует.");
                    await LoadViewBags(bookLoan.BookId, bookLoan.ReaderId, bookLoan.EmployeeId);
                    return View(bookLoan);
                }

                book.Quantity--;
                _context.Update(book);

                bookLoan.LoanDate = DateTime.Now; // Установи дату выдачи, если не в модели
                _context.Add(bookLoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // При ошибке: Перезагрузи ViewBag с выбранными значениями
            await LoadViewBags(bookLoan.BookId, bookLoan.ReaderId, bookLoan.EmployeeId);
            return View(bookLoan);
        }

        // POST: BookLoans/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var loan = await _context.BookLoans
                .Include(bl => bl.Book)
                .FirstOrDefaultAsync(bl => bl.IdBookLoan == id);

            if (loan == null)
            {
                return NotFound();
            }

            // Возвращаем книгу
            loan.ReturnDate = DateTime.Now;
            if (loan.Book != null)
            {
                loan.Book.Quantity++;
                _context.Update(loan.Book);
            }
            _context.Update(loan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Вспомогательный метод для загрузки ViewBag (используется в Create POST)
        private async Task LoadViewBags(int? selectedBookId = null, int? selectedReaderId = null, int? selectedEmployeeId = null)
        {
            ViewBag.Books = new SelectList(
                await _context.Books.Where(b => b.Quantity > 0).ToListAsync(),
                "IdBook", "Title", selectedBookId);

            ViewBag.Readers = new SelectList(
                await _context.Readers.ToListAsync(),
                "IdReader", "FullName", selectedReaderId); // Замени "FullName" на реальное

            ViewBag.Employees = new SelectList(
                await _context.Employees.ToListAsync(),
                "IdEmployee", "Name", selectedEmployeeId); // Замени "Name" на реальное
        }
    }
}