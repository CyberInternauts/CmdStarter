using CyberInternauts.CmdStarter.Tests.Commands.GlobalOptions;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
using CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;
using CyberInternauts.CmdStarter.Tests.Commands.PlainCommands.Erroneous;
using CyberInternauts.CmdStarter.Tests.Commands.States;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace CyberInternauts.CmdStarter.Tests;

// A plain command is a class that carries [Command], declares no [Command] method and has one public instance method: the model by classes applies to it.
[Category("* All *")]
[Category("PlainCommands")]
public class PlainCommands
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [SetUp]
    public void Setup() => PlainLog.Reset();

    [Test]
    public void NameIsTheKebabCaseOfTheClassOrTheExplicitName()
    {
        var starter = CreateStarter(typeof(ImplicitName), typeof(ExplicitName), typeof(ConvertedName));

        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EquivalentTo(new[] { "implicit-name", "custom-name", "pascal-name" }));
    }

    [Test]
    public void PlainCommandsAreCommandsOfTheClassModel()
    {
        var starter = CreateStarter(typeof(ImplicitName), typeof(ExplicitName));

        starter.InstantiateCommands();

        Assert.That(starter.CommandsTypes, Is.EquivalentTo(new[] { typeof(ImplicitName), typeof(ExplicitName) }));
        Assert.That(starter.MethodCommandsTypes, Is.Empty);
    }

    [Test]
    public void PlainCommandsAreFoundWithoutTheCatalogToo()
    {
        var starter = CreateStarter(typeof(ImplicitName), typeof(ExplicitName));
        starter.UseCommandCatalogs = false;

        starter.InstantiateCommands();

        Assert.That(starter.CommandsTypes, Is.EquivalentTo(new[] { typeof(ImplicitName), typeof(ExplicitName) }));
    }

    [Test]
    public void NameWithASpaceIsRefused()
    {
        var starter = CreateStarter(typeof(SpaceNamePlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain(typeof(SpaceNamePlain).FullName));
    }

    [Test]
    public void BlankNameIsRefused()
    {
        var starter = CreateStarter(typeof(BlankNamePlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain(typeof(BlankNamePlain).FullName).And.Contain("empty"));
    }

    [Test]
    public void ClassWithoutMethodIsRefused()
    {
        var starter = CreateStarter(typeof(ZeroMethodsPlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain(typeof(ZeroMethodsPlain).FullName));
    }

    [Test]
    public void ClassWithTwoMethodsIsRefused()
    {
        var starter = CreateStarter(typeof(TwoMethodsPlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain(typeof(TwoMethodsPlain).FullName));
    }

    [Test]
    public void ClassOfBothModelsIsRefused()
    {
        var starter = CreateStarter(typeof(BothModelsPlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("both command models").And.Contain(typeof(BothModelsPlain).FullName));
    }

    [Test]
    public void GenericMethodIsRefused()
    {
        var starter = CreateStarter(typeof(GenericMethodPlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("generic").And.Contain(typeof(GenericMethodPlain).FullName));
    }

    [Test]
    public void RefParameterIsRefused()
    {
        var starter = CreateStarter(typeof(RefParameterPlain));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("ref").And.Contain(typeof(RefParameterPlain).FullName));
    }

    [Test]
    public async Task MethodsOfObjectAndDisposeAreNotHandlers()
    {
        var starter = CreateStarter(typeof(ThrowawayPlain), typeof(ImplicitName));

        Assert.That(await starter.Start("throwaway-plain"), Is.EqualTo(21));
        // The first disposal is the one of the description instance, released when the tree is built.
        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "dispose", "run", "dispose" }));
    }

    [Test]
    public void PropertiesAreOptionsWithTheirInitialValueAsDefault()
    {
        var starter = CreateStarter(typeof(OptionsPlain), typeof(ImplicitName));

        starter.InstantiateCommands();
        var command = starter.FindCommand<OptionsPlain>()!;

        Assert.That(command.Options.Where(option => option is not Option<bool> || option.Name != "--help").Select(option => option.Name), Does.Contain("--greeting").And.Contain("--times").And.Contain("--loud"));
        Assert.That(TestsCommon.FindOption(command, "greeting")!.HasDefaultValue, Is.True);
        Assert.That(TestsCommon.FindOption(command, "greeting")!.GetDefaultValue(), Is.EqualTo("Hello"));
        Assert.That(TestsCommon.FindOption(command, "times")!.GetDefaultValue(), Is.EqualTo(1));
    }

    [Test]
    public async Task OptionsAreBoundToTheProperties()
    {
        var starter = CreateStarter(typeof(OptionsPlain), typeof(ImplicitName));

        Assert.That(await starter.Start("options-plain world --greeting Hi --times 3 --loud"), Is.EqualTo(31));
        Assert.That(await starter.Start("options-plain again"), Is.EqualTo(31));

        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "Hi:world:3:True", "Hello:again:1:False" }));
    }

    [Test]
    public void PropertiesOfBaseClassesAreOptions()
    {
        var starter = CreateStarter(typeof(InheritingPlain), typeof(ImplicitName));

        starter.InstantiateCommands();

        Assert.That(starter.FindCommand<InheritingPlain>()!.Options.Select(option => option.Name), Does.Contain("--inherited").And.Contain("--own"));
    }

    [Test]
    public void NotOptionPropertyIsExcluded()
    {
        var starter = CreateStarter(typeof(ExcludedPlain), typeof(ImplicitName));

        starter.InstantiateCommands();
        var names = starter.FindCommand<ExcludedPlain>()!.Options.Select(option => option.Name).ToList();

        Assert.That(names, Does.Contain("--shown"));
        Assert.That(names, Does.Not.Contain("--excluded"));
    }

    [Test]
    public async Task HandlerNamesTheMethodThatRuns()
    {
        var starter = CreateStarter(typeof(DesignatedPlain), typeof(NamedDesignatedPlain), typeof(InheritedHandlerPlain));

        starter.InstantiateCommands();

        Assert.That(starter.RootCommand.Subcommands.Select(command => command.Name), Is.EquivalentTo(new[] { "designated-plain", "named-designated", "inherited-handler-plain" }));
        Assert.That(await starter.Start("designated-plain"), Is.EqualTo(81));
        Assert.That(await starter.Start("named-designated"), Is.EqualTo(83));
        Assert.That(await starter.Start("inherited-handler-plain"), Is.EqualTo(82));
    }

    [TestCase(typeof(OverloadedHandlerPlain), "ambiguous")]
    [TestCase(typeof(MissingHandlerPlain), "Missing")]
    [TestCase(typeof(PrivateHandlerPlain), "Run")]
    [TestCase(typeof(StaticHandlerPlain), "Run")]
    [TestCase(typeof(GenericHandlerPlain), "generic")]
    [TestCase(typeof(RefHandlerPlain), "ref")]
    [TestCase(typeof(HandlerOnGroup), "Handler")]
    [TestCase(typeof(HandlerOnMethod), "Handler")]
    public void InvalidHandlerIsAConstructionError(Type type, string expected)
    {
        var starter = CreateStarter(type);

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain(expected).And.Contain(type.FullName));
    }
    [Test]
    public async Task ParametersAreArguments()
    {
        var starter = CreateStarter(typeof(ArgumentsPlain), typeof(ImplicitName));

        starter.InstantiateCommands();
        var command = starter.FindCommand<ArgumentsPlain>()!;
        Assert.That(command.Arguments.Select(argument => argument.Name), Is.EqualTo(new[] { "text", "count" }));
        Assert.That(command.Arguments[0].Description, Is.EqualTo("The text"));
        Assert.That(command.Arguments[1].HasDefaultValue, Is.True);

        Assert.That(await starter.Start("arguments-plain abc"), Is.EqualTo(2));
        Assert.That(await starter.Start("arguments-plain abc 5"), Is.EqualTo(5));
        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "abc:2", "abc:5" }));
    }

    [Test]
    public async Task TokenAndConsoleAreNotArguments()
    {
        var starter = CreateStarter(typeof(TokenAndConsolePlain), typeof(ImplicitName));
        using var output = new StringWriter();
        var console = new CommandConsole(TextReader.Null, output, TextWriter.Null);

        starter.InstantiateCommands();
        Assert.That(starter.FindCommand<TokenAndConsolePlain>()!.Arguments, Is.Empty);

        Assert.That(await starter.Start("token-and-console-plain", console), Is.Zero);
        Assert.That(output.ToString(), Does.Contain("plain console"));
    }

    [Test]
    public async Task CancellationReachesTheHandler()
    {
        var starter = CreateStarter(typeof(TokenAndConsolePlain), typeof(ImplicitName));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        var code = await starter.Start("token-and-console-plain --wait", new CommandConsole(TextReader.Null, TextWriter.Null, TextWriter.Null), cancellation.Token);

        Assert.That(code, Is.EqualTo(130));
    }

    [Test]
    public async Task GlobalOptionsContainerIsAParameter()
    {
        var starter = CreateStarter(typeof(ContainerPlain), typeof(ImplicitName), typeof(MainGlobalOptions));

        starter.InstantiateCommands();
        Assert.That(starter.FindCommand<ContainerPlain>()!.Arguments, Is.Empty);

        Assert.That(await starter.Start("container-plain --int-global-option 5"), Is.EqualTo(5));
        Assert.That(await starter.Start("container-plain"), Is.EqualTo(MainGlobalOptions.INT_GLOBAL_OPTION_VALUE));
    }

    [Test]
    public void ContainerThatTheFiltersExcludeIsAConstructionError()
    {
        var starter = CreateStarter(typeof(ContainerPlain), typeof(ImplicitName));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("'globals'").And.Contain(typeof(ContainerPlain).FullName).And.Contain(typeof(MainGlobalOptions).FullName));
    }

    [Test]
    public void FromServicesNeedsAServiceProvider()
    {
        var starter = CreateStarter(typeof(ServicesPlain), typeof(ImplicitName));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("FromServices").And.Contain(typeof(ServicesPlain).FullName));
    }

    [Test]
    public async Task ParametersFollowTheClassificationOfMethodCommands()
    {
        using var provider = new ServiceCollection().AddScoped<ScopedService>().AddTransient<TransientService>().BuildServiceProvider();
        var starter = CreateStarter(typeof(ServicesPlain), typeof(ImplicitName));
        starter.ServiceProvider = provider;

        starter.InstantiateCommands();
        var command = starter.FindCommand<ServicesPlain>()!;
        Assert.That(command.Options.Select(option => option.Name), Does.Contain("--level"));
        Assert.That(command.Arguments.Select(argument => argument.Name), Is.EqualTo(new[] { "name" }));

        Assert.That(await starter.Start("services-plain bob --level 4"), Is.EqualTo(4));
        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "bob:4" }));
        Assert.That(PlainLog.Received, Has.Count.EqualTo(2));
        Assert.That(PlainLog.Received[0], Is.TypeOf<TransientService>());
        Assert.That(PlainLog.Received[1], Is.TypeOf<ScopedService>());
    }

    [TestCase(typeof(ExitInt), "exit-int", 7)]
    [TestCase(typeof(ExitTask), "exit-task", 0)]
    [TestCase(typeof(ExitTaskInt), "exit-task-int", 9)]
    [TestCase(typeof(ExitVoid), "exit-void", 0)]
    public async Task ReturnValueGivesTheExitCode(Type type, string name, int expected)
    {
        var starter = CreateStarter(type, typeof(ImplicitName));

        Assert.That(await starter.Start(name), Is.EqualTo(expected));
    }

    [Test]
    public async Task ChildAndParentMayBePlainOrClassCommands()
    {
        var starter = CreateStarter(typeof(TopPlain), typeof(ChildPlain), typeof(ClassTop), typeof(PlainUnderClass), typeof(ClassUnderPlain));

        Assert.That(await starter.Start("top-plain child-plain"), Is.EqualTo(42));
        Assert.That(await starter.Start("class-top plain-under-class"), Is.EqualTo(44));
        Assert.That(await starter.Start("top-plain class-under-plain"), Is.EqualTo(45));
        Assert.That(starter.GetFullCommandString<ChildPlain>(), Is.EqualTo("top-plain child-plain"));
        Assert.That(starter.GetFullCommandString<PlainUnderClass>(), Is.EqualTo("class-top plain-under-class"));
    }

    [Test]
    public async Task FluentTreeAcceptsPlainCommandsAndTheirNames()
    {
        var starter = CreateStarter(typeof(TopPlain));
        starter.UseTree(tree => tree.Add<TopPlain>(top => top.WithName("renamed").Add<ChildPlain>().Add<ImplicitName>()));

        Assert.That(await starter.Start("renamed child-plain"), Is.EqualTo(42));
        Assert.That(await starter.Start("renamed implicit-name"), Is.EqualTo(11));
        Assert.That(starter.GetFullCommandString<ImplicitName>(), Is.EqualTo("renamed implicit-name"));
    }

    [Test]
    public async Task CommandsTypesAcceptsPlainCommands()
    {
        var starter = CreateStarter(typeof(ImplicitName));
        starter.CommandsTypes = [typeof(ImplicitName), typeof(ExplicitName)];

        Assert.That(await starter.Start("custom-name"), Is.EqualTo(12));
        Assert.That(starter.CommandsTypes, Is.EqualTo(new[] { typeof(ImplicitName), typeof(ExplicitName) }));
    }

    [Test]
    public void CommandsTypesRefusesWhatIsNotACommandClass()
    {
        var starter = new Starter();

        Assert.Throws<ArgumentException>(() => starter.CommandsTypes = [typeof(string)]);
        Assert.Throws<ArgumentException>(() => starter.CommandsTypes = [typeof(PlainBase)]);
    }

    [Test]
    public async Task LonelyPlainCommandIsRooted()
    {
        var starter = CreateStarter(typeof(OptionsPlain));

        Assert.That(await starter.Start("world --greeting Yo"), Is.EqualTo(31));

        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "Yo:world:1:False" }));
        Assert.That(starter.RootCommand.Options.Select(option => option.Name), Does.Contain("--greeting"));
    }

    [Test]
    public async Task StatesHideAndRefusePlainCommands()
    {
        var provider = new TestStateProvider { CurrentState = TestState.Anonymous };
        var starter = CreateStarter(typeof(MemberPlain), typeof(ImplicitName));
        starter.StateProvider = provider;

        Assert.That(await starter.Start("member-plain"), Is.EqualTo(1));
        Assert.That(starter.FindCommand<MemberPlain>()!.Hidden, Is.True);

        provider.CurrentState = TestState.Member;
        Assert.That(await starter.Start("member-plain"), Is.EqualTo(51));
        Assert.That(starter.FindCommand<MemberPlain>()!.Hidden, Is.False);
    }

    [Test]
    public void HiddenAliasAndDescriptionApply()
    {
        var starter = CreateStarter(typeof(DecoratedPlain), typeof(ImplicitName));

        starter.InstantiateCommands();
        var command = starter.FindCommand<DecoratedPlain>()!;

        Assert.That(command.Hidden, Is.True);
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "dec", "decorated" }));
        Assert.That(command.Description, Is.EqualTo("A decorated plain command"));
    }

    [Test]
    public async Task ValidationAttributesApply()
    {
        var starter = CreateStarter(typeof(ValidatedPlain), typeof(ImplicitName));
        using var error = new StringWriter();
        var console = new CommandConsole(TextReader.Null, TextWriter.Null, error);

        Assert.That(await starter.Start("validated-plain abc --level 3", console), Is.Zero);
        Assert.That(await starter.Start("validated-plain abc --level 9", console), Is.Not.Zero);
        Assert.That(await starter.Start("validated-plain toolong --level 3", console), Is.Not.Zero);
    }

    [Test]
    public async Task FilterSeesThePlainInstance()
    {
        CommandContext? captured = null;
        var starter = CreateStarter(typeof(OptionsPlain), typeof(ImplicitName)).AddFilter(new CapturingFilter(context => captured = context));

        await starter.Start("options-plain world --times 4");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Instance, Is.TypeOf<OptionsPlain>());
        Assert.That(((OptionsPlain)captured.Instance!).Times, Is.EqualTo(4));
        Assert.That(captured.Member, Is.EqualTo(typeof(OptionsPlain)));
        Assert.That(captured.CommandPath, Is.EqualTo("options-plain"));
    }

    [Test]
    public async Task CommandHandledGivesThePlainInstance()
    {
        object? instance = null;
        var exitCode = -1;
        var starter = CreateStarter(typeof(OptionsPlain), typeof(ImplicitName));
        starter.CommandHandled += (_, args) => { instance = args.Instance; exitCode = args.ExitCode; };

        await starter.Start("options-plain world");

        Assert.That(instance, Is.TypeOf<OptionsPlain>());
        Assert.That(exitCode, Is.EqualTo(31));
    }

    [Test]
    public async Task ServicesAreInjectedInTheConstructorAndScopedPerExecution()
    {
        using var provider = new ServiceCollection().AddSingleton<SingletonService>().AddScoped<ScopedService>().BuildServiceProvider();
        var starter = CreateStarter(typeof(InjectedPlain), typeof(ImplicitName));
        starter.ServiceProvider = provider;

        await starter.Start("injected-plain");
        PlainLog.Received.Clear();
        await starter.Start("injected-plain");
        var second = PlainLog.Received.ToList();
        PlainLog.Received.Clear();
        await starter.Start("injected-plain");
        var third = PlainLog.Received.ToList();

        Assert.That(second[0], Is.SameAs(third[0]), "the singleton is shared");
        Assert.That(second[1], Is.Not.SameAs(third[1]), "each execution has its own scope");
    }

    [Test]
    public async Task FactoryCreatesThePlainCommand()
    {
        var starter = CreateStarter(typeof(FactoryPlain), typeof(ImplicitName));
        starter.SetFactory(type => type == typeof(FactoryPlain) ? new FactoryPlain("from the factory") : Activator.CreateInstance(type)!);

        Assert.That(await starter.Start("factory-plain"), Is.EqualTo(71));
        Assert.That(PlainLog.Entries, Does.Contain("from the factory"));
    }

    [Test]
    public void FactoryMustReturnAnInstanceOfTheCommandType()
    {
        var starter = CreateStarter(typeof(FactoryPlain), typeof(ImplicitName));
        starter.SetFactory(_ => new object());

        var exception = Assert.Throws<InvalidOperationException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("The command factory returned an instance of 'System.Object' for").And.Contain(typeof(FactoryPlain).FullName));
    }

    [Test]
    public void CommandAndInstanceCanBeFound()
    {
        var starter = CreateStarter(typeof(OptionsPlain), typeof(ImplicitName));

        starter.InstantiateCommands();
        var command = starter.FindCommand<OptionsPlain>()!;

        Assert.That(command.Name, Is.EqualTo("options-plain"));
        Assert.That(starter.FindCommandInstance<OptionsPlain>(), Is.TypeOf<OptionsPlain>());
        Assert.That(starter.FindCommandInstance(command), Is.TypeOf<OptionsPlain>());
        Assert.That(starter.GetFullCommandString<OptionsPlain>(), Is.EqualTo("options-plain"));
        Assert.That(starter.FindCommandInstance<ExitInt>(), Is.Null);
    }

    [Test]
    public void StaticClassWithoutAMethodIsAGroupWithoutCommands()
    {
        // A static class cannot be a plain command: it stays a group of the method model, which needs a [Command] method.
        var starter = CreateStarter(typeof(StaticWithoutMethod));

        var exception = Assert.Throws<InvalidAttributeException>(starter.InstantiateCommands);

        Assert.That(exception!.Message, Does.Contain("must declare at least one command method").And.Contain(typeof(StaticWithoutMethod).FullName));
    }

    [Test]
    public async Task DisposeAsyncIsNotTheHandler()
    {
        var starter = CreateStarter(typeof(AsyncDisposablePlain), typeof(ImplicitName));

        Assert.That(await starter.Start("async-disposable-plain"), Is.EqualTo(91));

        // The description instance and the instance of the execution are both released with DisposeAsync.
        Assert.That(PlainLog.Entries, Is.EqualTo(new[] { "dispose-async", "run", "dispose-async" }));
    }

    [Test]
    public async Task MethodsThatTheCompilerGeneratesForARecordAreNotHandlers()
    {
        var starter = CreateStarter(typeof(RecordPlain), typeof(ImplicitName));

        Assert.That(await starter.Start("record-plain"), Is.EqualTo(92));
    }

    private static Starter CreateStarter(params Type[] types)
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().AddRange(types.Select(type => type.FullName!));
        return starter;
    }

    private sealed class CapturingFilter(Action<CommandContext> capture) : ICommandFilter
    {
        public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
        {
            capture(context);
            return next();
        }
    }
}
