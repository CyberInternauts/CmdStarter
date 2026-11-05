using CyberInternauts.CmdStarter.Reflection;

namespace CyberInternauts.CmdStarter.Tests;

/// <summary>
/// The characters of a <c>Classes</c> filter that mean something to a regular expression are plain text.
/// </summary>
[Category("* All *")]
[Category("CmdStarter")]
[Category("Filters")]
public class ClassesFilterCharacters
{
    [Test]
    public void ClassesFilterTreatsRegexCharactersAsText()
    {
        // The full name of the nested type holds a '+'. As a regular expression, "ClassesFilterCharacters+Plus" would match the other type.
        Type[] types = [typeof(Plus), typeof(ClassesFilterCharactersPlus)];

        Assert.Multiple(() =>
        {
            Assert.That(Helper.FilterTypesByClasses(types, ["ClassesFilterCharacters+Plus"]), Is.EqualTo(new[] { typeof(Plus) }));
            Assert.That(Helper.FilterTypesByClasses(types, ["~ClassesFilterCharacters+Plus"]), Is.EqualTo(new[] { typeof(ClassesFilterCharactersPlus) }));
            Assert.That(() => Helper.FilterTypesByClasses(types, ["("]), Throws.Nothing);
            Assert.That(Helper.FilterTypesByClasses(types, ["("]), Is.Empty);
            Assert.That(Helper.FilterTypesByClasses(types, ["Plus["]), Is.Empty);
        });
    }

    public sealed class Plus
    {
    }
}

public sealed class ClassesFilterCharactersPlus
{
}
