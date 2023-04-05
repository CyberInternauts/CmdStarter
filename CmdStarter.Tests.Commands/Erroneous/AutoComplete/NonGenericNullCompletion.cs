using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public sealed class NonGenericNullCompletion : IErrorRunner, IGetInstance<NonGenericNullCompletion>
    {
        public Type TypeOfException => typeof(ArgumentNullException);

        public void ErrorInvoker() => new AutoCompleteAttribute(new object[]{ null! });

        public static NonGenericNullCompletion GetInstance() => new NonGenericNullCompletion();
    }
}
