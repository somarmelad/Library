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
using System.Security.Claims;
using System.Threading.Tasks;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    public class ReservationsControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private ReservationsController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
        .Options;

            _context = new ApplicationDbContext(options);
            _controller = new ReservationsController(_context);

            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
        }

        private void MockUser(string id, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Важно привязать TempData к новому HttpContext
            var tempDataProvider = new Mock<ITempDataProvider>();
            _controller.TempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, tempDataProvider.Object);
        }

        // Вспомогательный метод для создания валидного читателя
        private Reader CreateValidReader(int id)
        {
            return new Reader
            {
                IdReader = id,
                FirstName = "Test",
                LastName = "User",
                MiddleName = "Testovich",
                Login = $"user{id}",
                PasswordHash = "hash",
                Phone = "12345"
            };
        }

        [Test]
        public async Task Reserve_Fails_WhenBookIsOutOfStock()
        {// Arrange
            MockUser("1", "Reader");
            var book = new Book { IdBook = 1, Title = "No Stock Book", Status = BookStatus.Issued };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Reserve(1) as RedirectToActionResult;

            // Assert
            Assert.That(_controller.TempData["Error"].ToString(), Does.Contain("занята"));
        }

        [Test]
        public async Task Reserve_Fails_WhenAlreadyReservedBySameReader()
        {
            // Arrange
            MockUser("1", "Reader");
            var book = new Book { IdBook = 1, Title = "No Stock Book", Status = BookStatus.Issued };
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Reserve(1) as RedirectToActionResult;

            // Assert
            Assert.That(_controller.TempData["Error"].ToString(), Does.Contain("занята"));
        }

        [Test]
        public async Task IssueBook_CreatesLoan_AndCompletesReservation()
        {
            // Arrange
            MockUser("99", "Librarian"); 
            var reader = CreateValidReader(5);
            _context.Readers.Add(reader);
            _context.Books.Add(new Book { IdBook = 10, Title = "Issue Test", Status = BookStatus.Available });

            var res = new Reservation { IdReservation = 1, BookId = 10, ReaderId = 5, Status = "Pending" };
            _context.Reservations.Add(res);
            await _context.SaveChangesAsync();

            // Act
            await _controller.IssueBook(1);

            // Assert
            var reservation = await _context.Reservations.FindAsync(1);
            var loan = await _context.BookLoans.FirstOrDefaultAsync(l => l.ReaderId == 5);

            Assert.That(reservation.Status, Is.EqualTo("Issued"));
            Assert.That(loan, Is.Not.Null);
            Assert.That(loan.EmployeeId, Is.EqualTo(99));
        }

        [Test]
        public async Task Cancel_ChangesStatus_ToCanceled()
        {
            /// Arrange
            MockUser("1", "Reader");

            _context.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                Status = "Pending",
                BookId = 1,
                ReaderId = 1 
            });
            await _context.SaveChangesAsync();

            // Act
            await _controller.CancelReservation(1);

            // Assert
            var res = await _context.Reservations.FindAsync(1);

            Assert.That(res.Status, Is.EqualTo("Cancelled"));
        }

        [TearDown]
        public void TearDown() => Dispose();

        public void Dispose()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }
    }
}