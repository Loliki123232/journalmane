using journal.Models;
using journal.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
namespace journal.Controllers
{
    public class DateBaseConnection : IDisposable
    {
        private static DateBaseConnection _instance;
        private static readonly object _lock = new object();
        private SqlConnection _connection;
        private readonly string _connectionString;

        private static string GetDatabasePath()
        {
            var repoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source",
                "repos");
            return Path.Combine(repoPath, "journalmane", "DataBase", "Database1.mdf");
        }

        private DateBaseConnection()
        {
            var databasePath = GetDatabasePath();
            _connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={databasePath};Integrated Security=True;Connect Timeout=30;";
            _connection = new SqlConnection(_connectionString);
        }

        public static DateBaseConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DateBaseConnection();
                    }
                }
                return _instance;
            }
        }

        // Метод для открытия соединения
        public void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        // Метод для закрытия соединения
        public void CloseConnection()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        // Получить текущее соединение
        public SqlConnection GetConnection()
        {
            return _connection;
        }

        // Создать команду с текущим соединением
        public SqlCommand CreateCommand(string query)
        {
            return new SqlCommand(query, _connection);
        }

        // Проверить состояние соединения
        public ConnectionState GetConnectionState()
        {
            return _connection.State;
        }

        // Освобождение ресурсов
        public void Dispose()
        {
            CloseConnection();
            _connection?.Dispose();
        }

        // Деструктор
        ~DateBaseConnection()
        {
            Dispose();
        }
    }

    public class DataBaseManager : IDisposable
    {
        private DateBaseConnection _dateBaseConnection;
        private bool _isConnectionOwned = false;
        private readonly PasswordHasherService _passwordHasher;

        public DataBaseManager()
        {
            _dateBaseConnection = DateBaseConnection.Instance;
            _passwordHasher = new PasswordHasherService();
        }

        public void OpenConnection()
        {
            _dateBaseConnection.OpenConnection();
            _isConnectionOwned = true;
        }

        public void CloseConnection()
        {
            if (_isConnectionOwned)
            {
                _dateBaseConnection.CloseConnection();
                _isConnectionOwned = false;
            }
        }

        // МЕТОДЫ АУТЕНТИФИКАЦИИ
        public async Task<bool> AuthenticateStudentAsync(string login, string password)
        {
            try
            {
                if (_dateBaseConnection.GetConnectionState() != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Соединение не открыто. Вызовите OpenConnection() сначала.");
                }

                using var command = _dateBaseConnection.CreateCommand(
                    "SELECT Password, Salt FROM Students WHERE Login = @login");

                command.Parameters.AddWithValue("@login", login);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedHash = reader.GetString(0);
                    string salt = reader.GetString(1);
                    return _passwordHasher.VerifyPassword(password, storedHash, salt);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка аутентификации студента: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AuthenticateTeacherAsync(string login, string password)
        {
            try
            {
                if (_dateBaseConnection.GetConnectionState() != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Соединение не открыто. Вызовите OpenConnection() сначала.");
                }

                using var command = _dateBaseConnection.CreateCommand(
                    "SELECT Password, Salt FROM Teachers WHERE Login = @login");

                command.Parameters.AddWithValue("@login", login);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedHash = reader.GetString(0);
                    string salt = reader.GetString(1);
                    return _passwordHasher.VerifyPassword(password, storedHash, salt);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка аутентификации преподавателя: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> AuthenticateAdminAsync(string login, string inputpassword)
        {

            try
            {
                // Проверяем, открыто ли соединение
                if (_dateBaseConnection.GetConnectionState() != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Соединение не открыто. Вызовите OpenConnection() сначала.");
                }

                // Создаем команду
                using var command = _dateBaseConnection.CreateCommand(
                    "SELECT * FROM AdminLogin WHERE Login=@login AND Password=@password");

                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", inputpassword);

                // Выполняем запрос
                using var reader = await command.ExecuteReaderAsync();
                return await reader.ReadAsync(); // true если пользователь найден

            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка аутентификации: {ex.Message}");
                return false;
            }
        }


        // МЕТОДЫ ДЛЯ ГРУПП
        public async Task<bool> AddGroupAsync(Group group)
        {
            try
            {
                Console.WriteLine($"Добавление группы в БД: {group.Name}, {group.Course}, {group.Specialty}");

                var query = "INSERT INTO Groups (Name, Course, Specialty, StudentCount) VALUES (@Name, @Course, @Specialty, @StudentCount)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Name", group.Name ?? "");
                    command.Parameters.AddWithValue("@Course", group.Course);
                    command.Parameters.AddWithValue("@Specialty", group.Specialty ?? "");
                    command.Parameters.AddWithValue("@StudentCount", group.StudentCount);

                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Результат выполнения: {result} строк добавлено");
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении группы: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<Group>> GetGroupsAsync()
        {
            var groups = new List<Group>();

            try
            {
                var query = "SELECT Id, Name, Course, Specialty, StudentCount FROM Groups";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        groups.Add(new Group
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Course = reader.GetInt32(2),
                            Specialty = reader.GetString(3),
                            StudentCount = reader.GetInt32(4)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении групп: {ex.Message}");
            }

            return groups;
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            try
            {
                var query = "DELETE FROM Groups WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении группы: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ СТУДЕНТОВ
        public async Task<bool> AddStudentAsync(Student student)
        {
            try
            {
                Console.WriteLine($"Добавление студента в БД: {student.Login}, {student.FullName}, {student.GroupId}");

                // Хешируем пароль
                var (hash, salt) = _passwordHasher.HashPassword(student.Password);

                // Получаем название группы для GroupName
                var groupName = await GetGroupNameByIdAsync(student.GroupId);

                var query = @"INSERT INTO Students (Login, Password, Salt, FullName, GroupId, GroupName) 
                             VALUES (@Login, @Password, @Salt, @FullName, @GroupId, @GroupName)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Login", student.Login ?? "");
                    command.Parameters.AddWithValue("@Password", hash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@FullName", student.FullName ?? "");
                    command.Parameters.AddWithValue("@GroupId", student.GroupId);
                    command.Parameters.AddWithValue("@GroupName", groupName ?? "");

                    var result = await command.ExecuteNonQueryAsync();

                    // Обновляем счетчик студентов в группе
                    if (result > 0)
                    {
                        await UpdateStudentCountAsync(student.GroupId);
                    }

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении студента: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Student>> GetStudentsAsync()
        {
            var students = new List<Student>();

            try
            {
                var query = @"SELECT s.Id, s.Login, s.Password, s.Salt, s.FullName, s.GroupId, s.GroupName, g.Name 
                             FROM Students s 
                             LEFT JOIN Groups g ON s.GroupId = g.Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        students.Add(new Student
                        {
                            Id = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(4),
                            GroupId = reader.GetInt32(5),
                            GroupName = !reader.IsDBNull(6) ? reader.GetString(6) : reader.GetString(7)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении студентов: {ex.Message}");
            }

            return students;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            try
            {
                // Сначала получаем GroupId студента для обновления счетчика
                var groupId = await GetStudentGroupIdAsync(id);

                var query = "DELETE FROM Students WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = await command.ExecuteNonQueryAsync();

                    // Обновляем счетчик студентов в группе
                    if (result > 0 && groupId > 0)
                    {
                        await UpdateStudentCountAsync(groupId);
                    }

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении студента: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ ПРЕПОДАВАТЕЛЕЙ
        public async Task<bool> AddTeacherAsync(Teacher teacher)
        {
            try
            {
                Console.WriteLine($"Добавление преподавателя в БД: {teacher.Login}, {teacher.FullName}, {teacher.Subject}");

                // Хешируем пароль
                var (hash, salt) = _passwordHasher.HashPassword(teacher.Password);

                var query = @"INSERT INTO Teachers (Login, Password, Salt, FullName, Subject) 
                             VALUES (@Login, @Password, @Salt, @FullName, @Subject)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Login", teacher.Login ?? "");
                    command.Parameters.AddWithValue("@Password", hash);
                    command.Parameters.AddWithValue("@Salt", salt);
                    command.Parameters.AddWithValue("@FullName", teacher.FullName ?? "");
                    command.Parameters.AddWithValue("@Subject", teacher.Subject ?? "");

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении преподавателя: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Teacher>> GetTeachersAsync()
        {
            var teachers = new List<Teacher>();

            try
            {
                var query = "SELECT Id, Login, Password, Salt, FullName, Subject FROM Teachers";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        teachers.Add(new Teacher
                        {
                            Id = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(4),
                            Subject = reader.GetString(5)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении преподавателей: {ex.Message}");
            }

            return teachers;
        }

        public async Task<bool> DeleteTeacherAsync(int id)
        {
            try
            {
                var query = "DELETE FROM Teachers WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении преподавателя: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ РАСПИСАНИЯ
        public async Task<bool> AddScheduleAsync(Schedule schedule)
        {
            try
            {
                Console.WriteLine($"Добавление расписания в БД: GroupId={schedule.GroupId}, Day={schedule.DayOfWeek}, Time={schedule.Time}");

                // Получаем названия группы и преподавателя
                var groupName = await GetGroupNameByIdAsync(schedule.GroupId);
                var teacherName = await GetTeacherNameByIdAsync(schedule.TeacherId);

                var query = @"INSERT INTO Schedule (GroupId, GroupName, DayOfWeek, Time, Subject, TeacherId, TeacherName, Room) 
                             VALUES (@GroupId, @GroupName, @DayOfWeek, @Time, @Subject, @TeacherId, @TeacherName, @Room)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@GroupId", schedule.GroupId);
                    command.Parameters.AddWithValue("@GroupName", groupName ?? "");
                    command.Parameters.AddWithValue("@DayOfWeek", schedule.DayOfWeek ?? "");
                    command.Parameters.AddWithValue("@Time", schedule.Time ?? "");
                    command.Parameters.AddWithValue("@Subject", schedule.Subject ?? "");
                    command.Parameters.AddWithValue("@TeacherId", schedule.TeacherId);
                    command.Parameters.AddWithValue("@TeacherName", teacherName ?? "");
                    command.Parameters.AddWithValue("@Room", schedule.Room ?? "");

                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении расписания: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Schedule>> GetScheduleAsync()
        {
            var schedules = new List<Schedule>();

            try
            {
                var query = @"SELECT Id, GroupId, GroupName, DayOfWeek, Time, Subject, TeacherId, TeacherName, Room 
                             FROM Schedule 
                             ORDER BY 
                                 CASE DayOfWeek 
                                     WHEN 'Monday' THEN 1 
                                     WHEN 'Tuesday' THEN 2 
                                     WHEN 'Wednesday' THEN 3 
                                     WHEN 'Thursday' THEN 4 
                                     WHEN 'Friday' THEN 5 
                                     ELSE 6 
                                 END, Time";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        schedules.Add(new Schedule
                        {
                            Id = reader.GetInt32(0),
                            GroupId = reader.GetInt32(1),
                            GroupName = reader.GetString(2),
                            DayOfWeek = reader.GetString(3),
                            Time = reader.GetString(4),
                            Subject = reader.GetString(5),
                            TeacherId = reader.GetInt32(6),
                            TeacherName = reader.GetString(7),
                            Room = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении расписания: {ex.Message}");
            }

            return schedules;
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            try
            {
                var query = "DELETE FROM Schedule WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении расписания: {ex.Message}");
                return false;
            }
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private async Task<string> GetGroupNameByIdAsync(int groupId)
        {
            try
            {
                var query = "SELECT Name FROM Groups WHERE Id = @Id";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", groupId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private async Task<string> GetTeacherNameByIdAsync(int teacherId)
        {
            try
            {
                var query = "SELECT FullName FROM Teachers WHERE Id = @Id";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", teacherId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        private async Task<int> GetStudentGroupIdAsync(int studentId)
        {
            try
            {
                var query = "SELECT GroupId FROM Students WHERE Id = @Id";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", studentId);
                    var result = await command.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private async Task UpdateStudentCountAsync(int groupId)
        {
            try
            {
                var query = @"UPDATE Groups 
                             SET StudentCount = (SELECT COUNT(*) FROM Students WHERE GroupId = @GroupId) 
                             WHERE Id = @GroupId";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении счетчика студентов: {ex.Message}");
            }
        }

        public void Dispose()
        {
            CloseConnection();
        }
    }
}