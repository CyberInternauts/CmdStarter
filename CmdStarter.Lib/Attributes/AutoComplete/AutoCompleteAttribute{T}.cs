using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    /// <inheritdoc cref="AutoCompleteAttribute"/>
    /// <typeparam name="T">
    /// Defines the behaviour of the attribute.
    /// <para>
    /// More info in constructors.
    /// </para>
    /// </typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        /// <summary>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>
        /// generates auto completions from all values. 
        /// </para>
        /// <para>
        /// If <typeparamref name="T"/> is <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>
        /// retrieves auto completions from there.
        /// </para>
        /// <para>
        /// Can also be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/> besides these.
        /// </para>
        /// </summary>
        public AutoCompleteAttribute()
            : base(typeof(T))
        { }

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and handles <typeparamref name="T"/>.
        /// <para>
        /// <typeparamref name="T"/> must be <see langword="typeof"/> <see cref="IAutoCompleteProvider"/>.
        /// <para>
        /// Can also be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.
        /// </para>
        /// </para>
        /// </summary>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(params object[] completions)
            : base(typeof(T), completions)
        { }
    }
}
