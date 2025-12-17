using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library2.Controllers
{
    // Доступен только пользователям с ролью "Librarian"
    [Authorize(Roles = "Librarian")]
    public class LibrarianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LibrarianController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Librarian/Index (Теперь это навигационная страница)
        [HttpGet]
        public IActionResult Index()
        {
            // Просто возвращаем представление для навигационной панели
            return View();
        }

        // GET: Librarian/BookList 
        public async Task<IActionResult> BookList(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            IQueryable<Book> books = _context.Books;

            // Включаем все связанные данные для полного отображения в таблице управления
            books = books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                // Фильтруем активные выдачи: те, которые не возвращены (ReturnDate == null).
                // Внимание: В BookLoan нет IsReserved. Все незавершенные считаются активными/бронями.
                .Include(b => b.BookLoans.Where(bl => bl.ReturnDate == null));

            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();

                books = books.Where(b =>
                    b.Title.ToLower().Contains(lowerSearch) ||
                    (b.Annotation != null && b.Annotation.ToLower().Contains(lowerSearch)) ||
                    b.BookAuthors.Any(ba =>
                        ba.Author.FirstName.ToLower().Contains(lowerSearch) ||
                        ba.Author.LastName.ToLower().Contains(lowerSearch)
                    )
                );
            }

            // Используем представление LibrarianList.cshtml
            return View(await books.OrderBy(b => b.Title).ToListAsync());
        }

        // GET: Librarian/Details/5 (Просмотр подробностей книги)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            // Включаем Reader через BookLoan.Reader, так как BookLoan.User не существует
            var book = await _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .Include(b => b.BookLoans)
                    .ThenInclude(bl => bl.Reader) // Используем Reader
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // GET: Librarian/Create
        public async Task<IActionResult> Create()
        {
            // Загрузка списков для DropDown/MultiSelect
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

        // POST: Librarian/Create
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

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(book);
                    await _context.SaveChangesAsync();

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

            // Перезагрузка ViewBag при ошибке
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

        // GET: Librarian/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null) return NotFound();

            // Загрузка списков и выделение текущих значений
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

        // POST: Librarian/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, List<int> selectedAuthors, List<int> selectedGenres)
        {
            if (id != book.IdBook) return NotFound();

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
                    // Обновляем саму книгу
                    _context.Update(book);

                    // Управление связями Авторов
                    var oldAuthors = _context.BookAuthors.Where(ba => ba.BookId == book.IdBook);
                    _context.BookAuthors.RemoveRange(oldAuthors);

                    foreach (var authorId in selectedAuthors)
                    {
                        _context.BookAuthors.Add(new BookAuthor { BookId = book.IdBook, AuthorId = authorId });
                    }

                    // Управление связями Жанров
                    var oldGenres = _context.BookGenres.Where(bg => bg.BookId == book.IdBook);
                    _context.BookGenres.RemoveRange(oldGenres);

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

            // Перезагрузка ViewBag при ошибке
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


        // GET: Librarian/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                .FirstOrDefaultAsync(m => m.IdBook == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Librarian/Delete/5
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

        // POST: Librarian/CreatePublisherAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePublisherAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Имя издателя не может быть пустым." });
            }

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
                return Json(new { success = false, message = "Ошибка при сохранении издателя: " + ex.Message });
            }
        }


        // POST: Librarian/CreateAuthorAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
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


        // POST: Librarian/CreateGenreAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGenreAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Название жанра не может быть пустым." });
            }

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