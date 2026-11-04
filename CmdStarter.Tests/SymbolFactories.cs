using System.CommandLine;
using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Catalog;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// The typed symbol factories. The registry is static for the whole process, so each test uses a type of its own, declared here:
/// no test depends on the order of the others, and none of these types is in a catalog that the generator wrote.
/// </summary>
[Category("* All *")]
[Category("Catalog")]
public class SymbolFactoryTests
{
    // One type for each test that registers a factory.
    // A struct with a parameterless constructor of its own.
    private readonly struct ConstructedStruct
    {
        public ConstructedStruct()
        {
            Value = 5;
        }

        public int Value { get; }
    }

    private struct UnregisteredStruct { }

    private struct FirstWinsStruct { }

    private struct ReplacedStruct { }

    private struct CatalogRegisteredStruct { }

    [Test]
    public void FallbackFactoryBehavesLikeTheGeneratedOne()
    {
        AssertSameBehavior(7);
        AssertSameBehavior("text");
        AssertSameBehavior(true);
        AssertSameBehavior((int?)5);
        AssertSameBehavior(new[] { "a", "b" });
        AssertSameBehavior(new FileInfo("file.txt"));
    }

    [Test]
    public void DefaultValueRunsTheParameterlessConstructorOfAStruct()
    {
        // A struct can declare a parameterless constructor: the default value is what Activator.CreateInstance gave before factories, not default(T).
        var expected = Activator.CreateInstance(typeof(ConstructedStruct));
        Assert.That(expected, Is.Not.EqualTo(default(ConstructedStruct)));

        Assert.That(SymbolFactories.CreateFallback(typeof(ConstructedStruct)).DefaultValue, Is.EqualTo(expected));
        Assert.That(new SymbolFactory<ConstructedStruct>().DefaultValue, Is.EqualTo(expected));
        Assert.That(new SymbolFactory<string>().DefaultValue, Is.Null);
    }

    [Test]
    public void FallbackIsUsedForAnUnregisteredType()
    {
        var type = typeof(UnregisteredStruct);

        var factory = SymbolFactories.For(type);

        Assert.That(SymbolFactories.IsGenerated(type), Is.False);
        Assert.That(factory.ValueType, Is.EqualTo(type));
        Assert.That(factory.CreateOption("--x"), Is.TypeOf<Option<UnregisteredStruct>>());
        Assert.That(SymbolFactories.For(type), Is.SameAs(factory), "the fallback is made once");
    }

    [Test]
    public void FirstRegistrationWins()
    {
        var first = new SymbolFactory<FirstWinsStruct>();
        var second = new SymbolFactory<FirstWinsStruct>();

        SymbolFactories.Register(first);
        SymbolFactories.Register(second);

        Assert.That(SymbolFactories.IsGenerated(typeof(FirstWinsStruct)), Is.True);
        Assert.That(SymbolFactories.For(typeof(FirstWinsStruct)), Is.SameAs(first));
    }

    [Test]
    public void RegistrationReplacesAFallback()
    {
        var type = typeof(ReplacedStruct);
        var fallback = SymbolFactories.For(type);
        Assert.That(SymbolFactories.IsGenerated(type), Is.False);

        var registered = new SymbolFactory<ReplacedStruct>();
        SymbolFactories.Register(registered);

        Assert.That(SymbolFactories.IsGenerated(type), Is.True);
        Assert.That(SymbolFactories.For(type), Is.SameAs(registered));
        Assert.That(SymbolFactories.For(type), Is.Not.SameAs(fallback));
    }

    [Test]
    public void CatalogRegistersItsSymbolTypes()
    {
        // A catalog read by a Starter registers its symbol types, through the builder.
        var starter = new Starter { Assemblies = ImmutableList.Create<System.Reflection.Assembly>(new CatalogAssembly("SymbolCatalogAssembly", typeof(SymbolTypeCatalog))) };

        starter.FindCommandsTypes();

        Assert.That(SymbolFactories.IsGenerated(typeof(CatalogRegisteredStruct)), Is.True);
        Assert.That(SymbolFactories.For(typeof(CatalogRegisteredStruct)), Is.TypeOf<SymbolFactory<CatalogRegisteredStruct>>());
    }

    private static void AssertSameBehavior<T>(T sample)
    {
        var fallback = SymbolFactories.CreateFallback(typeof(T));
        var generated = new SymbolFactory<T>();

        var fromFallback = Observe(fallback, sample);
        var fromGenerated = Observe(generated, sample);

        Assert.That(fromFallback.OptionType, Is.EqualTo(typeof(Option<T>)));
        Assert.That(fromFallback.ArgumentType, Is.EqualTo(typeof(Argument<T>)));
        Assert.That(fromFallback.OptionType, Is.EqualTo(fromGenerated.OptionType));
        Assert.That(fromFallback.ArgumentType, Is.EqualTo(fromGenerated.ArgumentType));
        Assert.That(fromFallback.ValueType, Is.EqualTo(typeof(T)));
        Assert.That(fromFallback.DefaultValue, Is.EqualTo(default(T)));
        Assert.That(fromFallback.DefaultValue, Is.EqualTo(fromGenerated.DefaultValue));
        Assert.That(fromFallback.OptionValue, Is.EqualTo(sample));
        Assert.That(fromFallback.OptionValue, Is.EqualTo(fromGenerated.OptionValue));
        Assert.That(fromFallback.ArgumentValue, Is.EqualTo(sample));
        Assert.That(fromFallback.ArgumentValue, Is.EqualTo(fromGenerated.ArgumentValue));
    }

    // What a factory gives: the types of its symbols, its default value, and the values read after SetDefault on a command line that does not mention the symbols.
    private static Observation Observe<T>(ISymbolFactory factory, T sample)
    {
        var option = factory.CreateOption("--x");
        var argument = factory.CreateArgument("x");
        factory.SetDefault(option, sample);
        factory.SetDefault(argument, sample);
        var root = new RootCommand();
        root.Options.Add(option);
        root.Arguments.Add(argument);
        var parseResult = root.Parse(Array.Empty<string>());

        return new Observation(factory.ValueType, option.GetType(), argument.GetType(), factory.DefaultValue, factory.GetValue(parseResult, option), factory.GetValue(parseResult, argument));
    }

    private sealed record Observation(Type ValueType, Type OptionType, Type ArgumentType, object? DefaultValue, object? OptionValue, object? ArgumentValue);

    // A catalog written by hand that registers the value type of its test. It declares a command too, so that the search of its assembly finds one.
    public sealed class SymbolTypeCatalog : ICommandCatalog
    {
        public void Describe(ICommandCatalogBuilder builder)
        {
            builder.AddCommand<Commands.Catalog.Listed>();
            builder.AddSymbolType<CatalogRegisteredStruct>();
        }
    }
}
