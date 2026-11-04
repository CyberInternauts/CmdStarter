using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Tests.Commands.DefaultValues;

public sealed class DefaultsCommand : StarterCommand
{
    public string Greeting { get; set; } = "hello-default";
    public int Count { get; set; } = 3;
    public int Zero { get; set; }
    public string? Missing { get; set; }
    public string Empty { get; set; } = "";
    public List<string> Tags { get; set; } = new();
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
