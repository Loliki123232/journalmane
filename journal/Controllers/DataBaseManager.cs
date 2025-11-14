using Microsoft.Data.SqlClient;
using System.Data;
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
            return Path.Combine(repoPath, "journal", "journal", "Database1.mdf");
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
        public async Task<bool> AuthenticateUserAsync(string login, string hashedPassword)
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
        public void Dispose()
        {
            CloseConnection();
        }
    }

}