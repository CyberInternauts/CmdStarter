using CyberInternauts.CmdStarter.Reflection;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("ReflectionCache")]
public class ReflectionCache
{
    [Test]
    public void GetInstanceMethodIsCached()
    {
        // A command that declares GetInstance, and one that uses the default implementation of the interface.
        foreach (var commandType in new[] { typeof(SelfCreatedCommand), typeof(DisposableCommand) })
        {
            var first = Helper.FindGetInstanceMethod(commandType);
            var second = Helper.FindGetInstanceMethod(commandType);

            Assert.That(second, Is.SameAs(first), commandType.Name);
        }

        Assert.That(Helper.FindGetInstanceMethod(typeof(SelfCreatedCommand)).DeclaringType, Is.EqualTo(typeof(SelfCreatedCommand)));
        Assert.That(Helper.FindGetInstanceMethod(typeof(DisposableCommand)).DeclaringType, Is.EqualTo(typeof(IStarterCommand)));
    }
}
