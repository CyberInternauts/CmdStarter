using NUnit.Framework;

namespace CyberInternauts.CmdStarter.Tests.Integration;

internal static class RepositoryPaths
{
    /// <summary>Gets the repository root: the first folder above the test binaries that contains CmdStarter.sln.</summary>
    internal static string Root
    {
        get
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "CmdStarter.sln"))) root = root.Parent;
            Assert.That(root, Is.Not.Null, "Could not find the repository root containing CmdStarter.sln.");
            return root!.FullName;
        }
    }
}
