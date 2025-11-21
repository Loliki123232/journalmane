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
            Console.WriteLine($"Ïîïûòêà âõîäà ñòóäåíòà: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateStudentAsync(user.login, user.password);

                    Console.WriteLine($"Ðåçóëüòàò ñòóäåíòà: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        // Ñîõðàíÿåì ëîãèí ñòóäåíòà â ñåññèè
                        HttpContext.Session.SetString("StudentLogin", user.login);
                        return RedirectToAction("StudentDashboard");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Îøèáêà ÁÄ ïðè âõîäå ñòóäåíòà: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Íåâåðíûé ëîãèí èëè ïàðîëü");
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
                        HttpContext.Session.SetString("TeacherLogin", user.login);
                        // ВАЖНО: Редирект на TeacherController
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
            Console.WriteLine($"Ïîïûòêà âõîäà àäìèíèñòðàòîðà: {user.login}");

            if (!ModelState.IsValid)
                return View("Login", user);

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    bool isAuthenticated = await dbManager.AuthenticateAdminAsync(user.login, user.password);

                    Console.WriteLine($"Ðåçóëüòàò àäìèíèñòðàòîðà: {isAuthenticated}");

                    if (isAuthenticated)
                    {
                        return RedirectToAction("AdminBoard");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Îøèáêà ÁÄ ïðè âõîäå àäìèíèñòðàòîðà: {ex.Message}");
                }
            }

            ModelState.AddModelError("", "Íåâåðíûé ëîãèí èëè ïàðîëü");
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

                // Íàõîäèì ñòóäåíòà ïî ëîãèíó
                var students = await dbManager.GetStudentsAsync();
                var student = students.FirstOrDefault(s => s.Login == studentLogin);

                if (student == null)
                {
                    return RedirectToAction("Login");
                }

                // Ïîëó÷àåì äàííûå äëÿ ñòóäåíòà
                var model = new StudentDashboardViewModel
                {
                    Student = student,
                    Assignments = await dbManager.GetAssignmentsByGroupAsync(student.GroupId),
                    Grades = await dbManager.GetGradesByStudentAsync(student.Id)
                    // Äîáàâüòå äðóãèå äàííûå ïî íåîáõîäèìîñòè
                };

                return View(model);
            }
        }
        public async Task<IActionResult> AdminBoard()
        {
            try
            {
                // Ñîçäàåì ìîäåëü è çàãðóæàåì äàííûå èç ÁÄ
                var model = new AdminDashboardViewModel();

                using (var dbManager = new DataBaseManager())
                {
                    dbManager.OpenConnection();

                    // Çàãðóæàåì âñå äàííûå èç áàçû
                    model.Students = await dbManager.GetStudentsAsync();
                    model.Teachers = await dbManager.GetTeachersAsync();
                    model.Groups = await dbManager.GetGroupsAsync();
                    model.Schedules = await dbManager.GetScheduleAsync();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Îøèáêà ïðè çàãðóçêå äàííûõ äëÿ àäìèí-ïàíåëè: {ex.Message}");
                return View(new AdminDashboardViewModel());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public IActionResult GenerateAdminHash()
        {
            var password = "rute"; // ваш пароль

            // Используем ваш существующий PasswordHasherService
            var passwordHasher = new PasswordHasherService();
            var (hash, salt) = passwordHasher.HashPassword(password);

            Console.WriteLine("=== ДАННЫЕ ДЛЯ АДМИНИСТРАТОРА ===");
            Console.WriteLine($"Логин: admin");
            Console.WriteLine($"Пароль: {password}");
            Console.WriteLine($"Хеш: {hash}");
            Console.WriteLine($"Соль: {salt}");
            Console.WriteLine("=================================");

            // SQL для копирования
            var sql = $@"
    INSERT INTO [dbo].[AdminLogin] ([Login], [Password], [Salt])
    VALUES 
    (
        'admin',
        '{hash}',
        '{salt}'
    );";

            Console.WriteLine("SQL запрос:");
            Console.WriteLine(sql);

            return Content($"Логин: admin<br>Пароль: {password}<br>Хеш: {hash}<br>Соль: {salt}<br><br>SQL:<br>{sql}");
        }
    }
    //https://localhost:7091/Home/GenerateAdminHash
}