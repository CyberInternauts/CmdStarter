using System.Collections;
using System.Numerics;
using System.Text;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    /// <summary>
    /// Defines aliases for a command or an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class AliasAttribute : Attribute, IEnumerable<string>
    {
        private const bool USE_PREFIX_BY_DEFAULT = false;

        /// <summary>
        /// The default prefix used if none was given.
        /// </summary>
        public const string DEFAULT_PREFIX = "-";

        private readonly string[] _aliases;
        private readonly bool _usePrefix;
        private readonly string? _prefix = null;

        private bool _wasPrefixed;

        /// <summary>
        /// Gets the aliases.
        /// </summary>
        public IReadOnlyList<string> Aliases
        {
            get
            {
                SetPrefixes();
                return _aliases;
            }
        }

        /// <summary>
        /// Gets the prefix used.
        /// </summary>
        public string Prefix
        {
            get => _prefix ?? DEFAULT_PREFIX;
            init => _prefix = value;
        }

        /// <summary>
        /// Gets a value that represents wheter prefix should be used or not.
        /// </summary>
        public bool UsePrefix => _usePrefix;

        /// <summary>
        /// Creates a new instance of <see cref="AliasAttribute"/>.
        /// </summary>
        /// <param name="usePrefix">Determines wheter the aliases have prefixes or not.</param>
        /// <param name="aliases">The aliases.</param>
        public AliasAttribute(bool usePrefix, params string[] aliases)
        {
            _aliases = aliases;
            _usePrefix = usePrefix;
        }

        /// <summary>
        /// Creates a new instance of <see cref="AliasAttribute"/>.
        /// </summary>
        /// <param name="aliases">The aliases.</param>
        public AliasAttribute(params string[] aliases)
            : this(USE_PREFIX_BY_DEFAULT, aliases)
        {
        }

        /// <summary>
        /// Gets the alias at the specified index.
        /// </summary>
        /// <returns>The alias at the specified index.</returns>
        public string this[int index] => Aliases[index];

        public IEnumerator<string> GetEnumerator() => Aliases.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Aliases.GetEnumerator();

        private void SetPrefixes()
        {
            if (_wasPrefixed || !UsePrefix || Prefix is null) return;
            _wasPrefixed = true;

            StringBuilder builder = new();
            for (int i = 0; i < _aliases.Length; i++)
            {
                builder.Append(Prefix);
                builder.Append(_aliases[i]);

                _aliases[i] = builder.ToString();

                builder.Clear();
            }
        }
    }
}
