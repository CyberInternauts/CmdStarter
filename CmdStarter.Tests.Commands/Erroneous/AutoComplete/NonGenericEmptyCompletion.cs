using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public sealed class NonGenericEmptyCompletion : IErrorRunner, IGetInstance<NonGenericEmptyCompletion>
    {
        public Type TypeOfException => typeof(ArgumentNullException);

        public TestDelegate ErrorRunner => () => new AutoCompleteAttribute(string.Empty);

        public static NonGenericEmptyCompletion GetInstance() => new NonGenericEmptyCompletion();
    }
}
