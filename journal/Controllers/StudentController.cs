using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace journal.Controllers
{
    public class StudentController : Controller
    {
        private readonly DataBaseManager _dbManager;
        private readonly IWebHostEnvironment _environment;

        public StudentController(IWebHostEnvironment environment)
        {
            _dbManager = new DataBaseManager();
            _environment = environment;
        }

        // Главная панель студента
        public async Task<IActionResult> StudentDashboard()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();

                var student = (await _dbManager.GetStudentsAsync()).FirstOrDefault(s => s.Id == studentId);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "Студент не найден";
                    return RedirectToAction("Login", "Home");
                }

                // Получаем задания для группы студента
                var allAssignments = await _dbManager.GetAssignmentsAsync();
                var assignments = allAssignments.Where(a => a.GroupId == student.GroupId).ToList();

                // Получаем отправленные работы студента
                var submissions = await _dbManager.GetSubmissionsByStudentAsync(studentId);
                Console.WriteLine($"Найдено отправок для студента {studentId}: {submissions.Count}");

                // Получаем оценки студента
                var grades = await _dbManager.GetGradesByStudentAsync(studentId);

                // Получаем посещаемость студента
                var attendance = await _dbManager.GetAttendanceByStudentAsync(studentId);

                var viewModel = new StudentDashboardViewModel
                {
                    Student = student,
                    Assignments = assignments,
                    Submissions = submissions,
                    Grades = grades,
                    Attendance = attendance
                };

                return View("~/Views/Home/StudentDashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке панели студента: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("Login", "Home");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // ОТПРАВКА РЕШЕНИЯ ЗАДАНИЯ
        [HttpPost]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, IFormFile solutionFile)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0)
                return RedirectToAction("Login", "Home");

            Console.WriteLine($"=== ОТПРАВКА РЕШЕНИЯ ===");
            Console.WriteLine($"StudentId: {studentId}");
            Console.WriteLine($"AssignmentId: {assignmentId}");
            Console.WriteLine($"File: {solutionFile?.FileName}");

            if (solutionFile == null || solutionFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Пожалуйста, выберите файл для отправки";
                return RedirectToAction("StudentDashboard");
            }

            // Проверяем расширение файла
            var allowedExtensions = new[] { ".pdf", ".txt", ".docx", ".doc" };
            var fileExtension = Path.GetExtension(solutionFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Разрешены только файлы PDF, TXT, DOCX, DOC";
                return RedirectToAction("StudentDashboard");
            }

            try
            {
                _dbManager.OpenConnection();
                Console.WriteLine("Открыто соединение с БД");

                // ПРОВЕРЯЕМ, ЕСТЬ ЛИ УЖЕ ОТПРАВКА ДЛЯ ЭТОГО ЗАДАНИЯ
                bool submissionExists = await _dbManager.SubmissionExistsAsync(assignmentId, studentId);
                if (submissionExists)
                {
                    TempData["ErrorMessage"] = "Вы уже отправили решение для этого задания";
                    return RedirectToAction("StudentDashboard");
                }

                // Сохранение файла на диск
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "submissions");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{solutionFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                Console.WriteLine($"Сохранение файла: {filePath}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await solutionFile.CopyToAsync(stream);
                }

                var submission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    FilePath = $"/submissions/{fileName}",
                    SubmittedAt = DateTime.Now,
                    Grade = null,
                    Feedback = null
                };

                var result = await _dbManager.AddSubmissionAsync(submission);
                Console.WriteLine($"Результат добавления в БД: {result}");

                if (result)
                {
                    TempData["SuccessMessage"] = "Решение успешно отправлено!";
                    Console.WriteLine("Решение отправлено успешно");
                }
                else
                {
                    TempData["ErrorMessage"] = "Ошибка при отправке решения.";
                    Console.WriteLine("Ошибка при отправке решения");

                    // Удаляем файл если запись в БД не удалась
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке решения: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
                Console.WriteLine("Закрыто соединение с БД");
            }

            return RedirectToAction("StudentDashboard");
        }

        // СКАЧИВАНИЕ ФАЙЛА ЗАДАНИЯ
        [HttpGet]
        public IActionResult DownloadAssignment(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }

        // СКАЧИВАНИЕ СОБСТВЕННОЙ РАБОТЫ
        [HttpGet]
        public IActionResult DownloadSubmission(int submissionId)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                    return RedirectToAction("Login", "Home");

                _dbManager.OpenConnection();
                var submissions = _dbManager.GetSubmissionsByStudentAsync(studentId).Result;
                var submission = submissions.FirstOrDefault(s => s.Id == submissionId);

                if (submission == null || string.IsNullOrEmpty(submission.FilePath))
                {
                    TempData["ErrorMessage"] = "Файл не найден";
                    return RedirectToAction("StudentDashboard");
                }

                var fullPath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["ErrorMessage"] = "Файл не найден на сервере";
                    return RedirectToAction("StudentDashboard");
                }

                var fileBytes = System.IO.File.ReadAllBytes(fullPath);
                var fileName = Path.GetFileName(fullPath);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при скачивании работы: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при скачивании файла";
                return RedirectToAction("StudentDashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }



        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private int GetCurrentStudentId()
        {
            Console.WriteLine("=== ПОЛУЧЕНИЕ ТЕКУЩЕГО STUDENT ID ===");

            // Получаем логин из сессии
            var studentLogin = HttpContext.Session.GetString("StudentLogin");
            Console.WriteLine($"StudentLogin from Session: {studentLogin}");

            if (string.IsNullOrEmpty(studentLogin))
            {
                Console.WriteLine("StudentLogin не найден в сессии");
                return 0;
            }

            // Находим ID студента по логину
            try
            {
                // ОТКРЫВАЕМ соединение здесь, так как это самостоятельный вызов
                _dbManager.OpenConnection();
                var students = _dbManager.GetStudentsAsync().Result;
                var student = students.FirstOrDefault(t => t.Login == studentLogin);

                if (student != null)
                {
                    Console.WriteLine($"Найден студент: ID={student.Id}, Name={student.FullName}");
                    return student.Id;
                }
                else
                {
                    Console.WriteLine("Студент не найден в БД");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске студента: {ex.Message}");
                return 0;
            }
            finally
            {
                // ЗАКРЫВАЕМ соединение после использования
                _dbManager.CloseConnection();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}