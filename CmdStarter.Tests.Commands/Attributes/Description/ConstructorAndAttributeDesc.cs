using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Description;

[Description(ATTRIBUTE_DESC)]
public sealed class ConstructorAndAttributeDesc : StarterCommand
{
    public const string ATTRIBUTE_DESC = "From attribute";
    public const string CTOR_DESC = "Ignored constructor description";

    public ConstructorAndAttributeDesc() : base(nameof(ConstructorAndAttributeDesc), CTOR_DESC) { }
}
