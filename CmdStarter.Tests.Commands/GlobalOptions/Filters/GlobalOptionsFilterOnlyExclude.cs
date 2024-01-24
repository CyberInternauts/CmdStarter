﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions.Filters
{
    public sealed class GlobalOptionsFilterOnlyExclude : IGlobalOptionsContainer, IOptByAttribute
    {
        public static string[] IncludedOptions => new string[] { INCLUDED_OPTION };

        public static string[] ExcludedOptions => new string[] { NOT_INCLUDED_OPTION_1, NOT_INCLUDED_OPTION_2 };

        public int IncludedOption { get; set; }
        public const string INCLUDED_OPTION = "included-option";

        [NotOption]
        public int NotIncludedOption1 { get; set; }
        public const string NOT_INCLUDED_OPTION_1 = "not-included-option-1";

        [NotOption]
        public int NotIncludedOption2 { get; set; }
        public const string NOT_INCLUDED_OPTION_2 = "not-included-option-2";
    }
}
