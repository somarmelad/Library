using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework; 
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    internal class BookLoansControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private BookLoansController _controller;

        [SetUp] // Аналог @BeforeEach в JUnit
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new BookLoansController(_context);
        }

        [TearDown] // Аналог @AfterEach в JUnit
        public void TearDown()
        {
            Dispose();
        }

        public void Dispose()
        {
            _controller?.Dispose();
            _context?.Dispose();

            _controller = null;
            _context = null;
        }

        [Test] // Аналог @Test в JUnit для метода
        public async Task Create_Post_WhenBookNotAvailable_ReturnsViewWithError()
        {
            // Arrange
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var book = new Book
            {
                IdBook = 10,
                Title = "Чистый код",
                Status = BookStatus.Issued 
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var newLoan = new BookLoan
            {
                BookId = 10,
                ReaderId = 1,
                EmployeeId = 1,
                LoanDate = DateTime.Now
            };

            // Act
            var result = await _controller.Create(newLoan);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());

            var hasError = _controller.ModelState.ContainsKey("BookId");
            Assert.That(hasError, Is.True, "Ошибка для BookId не найдена в ModelState");

            var errorMessage = _controller.ModelState["BookId"].Errors[0].ErrorMessage;
            Assert.That(errorMessage, Is.EqualTo("Этот экземпляр уже выдан."));
        }

        [Test]
        public async Task Details_WhenIdNotFound_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteConfirmed_RemovesRecordSuccessfully()
        {
            // Arrange
            var loan = new BookLoan { IdBookLoan = 5, BookId = 1, LoanDate = DateTime.Now };
            _context.BookLoans.Add(loan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteConfirmed(5);

            // Assert
            var deletedLoan = await _context.BookLoans.FindAsync(5);
            Assert.That(deletedLoan, Is.Null);
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
    }
}
