using System.Data.SqlClient;
using System.Windows;
using CheckInSystem.Models;
using CheckInSystem.ViewModels;
using System.Diagnostics;
using Dapper;

namespace CheckInSystem.Database;

public class Maintenance
{
    public static void CheckOutEmployeesIfTheyForgot()
    {
        List<Employee> updatedEmployees = new List<Employee>();
        foreach (var employee in ViewmodelBase.Employees)
        {
            if (employee.ArrivalTime == null)
                continue;
            var lastCheckInDate = DateOnly.FromDateTime(employee.ArrivalTime.Value);
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            
            if (!Equals(lastCheckInDate, currentDate) && employee.DepartureTime == null)
            {
                Application.Current.Dispatcher.Invoke( () => {
                    employee.DepartureTime = lastCheckInDate.ToDateTime(TimeOnly.Parse("23:50"));
                    employee.IsCheckedIn = false;
                });
                updatedEmployees.Add(employee);
            }
        }
        Task.Run(() =>
        {
            foreach (var employee in updatedEmployees)
                InsrtNewCheckOutTime(employee);
        });
    }

    private static void InsrtNewCheckOutTime(Employee employee)
    {
        string updatequery = @"UPDATE onSiteTime
                            SET departureTime = @DepartureTime
                            WHERE employeeID = @ID AND arrivalTime = @ArrivalTime";
        
        try
        {
            using var connection = Database.GetConnection();
            if (connection == null)
                throw new Exception("Could not establish database connection!");

            var siteTime = connection.Execute(updatequery, employee);
        }
        catch (SqlException e)
        {
            Debug.WriteLine(e);
            MessageBox.Show($"Database Fejl: {e.Message}", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            MessageBox.Show($"Der opstod en fejl: {e.Message}", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void CheckForEndedOffSiteTime()
    {
        List<Employee> updatedEmployees = new List<Employee>();
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
        foreach (var employee in ViewmodelBase.Employees)
        {
            if (employee.OffSiteUntil == null)
                continue;

            if (DateOnly.FromDateTime(employee.OffSiteUntil.Value) < currentDate)
            {
                employee.OffSiteUntil = null;
                employee.IsOffSite = false;
                updatedEmployees.Add(employee);
            }
        }
        
        Task.Run(() =>
        {
            foreach (var employee in updatedEmployees)
                employee.UpdateDb();
        });
    }
}
