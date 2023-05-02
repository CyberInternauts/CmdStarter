using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Execution;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;
using System.CommandLine;
using System.Security.Cryptography;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    /// <summary>
    /// Class for tests that include commands responses
    /// </summary>
    [Category("* All *")]
    [Category("Commands")]
    [Category("Results")]
    public class CommandsResults
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

        [TestCase<ExecSum>]
        [TestCase<ExecOptionTypes>]
        [TestCase<ExecArgumentTypes>]
        public async Task EnsuresCommandHandling<CommandType>() where CommandType : StarterCommand, IHandleTester
        {
            starter.IsRootingLonelyCommand = false;
            starter.Classes = starter.Classes.Add(typeof(MainGlobalOptions).FullName!);
            starter.Classes = starter.Classes.Add(typeof(CommandType).FullName!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as IHandleTester;
            Assert.That(command, Is.Not.Null);

            var mainArgs = new List<string>();

            // Global options
            var globalOptionsArgs = SelectHandlerDataByFeature(command, HandlerData.CommandFeature.GlobalOption);
            if (globalOptionsArgs.Any()) mainArgs.AddRange(globalOptionsArgs);

            // Command name
            mainArgs.Add(((StarterCommand)command).Name);

            // Arguments
            var argumentsArgs = SelectHandlerDataByFeature(command, HandlerData.CommandFeature.Argument);
            if (argumentsArgs.Any()) mainArgs.AddRange(argumentsArgs);

            // Options
            var optionsArgs = SelectHandlerDataByFeature(command, HandlerData.CommandFeature.Option);
            if (optionsArgs.Any()) mainArgs.AddRange(optionsArgs);

            // Run and compare
            var actualReturn = await starter.Start(mainArgs.ToArray());
            Assert.That(actualReturn, Is.EqualTo(command.ExpectedReturn));

            TestsCommon.AssertIEnumerablesHaveSameElements(command.ActualHandlerData, command.ExpectedHandlerData);
        }


        private static IEnumerable<string> SelectHandlerDataByFeature(IHandleTester command, HandlerData.CommandFeature feature)
        {
            bool isOption = feature != HandlerData.CommandFeature.Argument;

            return command.ExpectedHandlerData
                .Where(d => d.Feature == feature)
                .SelectMany(d => 
                {
                    var list = new List<string>();
                    if (isOption)
                    {
                        list.AddRange(TestsCommon.PrepareOption(d.Name, d.Value));
                    }
                    else if (d.Value != null)
                    {
                        list.Add(d.Value!.ToString()!); // Null already tested
                    }
                    return list;
                }
                )
                .Where(s => !string.IsNullOrEmpty(s));
        }

    }
}
