using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes
{
    /// <summary>
    /// Attribute to enable generic test cases
    /// </summary>
    /// <seealso href="https://stackoverflow.com/a/43339950/214898">Class taken from</seealso>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseGenericAttribute : TestCaseAttribute, ITestBuilder
    {
        public TestCaseGenericAttribute(params object[] arguments)
            : base(arguments)
        {
        }
        public Type[] TypeArguments { get; set; } = Array.Empty<Type>();

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test? suite)
        {
            if (!method.IsGenericMethodDefinition)
                return base.BuildFrom(method, suite);

            if (TypeArguments == null || TypeArguments.Length != method.GetGenericArguments().Length)
            {
                var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
                parms.Properties.Set(PropertyNames.SkipReason, $"{nameof(TypeArguments)} should have {method.GetGenericArguments().Length} elements");
                return new[] { new NUnitTestCaseBuilder().BuildTestMethod(method, suite, parms) };
            }

            var genMethod = method.MakeGenericMethod(TypeArguments);
            return base.BuildFrom(genMethod, suite);
        }
    }
}
