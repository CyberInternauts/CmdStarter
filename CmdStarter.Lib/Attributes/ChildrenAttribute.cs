namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChildrenAttribute : Attribute
    {
        /// <summary>
        /// Namespace to look for children
        /// </summary>
        public string Namespace { get; private set; }

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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChildrenAttribute<ClassForNamespace> : ChildrenAttribute where ClassForNamespace : class
    {
        public ChildrenAttribute() : base(typeof(ClassForNamespace))
        {
        }
    }
}
