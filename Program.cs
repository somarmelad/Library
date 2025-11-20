using Library2.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
namespace Library2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация БД
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(5, 7, 24)),  // Твоя версия из XAMPP
                    mySqlOptions => mySqlOptions
                        .EnableRetryOnFailure()  // Опционально: retry при ошибках
                ));

            var app = builder.Build();

            // Создание БД при запуске (только для разработки)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated(); // Создает БД и применяет миграции
            }

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (context.Database.CanConnect())
                {
                    Console.WriteLine("Подключение к MySQL успешно!");
                }
                else
                {
                    Console.WriteLine("Ошибка подключения.");
                }
            }

            app.Run();
        }
    }
}
