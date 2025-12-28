using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Library2.Data; 
using Library2.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Library2.Controllers
{
    public class BookLoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookLoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        

        private async Task PopulateDropdowns(int? bookId = null, int? readerId = null, int? employeeId = null)
        {
            var booksData = await _context.Books
                .Where(b => b.Status == BookStatus.Available || b.IdBook == bookId)
                .Select(b => new { b.IdBook, b.Title }) 
                .ToListAsync();

            var availableBooks = booksData
                .Select(b => new
                {
                    b.IdBook,
                    Text = $"#{b.IdBook} - {b.Title} (Свободна)"
                })
                .OrderBy(b => b.Text)
                .ToList();

            ViewBag.Books = new SelectList(availableBooks, "IdBook", "Text", bookId);

            var readersData = await _context.Readers
                .Select(r => new { r.IdReader, r.FirstName, r.LastName, r.MiddleName })
                .ToListAsync();

            var readers = readersData
                .Select(r => new
                {
                    r.IdReader,
                    FullName = $"{r.LastName} {r.FirstName} {r.MiddleName}"
                })
                .OrderBy(r => r.FullName)
                .ToList();

            ViewBag.Readers = new SelectList(readers, "IdReader", "FullName", readerId);

            var employeesData = await _context.Employees
                .Select(e => new { e.IdEmployee, e.FirstName, e.LastName, e.MiddleName })
                .ToListAsync();

            var employees = employeesData
                .Select(e => new
                {
                    e.IdEmployee,
                    FullName = $"{e.LastName} {e.FirstName} {e.MiddleName}"
                })
                .OrderBy(e => e.FullName)
                .ToList();

            ViewBag.Employees = new SelectList(employees, "IdEmployee", "FullName", employeeId);
        }
        

        // ---
        // GET: BookLoans (Список всех выдач)
        // ---

        public async Task<IActionResult> Index()
        {

            var bookLoans = await _context.BookLoans
                .Include(b => b.Book)
                .Include(b => b.Reader)
                .Include(b => b.Employee)
                .OrderByDescending(b => b.LoanDate)
                .ToListAsync();

            return View(bookLoans);
        }

        // ---
        // GET: BookLoans/Details/5
        // ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookLoan = await _context.BookLoans
                .Include(b => b.Book)
                .Include(b => b.Reader)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.IdBookLoan == id);

            if (bookLoan == null)
            {
                return NotFound();
            }

            return View(bookLoan);
        }

        // ---
        // GET: BookLoans/Create (Выдача книги)
        // ---

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new BookLoan { LoanDate = DateTime.Today });
        }

        // ---
        // POST: BookLoans/Create
        // ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookLoan bookLoan)
        {
            // 1. Ищем книгу, которую пытаются выдать
            var book = await _context.Books.FindAsync(bookLoan.BookId);

            if (book == null)
            {
                ModelState.AddModelError("BookId", "Экземпляр не найден.");
            }
            else if (book.Status != BookStatus.Available && book.Status != BookStatus.Reserved)
            {
                ModelState.AddModelError("BookId", "Этот экземпляр уже выдан.");
            }

            if (ModelState.IsValid)
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            
                            book.Status = BookStatus.Issued;
                            _context.Update(book);

                            var reservation = await _context.Reservations
                                .FirstOrDefaultAsync(r => r.BookId == bookLoan.BookId
                                                     && r.ReaderId == bookLoan.ReaderId
                                                     && r.Status == "Ready");

                            if (reservation != null)
                            {
                                reservation.Status = "Issued";
                                _context.Update(reservation);
                            }

                            bookLoan.LoanDate = bookLoan.LoanDate.Date;
                            _context.Add(bookLoan);

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    });

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                }
            }

            // Если форма невалидна, заново заполняем списки и возвращаем вид
            await PopulateDropdowns(bookLoan.BookId, bookLoan.ReaderId, bookLoan.EmployeeId);
            return View(bookLoan);
        }

        // ---
        // GET: BookLoans/Edit/5 (Изменение или Возврат)
        // ---

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookLoan = await _context.BookLoans
            .Include(b => b.Book)    
            .Include(b => b.Reader)  
            .Include(b => b.Employee)
            .FirstOrDefaultAsync(m => m.IdBookLoan == id);
            if (bookLoan == null)
            {
                return NotFound();
            }

            var books = await _context.Books
                .Select(b => new
                {
                    b.IdBook,
                    Text = b.Title
                })
                .OrderBy(b => b.Text)
                .ToListAsync();

            ViewBag.Books = new SelectList(books, "IdBook", "Text", bookLoan.BookId);

            await PopulateDropdowns(bookLoan.BookId, bookLoan.ReaderId, bookLoan.EmployeeId);

            return View(bookLoan);
        }

        // ---
        // POST: BookLoans/Edit/5
        // ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookLoan bookLoan)
        {
            if (id != bookLoan.IdBookLoan) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (bookLoan.ReturnDate.HasValue)
                    {
                        var book = await _context.Books.FindAsync(bookLoan.BookId);
                        if (book != null)
                        {
                            book.Status = BookStatus.Available; 
                            _context.Update(book);
                        }
                    }

                    _context.Update(bookLoan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка: " + ex.Message);
                }
            }
            return View(bookLoan);
        }

        // ---
        // GET: BookLoans/Delete/5
        // ---

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bookLoan = await _context.BookLoans
                .Include(b => b.Book)
                .Include(b => b.Reader)
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(m => m.IdBookLoan == id);

            if (bookLoan == null)
            {
                return NotFound();
            }

            return View(bookLoan);
        }

        // ---
        // POST: BookLoans/Delete/5
        // ---

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bookLoan = await _context.BookLoans.FindAsync(id);
            if (bookLoan != null)
            {
                _context.BookLoans.Remove(bookLoan);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}