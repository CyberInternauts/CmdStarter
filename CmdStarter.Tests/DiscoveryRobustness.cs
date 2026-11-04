using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using System.Reflection;
using System.Reflection.Emit;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// The discovery scans every assembly loaded in the process: one that cannot be read must not stop it.
/// </summary>
[Category("* All *")]
[Category("CmdStarter")]
[Category("Discovery")]
public class DiscoveryRobustness
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public void DynamicAssemblyDoesNotBreakDiscovery()
    {
        // A dynamic assembly whose type is still being defined (a mocking library builds its proxies this way): GetTypes() throws on it.
        var typeBeingDefined = DefineDynamicAssembly();
        try
        {
            var starter = TestsCommon.CreateCmdStarter();

            Assert.DoesNotThrow(starter.FindCommandsTypes);

            Assert.That(starter.CommandsTypes, Is.Not.Empty);
        }
        finally
        {
            typeBeingDefined.CreateType(); // The assembly stays in the process: let it end up readable for the other tests
        }
    }

    [Test]
    public void UnloadableTypesDoNotBreakDiscovery()
    {
        // GetTypes() throws ReflectionTypeLoadException when one type of the assembly does not load.
        var brokenAssembly = LoadAssemblyWithUnloadableType();
        Assert.Throws<ReflectionTypeLoadException>(() => brokenAssembly.GetTypes(), "the fixture assembly must really be unreadable");
        var starter = TestsCommon.CreateCmdStarter();

        Assert.DoesNotThrow(starter.FindCommandsTypes);

        Assert.That(starter.CommandsTypes, Is.Not.Empty);
    }

    [Test]
    public void UnreadableAssembliesDoNotBreakGlobalOptionsDiscovery()
    {
        // The global options types are found once per process and kept in a private static field:
        // the test empties it, so that the next Starter scans again, then puts the previous value back.
        var field = typeof(GlobalOptionsManager).GetField("foundTypes", BindingFlags.NonPublic | BindingFlags.Static)!;
        var typeBeingDefined = DefineDynamicAssembly();
        LoadAssemblyWithUnloadableType();
        var previous = field.GetValue(null);
        try
        {
            field.SetValue(null, null);

            Assert.DoesNotThrow(() => _ = new Starter());

            var found = (IEnumerable<Type>)field.GetValue(null)!;
            Assert.That(found, Does.Contain(typeof(MainGlobalOptions)));
        }
        finally
        {
            field.SetValue(null, previous);
            typeBeingDefined.CreateType();
        }
    }

    // A dynamic assembly with a type that is not created yet.
    private static TypeBuilder DefineDynamicAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CmdStarter.Tests.DynamicDiscovery." + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        return assembly.DefineDynamicModule("main").DefineType("Dynamic.Thing", TypeAttributes.Public | TypeAttributes.Class);
    }

    // An assembly whose class derives from a class of an assembly that does not exist at run time.
    private static Assembly LoadAssemblyWithUnloadableType()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var dependencyName = new AssemblyName("CmdStarter.Tests.AbsentDependency." + suffix);
        var dependency = new PersistedAssemblyBuilder(dependencyName, typeof(object).Assembly);
        var baseType = dependency.DefineDynamicModule(dependencyName.Name!).DefineType("Absent.BaseType", TypeAttributes.Public | TypeAttributes.Class);
        baseType.CreateType();

        var brokenName = new AssemblyName("CmdStarter.Tests.BrokenLibrary." + suffix);
        var broken = new PersistedAssemblyBuilder(brokenName, typeof(object).Assembly);
        var module = broken.DefineDynamicModule(brokenName.Name!);
        module.DefineType("Broken.Derived", TypeAttributes.Public | TypeAttributes.Class, baseType).CreateType();
        module.DefineType("Broken.Fine", TypeAttributes.Public | TypeAttributes.Class).CreateType();

        using var stream = new MemoryStream();
        broken.Save(stream); // the dependency is never saved
        return Assembly.Load(stream.ToArray());
    }
}
