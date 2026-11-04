namespace CyberInternauts.CmdStarter.Tests.Commands.BrokenGlobalOptions;

// Only found when this namespace is asked for: it is excluded from the default discoveries of the tests.
public sealed class ContainerWithoutParameterlessConstructor : IGlobalOptionsContainer
{
    public ContainerWithoutParameterlessConstructor(string region)
    {
        Region = region;
    }

    public string Region { get; set; }
}

public sealed class HealthyCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
