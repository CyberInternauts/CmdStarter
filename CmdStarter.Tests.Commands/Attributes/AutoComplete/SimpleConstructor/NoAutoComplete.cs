﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Interfaces;
using System.CommandLine;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.AutoComplete.SimpleConstructor
{
    public sealed class NoAutoComplete : StarterCommand, IHasAutoComplete
    {
        public string PersonName { get; set; } = null!;
        public const string OPTION_NAME = "person-name";

        public override Delegate MethodForHandling => Execute;

        private void Execute(int age = 18)
        { }
        public const string ARGUMENT_NAME = "age";

        public IEnumerable<CompletionItem> OptionExpected()
        {
            Option<string> expected = new(OPTION_NAME);

            return expected.GetCompletions();
        }

        public IEnumerable<CompletionItem> ArgumentExpected()
        {
            Argument<int> expected = new();

            return expected.GetCompletions();
        }
    }
}