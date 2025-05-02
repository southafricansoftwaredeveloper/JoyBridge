using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Contracts.Messages;
using Websocket.Client;

namespace ThinClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    
    // We want a design time context so we can bind to the view model 
#if DEBUG
    public static MainViewModel DesignInstance => new()
    {
        Events =
        {
            new PatientEvent { PatientId="P-123", HeartRate=72, SpO2=98,Timestamp = DateTimeOffset.Now, DeviceId="Sim" }
        }
    };
#endif
    
    
    
    [ObservableProperty] private ObservableCollection<PatientEvent> _events;

    public MainViewModel()
    {
                
        var log = Serilog.Log.ForContext<MainViewModel>();
        
        // init collection
        _events = new ObservableCollection<PatientEvent>();

        // configure web socket connection
        var client = new WebsocketClient(new Uri("ws://localhost:5130/ws"));
        client.MessageReceived.Subscribe(msg =>
        {
            if (msg.Text == null) return;
            
            var ev = JsonSerializer.Deserialize<PatientEvent>(msg.Text);
            
            if (ev == null) return;

            Application.Current.Dispatcher.BeginInvoke(() => Events.Add(ev));
        });

        // handle disconnect
        client.DisconnectionHappened.Subscribe(info =>
        {
            log.Warning($"WS disconnected: {info.Type} / {info.CloseStatus} / {info.Exception}");
        });
        
        // handle reconnect
        client.ReconnectionHappened.Subscribe(info =>
        {
            log.Warning($"WS reconnected: {info.Type}");
        });

        client.Start();
        
        log.Information("ThinClient started.");
    }
}