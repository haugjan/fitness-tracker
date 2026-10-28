using FitnessTracker.Auth;

namespace FitnessTracker.Features;

public static class LoginAction
{
    public sealed record Command(string? Pin);

    public interface IHandler
    {
        LoginResponse? Handle(Command command);
    }

    internal sealed class Handler(PinAuthOptions options, TimeProvider time) : IHandler
    {
        public LoginResponse? Handle(Command command) =>
            PinAuthentication.PinMatches(command.Pin, options.Pin)
                ? PinAuthentication.IssueToken(options, time.GetUtcNow())
                : null;
    }
}
