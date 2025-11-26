using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Для SelectList и MultiSelectList
using Library2.Models;
using Library2.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
                .Include(b => b.BookLoans) // Если нужно показывать выдачи
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
                .Include(b => b.BookLoans)
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
            // SelectList для single-select (Publisher)
            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name"); // Предполагаю поля Id/Name в Publisher

            // MultiSelectList для many-to-many (Authors, Genres)
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "Name");
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "Name");

            return View(new Book());
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, List<int> selectedAuthors, List<int> selectedGenres)
        {
            if (ModelState.IsValid)
            {
                // Добавляем книгу сначала (для генерации IdBook)
                _context.Add(book);
                await _context.SaveChangesAsync();

                // Теперь добавляем связи many-to-many
                if (selectedAuthors != null && selectedAuthors.Any())
                {
                    foreach (var authorId in selectedAuthors)
                    {
                        _context.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }
                }

                if (selectedGenres != null && selectedGenres.Any())
                {
                    foreach (var genreId in selectedGenres)
                    {
                        _context.BookGenres.Add(new BookGenre { BookId = book.IdBook, GenreId = genreId });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // При ошибке валидации: Перезагрузи ViewBag
            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "Name");
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "Name");
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
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                .Include(b => b.BookGenres)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            // Выбранные значения для multi-select
            var selectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
            var selectedGenreIds = book.BookGenres.Select(bg => bg.GenreId).ToList();

            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "Name", selectedAuthorIds);
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "Name", selectedGenreIds);

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
                    // Удаляем старые связи many-to-many
                    var existingAuthors = _context.BookAuthors.Where(ba => ba.BookId == id);
                    _context.BookAuthors.RemoveRange(existingAuthors);

                    var existingGenres = _context.BookGenres.Where(bg => bg.BookId == id);
                    _context.BookGenres.RemoveRange(existingGenres);

                    // Добавляем новые
                    if (selectedAuthors != null && selectedAuthors.Any())
                    {
                        foreach (var authorId in selectedAuthors)
                        {
                            _context.BookAuthors.Add(new BookAuthor { BookId = id, AuthorId = authorId });
                        }
                    }

                    if (selectedGenres != null && selectedGenres.Any())
                    {
                        foreach (var genreId in selectedGenres)
                        {
                            _context.BookGenres.Add(new BookGenre { BookId = id, GenreId = genreId });
                        }
                    }

                    // Обновляем саму книгу
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

            // При ошибке: Перезагрузи ViewBag с выбранными
            var selectedAuthorIds = selectedAuthors ?? new List<int>();
            var selectedGenreIds = selectedGenres ?? new List<int>();
            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            ViewBag.Authors = new MultiSelectList(_context.Authors, "Id", "Name", selectedAuthorIds);
            ViewBag.Genres = new MultiSelectList(_context.Genres, "Id", "Name", selectedGenreIds);
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
                .Include(b => b.BookAuthors)
                .Include(b => b.BookGenres)
                .Include(b => b.BookLoans)
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
            // Удаляем связи many-to-many и BookLoans перед удалением книги
            var bookAuthors = _context.BookAuthors.Where(ba => ba.BookId == id);
            _context.BookAuthors.RemoveRange(bookAuthors);

            var bookGenres = _context.BookGenres.Where(bg => bg.BookId == id);
            _context.BookGenres.RemoveRange(bookGenres);

            var bookLoans = _context.BookLoans.Where(bl => bl.BookId == id); // Предполагаю BookId в BookLoan
            _context.BookLoans.RemoveRange(bookLoans);

            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.IdBook == id);
        }
    }
}