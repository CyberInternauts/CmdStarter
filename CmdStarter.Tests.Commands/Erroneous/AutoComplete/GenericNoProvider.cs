using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class GenericNoProvider : IErrorRunner, IGetInstance<GenericNoProvider>
    {
        const string PARAM_1 = "test";

        public Type TypeOfException => typeof(NotSupportedException);

        public void ErrorInvoker() => new AutoCompleteAttribute<GenericNoProvider>(PARAM_1);

        public static GenericNoProvider GetInstance() => new GenericNoProvider();
    }
}
