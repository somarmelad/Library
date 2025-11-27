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
        public async Task<IActionResult> Create()
        {
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name");

            // SelectList для основного автора (AuthorId)
            var authorsList = await _context.Authors
                .Select(a => new {
                    a.IdAuthor,
                    FullName = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new SelectList(authorsList, "IdAuthor", "FullName");

            // MultiSelectList для жанров (many-to-many)
            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name");

            return View(new Book());
        }

        // POST: Books/Create - ИСПРАВЛЕННАЯ ВЕРСИЯ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdBook,Title,PublisherId,AuthorId,Quantity")] Book book,
            List<int> selectedGenres)
        {
            // 🔍 ДИАГНОСТИКА: Покажи что пришло
            Console.WriteLine($"Title: {book.Title}");
            Console.WriteLine($"PublisherId: {book.PublisherId}");
            Console.WriteLine($"AuthorId: {book.AuthorId}");
            Console.WriteLine($"Quantity: {book.Quantity}");
            Console.WriteLine($"Selected Genres: {string.Join(", ", selectedGenres ?? new List<int>())}");

            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ Добавляем книгу
                    _context.Add(book);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Книга сохранена с ID: {book.IdBook}");

                    // ✅ Добавляем жанры (many-to-many)
                    if (selectedGenres != null && selectedGenres.Any())
                    {
                        foreach (var genreId in selectedGenres)
                        {
                            _context.BookGenres.Add(new BookGenre
                            {
                                BookId = book.IdBook,
                                GenreId = genreId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 🔍 Логируем ошибку
                    Console.WriteLine($"Ошибка сохранения: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка сохранения: {ex.Message}");
                }
            }

            // ❌ Ошибка валидации - перезагружаем ViewBag
            Console.WriteLine("ModelState НЕ валиден:");
            foreach (var error in ModelState)
            {
                foreach (var err in error.Value.Errors)
                {
                    Console.WriteLine($"Поле '{error.Key}': {err.ErrorMessage}");
                }
            }

            await ReloadViewBags(book);
            return View(book);
        }
        private async Task ReloadViewBags(Book book)
        {
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            var authorsList = await _context.Authors
                .Select(a => new {
                    a.IdAuthor,
                    FullName = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new SelectList(authorsList, "IdAuthor", "FullName", book.AuthorId);

            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name");
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

            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", selectedAuthorIds.Select(s => s.ToString()).ToArray());

            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name", selectedGenreIds);

            // Поля для новых (пустые)
            ViewBag.NewPublisherName = "";
            ViewBag.NewAuthorFirstName = "";
            ViewBag.NewAuthorLastName = "";
            ViewBag.NewAuthorMiddleName = "";
            ViewBag.NewGenreName = "";
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, List<int> selectedAuthors, List<int> selectedGenres,
            string newPublisherName, string newAuthorFirstName, string newAuthorLastName, string newAuthorMiddleName, string newGenreName)
        {
            if (id != book.IdBook)
            {
                return NotFound();
            }

            // Обработка нового издателя (обязательно: если new, создать и установить ID; иначе из select)
            if (!string.IsNullOrWhiteSpace(newPublisherName))
            {
                var trimmedPublisher = newPublisherName.Trim();
                var existingPublisher = await _context.Publishers.FirstOrDefaultAsync(p => p.Name == trimmedPublisher);
                if (existingPublisher != null)
                {
                    book.PublisherId = existingPublisher.IdPublisher;
                }
                else
                {
                    var newPublisher = new Publisher { Name = trimmedPublisher };
                    _context.Publishers.Add(newPublisher);
                    await _context.SaveChangesAsync();
                    book.PublisherId = newPublisher.IdPublisher;
                }
            }

            // Обработка нового автора
            int? newAuthorId = null;
            if (!string.IsNullOrWhiteSpace(newAuthorFirstName) && !string.IsNullOrWhiteSpace(newAuthorLastName))
            {
                var trimmedFirst = newAuthorFirstName.Trim();
                var trimmedLast = newAuthorLastName.Trim();
                var trimmedMiddle = newAuthorMiddleName?.Trim() ?? "";

                var existingAuthor = await _context.Authors
                    .FirstOrDefaultAsync(a => a.FirstName == trimmedFirst && a.LastName == trimmedLast);
                if (existingAuthor != null)
                {
                    newAuthorId = existingAuthor.IdAuthor;
                }
                else
                {
                    var newAuthor = new Author
                    {
                        FirstName = trimmedFirst,
                        LastName = trimmedLast,
                        MiddleName = string.IsNullOrWhiteSpace(trimmedMiddle) ? null : trimmedMiddle
                    };
                    _context.Authors.Add(newAuthor);
                    await _context.SaveChangesAsync();
                    newAuthorId = newAuthor.IdAuthor;
                }
                selectedAuthors ??= new List<int>();
                if (!selectedAuthors.Contains((int)newAuthorId))
                {
                    selectedAuthors.Add((int)newAuthorId);
                }
            }

            // Обработка нового жанра
            int? newGenreId = null;
            if (!string.IsNullOrWhiteSpace(newGenreName))
            {
                var trimmedGenre = newGenreName.Trim();
                var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == trimmedGenre);
                if (existingGenre != null)
                {
                    newGenreId = existingGenre.IdGenre;
                }
                else
                {
                    var newGenre = new Genre { Name = trimmedGenre };
                    _context.Genres.Add(newGenre);
                    await _context.SaveChangesAsync();
                    newGenreId = newGenre.IdGenre;
                }
                selectedGenres ??= new List<int>();
                if (!selectedGenres.Contains((int)newGenreId))
                {
                    selectedGenres.Add((int)newGenreId);
                }
            }

            // Проверки обязательности для авторов и жанров (минимум один)
            if (selectedAuthors == null || !selectedAuthors.Any())
            {
                ModelState.AddModelError("selectedAuthors", "Необходимо выбрать или добавить хотя бы одного автора.");
            }
            if (selectedGenres == null || !selectedGenres.Any())
            {
                ModelState.AddModelError("selectedGenres", "Необходимо выбрать или добавить хотя бы один жанр.");
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

                    // Добавляем новые (включая новые)
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
            await ReloadViewBags(book.PublisherId, selectedAuthors, selectedGenres, newPublisherName, newAuthorFirstName, newAuthorLastName, newAuthorMiddleName, newGenreName);
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
            // Удаляем связи many-to-многим и BookLoans перед удалением книги
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

        // Вспомогательный метод для перезагрузки ViewBag (для ошибок)
        private async Task ReloadViewBags(int? selectedPublisherId = null, List<int>? selectedAuthors = null, List<int>? selectedGenres = null,
            string? newPublisherName = null, string? newAuthorFirstName = null, string? newAuthorLastName = null, string? newAuthorMiddleName = null, string? newGenreName = null)
        {
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", selectedPublisherId);

            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();
            var authorValues = selectedAuthors?.Select(s => s.ToString()).ToArray() ?? Array.Empty<string>();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", authorValues);

            var genreValues = selectedGenres ?? new List<int>();
            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name", genreValues);

            ViewBag.NewPublisherName = newPublisherName ?? "";
            ViewBag.NewAuthorFirstName = newAuthorFirstName ?? "";
            ViewBag.NewAuthorLastName = newAuthorLastName ?? "";
            ViewBag.NewAuthorMiddleName = newAuthorMiddleName ?? "";
            ViewBag.NewGenreName = newGenreName ?? "";
        }
    }
}