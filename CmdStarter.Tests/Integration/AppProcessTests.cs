using System.Diagnostics;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Integration;

[Category("* All *")]
[Category("Integration")]
public sealed class AppProcessTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);

    [Test]
    public async Task CreatesUserWithDefaultOption()
    {
        var result = await RunAppAsync("users", "create", "bob");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("Hello bob (30)"));
    }

    [Test]
    public async Task BindsNamedOption()
    {
        var result = await RunAppAsync("users", "create", "bob", "--age", "42");
        Assert.That(result.StdOut, Does.Contain("Hello bob (42)"));
    }

    [Test]
    public async Task PassesGlobalOption()
    {
        var result = await RunAppAsync("users", "create", "bob", "--verbose");
        Assert.That(result.StdOut, Does.Contain("verbose"));
    }

    [Test]
    public async Task ReturnsAsyncExitCode()
    {
        var result = await RunAppAsync("users", "delete", "7");
        Assert.That(result.ExitCode, Is.EqualTo(7));
    }

    [Test]
    public async Task ReportsHandlerExceptionWithExitCodeOne()
    {
        var result = await RunAppAsync("users", "fail");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StdErr, Does.Contain("InvalidOperationException"));
    }

    [Test]
    public async Task UsesOneScopePerExecution()
    {
        var result = await RunAppAsync("users", "scope");
        Assert.That(result.StdOut, Does.Contain("same-scope"));
    }

    [Test]
    public async Task RunsNestedClassCommand()
    {
        var result = await RunAppAsync("admin", "reset", "--force");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("reset forced"));
    }

    [Test]
    public async Task ShowsHelpWithCommandNames()
    {
        var result = await RunAppAsync("--help");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Does.Contain("users"));
        Assert.That(result.StdOut, Does.Contain("admin"));
    }

    [Test]
    public async Task ShowsVersion()
    {
        var result = await RunAppAsync("--version");
        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.StdOut, Is.Not.Empty);
    }

    [Test]
    public async Task RejectsMissingArgument()
    {
        var result = await RunAppAsync("users", "create");
        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    [Test]
    public async Task RejectsUnknownCommand()
    {
        var result = await RunAppAsync("unknown-command");
        Assert.That(result.ExitCode, Is.Not.Zero);
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunAppAsync(params string[] args)
    {
        var (assemblyPath, runtimeConfigPath) = FindApplicationFiles();
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var argument in args) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        Assert.That(process.Start(), Is.True, "The application process could not be started.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(ProcessTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"The application process exceeded the {ProcessTimeout.TotalSeconds:0}-second timeout. Runtime config: {runtimeConfigPath}");
        }

        return (process.ExitCode, await standardOutputTask, await standardErrorTask);
    }

    private static (string AssemblyPath, string RuntimeConfigPath) FindApplicationFiles()
    {
        var candidates = new List<(string Assembly, string RuntimeConfig)>
        {
            (Path.Combine(AppContext.BaseDirectory, "CmdStarter.Tests.App.dll"),
             Path.Combine(AppContext.BaseDirectory, "CmdStarter.Tests.App.runtimeconfig.json"))
        };

        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = baseDirectory.Parent?.Name ?? "Debug";
        var targetFramework = baseDirectory.Name;
        var projectOutput = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "CmdStarter.Tests.App", "bin", configuration, targetFramework));
        candidates.Add((Path.Combine(projectOutput, "CmdStarter.Tests.App.dll"),
                         Path.Combine(projectOutput, "CmdStarter.Tests.App.runtimeconfig.json")));

        var found = candidates.FirstOrDefault(candidate => File.Exists(candidate.Assembly) && File.Exists(candidate.RuntimeConfig));
        if (found == default)
        {
            Assert.Fail("Could not find CmdStarter.Tests.App.dll and its runtimeconfig. Paths tried:\n" +
                string.Join("\n", candidates.Select(candidate => $"{candidate.Assembly}\n{candidate.RuntimeConfig}")));
        }

        return found;
    }
}
