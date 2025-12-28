using Library2.Controllers;
using Library2.Data;
using Library2.Models;
using Library2.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Library2.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private ApplicationDbContext _context;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            // 1. Настройка БД
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // 2. Создание заглушек для системных сервисов MVC
            var authServiceMock = new Mock<IAuthenticationService>();
            var tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();

            // Настройка возврата задачи для аутентификации
            authServiceMock
                .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // 3. Настройка ServiceProvider для разрешения системных зависимостей
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(authServiceMock.Object);
            serviceProviderMock.Setup(s => s.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataFactoryMock.Object);
            serviceProviderMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);

            // 4. Инициализация контроллера
            _controller = new AccountController(_context);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
            };

            // 5. Важно: напрямую создаем TempData, чтобы View() не выдавал ошибку
            _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());
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

        [Test]
        public async Task Register_Post_ValidModel_RedirectsToReaderIndex()
        {
            var model = new RegisterViewModel
            {
                Login = "testuser",
                Password = "Password123",
                FirstName = "Иван",
                LastName = "Иванов",
                MiddleName = "Иванович",
                BirthDate = DateTime.Now.AddYears(-20),
                Phone = "123456789"
            };

            var result = await _controller.Register(model);

            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("Reader"));
        }

        [Test]
        public async Task Register_Post_LoginExists_ReturnsViewWithError()
        {
            _context.Readers.Add(new Reader
            {
                Login = "exists",
                PasswordHash = "hash",
                FirstName = "A",
                LastName = "B",
                MiddleName = "C",
                Phone = "0",
                Role = "Reader"
            });
            await _context.SaveChangesAsync();

            var model = new RegisterViewModel { Login = "exists", Password = "123" };

            var result = await _controller.Register(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(_controller.ModelState.ContainsKey("Login"), Is.True);
        }

        [Test]
        public async Task Login_Post_InvalidCredentials_ReturnsViewWithError()
        {
            var model = new LoginViewModel { Login = "none", Password = "wrong" };

            var result = await _controller.Login(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(_controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void Login_Get_ReturnsView()
        {
            var result = _controller.Login();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
    }
}