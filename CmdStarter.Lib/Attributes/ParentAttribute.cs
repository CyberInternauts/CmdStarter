namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>
/// Set the type that will be used as parent
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ParentAttribute : Attribute
{
    /// <summary>
    /// Type of the parent. Can be null if no parent
    /// </summary>
    public Type? Parent { get; private set; }

    /// <summary>
    /// Constructor with the parent type
    /// </summary>
    /// <param name="parent"></param>
    public ParentAttribute(Type? parent)
    {
        Parent = parent;
    }
}

/// <inheritdoc cref="ParentAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ParentAttribute<TParentClass> : ParentAttribute
{
    /// <inheritdoc cref="ParentAttribute.ParentAttribute(Type?)"/>
    public ParentAttribute() : base(typeof(TParentClass))
    {
    }
}
