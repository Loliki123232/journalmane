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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User user)
        {
            if (!ModelState.IsValid)
                return View(user);


            string hashedPassword = _passwordHasher.HashPassword(user.password);

            using (var dbManager = new DataBaseManager())
            {
                dbManager.OpenConnection();
                bool isAuthenticated = await dbManager.AuthenticateUserAsync(user.login, hashedPassword);

                if (isAuthenticated)
                {
                    return RedirectToAction("Dashboard");
                }

                ModelState.AddModelError("", "Неверный логин или пароль");
                return View(user);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}