using Library2.Data;
using Microsoft.EntityFrameworkCore;
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
            // ДОБАВЬ ЭТО: Регистрация MVC (контроллеры + Views)
            builder.Services.AddControllersWithViews(); // Для MVC (включает Razor Pages опционально)

            // Опционально: Swagger для API-части, если нужно (но для чистого MVC не обязательно)
            // builder.Services.AddEndpointsApiExplorer();
            // builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Твоя часть: Создание/проверка БД (ок, но можно объединить в один scope для эффективности)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated(); // Создаёт БД и миграции

                if (context.Database.CanConnect())
                {
                    Console.WriteLine("Подключение к MySQL успешно!");
                }
                else
                {
                    Console.WriteLine("Ошибка подключения.");
                }
            }

            // ДОБАВЬ ЭТО: Middleware pipeline для MVC
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); // Детальные ошибки в браузере
                // Если Swagger:
                // app.UseSwagger();
                // app.UseSwaggerUI();
            }

            app.UseHttpsRedirection(); // HTTP -> HTTPS
            app.UseStaticFiles();      // Для CSS/JS/изображений в wwwroot
            app.UseRouting();          // Роутинг

            // Маппит MVC-роуты (default: / -> Home/Index)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"); // Стандартный роут для MVC

            // Опционально: Для Razor Pages, если используешь
            // app.MapRazorPages();

            app.Run();
        }
    }
}
