using Control;
using Grpc.Core;

namespace Gateway.Internal;

public class CommandChannelService : CommandChannel.CommandChannelBase
{
    private readonly ILogger<CommandChannelService> _log;
    
    private readonly CommandChannel.CommandChannelClient _agent;

    public CommandChannelService(ILogger<CommandChannelService> log, CommandChannel.CommandChannelClient agent)
    {
        _log = log;
        _agent = agent;
    }

    public override async Task<Ack> PauseStream(PauseRequest req, ServerCallContext ctx)
    {
        _log.LogInformation("Gateway forwarding PauseStream…");
        var ack = await _agent.PauseStreamAsync(req);
        _log.LogInformation("Forward succeeded: {Msg}", ack.Message);
        return ack;
    }
    
    public override async Task<Ack> ResumeStream(ResumeRequest req, ServerCallContext ctx)
    {
        _log.LogInformation("Gateway forwarding ResumeStream: {Reason}", req.Reason);
        var ack = await _agent.ResumeStreamAsync(req);
        _log.LogInformation("Forward succeeded: {Msg}", ack.Message);
        return ack;
    }
}