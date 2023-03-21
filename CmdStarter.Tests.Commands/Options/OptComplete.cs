using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options
{
    public class OptComplete : StarterCommand
    {

        [Required]
        public int IntOpt { get; set; }
        public const string INT_OPT_KEBAB = "--int-opt";

        [System.ComponentModel.Description(STRING_OPT_DESC)]
        public string? StringOpt { get; set; }
        public const string STRING_OPT_KEBAB = "--string-opt";
        public const string STRING_OPT_DESC = "String option";

        public DateTime? DateOpt { get; set; }
        public const string DATE_OPT_KEBAB = "--date-opt";

        private int PrivateOpt { get; set; } = 0;
        public const string PRIVATE_OPT_KEBAB = "--private-opt";

        protected int ProtectedOpt { get; set; } = 0;
        public const string PROTECTED_OPT_KEBAB = "--protected-opt";

        public int ReadOnlyOpt { get; }
        public const string READ_ONLY_OPT_KEBAB = "--read-only-opt";

        public static int StaticOpt { get; set; }
        public const string STATIC_OPT_KEBAB = "--static-opt";
    }
}
