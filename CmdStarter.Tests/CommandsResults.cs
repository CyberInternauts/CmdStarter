using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo.Types;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Execution;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.TestsCommandsAttributes;

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
        [TestCase<ExecSumByInterface>]
        [TestCase<ExecOptionTypesByInterface>]
        [TestCase<ExecArgumentTypesByInterface>]
        [TestCase<ExecSumAsync>]
        public async Task EnsuresCommandHandling<CommandType>() where CommandType : IStarterCommand, IHandleTester
        {
            starter.IsRootingLonelyCommand = false;
            starter.Classes = starter.Classes.Add(typeof(MainGlobalOptions).FullName!);
            starter.Classes = starter.Classes.Add(typeof(CommandType).FullName!);
            starter.InstantiateCommands();

            var command = starter.FindCommand<CommandType>() as StarterCommand;
            Assert.That(command, Is.Not.Null);
            var handleTester = (IHandleTester)command.UnderlyingCommand;

            var mainArgs = new List<string>();

            // Global options
            var globalOptionsArgs = SelectHandlerDataByFeature(handleTester, HandlerData.CommandFeature.GlobalOption);
            if (globalOptionsArgs.Any()) mainArgs.AddRange(globalOptionsArgs);

            // Command name
            mainArgs.Add(command.Name);

            // Arguments
            var argumentsArgs = SelectHandlerDataByFeature(handleTester, HandlerData.CommandFeature.Argument);
            if (argumentsArgs.Any()) mainArgs.AddRange(argumentsArgs);

            // Options
            var optionsArgs = SelectHandlerDataByFeature(handleTester, HandlerData.CommandFeature.Option);
            if (optionsArgs.Any()) mainArgs.AddRange(optionsArgs);

            // Run and compare
            var actualReturn = await starter.Start(mainArgs.ToArray());
            Assert.That(actualReturn, Is.EqualTo(handleTester.ExpectedReturn));

            TestsCommon.AssertIEnumerablesHaveSameElements(handleTester.ActualHandlerData, handleTester.ExpectedHandlerData);
        }

        [TestCase("list files PATH_TO_LIST")]
        [TestCase("list files PATH_TO_LIST " + "--" + Files.FILES_PATTERN_NAME + " \"abc def\"")]
        public async Task EnsuresStartUsingConcatenatedString(string args)
        {
            starter.ClassesBuildingMode = Lib.Loader.ClassesBuildingMode.Both;
            starter.InstantiateCommands();

            await starter.Start(args);
        }

        [TestCase<Dependent1, DependentService1>]
        [TestCase<Dependent1, DependentService2>]
        public async Task UsesDependencyInjection<CommandType, DependentServiceType>() 
            where CommandType : IStarterCommand 
            where DependentServiceType : IDependentService
        {
            var dependentService = (IDependentService)Activator.CreateInstance(typeof(DependentServiceType))!;
            var serviceManager = new ServiceManager(dependentService);

            starter.Namespaces = starter.Namespaces.Clear().Add(typeof(CommandType).Namespace!);
            var actualReturn = await starter.Start(serviceManager, Array.Empty<string>());

            Assert.That(dependentService.GetInt(), Is.EqualTo(actualReturn));
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
