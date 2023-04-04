namespace com.cyberinternauts.csharp.CmdStarter.Lib.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ParentAttribute : Attribute
    {
        /// <summary>
        /// Type of the parent. Can be null if no parent
        /// </summary>
        public Type? Parent { get; private set; }

        public ParentAttribute(Type? parent)
        {
            Parent = parent;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ParentAttribute<ParentClass> : ParentAttribute where ParentClass : StarterCommand
    {
        public ParentAttribute() : base(typeof(ParentClass))
        {
        }
    }

}
