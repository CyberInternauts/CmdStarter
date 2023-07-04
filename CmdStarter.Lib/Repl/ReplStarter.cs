﻿using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl
{
    /// <summary>
    /// Main class executing the command using the command line arguments in REPL mode.
    /// </summary>
    public sealed class ReplStarter : Starter
    {
        /// <summary>
        /// Gets invoked after the execution of a command.
        /// </summary>
        public event EventHandler<ReplCommandEventArgs>? OnCommandExecuted;

        private readonly IReplInputProvider inputProvider;

        /// <summary>
        /// If <see langword="true"/> REPL mode loop runs; otherwise it won't.
        /// </summary>
        private bool allowReplLoop = true;

        /// <summary>
        /// Constructor without namespace filters.
        /// </summary>
        /// <param name="inputProvider">Provides input for command parsing.</param>
        public ReplStarter(IReplInputProvider inputProvider) : base()
        {
            this.inputProvider = inputProvider;
        }

        /// <summary>
        /// Constructor with namespace filters.
        /// </summary>
        /// <param name="inputProvider">Provides input for command parsing.</param>
        /// <param name="namespaces">Array of included namespaces.</param>
        public ReplStarter(IReplInputProvider inputProvider, string[] namespaces) : base(namespaces)
        {
            this.inputProvider = inputProvider;
        }

        /// <summary>
        /// Constructor with namespace filters.
        /// </summary>
        /// <param name="inputProvider">Provides input for command parsing.</param>
        /// <param name="namespaces">Array of included namespaces.</param>
        public ReplStarter(IReplInputProvider inputProvider, List<string> namespaces) : base(namespaces)
        {
            this.inputProvider = inputProvider;
        }

        /// <summary>
        /// Runs the first command according to the args and starts the REPL mode loop.
        /// </summary>
        /// <param name="args">Arguments for the first command.</param>
        /// <param name="terminator">
        /// If this <see cref="string"/> is returned by the <see cref="IReplInputProvider"/> the REPL loop stops.
        /// </param>
        async public Task Launch(string[] args, string? terminator = null)
        {
            await Start(args);

            await Launch(terminator);
        }

        /// <summary>
        /// Starts the REPL mode loop.
        /// </summary>
        /// <param name="terminator">
        /// If this <see cref="string"/> is returned by the <see cref="IReplInputProvider"/> the REPL loop stops.
        /// </param>
        async public Task Launch(string? terminator = null)
        {
            allowReplLoop = true;

            while (allowReplLoop)
            {
                var inputToBeParsed = inputProvider.GetInput();

                if (string.Equals(terminator, inputToBeParsed)) return;

                await Start(inputToBeParsed);
            }
        }

        /// <summary>
        /// Stops the REPL loop.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Launch(string?)"/> or <see cref="Launch(string[], string?)"/> to start again.
        /// </remarks>
        public void Stop()
        {
            allowReplLoop = false;
        }

        /// <inheritdoc cref="Starter.Start(string[])"/>
        /// <remarks>This raises the <see cref="OnCommandExecuted"/> event.</remarks>
        public async new Task<int> Start(string[] args)
        {
            var returnCode = await Start(args);

            var eventArgs = new ReplCommandEventArgs(returnCode);
            OnCommandExecuted?.Invoke(this, eventArgs);

            return returnCode;
        }

        /// <inheritdoc cref="Starter.Start(string)"/>
        /// <remarks>This raises the <see cref="OnCommandExecuted"/> event.</remarks>
        public async new Task<int> Start(string args)
        {
            var returnCode = await Start(args);

            var eventArgs = new ReplCommandEventArgs(returnCode);
            OnCommandExecuted?.Invoke(this, eventArgs);

            return returnCode;
        }
    }
}
