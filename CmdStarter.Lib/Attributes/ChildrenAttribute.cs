namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
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
        public ChildrenAttribute(Type namespaceForChildren)
        {
            Namespace = namespaceForChildren.Namespace ?? string.Empty;
        }

        /// <summary>
        /// Possible usage:<br/>
        /// - [Children($"{nameof(com)}.{nameof(cyberinternauts)}")]<br/>
        /// - [Children("com.cyberinternauts")]
        /// </summary>
        /// <param name="namespaceForChildren"></param>
        public ChildrenAttribute(string namespaceForChildren)
        {
            Namespace = namespaceForChildren;
        }
    }

    /// <summary>
    /// Set children to a class using the namespace of the generic type
    /// </summary>
    /// <typeparam name="ClassForNamespace"></typeparam>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChildrenAttribute<ClassForNamespace> : ChildrenAttribute where ClassForNamespace : class
    {
        /// <summary>
        /// <see cref="ChildrenAttribute.ChildrenAttribute(Type)"/>
        /// </summary>
        public ChildrenAttribute() : base(typeof(ClassForNamespace))
        {
        }
    }
}
