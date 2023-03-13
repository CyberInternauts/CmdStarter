using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Naming.MultilevelSame;
using System.ComponentModel.Composition.Primitives;
using System.Security.Cryptography.X509Certificates;

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
        public void UsesClassesExclusion()
        {
            var mainType = typeof(Commands.Main);
            starter.Classes = starter.Classes.Add(TestsCommon.EXCLUSION_SYMBOL + mainType);
            starter.FindCommandsTypes();

            Assert.That(starter.CommandsTypes.Find(x => x.Equals(mainType)), Is.Null);
        }

        [Test]
        [Category("Classes")]
        public void UsesClassesWildcards()
        {
            starter.Classes = starter.Classes.Add(nameof(Do) + "");
            Assert.Fail("NOT DONE");
        }

        [Test]
        [Category("Classes")]
        public void UsesClassesPartialNamespace()
        {
            Assert.Fail("NOT DONE");
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