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
        /// </summary>
        public AutoCompleteAttribute()
            : base(typeof(T))
        { }

        /// <summary>
        /// Creates auto completions from the given <paramref name="completions"/> and runs them through the <typeparamref name="T"/> factory.
        /// </summary>
        /// <param name="factory">Must be <see langword="typeof"/> <see cref="IAutoCompleteFactory"/>.</param>
        /// <param name="completions">Labels for the auto completions.</param>
        public AutoCompleteAttribute(params object[] completions)
            : base(typeof(T), completions)
        { }
    }
}
