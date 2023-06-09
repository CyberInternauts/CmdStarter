﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Alias;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Hidden;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using System.CommandLine.Completions;

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
        [TestCase<OneAliasByInterface>(OneAliasByInterface.OPTION_NAME)]
        public void EnsuresAliasAttribute<CommandType>(string optionName)
            where CommandType : class, IStarterCommand, IHasAliases
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(command, Is.Not.Null);
            var expectation = (IHasAliases)command.UnderlyingCommand;
            TestsCommon.AssertIEnumerablesHaveSameElements(command.Aliases, expectation.ExpectedCommandAliases);

            var option = command.Options.FirstOrDefault(option => option.Name == optionName);
            Assert.That(option, Is.Not.Null);
            TestsCommon.AssertIEnumerablesHaveSameElements(option.Aliases, expectation.ExpectedOptionAliases);
        }

        [TestCase<HiddenCommand>(HiddenCommand.NAME_OF_OPTION, HiddenCommand.OPTION_IS_HIDDEN, HiddenCommand.NAME_OF_PARAMETER, HiddenCommand.PARAMETER_IS_HIDDEN, HiddenCommand.COMMAND_IS_HIDDEN)]
        [TestCase<VisibleCommand>(VisibleCommand.NAME_OF_OPTION, VisibleCommand.OPTION_IS_HIDDEN, VisibleCommand.NAME_OF_PARAMETER, VisibleCommand.PARAMETER_IS_HIDDEN, VisibleCommand.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandA>(PartlyHiddenCommandA.NAME_OF_OPTION, PartlyHiddenCommandA.OPTION_IS_HIDDEN, PartlyHiddenCommandA.NAME_OF_PARAMETER, PartlyHiddenCommandA.PARAMETER_IS_HIDDEN, PartlyHiddenCommandA.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandB>(PartlyHiddenCommandB.NAME_OF_OPTION, PartlyHiddenCommandB.OPTION_IS_HIDDEN, PartlyHiddenCommandB.NAME_OF_PARAMETER, PartlyHiddenCommandB.PARAMETER_IS_HIDDEN, PartlyHiddenCommandB.COMMAND_IS_HIDDEN)]
        [TestCase<PartlyHiddenCommandC>(PartlyHiddenCommandC.NAME_OF_OPTION, PartlyHiddenCommandC.OPTION_IS_HIDDEN, PartlyHiddenCommandC.NAME_OF_PARAMETER, PartlyHiddenCommandC.PARAMETER_IS_HIDDEN, PartlyHiddenCommandC.COMMAND_IS_HIDDEN)]
        public void EnsuresIsHiddenAttribute<CommandType>(string optionName, bool isOptionHidden, string argumentName, bool isArgumentHidden, bool commandHidden)
            where CommandType : IStarterCommand
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            //Test command
            var command = starter.FindCommand<CommandType>() as StarterCommand;
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
        [TestCase<SingleSingleAutoComplete>(SingleSingleAutoComplete.OPTION_NAME, SingleSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<SingleMultiAutoComplete>(SingleMultiAutoComplete.OPTION_NAME, SingleMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<MultiSingleAutoComplete>(MultiSingleAutoComplete.OPTION_NAME, MultiSingleAutoComplete.ARGUMENT_NAME)]
        [TestCase<MultiMultiAutoComplete>(MultiMultiAutoComplete.OPTION_NAME, MultiMultiAutoComplete.ARGUMENT_NAME)]
        [TestCase<LonelyEnumAutoComplete>(LonelyEnumAutoComplete.OPTION_NAME, LonelyEnumAutoComplete.ARGUMENT_NAME)]
        [TestCase<LonelyFeederAutoCompletion>(LonelyFeederAutoCompletion.OPTION_NAME, LonelyFeederAutoCompletion.ARGUMENT_NAME)]
        [TestCase<FactoryAutoComplete>(FactoryAutoComplete.OPTION_NAME, FactoryAutoComplete.ARGUMENT_NAME)]
        [TestCase<FullFeaturesAutoCompletion>(FullFeaturesAutoCompletion.OPTION_NAME, FullFeaturesAutoCompletion.ARGUMENT_NAME)]
        [TestCase<MergedIdenticalChoices>(MergedIdenticalChoices.OPTION_NAME, MergedIdenticalChoices.ARGUMENT_NAME)]
        [TestCase<SingleSingleAutoCompleteByInterface>(SingleSingleAutoCompleteByInterface.OPTION_NAME, SingleSingleAutoCompleteByInterface.ARGUMENT_NAME)]
        public void EnsuresAutoCompleteAttribute<CommandType>(string optionName, string argumentName)
            where CommandType : IStarterCommand, IHasAutoComplete
        {
            starter.Namespaces = starter.Namespaces.Add(typeof(CommandType).Namespace!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(command, Is.Not.Null);
            var autoCompleteCommand = (IHasAutoComplete)command.UnderlyingCommand;

            var option = command.Options.FirstOrDefault(option => option.Name == optionName);
            Assert.That(option, Is.Not.Null);
            AssertCompletionItemsEqual(option.GetCompletions(), autoCompleteCommand.OptionCompletionsExpected());

            var argument = command.Arguments.FirstOrDefault(argument => argument.Name == argumentName);
            Assert.That(argument, Is.Not.Null);
            AssertCompletionItemsEqual(argument.GetCompletions(), autoCompleteCommand.ArgumentCompletionsExpected());
        }

        [TestCase<NonGenericNullCompletion>]
        [TestCase<NonGenericEmptyCompletion>]
        [TestCase<NonGenericNotSupportedType>]
        [TestCase<GenericNoProvider>]
        [TestCase<OnlyIAutoCompleteFactory>]
        public void ThrowsAutoCompleteAttributeExceptions<ErrorRunner>()
            where ErrorRunner : IErrorRunner, IGetInstance<ErrorRunner>
        {
            var instance = ErrorRunner.GetInstance();
            Assert.Throws(instance.TypeOfException, () => instance.ErrorInvoker());
        }

        public static void AssertCompletionItemsEqual(IEnumerable<CompletionItem> actual, IEnumerable<CompletionItem> expected)
        {
            const string ERROR_MESSAGE = "Expected CompletionItem is different from actual.";

            TestsCommon.AssertIEnumerablesHaveSameElements(actual, expected);

            foreach (var expectedValue in expected)
            {

                var hasMatch = actual.Where(actualValue =>
                {
                    Assert.That(actualValue, Is.Not.Null);

                    return actualValue.Label == expectedValue.Label
                        && actualValue.SortText == expectedValue.SortText
                        && actualValue.InsertText == expectedValue.InsertText
                        && actualValue.Documentation == expectedValue.Documentation
                        && actualValue.Detail == expectedValue.Detail;
                });

                Assert.That(hasMatch.Count(), Is.EqualTo(1), ERROR_MESSAGE);
            }
        }
    }
}
