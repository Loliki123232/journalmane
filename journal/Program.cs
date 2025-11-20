using journal.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавьте эту строку для настройки сессий
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Другие сервисы
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<PasswordHasherService>();

var app = builder.Build();

// Добавьте middleware для сессий ПЕРЕЛЕ UseRouting


app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();