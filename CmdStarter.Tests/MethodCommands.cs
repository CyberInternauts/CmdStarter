using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods.Erroneous;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using System.CommandLine;

namespace com.cyberinternauts.csharp.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("Methods")]
public class MethodCommands
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => MethodsLog.Reset();

    [Test]
    public void BuildsGroupWithMethodSubcommands()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); starter.InstantiateCommands();
        var users = Find(starter.RootCommand, "users");
        Assert.That(users.Subcommands.Select(command => command.Name), Is.EqualTo(new[] { "create", "delete", "list" }));
    }

    [Test]
    public void UsesMethodAndParameterDescriptions()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); starter.InstantiateCommands();
        var create = Find(Find(starter.RootCommand, "users"), "create");
        Assert.That(create.Description, Is.EqualTo(UserCommands.CREATE_DESC));
        Assert.That(create.Arguments.Single().Description, Is.EqualTo(UserCommands.NAME_DESC));
    }

    [Test]
    public void ClassifiesParameters()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); starter.InstantiateCommands();
        var users = Find(starter.RootCommand, "users");
        var create = Find(users, "create"); var delete = Find(users, "delete");
        Assert.That(create.Arguments.Select(symbol => symbol.Name), Is.EqualTo(new[] { "name" }));
        Assert.That(create.Options.Select(symbol => symbol.Name), Is.EquivalentTo(new[] { "--age", "--admin" }));
        Assert.That(delete.Arguments.Select(symbol => symbol.Name), Is.EqualTo(new[] { "id" }));
        Assert.That(delete.Arguments.Any(symbol => symbol.Name == "cancellationToken"), Is.False);
    }

    [Test]
    public async Task InvokesInstanceMethodWithArgumentsAndOptions()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands));
        Assert.That(await starter.Start("users create bob --age 42 --admin"), Is.EqualTo(10));
        Assert.That(MethodsLog.Entries, Is.EqualTo(new[] { "create:bob:42:True", "dispose" }));
    }

    [Test]
    public async Task UsesParameterDefaultsWhenOptionsAreAbsent()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); await starter.Start("users create bob");
        Assert.That(MethodsLog.Entries[0], Is.EqualTo("create:bob:30:False"));
    }

    [Test]
    public async Task InvokesAsyncMethod()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands));
        Assert.That(await starter.Start("users delete 5"), Is.EqualTo(20));
        Assert.That(MethodsLog.Entries, Is.EqualTo(new[] { "delete:5", "dispose" }));
    }

    [Test]
    public async Task InvokesStaticMethodWithoutInstance()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands));
        Assert.That(await starter.Start("users list"), Is.Zero);
        Assert.That(MethodsLog.Entries, Is.EqualTo(new[] { "list" }));
    }

    [Test]
    public async Task RaisesCommandHandledForMethods()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); object? instance = new object();
        starter.CommandHandled += (_, args) => instance = args.Instance;
        await starter.Start("users create bob"); Assert.That(instance, Is.TypeOf<UserCommands>());
        await starter.Start("users list"); Assert.That(instance, Is.Null);
    }

    [Test]
    public async Task AddsTopLevelMethodCommands()
    {
        var starter = CreateMethodsStarter(typeof(ToolCommands)); starter.InstantiateCommands();
        Assert.That(await starter.Start("ping"), Is.EqualTo(7));
        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EquivalentTo(new[] { "ping", "status", "wait" }));
    }

    [Test]
    public async Task PassesGlobalOptionsToMethod()
    {
        var starter = CreateMethodsStarter(typeof(ToolCommands), typeof(MainGlobalOptions));
        Assert.That(await starter.Start("status --int-global-option 5"), Is.EqualTo(5));
        Assert.That(await starter.Start("status"), Is.EqualTo(MainGlobalOptions.INT_GLOBAL_OPTION_VALUE));
    }

    [Test]
    public void AppliesMethodAlias()
    {
        var starter = CreateMethodsStarter(typeof(ToolCommands)); starter.InstantiateCommands();
        Assert.That(Find(starter.RootCommand, "status").Aliases, Does.Contain("st"));
    }

    [Test]
    public async Task AwaitsValueTaskResult()
    {
        var starter = CreateMethodsStarter(typeof(ToolCommands)); Assert.That(await starter.Start("wait"), Is.EqualTo(3));
    }

    [Test]
    public async Task CombinesBothModelsWithoutRootingLonelyCommand()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands), typeof(DisposableCommand)); starter.InstantiateCommands();
        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EquivalentTo(new[] { "users", "disposable-command" }));
        Assert.That(await starter.Start("users list"), Is.Zero);
        Assert.That(await starter.Start("disposable-command"), Is.GreaterThan(0));
    }

    [Test]
    public void FindsMethodCommandsWithoutClassCommands()
    {
        var starter = CreateMethodsStarter(typeof(UserCommands)); starter.FindCommandsTypes();
        Assert.That(starter.CommandsTypes, Is.Empty);
        Assert.That(starter.MethodCommandsTypes, Is.EqualTo(new[] { typeof(UserCommands) }));
    }

    [TestCase<GenericMethodCommands>()]
    [TestCase<NonPublicMethodCommands>()]
    [TestCase<RefParameterCommands>()]
    [TestCase<MixedModelCommands>()]
    [TestCase<EmptyGroupCommands>()]
    [TestCase<AbstractMethodCommands>()]
    [TestCase<DuplicateNameCommands>()]
    public void RejectsInvalidMethodCommands<T>()
    {
        var starter = CreateMethodsStarter(typeof(T));
        Assert.Throws<InvalidAttributeException>(() => starter.InstantiateCommands());
    }

    private static Starter CreateMethodsStarter(params Type[] types)
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().AddRange(types.Select(type => type.FullName!));
        return starter;
    }

    private static Command Find(Command parent, string name) => parent.Subcommands.Single(command => command.Name == name);
}
