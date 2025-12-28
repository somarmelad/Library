using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    public class ReaderControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private ReaderController _controller;
        private const string TestReaderLogin = "reader@test.com";

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new ReaderController(_context);

            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, TestReaderLogin) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        private Reader CreateValidReader(int id, string login) => new Reader
        {
            IdReader = id,
            Login = login,
            FirstName = "T",
            LastName = "U",
            MiddleName = "M",
            PasswordHash = "h",
            Phone = "1"
        };

        private Publisher CreateValidPublisher() => new Publisher { IdPublisher = 1, Name = "Test Pub" };

        [Test]
        public async Task Index_FiltersBooks_ByTitle()
        {
            var pub = CreateValidPublisher();
            _context.Publishers.Add(pub);
            _context.Readers.Add(CreateValidReader(1, TestReaderLogin));

            _context.Books.AddRange(new List<Book> {
                new Book { IdBook = 101, Title = "War and Peace", PublisherId = 1, BookAuthors = new List<BookAuthor>() },
                new Book { IdBook = 102, Title = "1984", PublisherId = 1, BookAuthors = new List<BookAuthor>() }
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Index("1984") as ViewResult;

            Assert.That(result, Is.Not.Null);
            var model = result.Model as List<Book>;
            Assert.That(model.Count, Is.EqualTo(1));
            Assert.That(model[0].Title, Is.EqualTo("1984"));
        }

        [Test]
        public async Task Details_Calculates_CorrectAvailableCount()
        {
            // Arrange
            var pub = CreateValidPublisher();
            _context.Publishers.Add(pub);

            _context.Readers.Add(CreateValidReader(1, TestReaderLogin));

            string targetTitle = "Test Book";

            for (int i = 1; i <= 7; i++)
            {
                _context.Books.Add(new Book
                {
                    IdBook = i,
                    Title = targetTitle,
                    PublisherId = 1,
                    Status = BookStatus.Available,
                    BookAuthors = new List<BookAuthor>(),
                    BookLoans = new List<BookLoan>()
                });
            }

            _context.Books.Add(new Book
            {
                IdBook = 8,
                Title = targetTitle,
                PublisherId = 1,
                Status = BookStatus.Issued
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(1) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null, "Details вернул null. Проверьте IdBook.");

            Assert.That(result.ViewData["AvailableCount"], Is.EqualTo(7));
        }

        [Test]
        public async Task MyBooks_ShowsOnlyOwnData()
        {
            _context.Readers.AddRange(new List<Reader> {
                CreateValidReader(1, TestReaderLogin),
                CreateValidReader(2, "other@test.com")
            });

            var b1 = new Book { IdBook = 201, Title = "My Book",  BookAuthors = new List<BookAuthor>() };
            var b2 = new Book { IdBook = 202, Title = "Other Book", BookAuthors = new List<BookAuthor>() };
            _context.Books.AddRange(b1, b2);

            _context.BookLoans.AddRange(new List<BookLoan> {
                new BookLoan { IdBookLoan = 20, ReaderId = 1, BookId = 201, EmployeeId = 1, LoanDate = DateTime.Now },
                new BookLoan { IdBookLoan = 21, ReaderId = 2, BookId = 202, EmployeeId = 1, LoanDate = DateTime.Now }
            });
            await _context.SaveChangesAsync();

            var result = await _controller.MyBooks() as ViewResult;

            Assert.That(result, Is.Not.Null);
            var myLoans = result.ViewData["Loans"] as List<BookLoan>;
            Assert.That(myLoans.Count, Is.EqualTo(1));
            Assert.That(myLoans[0].Book.Title, Is.EqualTo("My Book"));
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
            _controller?.Dispose();
        }
    }
}