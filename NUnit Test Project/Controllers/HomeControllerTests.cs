using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Нужно для ViewData
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using static NUnit.Framework.Constraints.Tolerance;

namespace NUnit_Test_Project.Controllers
{
    [TestFixture]
    internal class HomeControllerTests : IDisposable
    {
        private ApplicationDbContext _context;
        private Mock<ILogger<HomeController>> _loggerMock;
        private HomeController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<HomeController>>();

            // ИСПРАВЛЕНО: Убрана строка _controller = null;
            _controller = new HomeController(_context, _loggerMock.Object);

            // Инициализируем ViewData, чтобы работал ViewBag
            var modelMetadataProvider = new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider();
            _controller.ViewData = new ViewDataDictionary(modelMetadataProvider, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
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

            _controller = null;
            _context = null;
        }

        // Вспомогательный метод для имитации авторизованного пользователя
        private void MockUser(string role = null, bool isAuthenticated = true)
        {
            var claims = new List<Claim>();
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Test]
        public void Index_WhenNotAuthenticated_ReturnsView()
        {
            // Arrange
            MockUser(isAuthenticated: false);

            // Act
            var result = _controller.Index();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public void Index_WhenLibrarian_RedirectsToLibrarianIndex()
        {
            // Arrange
            MockUser(role: "Librarian");

            // Act
            var result = _controller.Index();

            // Assert
            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null, "Должен быть редирект");
            Assert.That(redirect.ControllerName, Is.EqualTo("Librarian"));
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public void Index_WhenReader_RedirectsToReaderIndex()
        {
            // Arrange
            MockUser(role: "Reader");

            // Act
            var result = _controller.Index();

            // Assert
            var redirect = result as RedirectToActionResult;
            Assert.That(redirect, Is.Not.Null, "Должен быть редирект");
            Assert.That(redirect.ControllerName, Is.EqualTo("Reader"));
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task DbStatusCheck_ReturnsCorrectCounts_InViewBag()
        {
           // Arrange
    MockUser(role: "Librarian");

            _context.Books.Add(new Book
            {
                IdBook = 1,
                Title = "Test Book",
                PublisherId = 1
            });

            _context.Readers.Add(new Reader
            {
                IdReader = 1,
                Login = "test_login",           
                PasswordHash = "hashed_pass",   
                FirstName = "Ivan",
                LastName = "Ivanov",
                MiddleName = "Ivanovich",
                Phone = "123456789",
                Role = "Reader"                 
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DbStatusCheck();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ViewBag.BooksCount, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.ReadersCount, Is.EqualTo(1));
            Assert.That(_controller.ViewBag.DbStatus, Does.Contain("успешно"));
        }

        [Test]
        public void Test_ReturnsContentString()
        {
            // Act
            var result = _controller.Test();

            // Assert
            var contentResult = result as ContentResult;
            Assert.That(contentResult, Is.Not.Null);
            Assert.That(contentResult.Content, Is.EqualTo("HomeController работает!"));
        }
    }
}