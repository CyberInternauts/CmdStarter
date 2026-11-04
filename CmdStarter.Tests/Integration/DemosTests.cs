namespace com.cyberinternauts.csharp.CmdStarter.Tests.Integration;

[Category("* All *")]
[Category("Integration")]
public sealed class DemosTests
{
    [Test]
    public async Task SimpleInjectorDemoRunsLonelyCommand()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Demo.SimpleInjector");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("Service2=2"));
        Assert.That(result.StdOut, Does.Contain("MyInt=111"));
    }

    [Test]
    public async Task SimpleInjectorDemoBindsOption()
    {
        var result = await AppRunner.RunAsync("CmdStarter.Demo.SimpleInjector", "--my-int", "5");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("MyInt=5"));
    }

    [Test]
    public async Task ReplDemoUnlocksCommandsAfterLogin()
    {
        var result = await AppRunner.RunWithInputAsync(
            "CmdStarter.Demo.Repl",
            "login admin admin\nadd book1\nlist\nexit\n");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("You are now logged in as admin."));
        Assert.That(result.StdOut, Does.Contain("Book: book1 added."));
        Assert.That(result.StdOut, Does.Contain("book1"));
    }
}

