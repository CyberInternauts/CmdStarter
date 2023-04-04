using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class NonGenericNotSupportedType : IHasException, IErrorRunner, IGetDefault<NonGenericNotSupportedType>
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public TestDelegate ErrorRunner => () => new AutoCompleteAttribute(typeof(StarterCommand));

        public static NonGenericNotSupportedType GetDefault() => new NonGenericNotSupportedType();
    }
}
