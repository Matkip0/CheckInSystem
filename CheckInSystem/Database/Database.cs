namespace CheckInSystem.Database;

using System.Diagnostics;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Windows;
using Microsoft.Win32;

public static class Database
{
    private const string SQL_SERVICE_NAME = "MSSQL$SQLEXPRESS";
    private const int CONNECTION_TIMEOUT = 30;
    private const int RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 1000;

    public static string ConnectionString = $"Server=localhost\\SQLEXPRESS;Database=CheckInSystem;Trusted_Connection=True;Connect Timeout={CONNECTION_TIMEOUT};";
    public static SqlConnection Connection = new SqlConnection(ConnectionString);

    public static bool EnsureDatabaseAvailable()
    {
        try
        {
            if (!IsSqlServerInstalled())
            {
                MessageBox.Show("Microsoft SQL Server er ikke installeret!",
                    "Database Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!EnsureSqlServiceRunning())
            {
                MessageBox.Show("Kunne ikke starte SQL Serveren, kontakt IT support!",
                    "Database Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return TestDatabaseConnection();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);

            MessageBox.Show($"Database opstartsfejl: {e.Message}",
                "Database Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public static SqlConnection GetConnection()
    {
        SqlConnection connection = new SqlConnection(ConnectionString);
        
        for (int attempt = 1; attempt <= RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                return connection;
            }
            catch (SqlException e)
            {
                Debug.WriteLine(e);

                if (attempt == RETRY_ATTEMPTS)
                {
                    throw new Exception($"Failed to connect to database after {RETRY_ATTEMPTS} attempts: {e.Message}");
                }

                Thread.Sleep(RETRY_DELAY_MS);
            }
        }
        
        return null;
    }

    private static bool IsSqlServerInstalled()
    {
        return ServiceController
            .GetServices()
            .Any(sc => sc.ServiceName.Equals(SQL_SERVICE_NAME));
    }

    private static bool EnsureSqlServiceRunning()
    {
        try
        {
            using (ServiceController sc = new ServiceController(SQL_SERVICE_NAME))
            {
                var isRunning = sc.Status == ServiceControllerStatus.Running;
                if (isRunning) return true;

                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                   sc.Start();
                   sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }

                return isRunning;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);

            return false;
        }
    }

    private static bool TestDatabaseConnection()
    {
        try
        {
            using var connection = Database.GetConnection();
            if (connection == null) return false;

            // Execute a simple query to test the connection.
            using (var command = new SqlCommand("SELECT 1", connection))
            {
                command.ExecuteScalar();

                return true;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);

            return false;
        }
    }
}
