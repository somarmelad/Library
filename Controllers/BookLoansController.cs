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
            var booksWithAvailability = await _context.Books
                .Select(b => new
                {
                    b.IdBook,
                    b.Title,
                    b.Quantity,
                    LoanedCount = b.BookLoans.Count(bl => bl.ReturnDate == null)
                })
                .ToListAsync();

            var availableBooks = booksWithAvailability
                .Where(b => b.Quantity > b.LoanedCount)
                .Select(b => new
                {
                    b.IdBook,
                    Text = $"{b.Title} (Доступно: {b.Quantity - b.LoanedCount})"
                })
                .OrderBy(b => b.Text)
                .ToList();


            ViewBag.Books = new SelectList(availableBooks, "IdBook", "Text", bookId);

            
            var readers = await _context.Readers
                .Select(r => new
                {
                    r.IdReader,
                    r.LastName,    
                    r.FirstName,   
                    FullName = $"{r.LastName} {r.FirstName} {r.MiddleName}"
                })
                .OrderBy(r => r.LastName)
                .ToListAsync();
            ViewBag.Readers = new SelectList(readers, "IdReader", "FullName", readerId);

            var employees = await _context.Employees
                .Select(e => new
                {
                    e.IdEmployee,
                    e.LastName,    
                    e.FirstName,   
                    FullName = $"{e.LastName} {e.FirstName} {e.MiddleName}"
                })
        .OrderBy(e => e.LastName)
        .ToListAsync();

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
            
            var book = await _context.Books
                .Include(b => b.BookLoans)
                .FirstOrDefaultAsync(b => b.IdBook == bookLoan.BookId);

            if (book == null)
            {
                ModelState.AddModelError("BookId", "Выбранная книга не найдена.");
            }
            else
            {
                var loanedCount = book.BookLoans.Count(bl => bl.ReturnDate == null);
                if (book.Quantity <= loanedCount)
                {
                    ModelState.AddModelError("BookId", "Нет доступных экземпляров выбранной книги.");
                }
            }
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ОШИБКИ VALIDATION:");
                foreach (var modelStateEntry in ModelState.Where(e => e.Value.Errors.Any()))
                {
                    var key = modelStateEntry.Key;
                    var errors = modelStateEntry.Value.Errors.Select(e => e.ErrorMessage).ToList();
                    // Вывод в консоль или лог
                    Console.WriteLine($"Поле: '{key}', Ошибка(и): {string.Join("; ", errors)}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bookLoan.LoanDate = bookLoan.LoanDate.Date; 

                    _context.Add(bookLoan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx) 
                {
                    
                    var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                    ModelState.AddModelError("", $"Ошибка БД: {innerMessage}");
                    Console.WriteLine($"Ошибка БД при сохранении: {innerMessage}");
                }
                catch (Exception ex)
                {
                    
                    ModelState.AddModelError("", $"Ошибка при сохранении выдачи: {ex.Message}");
                }
            }

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

            // Для редактирования нам нужно показать все книги, включая ту, которая уже выдана
            var books = await _context.Books
                .Select(b => new
                {
                    b.IdBook,
                    Text = b.Title
                })
                .OrderBy(b => b.Text)
                .ToListAsync();

            ViewBag.Books = new SelectList(books, "IdBook", "Text", bookLoan.BookId);

            // Читатели и сотрудники
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
            if (id != bookLoan.IdBookLoan)
            {
                return NotFound();
            }

            // Если ReturnDate была установлена пользователем, нужно убедиться, что она не раньше LoanDate
            if (bookLoan.ReturnDate.HasValue && bookLoan.ReturnDate.Value.Date < bookLoan.LoanDate.Date)
            {
                ModelState.AddModelError("ReturnDate", "Дата возврата не может быть раньше даты выдачи.");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    bookLoan.LoanDate = bookLoan.LoanDate.Date;
                    if (bookLoan.ReturnDate.HasValue)
                    {
                        bookLoan.ReturnDate = bookLoan.ReturnDate.Value.Date;
                    }

                    _context.Update(bookLoan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.BookLoans.Any(e => e.IdBookLoan == bookLoan.IdBookLoan))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обновлении выдачи: {ex.Message}");
                }
            }

            // В случае ошибки возвращаем представление с заполненными DropDowns
            await PopulateDropdowns(bookLoan.BookId, bookLoan.ReaderId, bookLoan.EmployeeId);
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