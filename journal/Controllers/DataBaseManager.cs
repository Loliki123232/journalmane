using journal.Models;
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
        

        public DataBaseManager()
        {
            _dateBaseConnection = DateBaseConnection.Instance;
        }

        // Метод для открытия соединения
        public void OpenConnection()
        {
            _dateBaseConnection.OpenConnection();
            _isConnectionOwned = true;
        }

        // Метод для закрытия соединения
        public void CloseConnection()
        {
            if (_isConnectionOwned)
            {
                _dateBaseConnection.CloseConnection();
                _isConnectionOwned = false;
            }
        }

        // Метод для аутентификации пользователя
        public async Task<bool> AuthenticateStudentAsync(string login, string hashedPassword)
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
                    "SELECT * FROM Users WHERE Login=@login AND Password=@password");

                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", hashedPassword);

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
        public async Task<bool> AuthenticateTeacherAsync(string login, string hashedPassword)
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
                    "SELECT * FROM TeacherLogin WHERE Login=@login AND Password=@password");

                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", hashedPassword);

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
        public async Task<bool> AddGroupAsync(Group group)
        {
            try
            {
                Console.WriteLine($"Добавление группы в БД: {group.Name}, {group.Course}, {group.Specialty}");

                var query = "INSERT INTO Groups (Name, Course, Specialty, StudentCount) VALUES (@Name, @Course, @Specialty, @StudentCount)";

                using (var command = new SqlCommand(query, _dateBaseConnection.GetConnection()))
                {
                    command.Parameters.AddWithValue("@Name", group.Name);
                    command.Parameters.AddWithValue("@Course", group.Course);
                    command.Parameters.AddWithValue("@Specialty", group.Specialty);
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

        // Метод для получения групп
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

        public void Dispose()
        {
            CloseConnection();
        }
    }

}