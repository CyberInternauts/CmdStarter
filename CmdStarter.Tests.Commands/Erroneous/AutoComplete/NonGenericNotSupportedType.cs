using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class NonGenericNotSupportedType : IErrorRunner, IGetInstance<NonGenericNotSupportedType>
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public TestDelegate ErrorRunner => () => new AutoCompleteAttribute(typeof(StarterCommand));

        public static NonGenericNotSupportedType GetInstance() => new NonGenericNotSupportedType();
    }
}
