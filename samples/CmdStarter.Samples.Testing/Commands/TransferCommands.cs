using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Samples.Testing.Services;

namespace CyberInternauts.CmdStarter.Samples.Testing.Commands;

public sealed class TransferCommands
{
    [Command]
    public static int Transfer(string from, string to, IAccounts accounts, [Option] int amount = 0)
    {
        if (amount <= 0)
        {
            Console.Error.WriteLine("The amount must be positive.");
            return 2;
        }

        if (!accounts.Transfer(from, to, amount))
        {
            Console.Error.WriteLine("Transfer refused.");
            return 1;
        }

        Console.Out.WriteLine($"Transferred {amount} from {from} to {to}.");
        return 0;
    }
}

