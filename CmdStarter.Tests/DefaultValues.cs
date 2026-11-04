using CyberInternauts.CmdStarter.Tests.Commands.DefaultValues;
using System.CommandLine;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("DefaultValues")]
public sealed class DefaultValues
{
    private static Starter CreateStarter()
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Add(typeof(DefaultsCommand).Namespace!);
        return starter;
    }

    private static Option FindOption(Command command, string name) =>
        command.Options.Single(option => option.Name == name);

    [Test]
    public void PropertyInitialValueBecomesDefault()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var command = starter.FindCommand<DefaultsCommand>()!;

        Assert.That(FindOption(command, "--greeting").HasDefaultValue, Is.True);
        Assert.That(FindOption(command, "--greeting").GetDefaultValue(), Is.EqualTo("hello-default"));
        Assert.That(FindOption(command, "--count").HasDefaultValue, Is.True);
        Assert.That(FindOption(command, "--count").GetDefaultValue(), Is.EqualTo(3));
    }

    [Test]
    public void TypeDefaultsAreNotShown()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var command = starter.FindCommand<DefaultsCommand>()!;

        Assert.Multiple(() =>
        {
            Assert.That(FindOption(command, "--zero").HasDefaultValue, Is.False);
            Assert.That(FindOption(command, "--missing").HasDefaultValue, Is.False);
            Assert.That(FindOption(command, "--empty").HasDefaultValue, Is.False);
            Assert.That(FindOption(command, "--tags").HasDefaultValue, Is.False);
            Assert.That(FindOption(command, "--flag").HasDefaultValue, Is.True);
            Assert.That(FindOption(command, "--flag").GetDefaultValue(), Is.EqualTo(false));
        });
    }

    [Test]
    public async Task RequiredOptionGetsNoDefault()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var option = FindOption(starter.FindCommand<DefaultsCommand>()!, "--needed");

        Assert.That(option.Required, Is.True);
        Assert.That(option.HasDefaultValue, Is.False);
        Assert.That(await starter.Start("defaults-command"), Is.Not.EqualTo(0));
    }

    [Test]
    public void OptionalParameterWithoutValueGetsNoDefault()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();

        var argument = starter.FindCommand<OptionalParameterCommand>()!.Arguments.Single();

        Assert.That(argument.HasDefaultValue, Is.False);
    }

    [Test]
    public void GlobalOptionInitialValueBecomesDefault()
    {
        var starter = CreateStarter();
        starter.InstantiateCommands();
        var region = FindOption(starter.RootCommand, "--region");

        Assert.That(region.HasDefaultValue, Is.True);
        Assert.That(region.GetDefaultValue(), Is.EqualTo("region-default"));
    }

    [Test]
    public async Task UnsetOptionKeepsInitialValue()
    {
        var starter = CreateStarter();

        Assert.That(await starter.Start("defaults-command --needed x"), Is.EqualTo(3));
        Assert.That(await starter.Start("defaults-command --needed x --count 5"), Is.EqualTo(5));
    }

    [Test]
    public async Task BoolInitialValueTrueIsKept()
    {
        var starter = CreateStarter();
        DefaultsCommand? handledCommand = null;
        starter.CommandHandled += (_, eventArgs) => handledCommand = (DefaultsCommand)eventArgs.Instance!;

        var flagOn = FindOptionAfterInstantiation(starter, "--flag-on");
        Assert.That(flagOn.GetDefaultValue(), Is.EqualTo(true));
        Assert.That(await starter.Start("defaults-command --needed x"), Is.EqualTo(3));
        Assert.That(handledCommand!.FlagOn, Is.True);
        Assert.That(starter.GlobalOptionsManager.GetGlobalOptions<DefaultsGlobalOptions>()!.Verbose, Is.True);

        Assert.That(await starter.Start("defaults-command --needed x --flag-on false"), Is.EqualTo(3));
        Assert.That(handledCommand!.FlagOn, Is.False);
    }

    [Test]
    public async Task HelpShowsInitialValue()
    {
        var starter = CreateStarter();
        var previousOutput = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            await starter.Start("defaults-command --help");
        }
        finally
        {
            Console.SetOut(previousOutput);
        }

        Assert.That(output.ToString(), Does.Contain("hello-default"));
        Assert.That(output.ToString(), Does.Not.Contain("needed-default"));
    }

    private static Option FindOptionAfterInstantiation(Starter starter, string name)
    {
        starter.InstantiateCommands();
        return FindOption(starter.FindCommand<DefaultsCommand>()!, name);
    }
}
