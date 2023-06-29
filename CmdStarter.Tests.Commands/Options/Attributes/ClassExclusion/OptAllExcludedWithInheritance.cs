namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes.ClassExclusion
{
    public sealed class OptAllExcludedWithInheritance : OptAllPropertiesExcluded
    {
        public new const int OPTION_COUNT = 1;

        public int IncludedOption { get; set; }
    }
}
