// Sample: the command tree described in code with Starter.UseTree, without [Parent] attributes or namespaces.

using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Samples.FluentTree.Commands;

return await new Starter()
    .UseTree(tree => tree
        .Add<Users>(users => users
            .Add<ListUsers>(list => list.WithAliases("ls")))
        .Add<Groups>(groups => groups
            .Add<ListGroups>())
        .Group("admin", admin => admin
            .Add<Reset>()))
    .Start(args);
