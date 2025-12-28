using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    internal class ReadersControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private ReadersController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new ReadersController(_context);
        }

        // Вспомогательный метод для создания валидного объекта Reader
        private Reader GetValidReader(int id, string lastName, string login = "testlogin")
        {
            return new Reader
            {
                IdReader = id,
                LastName = lastName,
                FirstName = "Ivan",       // Обязательное поле
                MiddleName = "Ivanovich", // Обязательное поле
                Login = login,            // Обязательное поле
                PasswordHash = "hash123", // Обязательное поле
                Phone = "123456789",      // Обязательное поле
                BookLoans = new List<BookLoan>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        public void Dispose()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public async Task Index_ReturnsViewWithAllReaders()
        {
            // Arrange
            _context.Readers.AddRange(new List<Reader> {
                GetValidReader(1, "Ivanov", "ivan"),
                GetValidReader(2, "Petrov", "petr")
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Index() as ViewResult;
            var model = result.Model as List<Reader>;

            // Assert
            Assert.That(model.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Create_Post_RedirectsToIndex_WhenValid()
        {
            // Arrange
            var newReader = GetValidReader(3, "Sidorov", "sid");

            // Act
            var result = await _controller.Create(newReader) as RedirectToActionResult;

            // Assert
            Assert.That(result.ActionName, Is.EqualTo("Index"));
            Assert.That(_context.Readers.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task Delete_Get_ShowsError_IfActiveLoansExist()
        {
            // Arrange
            var reader = GetValidReader(1, "Debtor");
            _context.Readers.Add(reader);
            _context.BookLoans.Add(new BookLoan { ReaderId = 1, ReturnDate = null });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(1) as ViewResult;

            // Assert
            var errorMessage = _controller.ViewData["ActiveLoansError"]?.ToString();
            Assert.That(errorMessage, Does.Contain("Невозможно удалить"));
        }

        [Test]
        public async Task DeleteConfirmed_RemovesReader_IfNoActiveLoans()
        {
            // Arrange
            var reader = GetValidReader(1, "Clean Record");
            _context.Readers.Add(reader);
            _context.BookLoans.Add(new BookLoan { ReaderId = 1, ReturnDate = DateTime.Now });
            await _context.SaveChangesAsync();

            // Act
            await _controller.DeleteConfirmed(1);

            // Assert
            Assert.That(_context.Readers.Any(r => r.IdReader == 1), Is.False);
            Assert.That(_context.BookLoans.Any(l => l.ReaderId == 1), Is.False);
        }

        [Test]
        public async Task Edit_Post_UpdatesReaderData()
        {
            // Arrange
            var reader = GetValidReader(1, "OldName");
            _context.Readers.Add(reader);
            await _context.SaveChangesAsync();
            _context.Entry(reader).State = EntityState.Detached;

            var updatedReader = GetValidReader(1, "NewName");

            // Act
            await _controller.Edit(1, updatedReader);

            // Assert
            var dbReader = await _context.Readers.FindAsync(1);
            Assert.That(dbReader.LastName, Is.EqualTo("NewName"));
        }
    }
}