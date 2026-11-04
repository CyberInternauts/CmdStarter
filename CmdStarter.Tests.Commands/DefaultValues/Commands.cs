using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace CyberInternauts.CmdStarter.Tests.Commands.DefaultValues;

public sealed class DefaultsCommand : StarterCommand
{
    public string Greeting { get; set; } = "hello-default";
    public int Count { get; set; } = 3;
    public int Zero { get; set; }
    public string? Missing { get; set; }
    public string Empty { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public bool Flag { get; set; }
    public bool FlagOn { get; set; } = true;

    [Required]
    public string Needed { get; set; } = "needed-default";

    public override Delegate HandlingMethod => (Func<int>)(() => Count);
}

public sealed class OtherCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}

public sealed class OptionalParameterCommand : StarterCommand
{
    public override Delegate HandlingMethod => Handle;

    // [Optional] without a value: ParameterInfo.DefaultValue is Missing.Value and HasDefaultValue is false.
    private static int Handle([Optional] string text) => text is null ? 0 : 1;
}
