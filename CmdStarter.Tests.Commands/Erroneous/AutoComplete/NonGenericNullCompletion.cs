using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.AutoComplete;

public sealed class NonGenericNullCompletion : IErrorRunner, IGetInstance<NonGenericNullCompletion>
{
    public Type TypeOfException => typeof(NotSupportedException);

    public void ErrorInvoker() => new AutoCompleteAttribute([null!]);

    public static NonGenericNullCompletion GetInstance() => new();
}
