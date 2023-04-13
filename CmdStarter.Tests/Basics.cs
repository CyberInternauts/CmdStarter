using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
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
        public void EnsuresNamespacesCannotBeNull()
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
            starter.Classes = starter.Classes.Add(nameof(Commands.Main));
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

            Assert.That(starter.CommandsTypes, Is.Not.Empty);
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

            Assert.That(starter.CommandsTypes, Is.Not.Empty);
            Assert.That(!starter.CommandsTypes.Any(x => x.Name.StartsWith(ExcludedClassName)));
        }

        [TestCaseSource(nameof(ClassesFilter))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesInclusion_WithoutWildcard(string namespaceFilter, IEnumerable<Type> types)
        {
            const string IncludedClassName = nameof(Commands.Filtering.Starter);

            IEnumerable<Type> expectedTypes = types.Where(t => t.Name.Equals(IncludedClassName));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(IncludedClassName);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(starter.CommandsTypes, expectedTypes);
        }

        [TestCaseSource(nameof(ClassesFilterRegexSource))]
        [Category("Classes")]
        [Category("Filters")]
        public void ClassesFilterRegexTest(string namespaceFilter, IEnumerable<Type> types, string regex, string finalFilter)
        {
            Regex matcher = new(regex, RegexOptions.RightToLeft);

            var expectedTypes = types.Where(t => matcher.IsMatch(t.FullName ?? string.Empty));

            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.Classes = starter.Classes.Add(finalFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(starter.CommandsTypes, expectedTypes);
        }

        [TestCaseSource(nameof(ClassesFilter))]
        [Category("Classes")]
        [Category("Filters")]
        public void UsesClassesNoFilter(string namespaceFilter, IEnumerable<Type> types)
        {
            starter.Namespaces = starter.Namespaces.Add(namespaceFilter);
            starter.FindCommandsTypes();

            TestsCommon.AssertIEnumerablesHaveSameElements(starter.CommandsTypes, types);
        }

        [TestCase<Commands.Listing.Types.Folders>("list folders")]
        public void EnsuresFullCommand<CommandType>(string expected) where CommandType : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as CommandType;
            Assert.That(command, Is.Not.Null);
            Assert.That(command.GetFullCommandString(), Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> ClassesFilterRegexSource
        {
            get
            {
                string namespaceFilter = typeof(Commands.Filtering.Starter).Namespace ?? string.Empty;
                IList<Type> types = TestsCommon.CLASS_FILTERING_TYPES;

                string nameofRootStarter = nameof(Commands.Filtering.Starter);

                //FIRST = UsesClassesInclusion_WithinNamespace_WithWildCardNoDotsWithinRegex
                string firstRegex = @$"(\.|^){nameof(Commands.Filtering)}\.\w*$";
                string firstFinalFilter = $"{nameof(Commands.Filtering)}.{TestsCommon.MULTI_ANY_CHAR_SYMBOL}";
                TestCaseData firstCase = new(namespaceFilter, types, firstRegex, firstFinalFilter);
                firstCase.SetArgDisplayNames("1 - UsesClassesInclusion_WithinNamespace_WithWildCardNoDotsWithinRegex");

                yield return firstCase;

                //SECOND = UsesClassesInclusion_WithinNamespace_WithWildCardWithinRegex
                string secondRegex = @$"(\.|^)N\w*S\.{nameofRootStarter}";
                string secondFinalFilter = $"N{TestsCommon.MULTI_ANY_CHAR_SYMBOL}S.{nameofRootStarter}";
                TestCaseData secondCase = new(namespaceFilter, types, secondRegex, secondFinalFilter);
                secondCase.SetArgDisplayNames("2 - UsesClassesInclusion_WithinNamespace_WithWildCardWithinRegex");

                yield return secondCase;

                //THIRD = UsesClassesInclusion_WithinNamespace_WithWildCardBefore
                string thirdRegex = @$"(\.|^)\w*NS\.{nameofRootStarter}";
                string thirdFinalFilter = $"{TestsCommon.MULTI_ANY_CHAR_SYMBOL}NS.{nameofRootStarter}";
                TestCaseData thirdCase = new(namespaceFilter, types, thirdRegex, thirdFinalFilter);
                thirdCase.SetArgDisplayNames("3 - UsesClassesInclusion_WithinNamespace_WithWildCardBefore");

                yield return thirdCase;

                //FOURTH = UsesClassesInclusion_WithinNamespace_WithWildCardAfter
                string fourthRegex = @$"(\.|^)NS\w*\.{nameofRootStarter}";
                string fourthFinalFilter = $"NS{TestsCommon.MULTI_ANY_CHAR_SYMBOL}.{nameofRootStarter}";
                TestCaseData fourthCase = new(namespaceFilter, types, fourthRegex, fourthFinalFilter);
                fourthCase.SetArgDisplayNames("4 - UsesClassesInclusion_WithinNamespace_WithWildCardAfter");

                yield return fourthCase;

                //FIFTH = UsesClassesInclusion_WithinNamespace_WithoutWildCard
                string fifthRegex = @$"IO\.{nameofRootStarter}";
                string fifthFinalFilter = $"IO.{nameofRootStarter}";
                TestCaseData fifthCase = new(namespaceFilter, types, fifthRegex, fifthFinalFilter);
                fifthCase.SetArgDisplayNames("5 - UsesClassesInclusion_WithinNamespace_WithoutWildCard");

                yield return fifthCase;

                //SIXTH = UsesClassesInclusion_AnyCharWildcard
                string sixthRegex = @$"{nameofRootStarter}\w*$";
                string sixthFinalFilter = $"{nameofRootStarter}{TestsCommon.MULTI_ANY_CHAR_SYMBOL}";
                TestCaseData sixthCase = new(namespaceFilter, types, sixthRegex, sixthFinalFilter);
                sixthCase.SetArgDisplayNames("6 - UsesClassesInclusion_AnyCharWildcard");

                yield return sixthCase;

                //SEVENTH = UsesClassesInclusion_SingleCharWildcard
                string seventhRegex = @$"{nameofRootStarter}.$";
                string seventhFinalFilter = $"{nameofRootStarter}{TestsCommon.ANY_CHAR_SYMBOL}";
                TestCaseData seventhCase = new(namespaceFilter, types, seventhRegex, seventhFinalFilter);
                seventhCase.SetArgDisplayNames("7 - UsesClassesInclusion_SingleCharWildcard");

                yield return seventhCase;

                //EIGTH = UsesClassesInclusion_MultiAnyCharIncludeDots
                string eigthRegex = @$"(\.|^)Filtering.*\.IO\.\w*$";
                string eigthFinalFilter = $"Filtering{TestsCommon.MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS}.IO.{TestsCommon.MULTI_ANY_CHAR_SYMBOL}";
                TestCaseData eigthCase = new(namespaceFilter, types, eigthRegex, eigthFinalFilter);
                eigthCase.SetArgDisplayNames("8 - UsesClassesInclusion_MultiCharWithDotsWildcard");

                yield return eigthCase;

                //NINTH = UsesClassesInclusion_AnyCharIncludeDots
                string ninthRegex = @$"(\.|^)Filtering\...IO\.{nameofRootStarter}$";
                string ninthFinalFilter = $"Filtering.{TestsCommon.ANY_CHAR_SYMBOL_INCLUDE_DOTS}{TestsCommon.ANY_CHAR_SYMBOL_INCLUDE_DOTS}IO.{nameofRootStarter}";
                TestCaseData ninthCase = new(namespaceFilter, types, ninthRegex, ninthFinalFilter);
                ninthCase.SetArgDisplayNames("9 - UsesClassesInclusion_CharWithDotsWildcard");

                yield return ninthCase;
            }
        }

        private static IEnumerable<TestCaseData> ClassesFilter
        {
            get
            {
                string namespaceFilter = typeof(Commands.Filtering.Starter).Namespace ?? string.Empty;

                IList<Type> types = TestsCommon.CLASS_FILTERING_TYPES;

                yield return new(namespaceFilter, types);
            }
        }
    }
}