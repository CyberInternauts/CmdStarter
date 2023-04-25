using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Execution
{
    public class ExecSum : StarterCommand, IHandleTester
    {
        private const int EXPECTED_RETURN = GLOBAL_INT_OPTION_VALUE + INT_OPTION_VALUE + PARAM1_VALUE;
        private const int DEFAULT_INT_OPTION_VALUE = 11;
        private const int INT_OPTION_VALUE = 22;
        private const int PARAM1_VALUE = 10000;
        private const int GLOBAL_INT_OPTION_VALUE = 999;

        private List<HandlerData> actualHandlerData = new();

        public int MyInt { get; set; } = DEFAULT_INT_OPTION_VALUE;
        private const string MY_INT_KEBAB = "my-int";

        public override Delegate HandlingMethod => Execute;

        public int Execute(int param1)
        {
            var globalOptions = this.GlobalOptionsManager.GetGlobalOptions<MainGlobalOptions>();
            var globalInt = (globalOptions?.IntGlobalOption ?? 0);

            actualHandlerData = CreateData(globalInt, MyInt, param1);

            return globalInt + MyInt + param1;
        }

        public int ExpectedReturn => EXPECTED_RETURN;

        public List<HandlerData> ExpectedHandlerData => CreateData(GLOBAL_INT_OPTION_VALUE, INT_OPTION_VALUE, PARAM1_VALUE);

        public List<HandlerData> ActualHandlerData => actualHandlerData;

        private static List<HandlerData> CreateData(int globalOptionValue, int optionValue, int argumentValue)
        {
            var data = new List<HandlerData>
            {
                new HandlerData(HandlerData.CommandFeature.GlobalOption, MainGlobalOptions.INT_GLOBAL_OPTION_KEBAB, globalOptionValue),
                new HandlerData(HandlerData.CommandFeature.Option, MY_INT_KEBAB, optionValue),
                new HandlerData(HandlerData.CommandFeature.Argument, string.Empty, argumentValue)
            };

            return data;
        }
    }
}
