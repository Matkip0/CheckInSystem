﻿using System.Data.SqlClient;
using System.Diagnostics;
using CheckInSystem.Models;
using CheckInSystem.ViewModels;
using Dapper;
using FrApp42.ACR122U;


namespace CheckInSystem.CardReader;

public class ACR122U
{
    public static readonly Reader Reader = new Reader();
    
    public static void StartReader()
    {
        Reader.Connected += OnReaderConnected;
        Reader.Disconnected += OnReaderDisconnected;
        Reader.Inserted += OnCardInserted;
        Reader.Removed += OnCardRemoved;
    }
    
    private static void OnReaderConnected(string value)
    {
        Debug.WriteLine($"New reader connected : {value}");
    }

    private static void OnReaderDisconnected(string value)
    {
        Debug.WriteLine($"Reader disconnected : {value}");
    }

    private static async void OnCardInserted(string uid)
    {
        Debug.WriteLine($"New card detected : {uid}");
        CardScanned(uid);
    }

    private static void OnCardRemoved()
    {
        Debug.WriteLine("Card removed");
    } 
    
    private static void CardScanned(string cardID)
    {
        if (cardID == "") return;

        string insertQuery = "EXEC CardScanned @cardID";
        
        using (var connection = new SqlConnection(Database.Database.ConnectionString))
        {
            connection.Query(insertQuery, new {cardID = cardID});
        }
        UpdateEmployeeLocal(cardID);
    }

    private static void UpdateEmployeeLocal(string cardID)
    {
        Employee? employee = ViewmodelBase.employees.Where(e => e.CardID == cardID).FirstOrDefault();
        if (employee != null)
        {
            employee.CardScanned();
        }
        else
        {
            var dbEmployee = Employee.GetFromCardId(cardID);
            if (dbEmployee != null)
            {
                ViewmodelBase.employees.Add(dbEmployee);
            }
        }
    }
}