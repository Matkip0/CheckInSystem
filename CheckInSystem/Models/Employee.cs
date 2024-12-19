using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dapper;
using System.Data.SqlClient;
using CheckInSystem.Database;

namespace CheckInSystem.Models;


public class Employee : INotifyPropertyChanged
{
    public int ID { get; private set; }
    private string _cardID;
    public string CardID
    {
        get => _cardID;
        set => SetProperty(ref _cardID, value);
    }
    
    private string? _firstName;
    public string? FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    private string? _middleName;
    public string? MiddleName
    {
        get => _middleName;
        set => SetProperty(ref _middleName, value);
    }
    
    private string? _lastName;

    public string? LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }
    
    private bool _isOffSite;
    public bool IsOffSite
    {
        get => _isOffSite;
        set => SetProperty(ref _isOffSite, value);
    }
    
    public DateTime? OffSiteUntil { get; set; }

    private bool _isCheckedIn;
    public bool IsCheckedIn
    {
        get => _isCheckedIn;
        set => SetProperty(ref _isCheckedIn, value);
    }

    private DateTime? _arrivalTime;
    public DateTime? ArrivalTime
    {
        get => _arrivalTime;
        set => SetProperty(ref _arrivalTime, value);
    }

    private DateTime? _departureTime;
    public DateTime? DepartureTime
    {
        get => _departureTime;
        set => SetProperty(ref _departureTime, value);
    }
    
    public void CardScanned(string cardID)
    {
        Employee? tempEmployee = GetFromCardId(cardID);
        if (tempEmployee == null) return;

        SetProperty(ref _arrivalTime, tempEmployee.ArrivalTime, nameof(ArrivalTime));
        SetProperty(ref _departureTime, tempEmployee.DepartureTime, nameof(DepartureTime));
        SetProperty(ref _isCheckedIn, tempEmployee.IsCheckedIn, nameof(IsCheckedIn));
    }
    
    public static List<Employee> GetAllEmployees()
    {
        string selectQuery = @"SELECT employee.ID, cardid, firstname, middlename, lastname, isoffsite, offsiteuntil, arrivaltime, departuretime,
            [dbo].[IsEmployeeCheckedIn](employee.ID) as IsCheckedIn
            FROM employee
            LEFT JOIN dbo.onSiteTime on onSiteTime.ID = (
            SELECT TOP (1) ID 
            FROM OnSiteTime
            WHERE employee.ID = OnSiteTime.employeeID
            ORDER BY arrivalTime DESC)";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        var employees = connection.Query<Employee>(selectQuery).ToList();
        return employees;
    }

    public static Employee? GetFromCardId(string cardID)
    {
        string selectQuery = @"SELECT employee.ID, cardid, firstname, middlename, lastname, isoffsite, offsiteuntil, arrivaltime, departuretime,
            [dbo].[IsEmployeeCheckedIn](employee.ID) as IsCheckedIn
            FROM employee
            LEFT JOIN dbo.onSiteTime on onSiteTime.ID = (
            SELECT TOP (1) ID 
            FROM OnSiteTime
            WHERE employee.ID = OnSiteTime.employeeID
            ORDER BY arrivalTime DESC)
            WHERE cardID = @cardID";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        var employees = connection.Query<Employee>(selectQuery, new {cardID = cardID}).FirstOrDefault();
        return employees;
    }

    public void GetUpdatedSiteTimes()
    {
        string selectQuery = @"Select TOP(1) * FROM onSiteTime
                        WHERE employeeID = @ID
                        ORDER BY arrivalTime desc";

        try
        {
            using var connection = Database.Database.GetConnection();
            if (connection == null)
                throw new Exception("Could not establish database connection!");
            
            var siteTime = connection.QuerySingle<OnSiteTime>(selectQuery, this);

            ArrivalTime = siteTime.ArrivalTime;
            DepartureTime = siteTime.DepartureTime;
        }
        catch (Exception)
        {
            ArrivalTime = null;
            DepartureTime = null;
        }
    }

    public void UpdateDb()
    {
        string updateQuery = @"
            UPDATE employee
            SET cardID = @CardID,
            firstName = @FirstName,
            middleName = @MiddleName,
            lastName = @LastName,
            isOffSite = @IsOffSite,
            offSiteUntil = @OffSiteUntil
            WHERE ID = @id";
        
        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(updateQuery, this);
    }

    public void DeleteFromDb()
    {
        string deletionQuery = @"DELETE employee WHERE ID = @ID";
        
        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(deletionQuery, this);
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

