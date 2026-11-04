using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using CyberInternauts.CmdStarter.Hosting;

namespace CyberInternauts.CmdStarter.Tests.Integration;

/// <summary>Checks the NuGet packages that the build writes in artefacts/ (the build of the solution produces them).</summary>
[Category("* All *")]
[Category("Packaging")]
public sealed class PackageTests
{
    private const string RepositoryUrl = "https://github.com/CyberInternauts/CmdStarter";

    [Test]
    public void LibraryPackageIsComplete()
    {
        var assembly = typeof(Starter).Assembly;
        using var package = OpenPackage(assembly, "cints.CmdStarter", out var version);
        var metadata = ReadNuspec(package);

        AssertCommonMetadata(metadata, "cints.CmdStarter", version);
        AssertPackageContent(package, assembly);
        Assert.That(package.Entries.Select(entry => entry.FullName), Does.Contain("analyzers/dotnet/cs/CmdStarter.Analyzers.dll"));
        Assert.That(metadata.Descendants().Where(element => element.Name.LocalName == "dependency").Select(dependency => (string?)dependency.Attribute("id")),
            Has.None.Contain("CmdStarter.Analyzers"));
    }

    [Test]
    public void HostingPackageIsComplete()
    {
        var assembly = typeof(CmdStarterHostExtensions).Assembly;
        using var package = OpenPackage(assembly, "cints.CmdStarter.Hosting", out var version);
        var metadata = ReadNuspec(package);

        AssertCommonMetadata(metadata, "cints.CmdStarter.Hosting", version);
        AssertPackageContent(package, assembly);

        // The dependencies sit in groups, one per target framework.
        var library = metadata.Descendants()
            .Where(element => element.Name.LocalName == "dependency" && (string?)element.Attribute("id") == "cints.CmdStarter")
            .ToList();
        Assert.That(library, Has.Count.EqualTo(1));
        Assert.That((string?)library[0].Attribute("version"), Is.EqualTo(version));
        Assert.That(library[0].Parent!.Name.LocalName, Is.EqualTo("group"));
    }

    // The expected version is the one of the assembly (without the source revision after '+'); the package is artefacts/<id>.<version>.nupkg.
    private static ZipArchive OpenPackage(Assembly assembly, string packageId, out string version)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        version = plus < 0 ? informationalVersion : informationalVersion[..plus];

        var artefacts = Path.Combine(RepositoryPaths.Root, "artefacts");
        var packagePath = Path.Combine(artefacts, $"{packageId}.{version}.nupkg");
        Assert.That(File.Exists(packagePath), Is.True, $"Package not found: {packagePath}");
        Assert.That(File.Exists(Path.Combine(artefacts, $"{packageId}.{version}.snupkg")), Is.True, "The symbol package is missing.");
        return ZipFile.OpenRead(packagePath);
    }

    private static XElement ReadNuspec(ZipArchive package)
    {
        var entry = package.Entries.Single(candidate => candidate.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Root!.Elements().Single(element => element.Name.LocalName == "metadata");
    }

    private static string? Value(XElement metadata, string name) =>
        metadata.Elements().SingleOrDefault(element => element.Name.LocalName == name)?.Value;

    private static void AssertCommonMetadata(XElement metadata, string packageId, string version)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(metadata, "id"), Is.EqualTo(packageId));
            Assert.That(Value(metadata, "version"), Is.EqualTo(version));
            Assert.That(Value(metadata, "description"), Is.Not.Null.And.Not.Empty);
            Assert.That(Value(metadata, "authors"), Is.Not.Null.And.Not.Empty);
            Assert.That(Value(metadata, "tags"), Is.Not.Null.And.Not.Empty);

            var license = metadata.Elements().Single(element => element.Name.LocalName == "license");
            Assert.That((string?)license.Attribute("type"), Is.EqualTo("file"));
            Assert.That(license.Value, Is.EqualTo("LICENSE.txt"));

            Assert.That(Value(metadata, "readme"), Is.EqualTo("PKG-README.md"));
            Assert.That(Value(metadata, "icon"), Is.EqualTo("Icon.png"));

            var repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
            Assert.That((string?)repository.Attribute("type"), Is.EqualTo("git"));
            Assert.That((string?)repository.Attribute("url"), Is.EqualTo(RepositoryUrl));
        });
    }

    private static void AssertPackageContent(ZipArchive package, Assembly assembly)
    {
        var name = assembly.GetName().Name;
        var entries = package.Entries.Select(entry => entry.FullName).ToList();
        Assert.That(entries, Does.Contain("PKG-README.md"));
        Assert.That(entries, Does.Contain("LICENSE.txt"));
        Assert.That(entries, Does.Contain("Icon.png"));
        Assert.That(entries, Does.Contain($"lib/net10.0/{name}.dll"));
        Assert.That(entries, Does.Contain($"lib/net10.0/{name}.xml"));
    }
}
