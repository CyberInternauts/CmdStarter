﻿using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AutoComplete;
using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    public sealed class AutoCompleteAttribute<T> : AutoCompleteAttribute
    {
        internal static readonly Func<T, string?> NullFactory = (obj) => null;

        /// <summary>
        /// Creates autocompletion for all values of an <see cref="Enum"/>.
        /// </summary>
        /// <remarks>
        /// Can only be used if <typeparamref name="T"/> is <see langword="typeof"/> <see cref="Enum"/>.
        /// </remarks>
        public AutoCompleteAttribute()
            : base(HandleGenericConstructor(typeof(T)))
        { }

        public AutoCompleteAttribute(params T[] completions)
            : base(completions.ToObjectArray())
        { }

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
