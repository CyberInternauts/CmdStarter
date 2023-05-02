using System.Collections;

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

        public object Value { get; init; }

        public HandlerData(CommandFeature feature, string name, object value)
        {
            this.Feature = feature;
            this.Name = name;
            this.Value = value ?? string.Empty;
        }

        public override int GetHashCode()
        {
            var valueHash = 0;
            if (Value is not string && Value is IEnumerable values)
            {
                foreach (var item in values)
                {
                    valueHash = HashCode.Combine(valueHash, item.GetHashCode());
                }
            } 
            else
            {
                valueHash = Value.GetHashCode();
            }

            return HashCode.Combine(this.Name.GetHashCode(), valueHash);
        }

        public override bool Equals(object? obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }
    }
}
