using System.Collections;
using System.Collections.ObjectModel;
using System.CommandLine.Binding;
using System.Data;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
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

        public TreeNode(T? value)
        {
            _value = value;
        }

        public TreeNode<T> this[int i]
        {
            get { return _children[i]; }
        }

        public bool IsImmutable { get => isImmutable; protected set => isImmutable = value; }

        public TreeNode<T>? Parent { get; private set; }

        public T? Value { get { return _value; } }

        public ReadOnlyCollection<TreeNode<T>> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public void Traverse(Action<T> action)
        {
            if (Value != null)
            {
                action(Value);
            }
            foreach (var child in _children)
                child.Traverse(action);
        }

        public IEnumerable<TreeNode<T>> FlattenNodes()
        {
            var valueAsArray = new[] { this };

            return valueAsArray.Concat(_children.SelectMany(x => x.FlattenNodes()));
        }

        public IEnumerable<T> FlattenValues()
        {
            var valueAsArray = Array.Empty<T>();
            if (Value != null) valueAsArray = new[] { Value };

            return valueAsArray.Concat(_children.SelectMany(x => x.FlattenValues()));
        }

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

        public TreeNode<T>? GetChildNode(T value)
        {
            return this.Children.FirstOrDefault(x => x.Value == value);
        }

        public TreeNode<T> AddChild(T value)
        {
            if (isImmutable) throw new ReadOnlyException();

            var node = new TreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        public TreeNode<T> AddChild(TreeNode<T> node)
        {
            if (isImmutable) throw new ReadOnlyException();

            node.Parent = this;
            _children.Add(node);
            return node;
        }

        public TreeNode<T>[] AddChildren(params T[] values)
        {
            if (isImmutable) throw new ReadOnlyException();

            return values.Select(AddChild).ToArray();
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            if (isImmutable) throw new ReadOnlyException();

            return _children.Remove(node);
        }

        public void MoveNode(TreeNode<T> newParent)
        {
            if (isImmutable) throw new ReadOnlyException();

            this.Parent?.RemoveChild(this);
            newParent.AddChild(this);
        }
    }
}
