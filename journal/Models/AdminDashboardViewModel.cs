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
}