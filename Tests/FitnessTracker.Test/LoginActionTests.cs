using FitnessTracker.Auth;
using FitnessTracker.Features;

namespace FitnessTracker.Test;

public class LoginActionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 6, 0, 0, TimeSpan.Zero);

    private static readonly PinAuthOptions Options = new(
        "4711",
        "a-test-signing-key-with-enough-bytes-0123456789",
        180
    );

    private static LoginAction.Handler Handler() =>
        new(Options, new FixedTimeProvider(Now));

    [Fact]
    public void The_right_pin_returns_a_token_that_lasts_the_configured_time()
    {
        var response = Handler().Handle(new LoginAction.Command("4711"));

        response.ShouldNotBeNull();
        response.Token.ShouldNotBeNullOrWhiteSpace();
        response.ExpiresUtc.ShouldBe(Now.AddDays(180));
    }

    [Theory]
    [InlineData("4712")]
    [InlineData("471")]
    [InlineData("")]
    [InlineData(null)]
    public void Any_other_pin_returns_nothing(string? pin) =>
        Handler().Handle(new LoginAction.Command(pin)).ShouldBeNull();
}
