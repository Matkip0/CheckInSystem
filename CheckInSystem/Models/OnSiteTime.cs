using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using CheckInSystem.Database;
using Dapper;

namespace CheckInSystem.Models;

public class OnSiteTime : INotifyPropertyChanged
{
    private int _id;
    public int Id
    {
        get => _id;
        private set => SetProperty(ref _id, value);
    }

    private int _employeeID;
    public int EmployeeID
    {
        get => _employeeID;
        set => SetProperty(ref _employeeID, value);
    }

    private DateTime? _oldArrivalTime;
    private DateTime? _arrivalTime;
    public DateTime? ArrivalTime
    {
        get => _arrivalTime;
        set => SetProperty(ref _arrivalTime, value);
    }

    private DateTime? _oldDepartureTime;
    private DateTime? _departureTime;
    public DateTime? DepartureTime
    {
        get => _departureTime;
        set => SetProperty(ref _departureTime, value);
    }

    public OnSiteTime()
    {
        
    }
    public OnSiteTime(int id, int employeeId, DateTime arrivalTime, DateTime? departureTime)
    {
        _id = id;
        EmployeeID = employeeId;
        ArrivalTime = arrivalTime;
        DepartureTime = departureTime;
        _oldArrivalTime = arrivalTime;
        _oldDepartureTime = departureTime;
    } 
    public OnSiteTime(OnSiteTime newOnSiteTime)
    {
        _id = newOnSiteTime.Id;
        EmployeeID = newOnSiteTime.EmployeeID;
        ArrivalTime = newOnSiteTime.ArrivalTime;
        DepartureTime = newOnSiteTime.DepartureTime;
        _oldArrivalTime = newOnSiteTime.ArrivalTime;
        _oldDepartureTime = newOnSiteTime.DepartureTime;
    }

    public static List<OnSiteTime> GetOnsiteTimesForEmployee(Employee employee)
    {
        string selectQuery = @"SELECT * FROM onSiteTime 
            where employeeID = @employeeID";
        
        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        var onSiteTimes = connection.Query<OnSiteTime>(selectQuery, new { employeeID = employee.ID })
            .Select(t => new OnSiteTime(t)).ToList();
            
        return onSiteTimes;
    }

    public bool IsChanged()
    {
        if (_oldArrivalTime == ArrivalTime) return true;
        if (_oldDepartureTime == DepartureTime) return true;
        return false;
    }

    public void RevertTopreviousTime()
    {
        ArrivalTime = _oldArrivalTime;
        DepartureTime = _oldDepartureTime;
    }

    public void DeleteFromDb()
    {
        string deletionQuery = @"DELETE FROM onSiteTime WHERE ID = @id";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(deletionQuery, new { id = Id });
    }

    public static void UpdateMutipleSiteTimes(List<OnSiteTime> siteTimes)
    {
        string UpdateQuery = @"UPDATE onSiteTime SET 
                      arrivalTime = @ArrivalTime,
                      departureTime = @DepartureTime
                      WHERE ID = @Id";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Execute(UpdateQuery, siteTimes);
    }

    public static OnSiteTime AddTimeToDb(int employeeId, DateTime arrivalTime, DateTime? departureTime)
    {
        string insertQuery = @"INSERT INTO onSiteTime (employeeID, arrivalTime, departureTime)
                        VALUES (@employeeId, @arrivalTime, @departureTime)
                        SELECT SCOPE_IDENTITY()";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        var siteTimeId = connection.ExecuteScalar<int>(insertQuery, new { employeeId, arrivalTime, departureTime });

        return new OnSiteTime(siteTimeId, employeeId, arrivalTime, departureTime);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    protected void SetProperty<T>(ref T variable, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(variable, value))
        {
            variable = value;
            OnPropertyChanged(propertyName);
        }
    }
}
