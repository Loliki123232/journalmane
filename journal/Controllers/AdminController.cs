using journal.Models;
using Microsoft.AspNetCore.Mvc;

namespace journal.Controllers
{
    public class AdminController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> AddGroup(AddGroupFormModel formModel)
        {
            Console.WriteLine($"=== НАЧАЛО ОБРАБОТКИ ФОРМЫ ГРУППЫ ===");
            Console.WriteLine($"Метод AddGroup вызван");

            // Логируем все данные формы для отладки
            if (HttpContext.Request.HasFormContentType)
            {
                Console.WriteLine("Данные формы:");
                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    Console.WriteLine($"  {key} = {HttpContext.Request.Form[key]}");
                }
            }

            Console.WriteLine($"Получены данные модели: Name='{formModel?.Name}', Course={formModel?.Course}, Specialty='{formModel?.Specialty}'");

            // Проверяем, что модель не null
            if (formModel == null)
            {
                Console.WriteLine("ОШИБКА: formModel is NULL");
                TempData["Error"] = "Данные формы не получены";
                return RedirectToAction("AdminBoard", "Home");
            }

            // Валидация только текущей формы
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Ошибки валидации для формы группы:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме группы";
                return RedirectToAction("AdminBoard", "Home");
            }

            Console.WriteLine("Данные прошли валидацию, создаем группу...");

            var group = new Group
            {
                Name = formModel.Name,
                Course = formModel.Course,
                Specialty = formModel.Specialty,
                StudentCount = 0
            };

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    Console.WriteLine("Открываем соединение с БД...");
                    dbManager.OpenConnection();

                    Console.WriteLine("Вызываем AddGroupAsync...");
                    var success = await dbManager.AddGroupAsync(group);

                    if (success)
                    {
                        Console.WriteLine($"Группа '{group.Name}' успешно создана!");
                        TempData["Success"] = $"Группа '{group.Name}' успешно создана!";
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при создании группы в БД");
                        TempData["Error"] = "Ошибка при создании группы в базе данных";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Исключение при работе с БД: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    TempData["Error"] = $"Ошибка базы данных: {ex.Message}";
                }
            }

            Console.WriteLine("=== КОНЕЦ ОБРАБОТКИ ФОРМЫ ГРУППЫ ===");
            return RedirectToAction("AdminBoard", "Home");
        }


        [HttpPost]
        public async Task<IActionResult> AddStudent(Student student)
        {
            // Здесь будет логика добавления студента
            using (var dbManager = new DataBaseManager())
            {
                dbManager.OpenConnection();
                // Добавьте метод AddStudentAsync в DataBaseManager
                // var success = await dbManager.AddStudentAsync(student);
            }

            TempData["Success"] = "Студент успешно добавлен!";
            return RedirectToAction("AdminBoard", "Home");
        }
    }
}