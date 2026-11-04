using System.Diagnostics;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Integration;

internal sealed record AppResult(int ExitCode, string StdOut, string StdErr)
{
    public string AllOutput => StdOut + StdErr;
}

internal static class AppRunner
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);

    internal static Task<AppResult> RunAsync(string projectPath, params string[] args) => RunAsync(projectPath, null, args);

    internal static async Task<AppResult> RunAsync(string projectPath, IReadOnlyDictionary<string, string?>? environment, params string[] args)
    {
        var projectName = projectPath.Split('/').Last();
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = baseDirectory.Parent?.Name ?? "Debug";
        var targetFramework = baseDirectory.Name;
        var root = baseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CmdStarter.sln"))) root = root.Parent;
        Assert.That(root, Is.Not.Null, "Could not find the repository root containing CmdStarter.sln.");
        var assemblyPath = Path.GetFullPath(Path.Combine(root!.FullName, projectPath.Replace('/', Path.DirectorySeparatorChar), "bin", configuration, targetFramework, $"{projectName}.dll"));
        var runtimeConfigPath = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");
        if (!File.Exists(assemblyPath) || !File.Exists(runtimeConfigPath))
            Assert.Fail($"Could not find application files. Paths tried:\n{assemblyPath}\n{runtimeConfigPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (environment is not null)
            foreach (var entry in environment)
                if (entry.Value is null) startInfo.Environment.Remove(entry.Key);
                else startInfo.Environment[entry.Key] = entry.Value;
        startInfo.ArgumentList.Add(assemblyPath);
        foreach (var argument in args) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        Assert.That(process.Start(), Is.True, "The application process could not be started.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(ProcessTimeout);
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail($"The application process exceeded the {ProcessTimeout.TotalSeconds:0}-second timeout. Runtime config: {runtimeConfigPath}");
        }
        return new AppResult(process.ExitCode, await standardOutputTask, await standardErrorTask);
    }
}
