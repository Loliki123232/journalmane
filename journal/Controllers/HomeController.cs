using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace journal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PasswordHasherService _passwordHasher;
        DataBaseManager _dataBaseManager;

        public HomeController(ILogger<HomeController> logger, PasswordHasherService passwordHasher)
        {
            _logger = logger;
            _passwordHasher = passwordHasher;
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Student(User user)
        //{
        //    if (!ModelState.IsValid)
        //        return View(user);


        //    string hashedPassword = _passwordHasher.HashPassword(user.password);

        //    using (var dbManager = new DataBaseManager())
        //    {
        //        dbManager.OpenConnection();
        //        bool isAuthenticated = await dbManager.AuthenticateStudentAsync(user.login, hashedPassword);

        //        if (isAuthenticated)
        //        {
        //            return RedirectToAction("Dashboard");
        //        }

        //        ModelState.AddModelError("", "Неверный логин или пароль");
        //        return View(user);
        //    }
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Teacher(User user)
        //{
        //    if (!ModelState.IsValid)
        //        return View(user);


        //    string hashedPassword = _passwordHasher.HashPassword(user.password);

        //    using (var dbManager = new DataBaseManager())
        //    {
        //        dbManager.OpenConnection();
        //        bool isAuthenticated = await dbManager.AuthenticateTeacherAsync(user.login, hashedPassword);

        //        if (isAuthenticated)
        //        {
        //            return RedirectToAction("Dashboard");
        //        }

        //        ModelState.AddModelError("", "Неверный логин или пароль");
        //        return View(user);
        //    }
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Admin(User user)
        {
            Console.WriteLine($"Попытка входа: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            string password = user.password;

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateAdminAsync(user.login, password);

                    Console.WriteLine($"Результат: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        return RedirectToAction("AdminBoard");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка БД: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Неверный логин или пароль");
            return View("Login", user);
        }
        public async Task<IActionResult> AdminBoard()
        {
            try
            {
                // Создаем модель и загружаем данные из БД
                var model = new AdminDashboardViewModel();

                using (var dbManager = new DataBaseManager())
                {
                    dbManager.OpenConnection();

                    // Загружаем все данные из базы
                    model.Students = await dbManager.GetStudentsAsync();
                    model.Teachers = await dbManager.GetTeachersAsync();
                    model.Groups = await dbManager.GetGroupsAsync();
                    model.Schedules = await dbManager.GetScheduleAsync();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных для админ-панели: {ex.Message}");

                // Возвращаем пустую модель в случае ошибки
                return View(new AdminDashboardViewModel());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}