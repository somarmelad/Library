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

        // GET: Books/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books
                .Include(b => b.Publisher)
                // Annotation загружается автоматически, как и Title, Quantity и т.д.
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

            // Annotation будет привязана к свойству book.Annotation автоматически

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

            // При ошибке возвращаем те же списки для ViewBag, сохраняя выбранные значения
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

            // Установка ViewBag для выпадающих списков
            ViewBag.Publishers = new SelectList(await _context.Publishers.ToListAsync(), "IdPublisher", "Name", book.PublisherId);

            // Установка ViewBag для авторов
            var authorsList = await _context.Authors
                .Select(a => new SelectListItem
                {
                    Value = a.IdAuthor.ToString(),
                    Text = $"{a.FirstName} {a.LastName}" + (!string.IsNullOrEmpty(a.MiddleName) ? $" {a.MiddleName}" : "")
                })
                .ToListAsync();

            var currentAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
            ViewBag.Authors = new MultiSelectList(authorsList, "Value", "Text", currentAuthorIds);

            // Установка ViewBag для жанров
            var currentGenreIds = book.BookGenres.Select(bg => bg.GenreId).ToList();
            ViewBag.Genres = new MultiSelectList(await _context.Genres.ToListAsync(), "IdGenre", "Name", currentGenreIds);

            return View(book);
        }

        // POST: Books/Edit
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
                // Загружаем существующую книгу с текущими связями.
                var bookToUpdate = await _context.Books
                    .Include(b => b.BookAuthors)
                    .Include(b => b.BookGenres)
                    .AsNoTracking() // Важно, чтобы избежать конфликтов отслеживания
                    .FirstOrDefaultAsync(m => m.IdBook == book.IdBook);

                if (bookToUpdate == null) return NotFound();

                try
                {
                    // 1. Обновляем основные свойства (включая Title, Quantity, PublisherId, и АННОТАЦИЮ)
                    _context.Update(book);

                    // 2. Управление авторами (Многий-ко-многим)
                    // Удаляем все старые связи
                    var oldAuthors = _context.BookAuthors.Where(ba => ba.BookId == book.IdBook);
                    _context.BookAuthors.RemoveRange(oldAuthors);

                    // Добавляем новые связи
                    foreach (var authorId in selectedAuthors)
                    {
                        _context.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }

                    // 3. Управление жанрами (Многий-ко-многим)
                    // Удаляем все старые связи
                    var oldGenres = _context.BookGenres.Where(bg => bg.BookId == book.IdBook);
                    _context.BookGenres.RemoveRange(oldGenres);

                    // Добавляем новые связи
                    foreach (var genreId in selectedGenres)
                    {
                        _context.BookGenres.Add(new BookGenre { BookId = book.IdBook, GenreId = genreId });
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
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
            }

            // При ошибке возвращаем те же списки для ViewBag, сохраняя выбранные значения
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

        // POST: Books/Delete
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


        // POST: Books/CreatePublisherAjax
        [HttpPost]
        [ValidateAntiForgeryToken] // Добавил [ValidateAntiForgeryToken] для безопасности
        public async Task<IActionResult> CreatePublisherAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Имя издателя не может быть пустым." });
            }

            // Проверка на существование
            if (await _context.Publishers.AnyAsync(p => p.Name.ToLower() == name.Trim().ToLower()))
            {
                return Json(new { success = false, message = $"Издательство '{name}' уже существует." });
            }

            var publisher = new Publisher { Name = name.Trim() };

            try
            {
                _context.Publishers.Add(publisher);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    id = publisher.IdPublisher,
                    name = publisher.Name
                });
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return Json(new { success = false, message = "Ошибка при сохранении издателя: " + ex.Message });
            }
        }


        // POST: Books/CreateAuthorAjax
        [HttpPost]
        [ValidateAntiForgeryToken] // Добавил [ValidateAntiForgeryToken] для безопасности
        public async Task<IActionResult> CreateAuthorAjax(string firstName, string lastName, string middleName)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return Json(new { success = false, message = "Имя и фамилия обязательны." });
            }

            string finalMiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();

            // Проверка на существование дубликата
            bool exists = await _context.Authors.AnyAsync(a =>
                a.FirstName.ToLower() == firstName.Trim().ToLower() &&
                a.LastName.ToLower() == lastName.Trim().ToLower() &&
                (finalMiddleName == null ? a.MiddleName == null : a.MiddleName.ToLower() == finalMiddleName.ToLower())
            );

            if (exists)
            {
                return Json(new { success = false, message = "Автор с таким именем уже существует." });
            }


            var author = new Author
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                MiddleName = finalMiddleName
            };

            try
            {
                _context.Authors.Add(author);
                await _context.SaveChangesAsync();

                // Предполагается, что в модели Author есть свойство FullName, которое возвращает ФИО
                // Если его нет, вам нужно будет создать эту строку здесь
                string fullName = $"{author.FirstName} {author.LastName}";
                if (!string.IsNullOrEmpty(author.MiddleName))
                {
                    fullName += $" {author.MiddleName}";
                }

                return Json(new
                {
                    success = true,
                    id = author.IdAuthor,
                    fullName = fullName
                });
            }
            catch (Exception ex)
            {
                string innerError = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine("Ошибка сохранения автора: " + innerError);

                return Json(new { success = false, message = "Ошибка при сохранении автора: " + innerError });
            }
        }


        // POST: Books/CreateGenreAjax
        [HttpPost]
        [ValidateAntiForgeryToken] // Добавил [ValidateAntiForgeryToken] для безопасности
        public async Task<IActionResult> CreateGenreAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Название жанра не может быть пустым." });
            }

            // Проверка на существование
            if (await _context.Genres.AnyAsync(g => g.Name.ToLower() == name.Trim().ToLower()))
            {
                return Json(new { success = false, message = $"Жанр '{name}' уже существует." });
            }

            var genre = new Genre { Name = name.Trim() };

            try
            {
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    id = genre.IdGenre,
                    name = genre.Name
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ошибка при сохранении жанра: " + ex.Message });
            }
        }
    }
}