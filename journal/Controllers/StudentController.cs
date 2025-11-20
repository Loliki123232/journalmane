using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace journal.Controllers
{
    public class StudentController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, IFormFile solutionFile)
        {
            var studentLogin = HttpContext.Session.GetString("StudentLogin");
            if (string.IsNullOrEmpty(studentLogin))
            {
                return RedirectToAction("Login", "Home");
            }

            if (solutionFile == null || solutionFile.Length == 0)
            {
                TempData["Error"] = "Файл не выбран";
                return RedirectToAction("StudentDashboard", "Home");
            }

            // Проверяем расширение файла
            var allowedExtensions = new[] { ".pdf", ".txt" };
            var fileExtension = Path.GetExtension(solutionFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Разрешены только файлы PDF и TXT";
                return RedirectToAction("StudentDashboard", "Home");
            }

            // Сохраняем файл
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "solutions");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{assignmentId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await solutionFile.CopyToAsync(stream);
            }

            // Сохраняем в базу данных
            using (var dbManager = new DataBaseManager())
            {
                dbManager.OpenConnection();

                // Находим студента
                var students = await dbManager.GetStudentsAsync();
                var student = students.FirstOrDefault(s => s.Login == studentLogin);

                if (student == null)
                {
                    TempData["Error"] = "Студент не найден";
                    return RedirectToAction("StudentDashboard", "Home");
                }

                var submission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = student.Id,
                    FilePath = $"/uploads/solutions/{fileName}"
                };

                var success = await dbManager.AddSubmissionAsync(submission);
                if (success)
                {
                    TempData["Success"] = "Решение успешно отправлено";
                }
                else
                {
                    TempData["Error"] = "Ошибка при отправке решения";
                }
            }

            return RedirectToAction("StudentDashboard", "Home");
        }
    }
}