using MediatR;

namespace TeensyRom.Core.Commands
{
    public class PingCommand : IRequest<PingResult> { }
}
