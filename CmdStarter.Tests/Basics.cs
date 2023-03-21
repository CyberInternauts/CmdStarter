using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using System.Text.RegularExpressions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    /// <summary>
    /// Class for tests that doesn't verify the commands responses.
    /// </summary>
    [Category("* All *")]
    [Category("CmdStarter")]
    public class Basics
    {
        private CmdStarter.Lib.Starter starter;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            TestsCommon.GlobalSetup();
        }

        [SetUp]
        public void MethodSetup()
        {
            starter = TestsCommon.CreateCmdStarter();
        }

        [Test]
        [Category("Namespaces")]
        public void EnsureNamespacesCannotBeNull()
        {
            starter.Namespaces = starter.Namespaces.Clear();

            // Initial values
            Assert.That(starter.Namespaces, Is.Not.Null);
            Assert.That(starter.Namespaces, Is.Empty);

            // After setting null
            starter.Namespaces = null!;
            Assert.That(starter.Namespaces, Is.Not.Null);
            Assert.That(starter.Namespaces, Is.Empty);
        }

        [Test]
        [Category("Commands")]
        [Category("Namespaces")]
        public void ChangesOnNamespacesEmptiesCommands()
        {
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);

            TestsCommon.SetDefaultNamespaces(starter);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            TestsCommon.SetDefaultNamespaces(starter);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            starter.Namespaces = starter.Namespaces.Add(this.GetType().Namespace ?? string.Empty);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            starter.Namespaces = starter.Namespaces.RemoveAt(starter.Namespaces.Count - 1); // "Add" have to be done before!!!
            TestsCommon.AssertCommandsAreEmpty(starter);
        }

        [Test]
        [Category("Commands")]
        [Category("Namespaces")]
        public void AddsEmptyNamespace()
        {
            starter.FindCommandsTypes();
            var initialCommands = new List<Type>(starter.CommandsTypes);

            starter.Namespaces = starter.Namespaces.Add(string.Empty);
            starter.FindCommandsTypes();
            Assert.That(initialCommands, Has.Count.EqualTo(starter.CommandsTypes.Count));
            Assert.That(initialCommands.Except(starter.CommandsTypes), Is.Empty);
        }

        [Test]
        [Category("Namespaces")]
        public void ThrowsInvalidNamespace()
        {
            starter.Namespaces = starter.Namespaces.Add("com2.cyberint");
            Assert.Throws<InvalidNamespaceException>(starter.FindCommandsTypes);
        }

        [Test]
        [Category("Commands")]
        [Category("Namespaces")]
        public void UsesNamespaceExclusion()
        {
            var listingNamespace = typeof(Commands.Listing.List).Namespace;
            starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + listingNamespace);
            starter.FindCommandsTypes();

            Assert.That(starter.CommandsTypes, Is.Not.Empty);
            Assert.That(starter.CommandsTypes.Where(t => t.Namespace == listingNamespace), Is.Empty);
        }

        [Test]
        [Category("Classes")]
        [Category("Commands")]
        public void ChangesOnClassesEmptiesCommands()
        {
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);

            TestsCommon.SetDefaultClasses(starter);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            TestsCommon.SetDefaultClasses(starter);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            starter.Classes = starter.Classes.Add(this.GetType().Name);
            TestsCommon.AssertCommandsAreEmpty(starter);

            starter.InstantiateCommands();
            TestsCommon.AssertCommandsExists(starter);
            starter.Classes = starter.Classes.RemoveAt(starter.Classes.Count - 1); // "Add" have to be done before!!!
            TestsCommon.AssertCommandsAreEmpty(starter);
        }

        [Test]
        [Category("Classes")]
        public void ThrowsWhenClassesFoundNothing()
        {
            //TODO: Change InvalidNamespaceException to NoCommandFoundException, but ensure if doing so
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesExclusion()
        {
            const string ExcludedClassName = nameof(Commands.Filtering.Starter);

            starter.Classes = starter.Classes.Add(TestsCommon.EXCLUSION_SYMBOL + ExcludedClassName);
            starter.FindCommandsTypes();

            Assert.That(!starter.CommandsTypes.Any(x => x.Name.Equals(ExcludedClassName)));
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesExclusion_Wildcard()
        {
            const string ExcludedClassName = nameof(Commands.Filtering.Starter);

            starter.Classes = starter.Classes.Add(TestsCommon.EXCLUSION_SYMBOL + ExcludedClassName + TestsCommon.MULTI_ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            Assert.That(!starter.CommandsTypes.Any(x => x.Name.StartsWith(ExcludedClassName)));
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithoutWildcard(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            IEnumerable<Type> expectedTypes = types.Where(t => t.Name.Equals(IncludedClassName));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(IncludedClassName);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_SingleCharWildcard(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            Regex matcher = new Regex(IncludedClassName + @".$", RegexOptions.RightToLeft);

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.Name));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(IncludedClassName + TestsCommon.ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_AnyCharWildcard(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            Regex matcher = new Regex(IncludedClassName + @"\w*$", RegexOptions.RightToLeft);

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.Name));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(IncludedClassName + TestsCommon.MULTI_ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithoutWildCard(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            string includedNamespace = nameof(Commands.Filtering.A.IO).Split(".").Last();
            string finalFilter = $"{includedNamespace}.{IncludedClassName}";

            Regex matcher = new Regex(includedNamespace + @"\." + IncludedClassName, RegexOptions.RightToLeft);

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardAfter(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{includedNamespace}{TestsCommon.MULTI_ANY_CHAR_SYMBOL}.{IncludedClassName}";

            Regex matcher = new Regex(@$"(\.|^){includedNamespace}.*\.{IncludedClassName}");

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardBefore(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{TestsCommon.MULTI_ANY_CHAR_SYMBOL}{includedNamespace}.{IncludedClassName}";

            Regex matcher = new Regex(@$".*{includedNamespace}\.{IncludedClassName}");

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardWithin(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{includedNamespace[0]}{TestsCommon.MULTI_ANY_CHAR_SYMBOL}{includedNamespace[^1]}.{IncludedClassName}";

            Regex matcher = new Regex(@$"(\.|^){includedNamespace[0]}.*{includedNamespace[^1]}\.{IncludedClassName}");

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardNoDotsWithin(string namespaceFilter, IEnumerable<Type> types)
        {
            const string NamespaceFilter = nameof(Commands.Filtering);
            string finalFilter = $"{NamespaceFilter}.*";

            Regex matcher = new Regex(@$"(\.|^){NamespaceFilter}\.\w*$");

            IEnumerable<Type> expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [TestCaseSource(nameof(FilterClasses))]
        public void UseClassesNoFilter(IEnumerable<Type> types)
        {
            const string MainNamespaceFilter = nameof(Commands.Filtering) + "**";

            starter.Classes = starter.Classes.Add(MainNamespaceFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(types, starter.CommandsTypes);
        }

        [TestCaseSource(nameof(FSource))]
        [Category("Classes")]
        public void TestingTest(Func<Type, bool> f)
        {
            var a = 1;
        }

        private static IEnumerable<TestCaseData> FSource
        {
            get
            {
                var first = new TestCaseData(new Func<Type, bool>(x => x.FullName?.Contains("Word1") ?? false));
                first.SetArgDisplayNames("AAA1");
                var second = new TestCaseData(new Func<Type, bool>(x => x.FullName?.Contains("Word2") ?? false));
                second.SetArgDisplayNames("AAA2");

                //return new List<TestCaseData>() { first, second};

                yield return first;
                yield return second;
            }
        }

        private static IEnumerable<TestCaseData> FilterClasses
        {
            get
            {
                string namespaceFilter = typeof(Commands.Filtering.Starter).Namespace ?? string.Empty;

                IList<Type> types = new List<Type>
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

                yield return new(namespaceFilter, types);
            }
        }
    }
}