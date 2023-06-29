namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options.Attributes.ClassExclusion
{
    public sealed class OptAllPropertiesExcludedWithInheritance : OptAllExcludedInheritor
    {
        public const int OPTION_COUNT = 1;

        public int IncludedOption { get; set; }
    }
}
