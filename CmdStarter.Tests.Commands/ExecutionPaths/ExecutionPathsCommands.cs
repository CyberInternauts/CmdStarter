using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

// Method commands that exercise the execution paths of Starter: errors, missing option values, services and groups.
// This namespace is excluded from the default discoveries of the tests: a test asks for one class with the Classes filter.
public static class ExecutionLog
{
    public static List<string> Entries { get; } = new();
}

public sealed class PlainService
{
}

public static class ThrowingMethods
{
    [Command("fail-method")]
    public static int Fail() => throw new InvalidOperationException("first line" + Environment.NewLine + "second line");
}

public static class OptionMethods
{
    // None of the options has a value on the command line: a value type gets default(T), a reference type gets null,
    // and a parameter with a default value of null gets that default.
    [Command("show-options")]
    public static int Show([Option] int count, [Option] string name, [Option] string? label = null)
    {
        ExecutionLog.Entries.Add($"count={count};name={name ?? "<null>"};label={label ?? "<null>"}");
        return 0;
    }
}

public static class ArrayMethods
{
    // An array argument has no default value and accepts zero values: absent, it has no parse result and no error.
    [Command("take-values")]
    public static int Take(string[] values)
    {
        ExecutionLog.Entries.Add(values is null ? "values=<null>" : $"values=[{string.Join(",", values)}]");
        return 0;
    }
}

public sealed class PlainMethods : IDisposable
{
    public static PlainMethods? Last { get; set; }

    public bool IsDisposed { get; private set; }

    [Command("plain-instance")]
    public int Instance()
    {
        Last = this;
        return 5;
    }

    [Command("plain-static")]
    public static int Static() => 6;

    [Command("plain-service")]
    public static int WithService(PlainService service) => service is null ? 1 : 0;

    [Command("needs-service")]
    public static int NeedsService([FromServices] PlainService service) => service is null ? 1 : 0;

    public void Dispose() => IsDisposed = true;
}

[Command]
public static class UnnamedGroupMethods
{
    [Command]
    public static int Go() => 7;
}
