using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using System.CommandLine.Completions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        private readonly IAutoCompleteFactory? _factory;

        /// <summary>
        /// Creates autocompletion for all values of an <see cref="Enum"/>.
        /// </summary>
        /// <remarks>
        /// Can only be used if <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>.
        /// </remarks>
        public AutoCompleteAttribute()
            : base(typeof(T))
        { }

        /// <inheritdoc cref="AutoCompleteAttribute(object[])"/>
        public AutoCompleteAttribute(params object[] completions)
            : base(completions)
        {
            _factory = GetFactory();
        }

        private static object[] HandleGenericConstructor(Type type)
        {
            const string EXCEPTION_MESSAGE = "This constructor is only supported with Enums";

            if (type.IsEnum)
            {
                return Enum.GetNames(type);
            }

            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }
    }
}
