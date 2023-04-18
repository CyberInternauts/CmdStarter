using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class OnlyIAutoCompleteFactory : IErrorRunner, IGetInstance<OnlyIAutoCompleteFactory>, IAutoCompleteFactory
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public void ErrorInvoker() => new AutoCompleteAttribute<OnlyIAutoCompleteFactory>();

        public static OnlyIAutoCompleteFactory GetInstance() => new();

        static IAutoCompleteFactory IAutoCompleteFactory.GetInstance() => GetInstance();
    }
}
