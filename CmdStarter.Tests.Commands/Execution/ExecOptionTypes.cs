using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using System.Drawing;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Execution
{
    public class ExecOptionTypes : StarterCommand, IHandleTester
    {
        private const int DEFAULT_RETURN = 0;
        private const string STRING_OPTION_VALUE = "hello";
        private const bool BOOL_OPTION_VALUE = true;
        private const int INT_OPTION_VALUE = 42;
        private readonly static DateTime DATE_OPTION_VALUE = new(2000, 1, 1);
        private const string LIST_OPTION_VALUE_1 = "list1";
        private const string LIST_OPTION_VALUE_2 = "list2";
        private readonly static List<string> LIST_OPTÌON_VALUE = new(new[] { LIST_OPTION_VALUE_1, LIST_OPTION_VALUE_2 });

        private List<HandlerData> actualHandlerData = new();

        public string StringOpt { get; set; } = string.Empty;
        private const string STRING_OPT_KEBAB = "string-opt";

        public bool BoolOpt { get; set; } = false;
        private const string BOOL_OPT_KEBAB = "bool-opt";

        public int IntOpt { get; set; } = 0;
        private const string INT_OPT_KEBAB = "int-opt";

        public DateTime DateTimeOpt { get; set; } = DateTime.MinValue;
        private const string DATETIME_OPT_KEBAB = "date-time-opt";

        public List<string> ListOpt { get; set; } = new();
        private const string LIST_OPT_KEBAB = "list-opt";

        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            actualHandlerData = CreateData(StringOpt, BoolOpt, IntOpt, DateTimeOpt, ListOpt);

            return DEFAULT_RETURN;
        }

        public int ExpectedReturn => DEFAULT_RETURN;

        public List<HandlerData> ExpectedHandlerData => CreateData(STRING_OPTION_VALUE, BOOL_OPTION_VALUE, INT_OPTION_VALUE, DATE_OPTION_VALUE, LIST_OPTÌON_VALUE);

        public List<HandlerData> ActualHandlerData => actualHandlerData;

        private static List<HandlerData> CreateData(string stringOpt, bool boolOpt, int intOpt, DateTime dateTimeOpt, List<string> listOpt)
        {
            var data = new List<HandlerData>
            {
                new HandlerData(HandlerData.CommandFeature.Option, STRING_OPT_KEBAB, stringOpt),
                new HandlerData(HandlerData.CommandFeature.Option, BOOL_OPT_KEBAB, boolOpt),
                new HandlerData(HandlerData.CommandFeature.Option, INT_OPT_KEBAB, intOpt),
                new HandlerData(HandlerData.CommandFeature.Option, DATETIME_OPT_KEBAB, dateTimeOpt),
                new HandlerData(HandlerData.CommandFeature.Option, LIST_OPT_KEBAB, listOpt),
            };

            return data;
        }
    }
}
