namespace com.cyberinternauts.csharp.CmdStarter.Tests.Integration;

[Category("* All *")]
[Category("Integration")]
public sealed class SamplesTests
{
    [Test] public async Task HelloWorldGreetsTheWorld() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.HelloWorld"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("Hello, World!")); }
    [Test] public async Task HelloWorldGreetsByName() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.HelloWorld", "--to", "Bob"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("Hello, Bob!")); }
    [Test] public async Task TwoCommandsAdds() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.TwoCommands", "add", "2", "3"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("5")); }
    [Test] public async Task TwoCommandsSubtracts() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.TwoCommands", "subtract", "5", "3"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("2")); }
    [Test] public async Task NestedCommandsAddsRemote() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.NestedCommands", "remote", "add", "origin", "https://example.com/repo.git"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("Added remote origin (https://example.com/repo.git)")); }
    [Test] public async Task NestedCommandsRemovesRemote() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.NestedCommands", "remote", "remove", "origin"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("Removed remote origin")); }
    [Test] public async Task NestedCommandsSetsConfig() { var result = await AppRunner.RunAsync("samples/CmdStarter.Samples.NestedCommands", "config", "set", "color", "blue"); Assert.That(result.ExitCode, Is.Zero); Assert.That(result.StdOut, Does.Contain("color=blue")); }
}
