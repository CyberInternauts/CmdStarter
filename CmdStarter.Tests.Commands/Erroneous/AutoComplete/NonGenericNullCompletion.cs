using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public sealed class NonGenericNullCompletion : IErrorRunner, IGetInstance<NonGenericNullCompletion>
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public void ErrorInvoker() => new AutoCompleteAttribute(new object[]{ null! });

        public static NonGenericNullCompletion GetInstance() => new();
    }
}
