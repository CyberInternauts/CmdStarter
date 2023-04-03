using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    [Category("* All *")]
    [Category("CmdStarter")]
    [Category("Attributes")]
    public class Attributes
    {
        private Starter starter;

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

        [TestCase<NoAlias>(NoAlias.OPTION_NAME)]
        [TestCase<OneAlias>(OneAlias.OPTION_NAME)]
        [TestCase<MultiAlias>(MultiAlias.OPTION_NAME)]
        [TestCase<AliasWithCustomPrefix>(AliasWithCustomPrefix.OPTION_NAME)]
        [TestCase<AliasWithPartlyCustomPrefix>(AliasWithPartlyCustomPrefix.OPTION_NAME)]
        public void EnsureAliasAttribute<CommandType>(string optionName)
            where CommandType : StarterCommand, IHasAliases
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as CommandType;
            Assert.That(command, Is.Not.Null);
            TestsCommon.AssertIEnumerablesHaveSameElements(command.Aliases, command.ExpectedCommandAliases);

            var option = command.Options.FirstOrDefault(option => option.Name == optionName);
            Assert.That(option, Is.Not.Null);
            TestsCommon.AssertIEnumerablesHaveSameElements(option.Aliases, command.ExpectedOptionAliases);
        }

        [TestCase<HiddenCommand>(HiddenCommand.NAME_OF_OPTION, HiddenCommand.OPTION_IS_HIDDEN, HiddenCommand.NAME_OF_PARAMETER, HiddenCommand.PARAMETER_IS_HIDDEN, HiddenCommand.COMMAND_IS_HIDDEN)]
        [TestCase<VisibleCommand>(VisibleCommand.NAME_OF_OPTION, VisibleCommand.OPTION_IS_HIDDEN, VisibleCommand.NAME_OF_PARAMETER, VisibleCommand.PARAMETER_IS_HIDDEN, VisibleCommand.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandA>(PartlyHiddenCommandA.NAME_OF_OPTION, PartlyHiddenCommandA.OPTION_IS_HIDDEN, PartlyHiddenCommandA.NAME_OF_PARAMETER, PartlyHiddenCommandA.PARAMETER_IS_HIDDEN, PartlyHiddenCommandA.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandB>(PartlyHiddenCommandB.NAME_OF_OPTION, PartlyHiddenCommandB.OPTION_IS_HIDDEN, PartlyHiddenCommandB.NAME_OF_PARAMETER, PartlyHiddenCommandB.PARAMETER_IS_HIDDEN, PartlyHiddenCommandB.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandC>(PartlyHiddenCommandC.NAME_OF_OPTION, PartlyHiddenCommandC.OPTION_IS_HIDDEN, PartlyHiddenCommandC.NAME_OF_PARAMETER, PartlyHiddenCommandC.PARAMETER_IS_HIDDEN, PartlyHiddenCommandC.COMMAND_IS_HIDDEN)]
        public void EnsureIsHiddenAttribute<CommandType>(string optionName, bool isOptionHidden, string argumentName, bool isArgumentHidden, bool commandHidden)
            where CommandType : StarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            //Test command
            var command = starter.FindCommand<CommandType>() as CommandType;
            Assert.That(command, Is.Not.Null);
            Assert.That(command.IsHidden, Is.EqualTo(commandHidden));

            //Test argument
            var argument = command.Arguments.FirstOrDefault(argument => argument.Name == argumentName);
            Assert.That(argument, Is.Not.Null);
            Assert.That(argument.IsHidden, Is.EqualTo(isArgumentHidden));

            //Test option
            var option = command.Options.FirstOrDefault(option => option.Name == optionName);
            Assert.That(option, Is.Not.Null);
            Assert.That(option.IsHidden, Is.EqualTo(isOptionHidden));
        }

        [TestCase<NoAutoComplete>(NoAutoComplete.OPTION_NAME, NoAutoComplete.ARGUMENT_NAME)]
        [TestCase<NonGenericSingleAutoComplete>(NonGenericSingleAutoComplete.OPTION_NAME, NonGenericSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<NonGenericMultiAutoComplete>(NonGenericMultiAutoComplete.OPTION_NAME, NonGenericMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<NonGenericMultiSingleAutoComplete>(NonGenericMultiSingleAutoComplete.OPTION_NAME, NonGenericMultiSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<NonGenericMultiMultiAutoComplete>(NonGenericMultiMultiAutoComplete.OPTION_NAME, NonGenericMultiMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<GenericSingleAutoComplete>(GenericSingleAutoComplete.OPTION_NAME, GenericSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<GenericMultiAutoComplete>(GenericMultiAutoComplete.OPTION_NAME, GenericMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<GenericMultiSingleAutoComplete>(GenericMultiSingleAutoComplete.OPTION_NAME, GenericMultiSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<GenericMultiMultiAutoComplete>(GenericMultiMultiAutoComplete.OPTION_NAME, GenericMultiMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<FactoryAutoComplete>(FactoryAutoComplete.OPTION_NAME, FactoryAutoComplete.ARGUMENT_NAME)]
        [TestCase<GenericEnumAutoComplete>(GenericEnumAutoComplete.OPTION_NAME, GenericEnumAutoComplete.ARGUMENT_NAME)]
        public void EnusreAutoCompleteAttribute<CommandType>(string optionName, string argumentName)
            where CommandType : StarterCommand, IHasAutoComplete
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as CommandType;
            Assert.That(command, Is.Not.Null);

            var option = command.Options.FirstOrDefault(option => option.Name == optionName);
            Assert.That(option, Is.Not.Null);
            TestsCommon.AssertIEnumerablesHaveSameElements(option.GetCompletions(), command.OptionExpected());

            var argument = command.Arguments.FirstOrDefault(argument => argument.Name == argumentName);
            Assert.That(argument, Is.Not.Null);
            TestsCommon.AssertIEnumerablesHaveSameElements(argument.GetCompletions(), command.ArgumentExpected());
        }
    }
}
