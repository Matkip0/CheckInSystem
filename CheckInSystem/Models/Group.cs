using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CheckInSystem.Database;
using Dapper;

namespace CheckInSystem.Models;


public class Group : INotifyPropertyChanged
{
    public int ID { get; private set; }
    
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private bool _isvisible;
    public bool Isvisible
    {
        get => _isvisible;
        set => SetProperty(ref _isvisible, value);
    }
    
    public ObservableCollection<Employee> Members { get; private set; }

    public static List<Group> GetAllGroups(List<Employee> employees)
    {
        string selectQueryGroups = @"SELECT * FROM [group]";
        string selectQueryEmployeesInGroup = @"SELECT employeeID FROM employeeGroup WHERE groupID = {0}";
        
        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        var groups = connection.Query<Group>(selectQueryGroups).ToList();

        foreach (var group in groups)
        {
            string formattedQuery = string.Format(selectQueryEmployeesInGroup, group.ID);
            var employeeIDs = connection.Query<int>(formattedQuery).ToList();
            group.Members = new ObservableCollection<Employee>();

            foreach (var employeeID in employeeIDs)
            {
                var temp = employees.Where(i => i.ID == employeeID).FirstOrDefault();
                if (temp != null)
                {
                    group.Members.Add(temp);
                }
            }
        }

        return groups;
    }

    public static Group NewGroup(String name)
    {
        string insertQuery = @"INSERT INTO [group] (name) VALUES (@name)
            SELECT SCOPE_IDENTITY();";
        Group newGroup = new Group()
        {
            Name = name,
            Members = new ObservableCollection<Employee>()
        };

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        newGroup.ID = connection.QueryFirst<int>(insertQuery, new {name = name});

        return newGroup;
    }

    public void RemoveGroupDb()
    {
        string deletionQuery = @"DELETE [group] WHERE ID = @ID";
        
        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(deletionQuery, new { ID = this.ID });
    }

    public void UpdateName(string name)
    {
        string updateQuery = @"UPDATE [group] 
            SET name = @name
            WHERE ID = @ID";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(updateQuery, new {name = name, ID = this.ID});

        this.Name = name;
    }

    public void Updatevisibility(bool visibility)
    {
        string updateQuery = @"UPDATE [group] 
            SET isvisible = @isvisible
            WHERE ID = @ID";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(updateQuery, new {isvisible = Isvisible, ID = this.ID});
    }
    
    public void AddEmployee(Employee employee)
    {
        if (Members.Contains(employee)) return;
        
        string insertQuery = @"INSERT INTO employeeGroup (employeeID, groupID) VALUES (@employeeID, @groupID)";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(insertQuery, new { employeeID = employee.ID, @groupID = this.ID });

        this.Members.Add(employee);
    }

    public void RemoveEmployee(Employee employee)
    {
        if (!Members.Contains(employee)) return;
        
        string deletionQuery = @"DELETE employeeGroup WHERE employeeID = @employeeID AND groupID = @groupID";

        using var connection = Database.Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(deletionQuery, new { employeeID = employee.ID, @groupID = this.ID });

        this.Members.Remove(employee);
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
