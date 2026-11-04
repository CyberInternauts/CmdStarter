using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Tests.Commands.Validation;

public sealed class LevelCommand : StarterCommand
{
    [Range(1, 10)]
    public int Level { get; set; } = 5;

    public override Delegate HandlingMethod => (Func<int>)(() => Level);
}

public sealed class NamedCommand : StarterCommand
{
    [Required]
    public new string? Name { get; set; }

    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}

public sealed class CodeCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<string, int>)(([RegularExpression("^[a-z]+$")] string code) => 0);
}

public sealed class SlotCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int, int>)(([Range(1, 9)] int slot) => slot);
}

public sealed class PickCommands
{
    [Command]
    public static int Pick(
        [AllowedValues("red", "blue")] string color,
        [Option]
        [Range(1, 3)]
        int size = 1) => size;
}

public sealed class ImplicitDefaultCommand : StarterCommand
{
    [Range(1, 3)]
    public int Level { get; set; } = 5;

    public override Delegate HandlingMethod => (Func<int>)(() => Level);
}

public sealed class ImplicitDefaultCommands
{
    [Command]
    public static int Fit([Option][Range(1, 3)] int size = 7) => size;
}

public sealed class AmountCommand : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int, int>)Handle;

    // The default of the argument is outside the allowed range.
    private static int Handle([Range(1, 3)] int amount = 7) => amount;
}

public sealed class ImplicitArgumentCommands
{
    [Command]
    public static int FitArgument([Range(1, 3)] int amount = 7) => amount;
}

// A validation that refuses every value and gives no message at all, not even the default one.
[AttributeUsage(AttributeTargets.Property)]
public sealed class MessagelessAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => new ValidationResult(null);

    public override string FormatErrorMessage(string name) => null!;
}

public sealed class MessagelessCommand : StarterCommand
{
    [Messageless]
    public int Level { get; set; }

    public override Delegate HandlingMethod => (Func<int>)(() => Level);
}

public sealed class LimitOptions : IGlobalOptionsContainer
{
    [Range(0, 3)]
    public int Verbosity { get; set; }
}
