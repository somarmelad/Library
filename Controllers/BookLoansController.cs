using Library2.Models;
using Library2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult Create()
        {
            ViewBag.Books = _context.Books.Where(b => b.Quantity > 0).ToList();
            ViewBag.Readers = _context.Readers.ToList();
            ViewBag.Employees = _context.Employees.ToList();

            return View();
        }

        // POST: BookLoans/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookLoan bookLoan)
        {
            if (ModelState.IsValid)
            {
                // Уменьшаем количество доступных книг
                var book = await _context.Books.FindAsync(bookLoan.BookId);
                if (book != null && book.Quantity > 0)
                {
                    book.Quantity--;
                    _context.Update(book);
                }

                _context.Add(bookLoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Books = _context.Books.Where(b => b.Quantity > 0).ToList();
            ViewBag.Readers = _context.Readers.ToList();
            ViewBag.Employees = _context.Employees.ToList();
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
            }

            _context.Update(loan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
