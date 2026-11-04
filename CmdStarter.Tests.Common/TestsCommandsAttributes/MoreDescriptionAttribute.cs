using System.ComponentModel;

namespace CyberInternauts.CmdStarter.Tests.Common.TestsCommandsAttributes;

public class MoreDescriptionAttribute : DescriptionAttribute
{
    public MoreDescriptionAttribute(string description) : base(description) { }
}
