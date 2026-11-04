using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Tests.Common.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.AutoComplete;

public class OnlyIAutoCompleteFactory : IErrorRunner, IGetInstance<OnlyIAutoCompleteFactory>, IAutoCompleteFactory
{
    public Type TypeOfException => typeof(NotSupportedException);

    public void ErrorInvoker() => new AutoCompleteAttribute<OnlyIAutoCompleteFactory>();

    public static OnlyIAutoCompleteFactory GetInstance() => new();

    static IAutoCompleteFactory IAutoCompleteFactory.GetInstance() => GetInstance();
}
