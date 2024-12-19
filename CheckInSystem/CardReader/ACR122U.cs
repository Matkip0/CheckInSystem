using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using CheckInSystem.Models;
using CheckInSystem.ViewModels;
using CheckInSystem.Views.Windows;
using Dapper;
using FrApp42.ACR122U;
using PCSC;
using PCSC.Iso7816;


namespace CheckInSystem.CardReader;

using Database;

public class ACR122U
{
    public static readonly Reader Reader = new Reader();
    
    [Flags]
    public enum LedStateControl
    {
        BlinkingMaskGreen = 0b1000_0000,
        BlinkingMastRed = 0b0100_0000,
        InitialblinkingStateGreen = 0b0010_0000,
        InitialblinkingStateRed = 0b0001_0000,
        StateMaskGreen = 0b0000_1000,
        StateMaskRed = 0b0000_0100,
        FinalStateGreen = 0b0000_0010,
        FinalStateRed = 0b0000_0001,
    }
    
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
        try
        {
            CardScanned(uid);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private static void OnCardRemoved()
    {
        Debug.WriteLine("Card removed");
    }

    private static bool SignalError()
    {
        var ctx = ContextFactory.Instance.Establish(SCardScope.System);
        var name = Reader.GetDevice();

        var isoReader = new IsoReader(
            context: ctx,
            readerName: name,
            mode: SCardShareMode.Shared,
            protocol: SCardProtocol.Any,
            releaseContextOnDispose: true);
        var apdu = new CommandApdu(IsoCase.Case3Short, isoReader.ActiveProtocol) {
            CLA = 0xFF,
            Instruction = 0x00,
            P1 = 0x40,
            P2 = (byte) LedStateControl.BlinkingMastRed,
            Data = new byte[]
            {
                0x1, //T1 Duration Initial Blinking State (Unit = 100 ms)
                0x2, //T2 Duration Toggle Blinking State (Unit = 100 ms)
                0x2, //Number of repetition
                0x2, //Link to Buzzer (00h - 03h)
            },
        };

        var response = isoReader.Transmit(apdu);
        
        return response.SW1 == 0x90;
    }
    
    private static void CardScanned(string cardID)
    {
        if (cardID == "")
        {
            SignalError();
            return;
        }
        
        if (State.UpdateNextEmployee)
        {
           UpdateNextEmployee(cardID);
           return;
        }

        if (State.UpdateCardId)
        {
            UpdateCardId(cardID);
            return;
        }

        string insertQuery = "EXEC CardScanned @cardID";

        using var connection = Database.GetConnection();
        if (connection == null)
            throw new Exception("Could not establish database connection!");

        connection.Query(insertQuery, new {cardID = cardID});

        UpdateEmployeeLocal(cardID);
    }

    private static void UpdateEmployeeLocal(string cardID)
    {
        Employee? employee = ViewmodelBase.Employees.Where(e => e.CardID == cardID).FirstOrDefault();
        if (employee != null)
        {
            employee.CardScanned(cardID);
        }
        else
        {
            var dbEmployee = Employee.GetFromCardId(cardID);
            if (dbEmployee != null)
            {
                Application.Current.Dispatcher.Invoke( () => {
                    ViewmodelBase.Employees.Add(dbEmployee);
                });
            }
        }
    }

    private static void UpdateNextEmployee(string cardID)
    {
        State.UpdateNextEmployee = false;
        Employee? editEmployee = ViewmodelBase.Employees.Where(e => e.CardID == cardID).FirstOrDefault();
        if (editEmployee == null)
        {
            CardScanned(cardID);
            editEmployee = ViewmodelBase.Employees.Where(e => e.CardID == cardID).FirstOrDefault();
        }
        if (Views.Dialog.WaitingForCardDialog.Instance != null) 
            Application.Current.Dispatcher.Invoke( () => {
                Views.Dialog.WaitingForCardDialog.Instance.Close();
            });
        
        EditEmployeeWindow.Open(editEmployee);
    }

    private static void UpdateCardId(string cardID)
    {
        State.UpdateCard(cardID);
    }
}
