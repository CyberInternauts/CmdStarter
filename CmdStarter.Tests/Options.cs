using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes.ClassExclusion;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Commands")]
    [Category("Options")]
    public class Options
    {
        private Lib.Starter starter;

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

        [TestCase<OptHandling>(nameof(OptHandling.MyOptInt), OptHandling.MY_OPT_INT_KEBAB, 111, 999)]
        [TestCase<OptHandling>(nameof(OptHandling.MyOptBool), OptHandling.MY_OPT_BOOL_KEBAB, false, true)]
        [TestCase<OptHandling>(nameof(OptHandling.MyOptListInt), OptHandling.MY_OPT_LIST_INT_KEBAB, null!, new int[] { 11, 22 })]
        [TestCase<OptByInterface>(nameof(OptByInterface.IntOpt), OptByInterface.INT_OPT_KEBAB, 111, 999)]
        public async Task IsPropertyFilledWithOption<CommandType>(string propertyName, string optionName, object defaultValue, object expectedValue) where CommandType : class, IStarterCommand
        {
            var commandType = typeof(CommandType);
            var commandName = commandType.Name.PascalToKebabCase();
            var propertyTested = commandType.GetProperty(propertyName);

            // Test without option for default value
            starter.Namespaces = starter.Namespaces.Add(commandType.Namespace!);
            await starter.Start(new string[] { commandName });
            var optionCommand = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(optionCommand, Is.Not.Null);
            Assert.That(propertyTested!.GetValue(optionCommand.UnderlyingCommand), Is.EqualTo(defaultValue));

            // Test with option
            starter = TestsCommon.CreateCmdStarter();
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);

            var args = TestsCommon.PrintOptionAsIs(optionName, expectedValue).Split(" ", 2).ToList();
            args.Insert(0, commandName);
            await starter.Start(args.ToArray());

            optionCommand = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(optionCommand, Is.Not.Null);
            Assert.That(propertyTested!.GetValue(optionCommand.UnderlyingCommand), Is.EqualTo(expectedValue));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, false)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.INT_OPT_KEBAB, true)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.STRING_OPT_KEBAB, false)]
        public void HasRequiredOption<OptClass>(string optionName, bool isRequired) where OptClass : IStarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>();
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            Assert.That(option, Is.Not.Null);
            Assert.That(option.IsRequired, Is.EqualTo(isRequired));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, "")]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, OptComplete.STRING_OPT_DESC)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.INT_OPT_KEBAB, "")]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.STRING_OPT_KEBAB, OptCompleteByInterface.STRING_OPT_DESC)]
        public void HasOptionDescription<OptClass>(string optionName, string description) where OptClass : IStarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>();
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            Assert.That(option, Is.Not.Null);
            Assert.That(option.Description, Is.EqualTo(description));
        }

        [TestCase<OptComplete>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.STRING_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.DATE_OPT_KEBAB, true)]
        [TestCase<OptComplete>(OptComplete.PRIVATE_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.PROTECTED_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.READ_ONLY_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.WRITE_ONLY_OPT_KEBAB, false)]
        [TestCase<OptComplete>(OptComplete.STATIC_OPT_KEBAB, false)]
        [TestCase<OptCompleteDerived>(OptComplete.INT_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptComplete.STRING_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptComplete.DATE_OPT_KEBAB, true)]
        [TestCase<OptCompleteDerived>(OptCompleteDerived.NEW_INT_OPT_KEBAB, true)]
        [TestCase<OptByInterface>(OptByInterface.INT_OPT_KEBAB, true)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.INT_OPT_KEBAB, true)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.STRING_OPT_KEBAB, true)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.DATE_OPT_KEBAB, true)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.PRIVATE_OPT_KEBAB, false)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.PROTECTED_OPT_KEBAB, false)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.READ_ONLY_OPT_KEBAB, false)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.WRITE_ONLY_OPT_KEBAB, false)]
        [TestCase<OptCompleteByInterface>(OptCompleteByInterface.STATIC_OPT_KEBAB, false)]
        public void EnsuresOptionsAreProperlyCreated<OptClass>(string optionName, bool shallBePresent) where OptClass : class, IStarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>();
            Assert.That(optionCommand, Is.Not.Null);

            var option = optionCommand.Options.FirstOrDefault(o => o.Name == optionName);
            if (shallBePresent)
            {
                Assert.That(option, Is.Not.Null);
            }
            else
            {
                Assert.That(option, Is.Null);
            }
        }

        [TestCase<OptByLackOfAttribute>]
        [TestCase<OptByAttribute>]
        [TestCase<OptByIncludeAttribute>]
        [TestCase<OptByExcludeAttribute>]
        [TestCase<OptAllPropertiesExcluded>]
        [TestCase<OptAllPropertiesExcludedWithInheritance>]
        public void EnsureOptionAttributes<OptClass>() where OptClass : class, IStarterCommand, IOptByAttribute
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(OptClass).Namespace!);
            starter.InstantiateCommands();

            var optionCommand = starter.FindCommand<OptClass>();
            Assert.That(optionCommand, Is.Not.Null);

            TestsCommon.AssertIEnumerablesHaveSameElements(
                optionCommand.Options.Select(o => o.Name),
                OptClass.IncludedOptions.Select(o => o.PascalToKebabCase()));

            foreach (var excludedOption in OptClass.ExcludedOptions)
            {
                Assert.That(optionCommand.Options.Any(option =>
                    option.Name.Equals(excludedOption.PascalToKebabCase())), Is.False);
            }
        }
    }
}
