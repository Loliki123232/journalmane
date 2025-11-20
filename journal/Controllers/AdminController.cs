using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

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
        public async Task<IActionResult> AddStudent(AddStudentFormModel formModel)
        {
            Console.WriteLine($"=== НАЧАЛО ОБРАБОТКИ ФОРМЫ СТУДЕНТА ===");

            // Логируем данные формы для отладки
            if (HttpContext.Request.HasFormContentType)
            {
                Console.WriteLine("Данные формы студента:");
                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    Console.WriteLine($"  {key} = {HttpContext.Request.Form[key]}");
                }
            }

            Console.WriteLine($"Получены данные модели: Login='{formModel?.Login}', FullName='{formModel?.FullName}', GroupId={formModel?.GroupId}");

            if (formModel == null)
            {
                Console.WriteLine("ОШИБКА: formModel is NULL");
                TempData["Error"] = "Данные формы не получены";
                return RedirectToAction("AdminBoard", "Home");
            }

            // Валидация только текущей формы
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Ошибки валидации для формы студента:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме студента";
                return RedirectToAction("AdminBoard", "Home");
            }

            Console.WriteLine("Данные прошли валидацию, создаем студента...");

            var student = new Student
            {
                Login = formModel.Login,
                Password = formModel.Password,
                FullName = formModel.FullName,
                GroupId = formModel.GroupId
            };

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    Console.WriteLine("Открываем соединение с БД...");
                    dbManager.OpenConnection();

                    Console.WriteLine("Вызываем AddStudentAsync...");
                    var success = await dbManager.AddStudentAsync(student);

                    if (success)
                    {
                        Console.WriteLine($"Студент '{student.FullName}' успешно добавлен!");
                        TempData["Success"] = $"Студент '{student.FullName}' успешно добавлен!";
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при добавлении студента в БД");
                        TempData["Error"] = "Ошибка при добавлении студента в базу данных";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении студента: {ex.Message}");
                    TempData["Error"] = $"Ошибка при добавлении студента: {ex.Message}";
                }
            }

            Console.WriteLine("=== КОНЕЦ ОБРАБОТКИ ФОРМЫ СТУДЕНТА ===");
            return RedirectToAction("AdminBoard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> AddTeacher(AddTeacherFormModel formModel)
        {
            Console.WriteLine($"=== НАЧАЛО ОБРАБОТКИ ФОРМЫ ПРЕПОДАВАТЕЛЯ ===");

            // Логируем данные формы для отладки
            if (HttpContext.Request.HasFormContentType)
            {
                Console.WriteLine("Данные формы преподавателя:");
                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    Console.WriteLine($"  {key} = {HttpContext.Request.Form[key]}");
                }
            }

            Console.WriteLine($"Получены данные модели: Login='{formModel?.Login}', FullName='{formModel?.FullName}', Subject='{formModel?.Subject}'");

            if (formModel == null)
            {
                Console.WriteLine("ОШИБКА: formModel is NULL");
                TempData["Error"] = "Данные формы не получены";
                return RedirectToAction("AdminBoard", "Home");
            }

            // Валидация только текущей формы
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Ошибки валидации для формы преподавателя:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме преподавателя";
                return RedirectToAction("AdminBoard", "Home");
            }

            Console.WriteLine("Данные прошли валидацию, создаем преподавателя...");

            var teacher = new Teacher
            {
                Login = formModel.Login,
                Password = formModel.Password,
                FullName = formModel.FullName,
                Subject = formModel.Subject
            };

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    Console.WriteLine("Открываем соединение с БД...");
                    dbManager.OpenConnection();

                    Console.WriteLine("Вызываем AddTeacherAsync...");
                    var success = await dbManager.AddTeacherAsync(teacher);

                    if (success)
                    {
                        Console.WriteLine($"Преподаватель '{teacher.FullName}' успешно добавлен!");
                        TempData["Success"] = $"Преподаватель '{teacher.FullName}' успешно добавлен!";
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при добавлении преподавателя в БД");
                        TempData["Error"] = "Ошибка при добавлении преподавателя в базу данных";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении преподавателя: {ex.Message}");
                    TempData["Error"] = $"Ошибка при добавлении преподавателя: {ex.Message}";
                }
            }

            Console.WriteLine("=== КОНЕЦ ОБРАБОТКИ ФОРМЫ ПРЕПОДАВАТЕЛЯ ===");
            return RedirectToAction("AdminBoard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> AddSchedule(AddScheduleFormModel formModel)
        {
            Console.WriteLine($"=== НАЧАЛО ОБРАБОТКИ ФОРМЫ РАСПИСАНИЯ ===");

            // Логируем данные формы для отладки
            if (HttpContext.Request.HasFormContentType)
            {
                Console.WriteLine("Данные формы расписания:");
                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    Console.WriteLine($"  {key} = {HttpContext.Request.Form[key]}");
                }
            }

            Console.WriteLine($"Получены данные модели: GroupId={formModel?.GroupId}, DayOfWeek='{formModel?.DayOfWeek}', Time='{formModel?.Time}', Subject='{formModel?.Subject}', TeacherId={formModel?.TeacherId}, Room='{formModel?.Room}'");

            if (formModel == null)
            {
                Console.WriteLine("ОШИБКА: formModel is NULL");
                TempData["Error"] = "Данные формы не получены";
                return RedirectToAction("AdminBoard", "Home");
            }

            // Валидация только текущей формы
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Ошибки валидации для формы расписания:");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                    }
                }
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме расписания";
                return RedirectToAction("AdminBoard", "Home");
            }

            Console.WriteLine("Данные прошли валидацию, создаем расписание...");

            var schedule = new Schedule
            {
                GroupId = formModel.GroupId,
                DayOfWeek = formModel.DayOfWeek,
                Time = formModel.Time,
                Subject = formModel.Subject,
                TeacherId = formModel.TeacherId,
                Room = formModel.Room
            };

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    Console.WriteLine("Открываем соединение с БД...");
                    dbManager.OpenConnection();

                    Console.WriteLine("Вызываем AddScheduleAsync...");
                    var success = await dbManager.AddScheduleAsync(schedule);

                    if (success)
                    {
                        Console.WriteLine($"Занятие по '{schedule.Subject}' успешно добавлено в расписание!");
                        TempData["Success"] = $"Занятие по '{schedule.Subject}' успешно добавлено в расписание!";
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при добавлении занятия в БД");
                        TempData["Error"] = "Ошибка при добавлении занятия в базу данных";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении занятия: {ex.Message}");
                    TempData["Error"] = $"Ошибка при добавлении занятия: {ex.Message}";
                }
            }

            Console.WriteLine("=== КОНЕЦ ОБРАБОТКИ ФОРМЫ РАСПИСАНИЯ ===");
            return RedirectToAction("AdminBoard", "Home");
        }

        // МЕТОДЫ УДАЛЕНИЯ
        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            Console.WriteLine($"=== УДАЛЕНИЕ СТУДЕНТА ===");
            Console.WriteLine($"Удаление студента с ID: {id}");

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    var success = await dbManager.DeleteStudentAsync(id);

                    if (success)
                    {
                        Console.WriteLine($"Студент с ID {id} успешно удален!");
                        TempData["Success"] = "Студент успешно удален!";
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при удалении студента с ID {id}");
                        TempData["Error"] = "Ошибка при удалении студента";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении студента: {ex.Message}");
                    TempData["Error"] = $"Ошибка при удалении студента: {ex.Message}";
                }
            }

            return RedirectToAction("AdminBoard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            Console.WriteLine($"=== УДАЛЕНИЕ ПРЕПОДАВАТЕЛЯ ===");
            Console.WriteLine($"Удаление преподавателя с ID: {id}");

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    var success = await dbManager.DeleteTeacherAsync(id);

                    if (success)
                    {
                        Console.WriteLine($"Преподаватель с ID {id} успешно удален!");
                        TempData["Success"] = "Преподаватель успешно удален!";
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при удалении преподавателя с ID {id}");
                        TempData["Error"] = "Ошибка при удалении преподавателя";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении преподавателя: {ex.Message}");
                    TempData["Error"] = $"Ошибка при удалении преподавателя: {ex.Message}";
                }
            }

            return RedirectToAction("AdminBoard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            Console.WriteLine($"=== УДАЛЕНИЕ ГРУППЫ ===");
            Console.WriteLine($"Удаление группы с ID: {id}");

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    var success = await dbManager.DeleteGroupAsync(id);

                    if (success)
                    {
                        Console.WriteLine($"Группа с ID {id} успешно удалена!");
                        TempData["Success"] = "Группа успешно удалена!";
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при удалении группы с ID {id}");
                        TempData["Error"] = "Ошибка при удалении группы";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении группы: {ex.Message}");
                    TempData["Error"] = $"Ошибка при удалении группы: {ex.Message}";
                }
            }

            return RedirectToAction("AdminBoard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            Console.WriteLine($"=== УДАЛЕНИЕ РАСПИСАНИЯ ===");
            Console.WriteLine($"Удаление занятия с ID: {id}");

            using (var dbManager = new DataBaseManager())
            {
                try
                {
                    dbManager.OpenConnection();
                    var success = await dbManager.DeleteScheduleAsync(id);

                    if (success)
                    {
                        Console.WriteLine($"Занятие с ID {id} успешно удалено!");
                        TempData["Success"] = "Занятие успешно удалено из расписания!";
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при удалении занятия с ID {id}");
                        TempData["Error"] = "Ошибка при удалении занятия";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при удалении занятия: {ex.Message}");
                    TempData["Error"] = $"Ошибка при удалении занятия: {ex.Message}";
                }
            }

            return RedirectToAction("AdminBoard", "Home");
        }
    }
}