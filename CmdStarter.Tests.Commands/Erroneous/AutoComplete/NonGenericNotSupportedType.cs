﻿using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;
using NUnit.Framework;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.AutoComplete
{
    public class NonGenericNotSupportedType : IErrorRunner, IGetInstance<NonGenericNotSupportedType>
    {
        public Type TypeOfException => typeof(NotSupportedException);

        public void ErrorInvoker() => new AutoCompleteAttribute(typeof(StarterCommand));

        public static NonGenericNotSupportedType GetInstance() => new();
    }
}
