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
    public class TeacherController : Controller
    {
        private readonly DataBaseManager _dbManager;
        private readonly IWebHostEnvironment _environment;

        public TeacherController(IWebHostEnvironment environment)
        {
            _dbManager = new DataBaseManager();
            _environment = environment;
        }

        // Главная панель преподавателя
        public async Task<IActionResult> TeacherDashboard()
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();

                var teacher = (await _dbManager.GetTeachersAsync()).FirstOrDefault(t => t.Id == teacherId);
                var groups = await _dbManager.GetGroupsAsync();
                var students = await _dbManager.GetStudentsAsync();
                var assignments = await _dbManager.GetAssignmentsByTeacherAsync(teacherId);
                var submissions = await GetSubmissionsForTeacher(teacherId);
                var allSchedules = await _dbManager.GetScheduleAsync();
                var schedule = allSchedules.Where(s => s.TeacherId == teacherId).ToList();

                var viewModel = new TeacherDashboardViewModel
                {
                    Teacher = teacher,
                    Groups = groups,
                    Students = students,
                    Assignments = assignments,
                    Submissions = submissions,
                    Schedule = schedule
                };

                return View("~/Views/Home/TeacherDashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке панели преподавателя: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("Login", "Home");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // СОЗДАНИЕ ЗАДАНИЯ
        [HttpPost]
        public async Task<IActionResult> CreateAssignment(CreateAssignmentModel model)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            Console.WriteLine($"=== СОЗДАНИЕ ЗАДАНИЯ ===");
            Console.WriteLine($"TeacherId: {teacherId}");
            Console.WriteLine($"Title: {model.Title}");
            Console.WriteLine($"Subject: {model.Subject}");
            Console.WriteLine($"GroupId: {model.GroupId}");
            Console.WriteLine($"DueDate: {model.DueDate}");
            Console.WriteLine($"Description: {model.Description}");
            Console.WriteLine($"File: {model.AssignmentFile?.FileName}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"Ошибки валидации: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Пожалуйста, заполните все обязательные поля";
                return RedirectToAction("TeacherDashboard");
            }

            try
            {
                var assignment = new Assignment
                {
                    Title = model.Title,
                    Description = model.Description,
                    Subject = model.Subject,
                    TeacherId = teacherId,
                    GroupId = model.GroupId,
                    DueDate = model.DueDate,
                    CreatedAt = DateTime.Now
                };

                // Сохранение файла на диск
                if (model.AssignmentFile != null && model.AssignmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "assignments");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{Guid.NewGuid()}_{model.AssignmentFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    Console.WriteLine($"Сохранение файла: {filePath}");

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AssignmentFile.CopyToAsync(stream);
                    }

                    assignment.FilePath = $"/assignments/{fileName}";
                }

                _dbManager.OpenConnection();
                Console.WriteLine("Открыто соединение с БД");

                var result = await _dbManager.AddAssignmentAsync(assignment);
                Console.WriteLine($"Результат добавления в БД: {result}");

                if (result)
                {
                    TempData["SuccessMessage"] = "Задание успешно создано!";
                    Console.WriteLine("Задание создано успешно");
                }
                else
                {
                    TempData["ErrorMessage"] = "Ошибка при создании задания.";
                    Console.WriteLine("Ошибка при создании задания");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании задания: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
                Console.WriteLine("Закрыто соединение с БД");
            }

            return RedirectToAction("TeacherDashboard");
        }

        // ОТМЕТКА ПОСЕЩАЕМОСТИ
        [HttpPost]
        public async Task<IActionResult> MarkAttendance(AttendanceMarkModel model)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Пожалуйста, заполните все обязательные поля";
                return RedirectToAction("TeacherDashboard");
            }

            try
            {
                _dbManager.OpenConnection();

                // Получаем расписание для группы и предмета
                var schedules = await _dbManager.GetScheduleAsync();
                var schedule = schedules.FirstOrDefault(s => s.GroupId == model.GroupId && s.Subject == model.Subject);

                if (schedule == null)
                {
                    TempData["ErrorMessage"] = "Расписание для указанной группы и предмета не найдено.";
                    return RedirectToAction("TeacherDashboard");
                }

                int successCount = 0;
                for (int i = 0; i < model.StudentIds.Count; i++)
                {
                    var attendanceRecord = new AttendanceRecord
                    {
                        StudentId = model.StudentIds[i],
                        ScheduleId = schedule.Id,
                        Date = model.Date,
                        IsPresent = model.AttendanceStatus[i],
                        RecordedBy = teacherId
                    };

                    if (await _dbManager.AddAttendanceAsync(attendanceRecord))
                        successCount++;
                }

                TempData["SuccessMessage"] = $"Посещаемость отмечена для {successCount} студентов.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отметке посещаемости: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при отметке посещаемости: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
            }

            return RedirectToAction("TeacherDashboard");
        }

        // ВЫСТАВЛЕНИЕ ОЦЕНКИ
        [HttpPost]
        public async Task<IActionResult> AddGrade(Grade grade)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();

                grade.TeacherId = teacherId;
                grade.Date = DateTime.Now;

                var result = await _dbManager.AddGradeAsync(grade);

                if (result)
                    TempData["SuccessMessage"] = "Оценка успешно выставлена!";
                else
                    TempData["ErrorMessage"] = "Ошибка при выставлении оценки.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выставлении оценки: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
            }

            return RedirectToAction("TeacherDashboard");
        }

        // ОЦЕНКА РАБОТЫ СТУДЕНТА
        [HttpPost]
        public async Task<IActionResult> GradeSubmission(GradeSubmissionModel model)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            Console.WriteLine($"=== ОЦЕНКА РАБОТЫ ===");
            Console.WriteLine($"SubmissionId: {model.SubmissionId}");
            Console.WriteLine($"Grade: {model.Grade}");
            Console.WriteLine($"Feedback: {model.Feedback}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"Ошибки валидации: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Пожалуйста, укажите оценку";
                return RedirectToAction("TeacherDashboard");
            }

            try
            {
                _dbManager.OpenConnection();

                // Проверяем существование отправки
                var submission = await _dbManager.GetSubmissionByIdAsync(model.SubmissionId);
                if (submission == null)
                {
                    Console.WriteLine("Отправка не найдена");
                    TempData["ErrorMessage"] = "Работа не найдена";
                    return RedirectToAction("TeacherDashboard");
                }

                Console.WriteLine($"Найдена отправка: AssignmentId={submission.AssignmentId}, StudentId={submission.StudentId}");

                // Обновляем оценку
                var result = await _dbManager.UpdateSubmissionGradeAsync(model.SubmissionId, model.Grade);
                Console.WriteLine($"Результат обновления оценки: {result}");

                // Обновляем комментарий, если он есть
                if (result && !string.IsNullOrEmpty(model.Feedback))
                {
                    var feedbackResult = await _dbManager.UpdateSubmissionFeedbackAsync(model.SubmissionId, model.Feedback);
                    Console.WriteLine($"Результат обновления комментария: {feedbackResult}");
                }

                if (result)
                {
                    TempData["SuccessMessage"] = "Работа успешно оценена!";
                    Console.WriteLine("Работа оценена успешно");
                }
                else
                {
                    TempData["ErrorMessage"] = "Ошибка при оценке работы.";
                    Console.WriteLine("Ошибка при оценке работы");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при оценке работы: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
            }

            return RedirectToAction("TeacherDashboard");
        }

        // AJAX: ПОЛУЧЕНИЕ СТУДЕНТОВ ПО ГРУППЕ
        [HttpGet]
        public async Task<JsonResult> GetStudentsByGroup(int groupId)
        {
            try
            {
                _dbManager.OpenConnection();
                var students = await _dbManager.GetStudentsByGroupAsync(groupId);

                return Json(students.Select(s => new {
                    id = s.Id,
                    name = s.FullName,
                    groupName = s.GroupName
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении студентов: {ex.Message}");
                return Json(new { error = ex.Message });
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // AJAX: ПОЛУЧЕНИЕ ПОСЕЩАЕМОСТИ ЗА ДАТУ
        [HttpGet]
        public async Task<JsonResult> GetAttendanceForDate(int groupId, DateTime date, string subject)
        {
            try
            {
                _dbManager.OpenConnection();
                var attendance = await _dbManager.GetAttendanceByDateAndGroupAsync(groupId, date, subject);
                var students = await _dbManager.GetStudentsByGroupAsync(groupId);

                var result = students.Select(student => {
                    var existingRecord = attendance.FirstOrDefault(a => a.StudentId == student.Id);
                    return new
                    {
                        studentId = student.Id,
                        studentName = student.FullName,
                        isPresent = existingRecord?.IsPresent ?? true
                    };
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении посещаемости: {ex.Message}");
                return Json(new { error = ex.Message });
            }
            finally
            {
                _dbManager.CloseConnection();
            }
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

        // СКАЧИВАНИЕ РАБОТЫ СТУДЕНТА

        [HttpGet]
        public async Task<IActionResult> DownloadSubmission(int submissionId)
        {
            try
            {
                _dbManager.OpenConnection();

                // Получаем конкретную отправку по ID
                var submission = await _dbManager.GetSubmissionByIdAsync(submissionId);

                if (submission == null || string.IsNullOrEmpty(submission.FilePath))
                {
                    TempData["ErrorMessage"] = "Файл не найден";
                    return RedirectToAction("TeacherDashboard");
                }

                var fullPath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["ErrorMessage"] = "Файл не найден на сервере";
                    return RedirectToAction("TeacherDashboard");
                }

                var fileBytes = System.IO.File.ReadAllBytes(fullPath);
                var fileName = Path.GetFileName(fullPath);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при скачивании работы: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при скачивании файла";
                return RedirectToAction("TeacherDashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // ПРОСМОТР ДЕТАЛЕЙ ЗАДАНИЯ
        public async Task<IActionResult> AssignmentDetails(int assignmentId)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();
                var assignments = await _dbManager.GetAssignmentsByTeacherAsync(teacherId);
                var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    TempData["ErrorMessage"] = "Задание не найдено";
                    return RedirectToAction("TeacherDashboard");
                }

                var submissions = await _dbManager.GetSubmissionsByAssignmentAsync(assignmentId);

                ViewBag.Submissions = submissions;

                return View("~/Views/Home/AssignmentDetails.cshtml", assignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке деталей задания: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("TeacherDashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // ПРОСМОТР ОЦЕНОК СТУДЕНТА
        public async Task<IActionResult> StudentGrades(int studentId)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();
                var grades = await _dbManager.GetGradesByStudentAsync(studentId);
                var student = (await _dbManager.GetStudentsAsync()).FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Студент не найден";
                    return RedirectToAction("TeacherDashboard");
                }

                ViewBag.Student = student;
                return View(grades);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке оценок: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("TeacherDashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // УДАЛЕНИЕ ЗАДАНИЯ
        [HttpPost]
        public async Task<IActionResult> DeleteAssignment(int assignmentId)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();
                var result = await _dbManager.DeleteAssignmentAsync(assignmentId, teacherId);

                if (result)
                    TempData["SuccessMessage"] = "Задание успешно удалено!";
                else
                    TempData["ErrorMessage"] = "Ошибка при удалении задания или задание не найдено.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении задания: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при удалении задания: {ex.Message}";
            }
            finally
            {
                _dbManager.CloseConnection();
            }

            return RedirectToAction("TeacherDashboard");
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private int GetCurrentTeacherId()
        {
            Console.WriteLine("=== ПОЛУЧЕНИЕ ТЕКУЩЕГО TEACHER ID ===");

            var teacherLogin = HttpContext.Session.GetString("TeacherLogin");
            Console.WriteLine($"TeacherLogin from Session: {teacherLogin}");

            if (string.IsNullOrEmpty(teacherLogin))
            {
                Console.WriteLine("TeacherLogin не найден в сессии");
                return 0;
            }

            try
            {
                _dbManager.OpenConnection();
                var teachers = _dbManager.GetTeachersAsync().Result;
                var teacher = teachers.FirstOrDefault(t => t.Login == teacherLogin);

                if (teacher != null)
                {
                    Console.WriteLine($"Найден преподаватель: ID={teacher.Id}, Name={teacher.FullName}");
                    return teacher.Id;
                }
                else
                {
                    Console.WriteLine("Преподаватель не найден в БД");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске преподавателя: {ex.Message}");
                return 0;
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        private async Task<List<Submission>> GetSubmissionsForTeacher(int teacherId)
        {
            var submissions = new List<Submission>();
            var assignments = await _dbManager.GetAssignmentsByTeacherAsync(teacherId);

            foreach (var assignment in assignments)
            {
                var assignmentSubmissions = await _dbManager.GetSubmissionsByAssignmentAsync(assignment.Id);
                submissions.AddRange(assignmentSubmissions);
            }

            return submissions;
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