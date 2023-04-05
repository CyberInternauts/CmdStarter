using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class GenericWrongFactory : IErrorRunner, IGetInstance<GenericWrongFactory>
    {
        const string PARAM_1 = "test";

        public Type TypeOfException => typeof(InvalidCastException);

        public TestDelegate ErrorRunner => () => new AutoCompleteAttribute<GenericWrongFactory>(PARAM_1);

        public static GenericWrongFactory GetInstance() => new GenericWrongFactory();
    }
}
