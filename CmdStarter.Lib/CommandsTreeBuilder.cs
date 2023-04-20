﻿using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cyberinternauts.csharp.CmdStarter.Lib
{
    internal class CommandsTreeBuilder
    {
        /// <summary>
        /// Configure how to build the commands tree (Order is important. Skipping other IFs when ClassC is assigned):
        /// 
        /// - Both: Use CommandsAttributes and if nothing then namespaces.
        ///     - If ClassC has a ParentAttribute set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a ChildrenAttribute set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassC's parent namespace has only one CmdStarterCommand (ClassP) AND :
        ///         - ClassC doesn't have a ParentAttribute ==> Assign ClassC as subcommand of ClassP.
        ///         - ClassC is not covered by a ChildrenAttribute ==> Assign ClassC as subcommand of ClassP.
        ///         
        /// - OnlyAttributes: Use only CommandsAttributes.
        ///     - If ClassC has a ParentAttribute set to ClassP ==> Assign ClassC as subcommand of ClassP.
        ///     - If ClassP has a ChildrenAttribute set to the namespace of ClassC ==> Assign ClassC as subcommand of ClassP.
        ///     
        /// - OnlyNamespaces:
        ///     - If ClassC's parent namespace has only one CmdStarterCommand (ClassP)
        /// </summary>
        public ClassesBuildingMode ClassesBuildingMode { get; set; } = ClassesBuildingMode.Both;

        public ImmutableList<Type> CommandsTypes { get; set; } = ImmutableList<Type>.Empty;

        public CommandsTreeBuilder(ClassesBuildingMode classesBuildingMode, ImmutableList<Type> commandsTypes)
        {
            ClassesBuildingMode = classesBuildingMode;
            CommandsTypes = commandsTypes;
        }

        public TreeNode<Type> BuildTree()
        {
            var tree = new TreeNode<Type>(null);

            /////////////
            // Attributes mode
            /////////////

            if (ClassesBuildingMode == ClassesBuildingMode.Both || ClassesBuildingMode == ClassesBuildingMode.OnlyAttributes)
            {
                // Within tree, add commands having Parent attribute
                AddParentsToTree(tree);

                // Within tree, add sub commands of commands having Children attribute
                AddChildrenToTree(tree);
            }

            /////////////
            // Namespaces mode
            /////////////

            if (ClassesBuildingMode == ClassesBuildingMode.Both || ClassesBuildingMode == ClassesBuildingMode.OnlyNamespaces)
            {
                // Within tree, add commands based on their namespace hierachy
                AddByNamespacesHierachy(tree, ClassesBuildingMode == ClassesBuildingMode.Both);
            }

            /////////////
            // Others
            /////////////

            // Add leftover commands as main ones
            AddLonelyToTree(tree);

            return tree;
        }

        private Func<object, bool> IsParentAttribute => (object a) => a.GetType().IsAssignableTo(typeof(ParentAttribute));
        private Func<Type, Type, bool> HasParentAttribute => (Type parent, Type child) => 
            child.GetCustomAttributes(false).Any(a => IsParentAttribute(a) && (((ParentAttribute)a).Parent?.Equals(parent) ?? false));
        private Func<object, bool> IsRootParentExplicitly => (object a) => IsParentAttribute(a) && ((ParentAttribute)a).Parent == null;
        private Func<Type, bool> HasRootParentImplicitly => (Type t) => !t.CustomAttributes.Any() && 
            CommandsTypes.Any(child => HasParentAttribute(t, child));

        private IEnumerable<ParentAttribute> GetParentAttributes(Type command)
            => command.GetCustomAttributes(false).Where(IsParentAttribute).Cast<ParentAttribute>() ?? Array.Empty<ParentAttribute>();


        /// <summary>
        /// Add to the tree all the commands types having a <see cref="ParentAttribute"/> and their parent
        /// </summary>
        /// <param name="tree"></param>
        private void AddParentsToTree(TreeNode<Type> tree)
        {
            var rootParentCommands = CommandsTypes
                .Where(t => t.GetCustomAttributes(false).Any(IsRootParentExplicitly));
            foreach (var command in rootParentCommands)
            {
                AddParentsToTree(tree, command);
            }
        }

        private void AddParentsToTree(TreeNode<Type> tree, Type command)
        {
            foreach (var parentAttribute in GetParentAttributes(command))
            {
                var parent = parentAttribute.Parent;
                var curNode = new TreeNode<Type>(command);
                var initialNode = curNode;
                if (parent != null)
                {
                    if (initialNode.FindNode(parent) != null)
                    {
                        throw new LoopException(parent, initialNode.Value!);
                    }

                    var childNode = curNode;
                    curNode = tree.FindNode(parent);
                    var found = curNode != null;

                    curNode ??= new TreeNode<Type>(parent);
                    curNode.AddChild(childNode);

                    if (found)
                    {
                        curNode = null; // No need to add
                        break; // Quit because the rest of the chain is already done
                    }

                    ___AddParentsToTree(tree, parent);
                    parent = parentAttribute?.Parent;
                }

                if (curNode != null)
                {
                    tree.AddChild(curNode);
                }
            }
        }

        /// <summary>
        /// Add all commands types under the namespaces from commands types having a <see cref="ChildrenAttribute"/> to them. <br/><br/>
        /// - If the parent (the command with <see cref="ChildrenAttribute"/>) isn't in the tree, it is added at the root.<br/>
        /// - If the command type is already in the tree without a <see cref="ParentAttribute"/>, it is moved to the parent.<br/>
        /// - Otherwise, the command type is added to the parent.
        /// </summary>
        /// <remarks>
        /// <see cref="ParentAttribute"/> has precedence over <see cref="ChildrenAttribute"/>
        /// </remarks>
        /// <param name="tree"></param>
        private void AddChildrenToTree(TreeNode<Type> tree)
        {
            var commandsWithChildren = CommandsTypes.Where(t => t.GetCustomAttributes(false).Any(a => a is ChildrenAttribute));
            foreach (var command in commandsWithChildren)
            {
                IEnumerable<string> childrenNamespace = command.GetCustomAttributes(false)
                    .Where(c => c is ChildrenAttribute)
                    .Select(a => (a as ChildrenAttribute)?.Namespace ?? string.Empty)
                    .Where(s => s != string.Empty);

                // Parent
                var parentNode = tree.FindNode(command);
                parentNode ??= tree.AddChild(command);

                // Children for each namespace
                var children = CommandsTypes.Where(t =>
                    !t.GetCustomAttributes(false).Any(a => a is ParentAttribute) &&
                    childrenNamespace.Any(n => t.Namespace?.StartsWith(n) ?? false)
                );

                foreach (var child in children)
                {
                    // No need to check for children of children because the main loop take care of this

                    var childNode = tree.FindNode(child);
                    if (childNode != null) // This child node shall have been under current parent node
                    {
                        var currentParent = parentNode;
                        while (currentParent != null)
                        {
                            if (currentParent.Parent?.Value == child)
                            {
                                throw new LoopException(currentParent.Value!, child);
                            }
                            currentParent = currentParent.Parent;
                        }

                        childNode.MoveNode(parentNode);
                    }
                    else
                    {
                        parentNode.AddChild(child);
                    }
                }
            }
        }

        /// <summary>
        /// Add commands types not already in the tree at the root
        /// </summary>
        /// <param name="tree"></param>
        private void AddLonelyToTree(TreeNode<Type> tree)
        {
            var lonelyCommands = CommandsTypes.Except(tree.FlattenValues());
            foreach (var command in lonelyCommands)
            {
                tree.AddChild(command);
            }
        }

        /// <summary>
        /// Add commands types using their namespace to build the tree. <br/>
        /// - If the command type is not already in the tree.<br/>
        /// - If the commant type is in the tree, but shall be under type, move it (except if <see cref="ParentAttribute"/> is present.
        /// </summary>
        /// <param name="tree"></param>
        /// <remarks>To be a parent, the command type has to be alone in its namespace</remarks>
        private void AddByNamespacesHierachy(TreeNode<Type> tree, bool noMoveIfParentAttribute)
        {
            var lonelyCommands = CommandsTypes.GroupBy(t => t.Namespace).Where(g => g.Count() == 1).SelectMany(g => g);
            foreach (var command in lonelyCommands)
            {
                var parentNode = tree.FindNode(command);
                parentNode ??= tree.AddChild(command);

                var children = CommandsTypes.Where(t => t.Namespace?.StartsWith(parentNode.Value!.Namespace + ".") ?? false);
                foreach (var child in children)
                {
                    var childNode = tree.FindNode(child);
                    if (childNode != null)
                    {
                        // Move only if the namespace is under previous one or if no parent
                        var currentNamespace = childNode.Parent?.Value?.Namespace ?? string.Empty;
                        var isDeeperNamespace = parentNode.Value!.Namespace?.StartsWith(currentNamespace) ?? false;
                        var hasParentAttribute = !noMoveIfParentAttribute || !childNode.Value!.GetCustomAttributes(false).Any(a => a is ParentAttribute);
                        if (hasParentAttribute && isDeeperNamespace)
                        {
                            childNode.MoveNode(parentNode);
                        }
                    }
                    else
                    {
                        parentNode.AddChild(child);
                    }
                }
            }
        }
    }
}
