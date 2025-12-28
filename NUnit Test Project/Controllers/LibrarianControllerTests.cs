using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    public class LibrarianControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private LibrarianController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new LibrarianController(_context);

            // Инициализируем ViewData для предотвращения NullReferenceException
            var details = new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider();
            _controller.ViewData = new ViewDataDictionary(details, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        }

        [TearDown]
        public void TearDown() => Dispose();

        public void Dispose()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        private void MockUser(string login)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, login) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Test]
        public async Task Index_SetsChartData_BasedOnPopularity()
        {
            // Arrange
            var book = new Book { IdBook = 1, Title = "Popular Book", PublisherId = 1 };
            _context.Books.Add(book);
            _context.BookLoans.AddRange(new List<BookLoan> {
                new BookLoan { BookId = 1, LoanDate = DateTime.Now.AddDays(-2) },
                new BookLoan { BookId = 1, LoanDate = DateTime.Now.AddDays(-3) }
            });
            await _context.SaveChangesAsync();

            // Act
            await _controller.Index();

            // Assert
            var labels = _controller.ViewBag.ChartLabels as List<string>;
            var values = _controller.ViewBag.ChartValues as List<int>;
            Assert.That(labels, Contains.Item("Popular Book"));
            Assert.That(values, Contains.Item(2));
        }

        [Test]
        public async Task IssueReservation_CreatesLoan_AndUpdatesStatus()
        {
            // Arrange
            string testLogin = "admin_user";
            MockUser(testLogin);

            var httpContext = _controller.ControllerContext.HttpContext;
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            var employee = new Employee
            {
                IdEmployee = 10,
                Login = testLogin,
                FirstName = "A",
                LastName = "B",
                MiddleName = "C",
                PasswordHash = "h",
                Phone = "1",
                Position = "L"
            };
            _context.Employees.Add(employee);

            var book = new Book { IdBook = 5, Title = "C# Guide", PublisherId = 1 };
            var reader = new Reader { IdReader = 1, LastName = "Smith", FirstName = "J", MiddleName = "K", Phone = "0", Login = "r1", PasswordHash = "p" };

            _context.Books.Add(book);
            _context.Readers.Add(reader);

            var reservation = new Reservation
            {
                IdReservation = 1,
                BookId = 5,
                ReaderId = 1,
                Status = "Ready",
                Book = book 
            };
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.IssueReservation(1);

            // Assert
            var loan = await _context.BookLoans.FirstOrDefaultAsync(l => l.BookId == 5);
            Assert.That(loan, Is.Not.Null);
            Assert.That(loan.EmployeeId, Is.EqualTo(10));
            Assert.That(reservation.Status, Is.EqualTo("Issued"));
        }

        [Test]
        public async Task BookList_FiltersResults()
        {
            // Arrange
            _context.Books.RemoveRange(_context.Books);
            _context.Authors.RemoveRange(_context.Authors);
            await _context.SaveChangesAsync();

            var author = new Author { IdAuthor = 1, FirstName = "John", LastName = "Doe" };
            var publisher = new Publisher { IdPublisher = 1, Name = "Test Pub" };
            _context.Authors.Add(author);
            _context.Publishers.Add(publisher);

            var book = new Book
            {
                IdBook = 1,
                Title = "Alpha Book",
                PublisherId = 1,
                Publisher = publisher
            };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            _context.BookAuthors.Add(new BookAuthor { BookId = 1, AuthorId = 1, Author = author });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.BookList("alpha") as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Book>;
            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].Title, Is.EqualTo("Alpha Book"));
        }

        [Test]
        public async Task Create_Ajax_Genre_ReturnsJsonSuccess()
        {
            // Act
            var result = await _controller.CreateGenreAjax("Science Fiction");

            // Assert
            var jsonResult = result as JsonResult;
            Assert.That(jsonResult, Is.Not.Null);

            // Исправление: Рефлексия вместо dynamic
            var data = jsonResult.Value;
            var successValue = (bool)data.GetType().GetProperty("success").GetValue(data);

            Assert.That(successValue, Is.True);
            Assert.That(_context.Genres.Any(g => g.Name == "Science Fiction"), Is.True);
        }

        [Test]
        public async Task Reservations_TriggersCleanup_ForExpiredItems()
        {
            // Arrange
            var expiredRes = new Reservation
            {
                IdReservation = 1,
                Status = "Pending",
                ExpirationDate = DateTime.Now.AddDays(-1), // Просрочено
                BookId = 1, // Добавьте связи, если контроллер их использует
                ReaderId = 1
            };
            _context.Reservations.Add(expiredRes);
            await _context.SaveChangesAsync();

            // Act
            await _controller.Reservations();

            // Assert
            var updatedRes = await _context.Reservations.FindAsync(1);
            Assert.That(updatedRes.Status, Is.EqualTo("Cancelled"));
        }
    }
}