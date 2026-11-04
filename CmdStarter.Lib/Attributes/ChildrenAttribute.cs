namespace CyberInternauts.CmdStarter.Attributes;

/// <summary>
/// Set children to a class using a namespace
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ChildrenAttribute : Attribute
{
    /// <summary>
    /// Namespace to look for children
    /// </summary>
    public string Namespace { get; private set; }

    /// <summary>
    /// Use the namespace of the type
    /// </summary>
    /// <param name="namespaceForChildren">Type to get the namespace</param>
    /// <exception cref="ArgumentNullException"><paramref name="namespaceForChildren"/> is <see langword="null"/>.</exception>
    public ChildrenAttribute(Type namespaceForChildren)
    {
        ArgumentNullException.ThrowIfNull(namespaceForChildren);
        Namespace = namespaceForChildren.Namespace ?? string.Empty;
    }

    /// <summary>
    /// Possible usage:<br/>
    /// - [Children($"{nameof(com)}.{nameof(cyberinternauts)}")]<br/>
    /// - [Children("CyberInternauts")]
    /// </summary>
    /// <param name="namespaceForChildren"></param>
    /// <exception cref="ArgumentNullException"><paramref name="namespaceForChildren"/> is <see langword="null"/>.</exception>
    public ChildrenAttribute(string namespaceForChildren)
    {
        ArgumentNullException.ThrowIfNull(namespaceForChildren);
        Namespace = namespaceForChildren;
    }
}

/// <summary>
/// Set children to a class using the namespace of the generic type
/// </summary>
/// <typeparam name="TClassForNamespace"></typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ChildrenAttribute<TClassForNamespace> : ChildrenAttribute where TClassForNamespace : class
{
    /// <summary>
    /// <see cref="ChildrenAttribute.ChildrenAttribute(Type)"/>
    /// </summary>
    public ChildrenAttribute() : base(typeof(TClassForNamespace))
    {
    }
}
