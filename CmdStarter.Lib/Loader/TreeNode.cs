using System.Collections.ObjectModel;
using System.Data;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Loader
{
    /// <summary>
    /// 
    /// Ref: https://stackoverflow.com/a/10442244/214898
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeNode<T> where T : class
    {
        private readonly T? _value;
        private readonly List<TreeNode<T>> _children = new();
        private bool isImmutable = false;

        /// <summary>
        /// Constructor of the node
        /// </summary>
        /// <param name="value">value to assign to this node</param>
        public TreeNode(T? value)
        {
            _value = value;
        }

        /// <summary>
        /// Indexer accessor
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public TreeNode<T> this[int i]
        {
            get { return _children[i]; }
        }

        /// <summary>
        /// Set if the node is immutable
        /// </summary>
        public bool IsImmutable { get => isImmutable; protected set => isImmutable = value; }

        /// <summary>
        /// Parent of the node or null if there is none
        /// </summary>
        public TreeNode<T>? Parent { get; private set; }

        /// <summary>
        /// Value of the node or none if there is none
        /// </summary>
        public T? Value { get { return _value; } }

        /// <summary>
        /// Children of the node
        /// </summary>
        public ReadOnlyCollection<TreeNode<T>> Children
        {
            get { return _children.AsReadOnly(); }
        }

        /// <summary>
        /// Execution on execution on all nodes
        /// </summary>
        /// <param name="action"></param>
        public void Traverse(Action<T> action)
        {
            if (Value != null)
            {
                action(Value);
            }
            foreach (var child in _children)
                child.Traverse(action);
        }

        /// <summary>
        /// List of this node and all its descendants
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TreeNode<T>> FlattenNodes()
        {
            var valueAsArray = new[] { this };

            return valueAsArray.Concat(_children.SelectMany(x => x.FlattenNodes()));
        }

        /// <summary>
        /// List of this node's value and all its descendants values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> FlattenValues()
        {
            var valueAsArray = Array.Empty<T>();
            if (Value != null) valueAsArray = new[] { Value };

            return valueAsArray.Concat(_children.SelectMany(x => x.FlattenValues()));
        }

        /// <summary>
        /// Find a node using its value
        /// </summary>
        /// <param name="value">Value to find</param>
        /// <returns><see cref="TreeNode{T}"/> if found otherwise null</returns>
        public TreeNode<T>? FindNode(T value)
        {
            if (value == null) return default;

            // If self
            if (Value?.Equals(value) ?? false) return this;

            // Look in children tree
            foreach (var node in _children)
            {
                var foundValue = node.FindNode(value);
                if (foundValue != null) return foundValue;
            }

            return null;
        }

        /// <summary>
        /// Return the root node
        /// </summary>
        /// <returns></returns>
        public TreeNode<T> GetRootNode()
        {
            var root = this;
            while (root.Parent != null) root = root.Parent;
            return root;
        }

        /// <summary>
        /// Lock permently the tree: no adding, removal nor move
        /// </summary>
        public void SetImmutable()
        {
            var root = GetRootNode();
            foreach (var node in root.FlattenNodes())
            {
                node.IsImmutable = true;
            }
        }

        /// <summary>
        /// Add a child using a value
        /// </summary>
        /// <param name="value">Value of the node</param>
        /// <returns>a newly created node</returns>
        /// <exception cref="ReadOnlyException">The tree is locked for modification</exception>
        public TreeNode<T> AddChild(T value)
        {
            if (isImmutable) throw new ReadOnlyException();

            var node = new TreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        /// <summary>
        /// Add a child using a node
        /// </summary>
        /// <param name="node">Node to add</param>
        /// <returns>node passed</returns>
        /// <exception cref="ReadOnlyException">The tree is locked for modification</exception>
        public TreeNode<T> AddChild(TreeNode<T> node)
        {
            if (isImmutable) throw new ReadOnlyException();

            node.Parent = this;
            _children.Add(node);
            return node;
        }

        /// <summary>
        /// Add multiple children using a list of value
        /// </summary>
        /// <param name="values">List of values to will each create a node</param>
        /// <returns>a list of newly created node</returns>
        /// <exception cref="ReadOnlyException">The tree is locked for modification</exception>
        public TreeNode<T>[] AddChildren(params T[] values)
        {
            if (isImmutable) throw new ReadOnlyException();

            return values.Select(AddChild).ToArray();
        }

        /// <summary>
        /// Remove a child using a node
        /// </summary>
        /// <param name="node">Node to remove</param>
        /// <returns>true if removed</returns>
        /// <exception cref="ReadOnlyException">The tree is locked for modification</exception>
        public bool RemoveChild(TreeNode<T> node)
        {
            if (isImmutable) throw new ReadOnlyException();

            return _children.Remove(node);
        }

        /// <summary>
        /// Move the node to a new parent
        /// </summary>
        /// <param name="newParent">Node that will be the new parent</param>
        /// <exception cref="ReadOnlyException">The tree is locked for modification</exception>
        public void MoveNode(TreeNode<T> newParent)
        {
            if (isImmutable) throw new ReadOnlyException();

            Parent?.RemoveChild(this);
            newParent.AddChild(this);
        }
    }
}
