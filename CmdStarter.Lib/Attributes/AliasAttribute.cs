namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class AliasAttribute : Attribute
    {
        public static string DEFAULT_PREFIX = "-";

        private readonly string _alias;

        public string Alias => _alias;

        public AliasAttribute(string alias)
            : this(alias, DEFAULT_PREFIX)
        { }

        public AliasAttribute(string alias, string prefix)
        {
            _alias = prefix + alias;
        }
    }
}
