using System.ComponentModel.Composition.Hosting;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    public static class TestsCommon
    {
        public const int NUMBER_OF_COMMANDS_IN_LISTING = 3; // Using a hardcoded value otherwise the test would be about the same code as the tested one.
        public const string ANY_CHAR_SYMBOL = CmdStarter.Lib.Starter.ANY_CHAR_SYMBOL;
        public const string MULTI_ANY_CHAR_SYMBOL = CmdStarter.Lib.Starter.MULTI_ANY_CHAR_SYMBOL;
        public const string EXCLUSION_SYMBOL = CmdStarter.Lib.Starter.EXCLUSION_SYMBOL;
        public readonly static string ERRONEOUS_NAMESPACE = typeof(Commands.Erroneous.Boggus).Namespace ?? string.Empty;

        public readonly static IList<Type> CLASS_FILTERING_TYPES = new List<Type>
                {
                    typeof(Commands.Filtering.Starter),
                    typeof(Commands.Filtering.StarterA),
                    typeof(Commands.Filtering.StarterB),
                    typeof(Commands.Filtering.StarterOn),
                    typeof(Commands.Filtering.StarterOff),
                    typeof(Commands.Filtering.A.IO.Starter),
                    typeof(Commands.Filtering.B.IO.Starter),
                    typeof(Commands.Filtering.NorthS.Starter),
                    typeof(Commands.Filtering.EastNS.Starter),
                    typeof(Commands.Filtering.NSouth.Starter)
                };

        public static void GlobalSetup()
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced
        }

        public static CmdStarter.Lib.Starter CreateCmdStarter()
        {
            var starter = new CmdStarter.Lib.Starter(); // Reset object to a new one, not to interfer between tests
            SetDefaultNamespaces(starter);
            return starter;
        }

        public static void SetDefaultNamespaces(CmdStarter.Lib.Starter starter)
        {
            starter.Namespaces = starter.Namespaces.Clear();
            if (TestsCommon.ERRONEOUS_NAMESPACE != string.Empty) // Remove, by default, erroneous namespace
            {
                starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + TestsCommon.ERRONEOUS_NAMESPACE);
            }
        }

        public static void SetDefaultClasses(CmdStarter.Lib.Starter starter)
        {
            starter.Classes = starter.Classes.Clear();
        }

        public static void AssertCommandsAreEmpty(CmdStarter.Lib.Starter starter, bool includeTypes = true)
        {
            Assert.Multiple(() =>
            {
                if (includeTypes)
                {
                    Assert.That(starter.CommandsTypes, Is.Empty);
                }
                Assert.That(starter.CommandsTypesTree.Children, Is.Empty);
                Assert.That(starter.RootCommand.Subcommands, Is.Empty);
            });
        }

        public static void AssertCommandsExists(CmdStarter.Lib.Starter starter, bool includeTypes = true)
        {
            Assert.Multiple(() =>
            {
                if (includeTypes)
                {
                    Assert.That(starter.CommandsTypes, Is.Not.Empty);
                }
                Assert.That(starter.CommandsTypesTree.Children, Is.Not.Empty);
                Assert.That(starter.RootCommand.Subcommands, Is.Not.Empty);
            });
        }

        public static void AssertIEnumerablesHaveSameElements<T>(IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Assert.That(actual.Count(), Is.EqualTo(expected.Count()));
            Assert.That(actual.Except(expected), Is.Empty);
        }
    }
}
