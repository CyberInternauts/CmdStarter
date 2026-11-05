namespace CyberInternauts.CmdStarter.Tests.Commands.MethodGroups;

[Command("admin")]
public sealed class AdminCommands
{
    [Command]
    public static int Status() => 12;
}

[Command("admin users")]
public sealed class AdminUserCommands
{
    [Command]
    public static int Create() => 11;
}

public sealed class Tools : StarterCommand
{
}

[Command("tools cleanup")]
public sealed class ToolsCleanupCommands
{
    [Command]
    public static int Run() => 13;
}

public sealed class ReportCommands
{
    [Command("report daily")]
    public static int Daily() => 14;
}

[Command("audit log")]
public sealed class AuditLogCommands
{
    [Command]
    public static int Show() => 15;
}

[Command("zone area")]
public sealed class AreaCommands
{
    [Command]
    public static int List() => 16;
}

[Command("zone")]
public sealed class ZoneCommands
{
    [Command]
    public static int Info() => 17;
}
