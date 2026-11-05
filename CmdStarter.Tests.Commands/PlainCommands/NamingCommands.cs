namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

[Command]
public sealed class ImplicitName
{
    public int Run() => 11;
}

[Command("custom-name")]
public sealed class ExplicitName
{
    public int Run() => 12;
}

[Command("PascalName")]
public sealed class ConvertedName
{
    public int Run() => 13;
}

// The only public instance methods are the handler and the ones that every object or disposable object has: the handler is Run.
[Command]
public sealed class ThrowawayPlain : IDisposable
{
    public int Run()
    {
        PlainLog.Entries.Add("run");
        return 21;
    }

    public void Dispose() => PlainLog.Entries.Add("dispose");

    public override string ToString() => "throwaway";

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => 1;
}
