namespace CyberInternauts.CmdStarter.Tests.Integration;

[Category("* All *")]
[Category("Integration")]
public sealed class AppProcessTests
{
    [Test]
    public async Task CreatesUserWithDefaultOption()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "create", "bob");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("Hello bob (30)"));
    }

    [Test]
    public async Task BindsNamedOption()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "create", "bob", "--age", "42");
        Assert.That(result.StdOut, Does.Contain("Hello bob (42)"));
    }

    [Test]
    public async Task PassesGlobalOption()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "create", "bob", "--verbose");
        Assert.That(result.StdOut, Does.Contain("verbose"));
    }

    [Test]
    public async Task ReturnsAsyncExitCode()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "delete", "7");
        Assert.That(result.ExitCode, Is.EqualTo(7));
    }

    [Test]
    public async Task ReportsHandlerExceptionWithExitCodeOne()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "fail");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("integration failure"));
        Assert.That(result.StdErr, Does.Not.Contain("InvalidOperationException"));
    }

    [Test]
    public async Task ReportsHandlerExceptionDetailsWhenEnabled()
    {
        var result = await AppRunner.RunAsync(
            "CmdStarter.Tests.App",
            new Dictionary<string, string?> { ["CMDSTARTER_TESTAPP_DETAILS"] = "1" },
            "users",
            "fail");

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("InvalidOperationException"));
        Assert.That(result.StdErr, Does.Contain("integration failure"));
    }

    [Test]
    public async Task UsesOneScopePerExecution()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "scope");
        Assert.That(result.StdOut, Does.Contain("same-scope"));
    }

    [Test]
    public async Task RunsNestedClassCommand()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "admin", "reset", "--force");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("reset forced"));
    }

    [Test]
    public async Task ShowsHelpWithCommandNames()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "--help");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("users"));
        Assert.That(result.StdOut, Does.Contain("admin"));
    }

    [Test]
    public async Task ShowsVersion()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "--version");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Is.Not.Empty);
    }

    [Test]
    public async Task RejectsMissingArgument()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "users", "create");
        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task RejectsUnknownCommand()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Tests.App", "unknown-command");
        Assert.That(result.ExitCode, Is.Not.Zero);
    }
}

