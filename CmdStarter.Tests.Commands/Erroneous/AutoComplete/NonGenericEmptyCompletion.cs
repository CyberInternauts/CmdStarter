using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public sealed class NonGenericEmptyCompletion : IErrorRunner, IGetInstance<NonGenericEmptyCompletion>
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public void ErrorInvoker() => new AutoCompleteAttribute(string.Empty);

        public static NonGenericEmptyCompletion GetInstance() => new();
    }
}
