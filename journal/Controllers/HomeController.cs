using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace journal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PasswordHasherService _passwordHasher;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Student(User user)
        {
            Console.WriteLine($"Попытка входа студента: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateStudentAsync(user.login, user.password);

                    Console.WriteLine($"Результат студента: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        // Сохраняем логин студента в сессии
                        HttpContext.Session.SetString("StudentLogin", user.login);
                        return RedirectToAction("StudentDashboard");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка БД при входе студента: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Неверный логин или пароль");
            return View("Login", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Teacher(User user)
        {
            Console.WriteLine($"Попытка входа преподавателя: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateTeacherAsync(user.login, user.password);

                    Console.WriteLine($"Результат преподавателя: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        // Сохраняем логин преподавателя в сессии
                        HttpContext.Session.SetString("TeacherLogin", user.login);
                        // ИЗМЕНИТЬ ЗДЕСЬ: перенаправляем на TeacherDashboard в TeacherController
                        return RedirectToAction("TeacherDashboard", "Teacher");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка БД при входе преподавателя: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Неверный логин или пароль");
            return View("Login", user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Admin(User user)
        {
            Console.WriteLine($"Попытка входа администратора: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateAdminAsync(user.login, user.password);

                    Console.WriteLine($"Результат администратора: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        return RedirectToAction("AdminBoard");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка БД при входе администратора: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Неверный логин или пароль");
            return View("Login", user);
        }

        public async Task<IActionResult> StudentDashboard()
        {
            var studentLogin = HttpContext.Session.GetString("StudentLogin");
            if (string.IsNullOrEmpty(studentLogin))
            {
                return RedirectToAction("Login");
            }

            using (var dbManager = new DataBaseManager())
            {
                dbManager.OpenConnection();

                // Находим студента по логину
                var students = await dbManager.GetStudentsAsync();
                var student = students.FirstOrDefault(s => s.Login == studentLogin);

                if (student == null)
                {
                    return RedirectToAction("Login");
                }

                // Получаем данные для студента
                var model = new StudentDashboardViewModel
                {
                    Student = student,
                    Assignments = await dbManager.GetAssignmentsByGroupAsync(student.GroupId),
                    Grades = await dbManager.GetGradesByStudentAsync(student.Id)
                    // Добавьте другие данные по необходимости
                };

                return View(model);
            }
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