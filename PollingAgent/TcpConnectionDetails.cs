namespace PollingAgent;

// we'll send data on port 50000 from the simulator, adding the class so we can configure it going forward
public class TcpConnectionDetails
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 50000;
}