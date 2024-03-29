﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Filters
{
    public sealed class OptByAttribute : StarterCommand, IOptByAttribute
    {
        public static string[] IncludedOptions => new[] { nameof(OptionToInclude) };

        public static string[] ExcludedOptions => new[] { nameof(OptionToExclude1), nameof(OptionToExclude2) };

        [Option]
        public int OptionToInclude { get; set; }

        [Option]
        [NotOption]
        public int OptionToExclude1 { get; set; }
        public int OptionToExclude2 { get; set; }
    }
}
