using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                .Include(b => b.BookLoans)
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
        public async Task<IActionResult> Create()
        {
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name");

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text");

            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name");

            return View(new Book());
        }


        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, List<int> selectedAuthors, List<int> selectedGenres)
        {
            
            if (selectedAuthors == null || selectedAuthors.Count == 0)
            {
                ModelState.AddModelError("selectedAuthors", "Выберите хотя бы одного автора.");
            }
            if (selectedGenres == null || selectedGenres.Count == 0)
            {
                ModelState.AddModelError("selectedGenres", "Выберите хотя бы один жанр.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState не валиден. Обнаружены следующие ошибки:");
                foreach (var entry in ModelState.Where(e => e.Value.Errors.Count > 0))
                {
                    Console.WriteLine($"Поле: {entry.Key}");
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"- Ошибка: {error.ErrorMessage}");
                    }
                }
            }


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(book);
                    await _context.SaveChangesAsync();

                    if (book.IdBook == 0)
                    {
                        throw new InvalidOperationException("Книга была добавлена, но IdBook не был сгенерирован.");
                    }

                    foreach (var authorId in selectedAuthors)
                    {
                        _context.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }

                    foreach (var genreId in selectedGenres)
                    {
                        _context.BookGenres.Add(new BookGenre { BookId = book.IdBook, GenreId = genreId });
                    }

                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    
                    Console.WriteLine("Ошибка сохранения БД: " + dbEx.InnerException?.Message ?? dbEx.Message);
                    ModelState.AddModelError("", "Ошибка сохранения книги в базу данных. Проверьте, существует ли выбранный Издатель.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Общая ошибка сохранения: " + ex.Message);
                    ModelState.AddModelError("", "Произошла непредвиденная ошибка при добавлении книги.");
                }
            }

            

            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", selectedAuthors);

            var genresList = await _context.Genres.ToListAsync();
            ViewBag.Genres = new MultiSelectList(genresList, "IdGenre", "Name", selectedGenres);

            return View(book);
        }


        // GET: Books/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();

            var currentAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", currentAuthorIds);

            var currentGenreIds = book.BookGenres.Select(bg => bg.GenreId).ToList();
            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name", currentGenreIds);

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

            if (selectedAuthors == null || selectedAuthors.Count == 0)
            {
                ModelState.AddModelError("selectedAuthors", "Выберите хотя бы одного автора.");
            }
            if (selectedGenres == null || selectedGenres.Count == 0)
            {
                ModelState.AddModelError("selectedGenres", "Выберите хотя бы один жанр.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);

                    var bookToUpdate = await _context.Books
                        .Include(b => b.BookAuthors)
                        .Include(b => b.BookGenres)
                        .FirstOrDefaultAsync(m => m.IdBook == book.IdBook);

                    if (bookToUpdate == null) return NotFound();
                    _context.BookAuthors.RemoveRange(bookToUpdate.BookAuthors);
                    foreach (var authorId in selectedAuthors)
                    {
                        bookToUpdate.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }

                    _context.BookGenres.RemoveRange(bookToUpdate.BookGenres);

                    foreach (var genreId in selectedGenres)
                    {
                        bookToUpdate.BookGenres.Add(new BookGenre { BookId = book.IdBook, GenreId = genreId });
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.IdBook == book.IdBook))
                    {
                        return NotFound();
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка сохранения: " + ex.Message);
                    
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", selectedAuthors);

            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name", selectedGenres);

            return View(book);
        }


        // GET: Books/Delete
        public async Task<IActionResult> Delete(int? id)
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

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


    }

}