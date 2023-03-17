namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes
{
    /// <summary>
    /// New version of <see cref="NUnit.Framework.TestCaseAttribute"/> that supports a generic parameter
    /// </summary>
    /// <seealso href="https://stackoverflow.com/a/43339950/214898">Class taken from</seealso>
    /// <typeparam name="T"></typeparam>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute<T> : TestCaseGenericAttribute
    {
        public TestCaseAttribute(params object[] arguments)
            : base(arguments) => TypeArguments = new[] { typeof(T) };
    }
}
