using Microsoft.AspNetCore.Mvc;
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Library2.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .ToListAsync();
            return View(books);
        }

        // GET: Books/Details/5
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
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewBag.Publishers = _context.Publishers.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, List<int> selectedAuthors, List<int> selectedGenres)
        {
            if (ModelState.IsValid)
            {
                // Добавляем связи с авторами
                if (selectedAuthors != null)
                {
                    foreach (var authorId in selectedAuthors)
                    {
                        book.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }
                }

                // Добавляем связи с жанрами
                if (selectedGenres != null)
                {
                    foreach (var genreId in selectedGenres)
                    {
                        book.BookGenres.Add(new BookGenre { BookId = book.IdBook, GenreId = genreId });
                    }
                }

                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Publishers = _context.Publishers.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .Include(b => b.BookGenres)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Publishers = _context.Publishers.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, List<int> selectedAuthors, List<int> selectedGenres)
        {
            if (id != book.IdBook)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Обновляем связи с авторами
                    var existingAuthors = _context.BookAuthors.Where(ba => ba.BookId == id);
                    _context.BookAuthors.RemoveRange(existingAuthors);

                    if (selectedAuthors != null)
                    {
                        foreach (var authorId in selectedAuthors)
                        {
                            _context.BookAuthors.Add(new BookAuthor { BookId = id, AuthorId = authorId });
                        }
                    }

                    // Обновляем связи с жанрами
                    var existingGenres = _context.BookGenres.Where(bg => bg.BookId == id);
                    _context.BookGenres.RemoveRange(existingGenres);

                    if (selectedGenres != null)
                    {
                        foreach (var genreId in selectedGenres)
                        {
                            _context.BookGenres.Add(new BookGenre { BookId = id, GenreId = genreId });
                        }
                    }

                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.IdBook))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Publishers = _context.Publishers.ToList();
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.IdBook == id);
        }
    }
}
