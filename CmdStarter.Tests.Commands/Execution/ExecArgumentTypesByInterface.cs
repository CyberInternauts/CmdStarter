using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Execution
{
    public class ExecArgumentTypesByInterface : IStarterCommand, IHandleTester
    {
        private const int DEFAULT_RETURN = 0;
        private const string STRING_PARAM_VALUE = "hello";
        private const bool BOOL_PARAM_VALUE = true;
        private const int INT_PARAM_VALUE = 42;

        private List<HandlerData> actualHandlerData = new();

        public Delegate HandlingMethod => Execute;

        public int Execute(string stringParam, bool boolParam, int intParam)
        {
            actualHandlerData = CreateData(stringParam, boolParam, intParam);

            return DEFAULT_RETURN;
        }

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new ExecArgumentTypesByInterface();

        public int ExpectedReturn => DEFAULT_RETURN;

        public List<HandlerData> ExpectedHandlerData => CreateData(STRING_PARAM_VALUE, BOOL_PARAM_VALUE, INT_PARAM_VALUE);

        public List<HandlerData> ActualHandlerData => actualHandlerData;

        private static List<HandlerData> CreateData(string stringOpt, bool boolOpt, int intOpt)
        {
            var data = new List<HandlerData>
            {
                new HandlerData(HandlerData.CommandFeature.Argument, string.Empty, stringOpt),
                new HandlerData(HandlerData.CommandFeature.Argument, string.Empty, boolOpt),
                new HandlerData(HandlerData.CommandFeature.Argument, string.Empty, intOpt),
            };

            return data;
        }
    }
}
