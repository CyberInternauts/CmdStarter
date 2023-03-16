using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Naming.MultilevelSame;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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

            Assert.That(starter.CommandsTypes.Find(x => x.Name.Equals(ExcludedClassName)), Is.Null);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesExclusion_Wildcard()
        {
            const string ExcludedClassName = nameof(Commands.Filtering.Starter);

            starter.Classes = starter.Classes.Add(TestsCommon.EXCLUSION_SYMBOL + ExcludedClassName + TestsCommon.MULTI_ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            Assert.That(starter.CommandsTypes.Find(x => x.Name.StartsWith(ExcludedClassName)), Is.Null);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithoutWildcard()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && t.Name.Equals(IncludedClassName);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(IncludedClassName);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_SingleCharWildcard()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            Regex matcher = new Regex(IncludedClassName + @".$", RegexOptions.RightToLeft);

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.Name);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(IncludedClassName + TestsCommon.ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_AnyCharWildcard()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            Regex matcher = new Regex(IncludedClassName + @"\w*$", RegexOptions.RightToLeft);

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.Name);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(IncludedClassName + TestsCommon.MULTI_ANY_CHAR_SYMBOL);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithoutWildCard()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            string includedNamespace = nameof(Commands.Filtering.A.IO).Split(".").Last();
            string finalFilter = $"{includedNamespace}.{IncludedClassName}";

            Regex matcher = new Regex(includedNamespace + @"\." + IncludedClassName, RegexOptions.RightToLeft);

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.FullName ?? string.Empty);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardAfter()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{includedNamespace}{TestsCommon.MULTI_ANY_CHAR_SYMBOL}.{IncludedClassName}";

            Regex matcher = new Regex(@$"(\.|^){includedNamespace}.*\.{IncludedClassName}");

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.FullName ?? string.Empty);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardBefore()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{TestsCommon.MULTI_ANY_CHAR_SYMBOL}{includedNamespace}.{IncludedClassName}";

            Regex matcher = new Regex(@$".*{includedNamespace}\.{IncludedClassName}");

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.FullName ?? string.Empty);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
        }

        [Test]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithinNamespace_WithWildCardWithin()
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);
            const string includedNamespace = "NS";
            string finalFilter = $"{includedNamespace[0]}{TestsCommon.MULTI_ANY_CHAR_SYMBOL}{includedNamespace[^1]}.{IncludedClassName}";

            Regex matcher = new Regex(@$"(\.|^){includedNamespace[0]}.*{includedNamespace[^1]}\.{IncludedClassName}");

            Type[] expectedTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                {
                    return t.IsClass && !t.IsAbstract
                        && t.IsSubclassOf(typeof(StarterCommand))
                        && t.Namespace != null
                        && matcher.IsMatch(t.FullName ?? string.Empty);
                })
                .ToArray();

            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(expectedTypes, starter.CommandsTypes);
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
    }
}