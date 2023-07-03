using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Lib.Repl
{
    /// <summary>
    /// Main class executing the command using the command line arguments in REPL mode.
    /// </summary>
    public sealed class ReplStarter : Starter
    {
        private readonly IReplInputProvider inputProvider;

        /// <summary>
        /// If <see langword="true"/> REPL mode loop runs; otherwise it won't.
        /// </summary>
        private bool allowReplLoop = true;

        /// <summary>
        /// Constructor without namespace filters.
        /// </summary>
        /// <remarks>
        /// Uses the default <see cref="ConsoleReplProvider"/>.
        /// </remarks>
        public ReplStarter() : base()
        {
            inputProvider = new ConsoleReplProvider();
        }

        /// <summary>
        /// Constructor with namespace filters.
        /// </summary>
        /// <remarks>
        /// Uses the default <see cref="ConsoleReplProvider"/>.
        /// </remarks>
        /// <param name="namespaces">Array of included namespaces.</param>
        public ReplStarter(string[] namespaces) : base(namespaces)
        {
            inputProvider = new ConsoleReplProvider();
        }

        /// <summary>
        /// Constructor with namespace filters.
        /// </summary>
        /// <remarks>
        /// Uses the default <see cref="ConsoleReplProvider"/>.
        /// </remarks>
        /// <param name="namespaces">Array of included namespaces.</param>
        public ReplStarter(List<string> namespaces) : base(namespaces)
        {
            inputProvider = new ConsoleReplProvider();
        }

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
        /// Use <see cref="Launch"/> to start again.
        /// </remarks>
        public void Stop()
        {
            allowReplLoop = false;
        }
    }
}
