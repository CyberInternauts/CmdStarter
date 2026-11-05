using CyberInternauts.CmdStarter.Reflection;
using CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;
using CyberInternauts.CmdStarter.Tests.Commands.Sequences;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Reflection")]
public class ReflectionHelperFilters
{
    private const string SomeNamespace = "Some.Namespace";

    // A type parameter has no FullName.
    private static readonly Type TypeWithoutFullName = typeof(List<>).GetGenericArguments()[0];

    [Test]
    public void OnlyPropertiesThatCanBeReadAndWrittenAreOptions()
    {
        var names = Helper.GetProperties(typeof(WriteOnlyProperty)).Select(property => property.Name);

        Assert.That(names, Is.EqualTo(new[] { nameof(WriteOnlyProperty.Readable) }));
    }

    [Test]
    public void NamespaceFilterReturnsWhatItWasGivenWhenThereIsNothingToFilter()
    {
        var types = new[] { typeof(Ok), typeof(Starter) };

        Assert.That(Helper.FilterTypesByNamespaces(Array.Empty<Type>(), [SomeNamespace]), Is.Empty);
        Assert.That(Helper.FilterTypesByNamespaces(types, []), Is.EqualTo(types));
    }

    [Test]
    public void TypeWithoutNamespaceIsOutsideEveryIncludedNamespaceAndInsideEveryOtherExclusion()
    {
        var types = new[] { typeof(GlobalNamespaceCommand), typeof(Ok) };

        var included = Helper.FilterTypesByNamespaces(types, [typeof(Ok).Namespace!]);
        var excluded = Helper.FilterTypesByNamespaces(types, [Starter.ExclusionSymbol + SomeNamespace]);

        Assert.That(included, Is.EqualTo(new[] { typeof(Ok) }));
        Assert.That(excluded, Is.EqualTo(types));
    }

    [Test]
    public void TypeWithoutFullNameMatchesNoClassFilterAndNoClassExclusion()
    {
        var types = new[] { TypeWithoutFullName, typeof(Ok) };

        var included = Helper.FilterTypesByClasses(types, [nameof(Ok)]);
        var excluded = Helper.FilterTypesByClasses(types, [Starter.ExclusionSymbol + nameof(Ok)]);

        Assert.That(TypeWithoutFullName.FullName, Is.Null);
        Assert.That(included, Is.EqualTo(new[] { typeof(Ok) }));
        Assert.That(excluded, Is.EqualTo(new[] { TypeWithoutFullName }));
    }
}
