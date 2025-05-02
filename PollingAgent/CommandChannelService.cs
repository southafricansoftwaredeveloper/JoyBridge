using Control;
using Grpc.Core;

namespace PollingAgent;

public class CommandChannelService : CommandChannel.CommandChannelBase
{
    private readonly Worker _worker;
    private readonly ILogger<CommandChannelService> _log;

    public CommandChannelService(Worker worker, ILogger<CommandChannelService> log)
        => (_worker, _log) = (worker, log);

    public override Task<Ack> PauseStream(PauseRequest request, ServerCallContext context)
    {
        _log.LogInformation("gRPC PauseStream received: {Reason}", request.Reason);
        _worker.Pause();
        return Task.FromResult(new Ack { Success = true, Message = "Paused" });
    }
    
    public override Task<Ack> ResumeStream(ResumeRequest req, ServerCallContext ctx)
    {
        _log.LogInformation("PollingAgent ResumeStream received: {Reason}", req.Reason);
        _worker.Resume();
        return Task.FromResult(new Ack { Success = true, Message = "Resumed" });
    }
}