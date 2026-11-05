using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Benchmarks.Services;

namespace CyberInternauts.CmdStarter.Benchmarks.Commands.Classes;

// Twelve class commands on three levels:
// alpha, beta, gamma / alpha-one, alpha-two, beta-one, beta-two, gamma-one / alpha-one-x, alpha-one-y, beta-one-x, gamma-one-x.

public sealed class Alpha : BenchmarkCommand
{
}

public sealed class Beta : BenchmarkCommand
{
}

public sealed class Gamma : BenchmarkCommand
{
}

[Parent<Alpha>]
public sealed class AlphaOne : BenchmarkCommand
{
}

[Parent<Alpha>]
public sealed class AlphaTwo : BenchmarkCommand
{
}

[Parent<Beta>]
public sealed class BetaOne : BenchmarkCommand
{
}

/// <summary>
/// Has a second constructor that takes a service: dependency injection picks it, the other creations use the parameterless one.
/// </summary>
[Parent<Beta>]
public sealed class BetaTwo : BenchmarkCommand
{
    private readonly IBenchmarkService? service;

    public BetaTwo()
    {
    }

    public BetaTwo(IBenchmarkService service)
    {
        this.service = service;
    }

    protected override int Run(string value) => service?.Handle(value) ?? 0;
}

[Parent<Gamma>]
public sealed class GammaOne : BenchmarkCommand
{
}

[Parent<AlphaOne>]
public sealed class AlphaOneX : BenchmarkCommand
{
}

[Parent<AlphaOne>]
public sealed class AlphaOneY : BenchmarkCommand
{
}

[Parent<BetaOne>]
public sealed class BetaOneX : BenchmarkCommand
{
}

[Parent<GammaOne>]
public sealed class GammaOneX : BenchmarkCommand
{
}
