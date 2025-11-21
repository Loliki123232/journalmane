using System.ComponentModel.DataAnnotations;

namespace journal.Models
{
    public class AdminDashboardViewModel
    {
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Teacher> Teachers { get; set; } = new List<Teacher>();
        public List<Group> Groups { get; set; } = new List<Group>();
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
    }
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; }
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();
        public List<Submission> Submissions { get; set; } = new List<Submission>();
        public List<Grade> Grades { get; set; } = new List<Grade>();
        public List<AttendanceRecord> Attendance { get; set; } = new List<AttendanceRecord>();
    }

    public class TeacherDashboardViewModel
    {
        public Teacher Teacher { get; set; }
        public IEnumerable<Group> Groups { get; set; }
        public IEnumerable<Student> Students { get; set; }
        public IEnumerable<Assignment> Assignments { get; set; }
        public IEnumerable<Submission> Submissions { get; set; }
        public IEnumerable<Schedule> Schedule { get; set; }
    }

    // Основные модели данных
    public class Student
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string FullName { get; set; }
        public string GroupName { get; set; }
        public int GroupId { get; set; }
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string FullName { get; set; }
        public string Subject { get; set; }
    }

    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Course { get; set; }
        public string Specialty { get; set; }
        public int StudentCount { get; set; }
    }

    public class Schedule
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int GroupId { get; set; }
        public string DayOfWeek { get; set; }
        public string Time { get; set; }
        public string Subject { get; set; }
        public string TeacherName { get; set; }
        public int TeacherId { get; set; }
        public string Room { get; set; }
    }
    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Subject { get; set; }
        public int TeacherId { get; set; }
        public int GroupId { get; set; }
        public DateTime DueDate { get; set; }
        public string FilePath { get; set; } // Для хранения на диске
        public string FileName { get; set; } // Для хранения в БД
        public byte[] FileData { get; set; } // Для хранения в БД как BLOB
        public DateTime CreatedAt { get; set; }
        public string TeacherName { get; set; }
        public string GroupName { get; set; }
    }

    public class Submission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string FilePath { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int? Grade { get; set; }
        public string Feedback { get; set; }
        public string StudentName { get; set; }
        public string AssignmentTitle { get; set; }
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int ScheduleId { get; set; }
        public DateTime Date { get; set; }
        public bool IsPresent { get; set; }
        public int RecordedBy { get; set; }
        public DateTime RecordedAt { get; set; }
        public string StudentName { get; set; }
        public string Subject { get; set; }
    }

    public class Grade
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Subject { get; set; }
        public int GradeValue { get; set; }
        public string GradeType { get; set; }
        public DateTime Date { get; set; }
        public int TeacherId { get; set; }
        public string Comments { get; set; }
        public string StudentName { get; set; }
    }

    // Модели для форм
    public class AddGroupFormModel
    {
        [Required(ErrorMessage = "Название группы обязательно")]
        [StringLength(50, ErrorMessage = "Название группы не должно превышать 50 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Выберите курс")]
        [Range(1, 4, ErrorMessage = "Курс должен быть от 1 до 4")]
        public int Course { get; set; }

        [Required(ErrorMessage = "Специальность обязательна")]
        [StringLength(200, ErrorMessage = "Специальность не должна превышать 200 символов")]
        public string Specialty { get; set; }
    }

    public class AddStudentFormModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, ErrorMessage = "Логин не должен превышать 50 символов")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, ErrorMessage = "Пароль не должен превышать 100 символов")]
        public string Password { get; set; }

        [Required(ErrorMessage = "ФИО обязательно")]
        [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Выберите группу")]
        public int GroupId { get; set; }
    }

    public class AddTeacherFormModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, ErrorMessage = "Логин не должен превышать 50 символов")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, ErrorMessage = "Пароль не должен превышать 100 символов")]
        public string Password { get; set; }

        [Required(ErrorMessage = "ФИО обязательно")]
        [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Предмет обязателен")]
        [StringLength(100, ErrorMessage = "Название предмета не должно превышать 100 символов")]
        public string Subject { get; set; }
    }

    public class AddScheduleFormModel
    {
        [Required(ErrorMessage = "Выберите группу")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Выберите день недели")]
        public string DayOfWeek { get; set; }

        [Required(ErrorMessage = "Выберите время")]
        public string Time { get; set; }

        [Required(ErrorMessage = "Предмет обязателен")]
        [StringLength(100, ErrorMessage = "Название предмета не должно превышать 100 символов")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Выберите преподавателя")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Аудитория обязательна")]
        [StringLength(50, ErrorMessage = "Название аудитории не должно превышать 50 символов")]
        public string Room { get; set; }
    }
    public class AttendanceMarkModel
    {
        [Required(ErrorMessage = "Выберите группу")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Укажите дату")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Укажите предмет")]
        public string Subject { get; set; }

        public List<int> StudentIds { get; set; } = new List<int>();
        public List<bool> AttendanceStatus { get; set; } = new List<bool>();
    }

    public class GradeSubmissionModel
    {
        [Required(ErrorMessage = "Укажите ID отправки")]
        public int SubmissionId { get; set; }

        [Required(ErrorMessage = "Укажите оценку")]
        [Range(1, 100, ErrorMessage = "Оценка должна быть от 1 до 100")]
        public int Grade { get; set; }

        public string Feedback { get; set; }
    }

    public class CreateAssignmentModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Предмет обязателен")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Группа обязательна")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Срок выполнения обязателен")]
        public DateTime DueDate { get; set; }

        public string Description { get; set; }

        // Сделать файл необязательным
        public IFormFile? AssignmentFile { get; set; }
    }
}