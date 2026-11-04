using System.Diagnostics.CodeAnalysis;

namespace CyberInternauts.CmdStarter.Interfaces;

/// <summary>
/// Interface to implement command line execution of a class
/// </summary>
public interface IStarterCommand
{
    /// <summary>
    /// Empty body method
    /// </summary>
    public readonly static Action EmptyExecution = () => { };

    /// <summary>
    /// Get/set the global options manager
    /// </summary>
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    /// <summary>
    /// Method that handles execution
    /// </summary>
    public Delegate HandlingMethod { get; }

    /// <summary>
    /// Create an instance of the command using its parameterless constructor. A command type can
    /// declare its own static method with this signature to control creation; it must return a new instance per call.
    /// </summary>
    /// <typeparam name="TCommand">Command type to get the instance of</typeparam>
    /// <returns>An instance of the class implementing <see cref="IStarterCommand"/></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "TCommand is a command type, and the command catalog keeps all the members of the commands it declares, their parameterless constructor included.")]
    public static virtual IStarterCommand GetInstance<TCommand>() where TCommand : IStarterCommand
    {
        return Activator.CreateInstance<TCommand>();
    }
}
