using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{

    public class OptCompleteByInterface : IStarterCommand
    {

        public List<int>? ListIntOpt { get; set; }
        public const string LIST_INT_OPT_KEBAB = "list-int-opt";

        [Required]
        public int IntOpt { get; set; }
        public const string INT_OPT_KEBAB = "int-opt";

        [System.ComponentModel.Description(STRING_OPT_DESC)]
        public string? StringOpt { get; set; }
        public const string STRING_OPT_KEBAB = "string-opt";
        public const string STRING_OPT_DESC = "String option";

        public DateTime? DateOpt { get; set; }
        public const string DATE_OPT_KEBAB = "date-opt";

        private int PrivateOpt { get; set; } = 0;
        public const string PRIVATE_OPT_KEBAB = "private-opt";

        protected int ProtectedOpt { get; set; } = 0;
        public const string PROTECTED_OPT_KEBAB = "protected-opt";

        public int ReadOnlyOpt { get; }
        public const string READ_ONLY_OPT_KEBAB = "read-only-opt";

        private int writeOnlyOpt = 0;
        public int WriteOnlyOpt { set => writeOnlyOpt = value; }
        public const string WRITE_ONLY_OPT_KEBAB = "write-only-opt";


        public static int StaticOpt { get; set; }
        public const string STATIC_OPT_KEBAB = "static-opt";

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => Execute;

        public void Execute()
        {

        }

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new OptCompleteByInterface();
    }
}
