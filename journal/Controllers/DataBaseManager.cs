using journal.Models;
using journal.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

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

        public void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public void CloseConnection()
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public SqlConnection GetConnection()
        {
            return _connection;
        }

        public SqlCommand CreateCommand(string query)
        {
            return new SqlCommand(query, _connection);
        }

        public ConnectionState GetConnectionState()
        {
            return _connection.State;
        }

        public void Dispose()
        {
            CloseConnection();
            _connection?.Dispose();
        }

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

        public SqlConnection GetConnection()
        {
            return _dateBaseConnection.GetConnection();
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

        public async Task<bool> AuthenticateAdminAsync(string login, string password)
        {
            try
            {
                if (_dateBaseConnection.GetConnectionState() != ConnectionState.Open)
                {
                    throw new InvalidOperationException("Соединение не открыто. Вызовите OpenConnection() сначала.");
                }

                using var command = _dateBaseConnection.CreateCommand(
                    "SELECT Password, Salt FROM AdminLogin WHERE Login = @login");

                command.Parameters.AddWithValue("@login", login);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedHash = reader.GetString(0);
                    string salt = reader.GetString(1);
                    bool result = _passwordHasher.VerifyPassword(password, storedHash, salt);
                    return result;
                }
                else
                {
                    Console.WriteLine($"Администратор с логином {login} не найден");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка аутентификации администратора: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAdminPasswordAsync(string login, string password)
        {
            try
            {
                var (hash, salt) = _passwordHasher.HashPassword(password);

                var deleteCommand = _dateBaseConnection.CreateCommand("DELETE FROM AdminLogin WHERE Login = @Login");
                deleteCommand.Parameters.AddWithValue("@Login", login);
                await deleteCommand.ExecuteNonQueryAsync();

                var insertCommand = _dateBaseConnection.CreateCommand(
                    "INSERT INTO AdminLogin (Login, Password, Salt) VALUES (@Login, @Password, @Salt)");

                insertCommand.Parameters.AddWithValue("@Login", login);
                insertCommand.Parameters.AddWithValue("@Password", hash);
                insertCommand.Parameters.AddWithValue("@Salt", salt);

                return await insertCommand.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении пароля администратора: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ ГРУПП
        public async Task<bool> AddGroupAsync(Group group)
        {
            try
            {
                var query = "INSERT INTO Groups (Name, Course, Specialty, StudentCount) VALUES (@Name, @Course, @Specialty, @StudentCount)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Name", group.Name ?? "");
                    command.Parameters.AddWithValue("@Course", group.Course);
                    command.Parameters.AddWithValue("@Specialty", group.Specialty ?? "");
                    command.Parameters.AddWithValue("@StudentCount", group.StudentCount);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении группы: {ex.Message}");
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
                    return await command.ExecuteNonQueryAsync() > 0;
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
                var (hash, salt) = _passwordHasher.HashPassword(student.Password);
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

                    var result = await command.ExecuteNonQueryAsync() > 0;

                    if (result)
                    {
                        await UpdateStudentCountAsync(student.GroupId);
                    }

                    return result;
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

        public async Task<List<Student>> GetStudentsByGroupAsync(int groupId)
        {
            var students = new List<Student>();
            try
            {
                var query = @"SELECT s.Id, s.Login, s.Password, s.Salt, s.FullName, s.GroupId, s.GroupName, g.Name 
                             FROM Students s 
                             LEFT JOIN Groups g ON s.GroupId = g.Id
                             WHERE s.GroupId = @GroupId";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    using var reader = await command.ExecuteReaderAsync();

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
                Console.WriteLine($"Ошибка при получении студентов по группе: {ex.Message}");
            }
            return students;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            try
            {
                var groupId = await GetStudentGroupIdAsync(id);
                var query = "DELETE FROM Students WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    var result = await command.ExecuteNonQueryAsync() > 0;

                    if (result && groupId > 0)
                    {
                        await UpdateStudentCountAsync(groupId);
                    }

                    return result;
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

                    return await command.ExecuteNonQueryAsync() > 0;
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
                    return await command.ExecuteNonQueryAsync() > 0;
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

                    return await command.ExecuteNonQueryAsync() > 0;
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
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении расписания: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ ЗАДАНИЙ
        public async Task<bool> AddAssignmentAsync(Assignment assignment)
        {
            try
            {
                var query = @"INSERT INTO Assignments (Title, Description, Subject, TeacherId, GroupId, DueDate, FilePath, CreatedAt) 
                     VALUES (@Title, @Description, @Subject, @TeacherId, @GroupId, @DueDate, @FilePath, @CreatedAt)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Title", assignment.Title ?? "");
                    command.Parameters.AddWithValue("@Description", assignment.Description ?? "");
                    command.Parameters.AddWithValue("@Subject", assignment.Subject ?? "");
                    command.Parameters.AddWithValue("@TeacherId", assignment.TeacherId);
                    command.Parameters.AddWithValue("@GroupId", assignment.GroupId);
                    command.Parameters.AddWithValue("@DueDate", assignment.DueDate);
                    command.Parameters.AddWithValue("@FilePath", assignment.FilePath ?? "");
                    command.Parameters.AddWithValue("@CreatedAt", assignment.CreatedAt);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении задания: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Assignment>> GetAssignmentsAsync()
        {
            var assignments = new List<Assignment>();
            try
            {
                var query = @"SELECT a.*, t.FullName as TeacherName, g.Name as GroupName 
                     FROM Assignments a
                     LEFT JOIN Teachers t ON a.TeacherId = t.Id
                     LEFT JOIN Groups g ON a.GroupId = g.Id
                     ORDER BY a.DueDate DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        assignments.Add(new Assignment
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Subject = reader.GetString(3),
                            TeacherId = reader.GetInt32(4),
                            GroupId = reader.GetInt32(5),
                            DueDate = reader.GetDateTime(6),
                            FilePath = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            TeacherName = reader.GetString(9),
                            GroupName = reader.GetString(10)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всех заданий: {ex.Message}");
            }
            return assignments;
        }

        public async Task<List<Assignment>> GetAssignmentsByTeacherAsync(int teacherId)
        {
            var assignments = new List<Assignment>();
            try
            {
                var query = @"SELECT a.*, t.FullName as TeacherName, g.Name as GroupName 
                     FROM Assignments a
                     LEFT JOIN Teachers t ON a.TeacherId = t.Id
                     LEFT JOIN Groups g ON a.GroupId = g.Id
                     WHERE a.TeacherId = @TeacherId
                     ORDER BY a.DueDate DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@TeacherId", teacherId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        assignments.Add(new Assignment
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Subject = reader.GetString(3),
                            TeacherId = reader.GetInt32(4),
                            GroupId = reader.GetInt32(5),
                            DueDate = reader.GetDateTime(6),
                            FilePath = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            TeacherName = reader.GetString(9),
                            GroupName = reader.GetString(10)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении заданий преподавателя: {ex.Message}");
            }
            return assignments;
        }

        public async Task<List<Assignment>> GetAssignmentsByGroupAsync(int groupId)
        {
            var assignments = new List<Assignment>();
            try
            {
                var query = @"SELECT a.*, t.FullName as TeacherName, g.Name as GroupName 
                     FROM Assignments a
                     LEFT JOIN Teachers t ON a.TeacherId = t.Id
                     LEFT JOIN Groups g ON a.GroupId = g.Id
                     WHERE a.GroupId = @GroupId
                     ORDER BY a.DueDate DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        assignments.Add(new Assignment
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Subject = reader.GetString(3),
                            TeacherId = reader.GetInt32(4),
                            GroupId = reader.GetInt32(5),
                            DueDate = reader.GetDateTime(6),
                            FilePath = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            TeacherName = reader.GetString(9),
                            GroupName = reader.GetString(10)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении заданий: {ex.Message}");
            }
            return assignments;
        }

        public async Task<Assignment> GetAssignmentByIdAsync(int assignmentId)
        {
            try
            {
                var query = @"SELECT a.*, t.FullName as TeacherName, g.Name as GroupName 
                     FROM Assignments a
                     LEFT JOIN Teachers t ON a.TeacherId = t.Id
                     LEFT JOIN Groups g ON a.GroupId = g.Id
                     WHERE a.Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", assignmentId);
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        return new Assignment
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Subject = reader.GetString(3),
                            TeacherId = reader.GetInt32(4),
                            GroupId = reader.GetInt32(5),
                            DueDate = reader.GetDateTime(6),
                            FilePath = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            TeacherName = reader.GetString(9),
                            GroupName = reader.GetString(10)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении задания по ID: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId, int teacherId)
        {
            try
            {
                var query = "DELETE FROM Assignments WHERE Id = @Id AND TeacherId = @TeacherId";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", assignmentId);
                    command.Parameters.AddWithValue("@TeacherId", teacherId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении задания: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ ОТПРАВОК
        public async Task<bool> AddSubmissionAsync(Submission submission)
        {
            try
            {
                var query = @"INSERT INTO Submissions (AssignmentId, StudentId, FilePath, SubmittedAt) 
                     VALUES (@AssignmentId, @StudentId, @FilePath, @SubmittedAt)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@AssignmentId", submission.AssignmentId);
                    command.Parameters.AddWithValue("@StudentId", submission.StudentId);
                    command.Parameters.AddWithValue("@FilePath", submission.FilePath ?? "");
                    command.Parameters.AddWithValue("@SubmittedAt", DateTime.Now);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении решения: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Submission>> GetSubmissionsByAssignmentAsync(int assignmentId)
        {
            var submissions = new List<Submission>();
            try
            {
                var query = @"SELECT s.*, st.FullName as StudentName, a.Title as AssignmentTitle
                     FROM Submissions s
                     LEFT JOIN Students st ON s.StudentId = st.Id
                     LEFT JOIN Assignments a ON s.AssignmentId = a.Id
                     WHERE s.AssignmentId = @AssignmentId";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@AssignmentId", assignmentId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        submissions.Add(new Submission
                        {
                            Id = reader.GetInt32(0),
                            AssignmentId = reader.GetInt32(1),
                            StudentId = reader.GetInt32(2),
                            FilePath = reader.GetString(3),
                            SubmittedAt = reader.GetDateTime(4),
                            Grade = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Feedback = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            StudentName = reader.GetString(7),
                            AssignmentTitle = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении отправок: {ex.Message}");
            }
            return submissions;
        }

        public async Task<List<Submission>> GetSubmissionsByStudentAsync(int studentId)
        {
            var submissions = new List<Submission>();
            try
            {
                var query = @"SELECT s.*, a.Title as AssignmentTitle, st.FullName as StudentName 
                     FROM Submissions s 
                     LEFT JOIN Assignments a ON s.AssignmentId = a.Id 
                     LEFT JOIN Students st ON s.StudentId = st.Id 
                     WHERE s.StudentId = @StudentId
                     ORDER BY s.SubmittedAt DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        submissions.Add(new Submission
                        {
                            Id = reader.GetInt32(0),
                            AssignmentId = reader.GetInt32(1),
                            StudentId = reader.GetInt32(2),
                            FilePath = reader.GetString(3),
                            SubmittedAt = reader.GetDateTime(4),
                            Grade = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Feedback = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            StudentName = reader.GetString(7),
                            AssignmentTitle = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении отправленных работ студента: {ex.Message}");
            }
            return submissions;
        }

        public async Task<Submission> GetSubmissionByIdAsync(int submissionId)
        {
            try
            {
                var query = @"SELECT s.*, st.FullName as StudentName, a.Title as AssignmentTitle
                     FROM Submissions s
                     LEFT JOIN Students st ON s.StudentId = st.Id
                     LEFT JOIN Assignments a ON s.AssignmentId = a.Id
                     WHERE s.Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", submissionId);
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        return new Submission
                        {
                            Id = reader.GetInt32(0),
                            AssignmentId = reader.GetInt32(1),
                            StudentId = reader.GetInt32(2),
                            FilePath = reader.GetString(3),
                            SubmittedAt = reader.GetDateTime(4),
                            Grade = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Feedback = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            StudentName = reader.GetString(7),
                            AssignmentTitle = reader.GetString(8)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении отправки по ID: {ex.Message}");
            }
            return null;
        }

        

        public async Task<bool> UpdateSubmissionAsync(Submission submission)
        {
            try
            {
                var query = @"UPDATE Submissions 
                     SET FilePath = @FilePath, SubmittedAt = @SubmittedAt, 
                         Grade = @Grade, Feedback = @Feedback 
                     WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@FilePath", submission.FilePath ?? "");
                    command.Parameters.AddWithValue("@SubmittedAt", submission.SubmittedAt);
                    command.Parameters.AddWithValue("@Grade", submission.Grade ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Feedback", submission.Feedback ?? "");
                    command.Parameters.AddWithValue("@Id", submission.Id);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении отправки: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSubmissionGradeAsync(int submissionId, int grade)
        {
            try
            {
                var query = "UPDATE Submissions SET Grade = @Grade WHERE Id = @Id";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Grade", grade);
                    command.Parameters.AddWithValue("@Id", submissionId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении оценки: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSubmissionFeedbackAsync(int submissionId, string feedback)
        {
            try
            {
                var query = "UPDATE Submissions SET Feedback = @Feedback WHERE Id = @Id";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Feedback", feedback ?? "");
                    command.Parameters.AddWithValue("@Id", submissionId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении комментария: {ex.Message}");
                return false;
            }
        }

        // МЕТОДЫ ДЛЯ ОЦЕНОК
        public async Task<bool> AddGradeAsync(Grade grade)
        {
            try
            {
                var query = @"INSERT INTO Grades (StudentId, Subject, GradeValue, GradeType, Date, TeacherId, Comments) 
                     VALUES (@StudentId, @Subject, @GradeValue, @GradeType, @Date, @TeacherId, @Comments)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@StudentId", grade.StudentId);
                    command.Parameters.AddWithValue("@Subject", grade.Subject ?? "");
                    command.Parameters.AddWithValue("@GradeValue", grade.GradeValue);
                    command.Parameters.AddWithValue("@GradeType", grade.GradeType ?? "");
                    command.Parameters.AddWithValue("@Date", grade.Date);
                    command.Parameters.AddWithValue("@TeacherId", grade.TeacherId);
                    command.Parameters.AddWithValue("@Comments", grade.Comments ?? "");

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении оценки: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Grade>> GetGradesAsync()
        {
            var grades = new List<Grade>();
            try
            {
                var query = @"SELECT g.*, s.FullName as StudentName 
                     FROM Grades g
                     LEFT JOIN Students s ON g.StudentId = s.Id
                     ORDER BY g.Date DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        grades.Add(new Grade
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            Subject = reader.GetString(2),
                            GradeValue = reader.GetInt32(3),
                            GradeType = reader.GetString(4),
                            Date = reader.GetDateTime(5),
                            TeacherId = reader.GetInt32(6),
                            Comments = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            StudentName = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всех оценок: {ex.Message}");
            }
            return grades;
        }

        public async Task<List<Grade>> GetGradesByStudentAsync(int studentId)
        {
            var grades = new List<Grade>();
            try
            {
                var query = @"SELECT g.*, s.FullName as StudentName 
                     FROM Grades g
                     LEFT JOIN Students s ON g.StudentId = s.Id
                     WHERE g.StudentId = @StudentId
                     ORDER BY g.Date DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        grades.Add(new Grade
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            Subject = reader.GetString(2),
                            GradeValue = reader.GetInt32(3),
                            GradeType = reader.GetString(4),
                            Date = reader.GetDateTime(5),
                            TeacherId = reader.GetInt32(6),
                            Comments = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            StudentName = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении оценок: {ex.Message}");
            }
            return grades;
        }

        // МЕТОДЫ ДЛЯ ПОСЕЩАЕМОСТИ
        public async Task<bool> AddAttendanceAsync(AttendanceRecord attendance)
        {
            try
            {
                var query = @"INSERT INTO Attendance (StudentId, ScheduleId, Date, IsPresent, RecordedBy, RecordedAt) 
                     VALUES (@StudentId, @ScheduleId, @Date, @IsPresent, @RecordedBy, @RecordedAt)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@StudentId", attendance.StudentId);
                    command.Parameters.AddWithValue("@ScheduleId", attendance.ScheduleId);
                    command.Parameters.AddWithValue("@Date", attendance.Date);
                    command.Parameters.AddWithValue("@IsPresent", attendance.IsPresent);
                    command.Parameters.AddWithValue("@RecordedBy", attendance.RecordedBy);
                    command.Parameters.AddWithValue("@RecordedAt", DateTime.Now);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении посещаемости: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AttendanceRecord>> GetAttendanceAsync()
        {
            var attendance = new List<AttendanceRecord>();
            try
            {
                var query = @"SELECT a.*, s.FullName as StudentName, sch.Subject
                     FROM Attendance a
                     LEFT JOIN Students s ON a.StudentId = s.Id
                     LEFT JOIN Schedule sch ON a.ScheduleId = sch.Id
                     ORDER BY a.Date DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        attendance.Add(new AttendanceRecord
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            ScheduleId = reader.GetInt32(2),
                            Date = reader.GetDateTime(3),
                            IsPresent = reader.GetBoolean(4),
                            RecordedBy = reader.GetInt32(5),
                            RecordedAt = reader.GetDateTime(6),
                            StudentName = reader.GetString(7),
                            Subject = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всей посещаемости: {ex.Message}");
            }
            return attendance;
        }

        public async Task<List<AttendanceRecord>> GetAttendanceByStudentAsync(int studentId)
        {
            var attendance = new List<AttendanceRecord>();
            try
            {
                var query = @"SELECT a.*, s.FullName as StudentName, sch.Subject 
                     FROM Attendance a 
                     LEFT JOIN Students s ON a.StudentId = s.Id 
                     LEFT JOIN Schedule sch ON a.ScheduleId = sch.Id 
                     WHERE a.StudentId = @StudentId 
                     ORDER BY a.Date DESC";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        attendance.Add(new AttendanceRecord
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            ScheduleId = reader.GetInt32(2),
                            Date = reader.GetDateTime(3),
                            IsPresent = reader.GetBoolean(4),
                            RecordedBy = reader.GetInt32(5),
                            RecordedAt = reader.GetDateTime(6),
                            StudentName = reader.GetString(7),
                            Subject = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении посещаемости студента: {ex.Message}");
            }
            return attendance;
        }

        public async Task<List<AttendanceRecord>> GetAttendanceByDateAndGroupAsync(int groupId, DateTime date, string subject)
        {
            var attendanceRecords = new List<AttendanceRecord>();
            try
            {
                var query = @"SELECT a.*, s.FullName as StudentName, sch.Subject
                     FROM Attendance a
                     LEFT JOIN Students s ON a.StudentId = s.Id
                     LEFT JOIN Schedule sch ON a.ScheduleId = sch.Id
                     WHERE s.GroupId = @GroupId AND a.Date = @Date AND sch.Subject = @Subject";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@GroupId", groupId);
                    command.Parameters.AddWithValue("@Date", date.Date);
                    command.Parameters.AddWithValue("@Subject", subject);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        attendanceRecords.Add(new AttendanceRecord
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            ScheduleId = reader.GetInt32(2),
                            Date = reader.GetDateTime(3),
                            IsPresent = reader.GetBoolean(4),
                            RecordedBy = reader.GetInt32(5),
                            RecordedAt = reader.GetDateTime(6),
                            StudentName = reader.GetString(7),
                            Subject = reader.GetString(8)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении посещаемости: {ex.Message}");
            }
            return attendanceRecords;
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

        public async Task<Student> GetStudentByIdAsync(int studentId)
        {
            try
            {
                var query = @"SELECT s.Id, s.Login, s.Password, s.Salt, s.FullName, s.GroupId, s.GroupName, g.Name 
                     FROM Students s 
                     LEFT JOIN Groups g ON s.GroupId = g.Id
                     WHERE s.Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", studentId);
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        return new Student
                        {
                            Id = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(4),
                            GroupId = reader.GetInt32(5),
                            GroupName = !reader.IsDBNull(6) ? reader.GetString(6) : reader.GetString(7)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении студента по ID: {ex.Message}");
            }
            return null;
        }
        public async Task<bool> SubmissionExistsAsync(int assignmentId, int studentId)
        {
            try
            {
                var query = "SELECT COUNT(*) FROM Submissions WHERE AssignmentId = @AssignmentId AND StudentId = @StudentId";
                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@AssignmentId", assignmentId);
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке существования отправки: {ex.Message}");
                return false;
            }
        }
        public async Task<Teacher> GetTeacherByIdAsync(int teacherId)
        {
            try
            {
                var query = "SELECT Id, Login, Password, Salt, FullName, Subject FROM Teachers WHERE Id = @Id";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Id", teacherId);
                    using var reader = await command.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        return new Teacher
                        {
                            Id = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(4),
                            Subject = reader.GetString(5)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении преподавателя по ID: {ex.Message}");
            }
            return null;
        }

        public void Dispose()
        {
            CloseConnection();
        }
    }
}