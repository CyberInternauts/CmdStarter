using System.Reflection.Emit;

namespace CyberInternauts.CmdStarter.Tests.Commands.ExecutionPaths;

// The handler is a dynamic method: its parameters have no name, so none of them can be an argument.
// It returns the first parameter plus 2 when the second one is null and plus 1 otherwise.
public sealed class NamelessParameterCommand : StarterCommand
{
    public override Delegate HandlingMethod
    {
        get
        {
            var method = new DynamicMethod("Nameless", typeof(int), new[] { typeof(int), typeof(string) }, typeof(NamelessParameterCommand).Module);
            var il = method.GetILGenerator();
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brtrue_S, notNull);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);
            return method.CreateDelegate(typeof(Func<int, string?, int>));
        }
    }
}
