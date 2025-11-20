using journal.Models;
using journal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

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
            Console.WriteLine($"TeacherId в Dashboard: {teacherId}");

            if (teacherId == 0)
            {
                Console.WriteLine("Редирект на Login - TeacherId = 0");
                return RedirectToAction("Login", "Home");
            }

            try
            {
                _dbManager.OpenConnection();

                var teacher = _dbManager.GetTeachersAsync().Result.FirstOrDefault(t => t.Id == teacherId);
                var groups = _dbManager.GetGroupsAsync().Result;
                var students = _dbManager.GetStudentsAsync().Result;
                var assignments = _dbManager.GetAssignmentsByTeacherAsync(teacherId).Result;
                var submissions = GetSubmissionsForTeacher(teacherId);
                var schedule = _dbManager.GetScheduleAsync().Result.Where(s => s.TeacherId == teacherId).ToList();

                var viewModel = new TeacherDashboardViewModel
                {
                    Teacher = teacher,
                    Groups = groups,
                    Students = students,
                    Assignments = assignments,
                    Submissions = submissions,
                    Schedule = schedule
                };

                // Явно указываем путь к представлению
                return View("~/Views/Home/TeacherDashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке панели преподавателя: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return View("~/Views/Home/TeacherDashboard.cshtml", new TeacherDashboardViewModel());
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
                return RedirectToAction("Dashboard");
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
                return RedirectToAction("Dashboard");
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
                    return RedirectToAction("Dashboard");
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

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Пожалуйста, укажите оценку";
                return RedirectToAction("TeacherDashboard");
            }

            try
            {
                _dbManager.OpenConnection();

                var result = await _dbManager.UpdateSubmissionGradeAsync(model.SubmissionId, model.Grade);

                if (result && !string.IsNullOrEmpty(model.Feedback))
                {
                    await _dbManager.UpdateSubmissionFeedbackAsync(model.SubmissionId, model.Feedback);
                }

                if (result)
                    TempData["SuccessMessage"] = "Работа успешно оценена!";
                else
                    TempData["ErrorMessage"] = "Ошибка при оценке работы.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при оценке работы: {ex.Message}");
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
        public IActionResult DownloadSubmission(int submissionId)
        {
            try
            {
                _dbManager.OpenConnection();
                var submissions = _dbManager.GetSubmissionsByAssignmentAsync(0).Result;
                var submission = submissions.FirstOrDefault(s => s.Id == submissionId);

                if (submission == null || string.IsNullOrEmpty(submission.FilePath))
                {
                    TempData["ErrorMessage"] = "Файл не найден";
                    return RedirectToAction("Dashboard");
                }

                var fullPath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["ErrorMessage"] = "Файл не найден на сервере";
                    return RedirectToAction("Dashboard");
                }

                var fileBytes = System.IO.File.ReadAllBytes(fullPath);
                var fileName = Path.GetFileName(fullPath);

                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при скачивании работы: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при скачивании файла";
                return RedirectToAction("Dashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // ПРОСМОТР ДЕТАЛЕЙ ЗАДАНИЯ
        public IActionResult AssignmentDetails(int assignmentId)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();
                var assignments = _dbManager.GetAssignmentsByTeacherAsync(teacherId).Result;
                var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    TempData["ErrorMessage"] = "Задание не найдено";
                    return RedirectToAction("Dashboard");
                }

                var submissions = _dbManager.GetSubmissionsByAssignmentAsync(assignmentId).Result;

                ViewBag.Submissions = submissions;
                return View(assignment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке деталей задания: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("Dashboard");
            }
            finally
            {
                _dbManager.CloseConnection();
            }
        }

        // ПРОСМОТР ОЦЕНОК СТУДЕНТА
        public IActionResult StudentGrades(int studentId)
        {
            var teacherId = GetCurrentTeacherId();
            if (teacherId == 0)
                return RedirectToAction("Login", "Home");

            try
            {
                _dbManager.OpenConnection();
                var grades = _dbManager.GetGradesByStudentAsync(studentId).Result;
                var student = _dbManager.GetStudentsAsync().Result.FirstOrDefault(s => s.Id == studentId);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Студент не найден";
                    return RedirectToAction("Dashboard");
                }

                ViewBag.Student = student;
                return View(grades);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке оценок: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных";
                return RedirectToAction("Dashboard");
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

            return RedirectToAction("Dashboard");
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private int GetCurrentTeacherId()
        {
            Console.WriteLine("=== ПОЛУЧЕНИЕ ТЕКУЩЕГО TEACHER ID ===");

            // Получаем логин из сессии
            var teacherLogin = HttpContext.Session.GetString("TeacherLogin");
            Console.WriteLine($"TeacherLogin from Session: {teacherLogin}");

            if (string.IsNullOrEmpty(teacherLogin))
            {
                Console.WriteLine("TeacherLogin не найден в сессии");
                return 0;
            }

            // Находим ID преподавателя по логину
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

        private List<Submission> GetSubmissionsForTeacher(int teacherId)
        {
            var submissions = new List<Submission>();
            var assignments = _dbManager.GetAssignmentsByTeacherAsync(teacherId).Result;

            foreach (var assignment in assignments)
            {
                var assignmentSubmissions = _dbManager.GetSubmissionsByAssignmentAsync(assignment.Id).Result;
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