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

        /// <inheritdoc cref="AutoCompleteAttribute(object[])"/>
        public AutoCompleteAttribute(params object[] completions)
            : base(typeof(T), completions)
        { }
    }
}
