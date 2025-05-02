using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Contracts.Messages;
using Control;
using Grpc.Core;
using Grpc.Net.Client;
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
    
    private bool _isPaused = false;
    
    public string ToggleText => _isPaused ? "Resume Stream" : "Pause Stream";
    
    // Command to send pause and resume RPCs to the polling agent
    public IRelayCommand ToggleCommand { get; }
    
    // used indicate the connection status to the user 
    [ObservableProperty] private string _status = "Disconnected";
    
    
    [ObservableProperty] private ObservableCollection<PatientEvent> _events;

    public MainViewModel()
    {
                
        var log = Serilog.Log.ForContext<MainViewModel>();
        
        // init collection
        _events = new ObservableCollection<PatientEvent>();
        
        ToggleCommand = new AsyncRelayCommand(ToggleAsync);
        
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
    
    private async Task ToggleAsync()
    {
        try
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5131",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            RemoteCertificateValidationCallback = (_,_,_,_) => true
                        }
                    }
                });

            var client = new CommandChannel.CommandChannelClient(channel);

            if (!_isPaused)
            {
                var ack = await client.PauseStreamAsync(
                    new PauseRequest { Reason = "User clicked pause" });
                if (ack.Success)
                {
                    _isPaused = true;
                    Status    = ack.Message;
                    OnPropertyChanged(nameof(ToggleText));
                }
                else Status = "✗ " + ack.Message;
            }
            else
            {
                var ack = await client.ResumeStreamAsync(
                    new ResumeRequest { Reason = "User clicked resume" });
                if (ack.Success)
                {
                    _isPaused = false;
                    Status    = ack.Message;
                    OnPropertyChanged(nameof(ToggleText));
                }
                else Status = "✗ " + ack.Message;
            }
        }
        catch (RpcException ex)
        {
            Status = $"gRPC error: {ex.StatusCode}";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }
    
}