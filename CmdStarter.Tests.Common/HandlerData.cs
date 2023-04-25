namespace com.cyberinternauts.csharp.CmdStarter.Tests.Common
{
    public class HandlerData
    {
        public enum CommandFeature
        {
            GlobalOption,
            Option,
            Argument
        }

        public CommandFeature Feature { get; init; }

        public string Name { get; init; }

        public Type Type { get; init; }

        public object Value { get; init; }

        public HandlerData(CommandFeature feature, string name, object value)
        {
            this.Feature = feature;
            this.Name = name;
            this.Type = value.GetType();
            this.Value = value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Name.GetHashCode() + this.Value.GetHashCode());
        }

        public override bool Equals(object? obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }
    }
}
