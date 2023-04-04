using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public sealed class NonGenericNullCompletion : IHasException, IErrorRunner, IGetDefault<NonGenericNullCompletion>
    {
        public Type TypeOfException => typeof(ArgumentNullException);

        public TestDelegate ErrorRunner => () => new AutoCompleteAttribute(null!, null!);

        public static NonGenericNullCompletion GetDefault() => new NonGenericNullCompletion();
    }
}
