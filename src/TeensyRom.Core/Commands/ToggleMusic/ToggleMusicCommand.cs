using MediatR;

namespace TeensyRom.Core.Commands
{
    public class ToggleMusicCommand() : IRequest<ToggleMusicResult>;
}
