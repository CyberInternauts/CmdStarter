# 0020. REPL Line Editing

Status: Accepted
Date: 2026-11-04

## Context

The console REPL read raw lines with `Console.In.ReadLineAsync`. A user could not recall the previous command with the Up arrow, and could not complete a command, option or value with Tab.

## Decision

Add `ReplLineEditor`, a public sealed class in the `CyberInternauts.CmdStarter.Repl` namespace. It reads keys from a `Func<ConsoleKeyInfo>` and writes to a `TextWriter`, so tests can drive it with simulated keys. Its members are:

- `HistorySize` (init-only, 100 by default, at least 1);
- `History`, from the oldest line to the most recent;
- `CompletionSource`, a `Func<string, int, IEnumerable<string>>` that receives the line and the cursor position and returns candidates;
- `ReadLine(string prompt)`, which returns the line, or `null` at end of input.

`ConsoleReplProvider` gains two init-only settings, `LineEditing` (true by default) and `HistorySize` (100), and a settable `CompletionSource`. It keeps one editor for the whole session, so the history lasts for the session. It reads keys with `Console.ReadKey(intercept: true)` and writes to `Console.Out`.

`ReplStarter.GetCompletions(string line, int position)` returns a sorted (ordinal), duplicate-free list. It builds the tree and applies command availability as `Start` does. It then takes the labels of the System.CommandLine completions (`RootCommand.Parse(line).GetCompletions(position)`): visible commands, options and `[AutoComplete]` values. When the cursor is in the first word, it also adds the built-in words (`ExitCommands`, `HelpCommands`) that start with the typed text. After a help word (compared without case) followed by white space, the rest of the line is completed as a command line, without the built-in words.

The three `ReplStarter` constructors give `GetCompletions` to a `ConsoleReplProvider` that has no `CompletionSource`. A source set by the application is kept.

## Keys

- A printable character is inserted at the cursor.
- `Backspace` deletes before the cursor, and `Delete` deletes under it.
- `LeftArrow`, `RightArrow`, `Home` and `End` move the cursor.
- `Escape` clears the line.
- `UpArrow` and `DownArrow` move through the history. The line being edited is kept as a draft and comes back after the most recent entry.
- `Enter` writes a line break and returns the line. The line enters the history unless it is empty or white space, or identical to the most recent entry. Beyond `HistorySize`, the oldest entry is dropped.
- `Tab` completes the current word, which runs from the last space before the cursor to the cursor. With one candidate, the word is replaced by the candidate followed by a space. With several, the word is completed to their longest common prefix. If that prefix adds nothing, the candidates are written on a new line (sorted, separated by two spaces), then the prompt and the line are redrawn. With none, nothing happens.
- `Ctrl+D` and `Ctrl+Z` (without Alt, since on Windows AltGr is reported as Ctrl+Alt) end the input when the line is empty. Otherwise they are ignored.
- Any other key is ignored.

Amended on 2026-11-05 (lot 038): `Ctrl+C` (without Alt) abandons the line. The editor writes the rest of the line from the cursor, then `^C` and a line break, empties the buffer, forgets the draft, puts the history position back at its end and writes the prompt again. `ReadLine` returns nothing at that point, and the history does not change. Ctrl+Alt+C, which some keyboards report for AltGr, is not an interruption and follows the rule of any other key.

`ConsoleReplProvider` sets `Console.TreatControlCAsInput` to `true` while the editor reads a line, so that Ctrl+C reaches the editor as a key, and restores the value it had before, not `false`, because an application may have set it itself. A running command therefore receives the signal as before, and returns 130 ([ADR 0011](0011-repl-mode.md)). Measured on 2026-11-05, with ConPTY on Windows (`\x03` and the win32-input-mode sequence) and a Linux pty (`script`): the key arrives as `C` with Control and `KeyChar` 0x03 on both sides.

- Residual window: a Ctrl+C between the end of the read and the registration of the command, between two steps of a sequence, or during an `OnCommandExecuted` handler still ends the process.
- Windows: Ctrl+Break stays a signal that ends the process, which is the way out.
- Linux and macOS: .NET switches ISIG off as a whole, so at the prompt Ctrl+\ is a key that the editor ignores, and Ctrl+Z follows the rule of the editor (end of input on an empty line, ignored otherwise) instead of suspending the process. There is no way out by signal at the prompt any more: `exit`, `quit`, Ctrl+D and the token of `Launch` remain.
- Without the line editor the process termination is kept. Measured on 2026-11-05: with `Cancel = true`, the read returns `null` before the handler runs, which looks like Ctrl+Z; with `TreatControlCAsInput = true`, Enter arrives as a lone CR and a line is lost.

Two other options were set aside. (B) Leaving Ctrl+C a signal and warning the editor does not work: `Console.ReadKey` stays blocked until a key arrives, so no new prompt appears before the next key, unless an input is injected by a Windows P/Invoke. (C) Ignoring only the signal keeps the line without a new prompt, and would make the REPL end without the editor.

Drawing uses `\r`, the prompt, the buffer, spaces to clear any leftover from a longer line, then backspaces to put the cursor back. No ANSI escape sequence is written.

## Completion and Command States

A command that is unavailable in the current state is hidden ([ADR 0012](0012-command-states.md)). System.CommandLine does not propose hidden commands, so they are not proposed either, and they are proposed again after the state changes.

## Redirected Input

Line editing needs a terminal on both sides. When `Console.IsInputRedirected` or `Console.IsOutputRedirected` is true, or `LineEditing` is false, the provider writes the prompt and reads with `Console.In.ReadLineAsync`, exactly as before. Integration tests and pipes keep this behavior. With only the output redirected (`app > log.txt` typed at a keyboard), editing would read keys without echo and write the redraws (`\r`, backspaces) into the file, so the console echoes the typing instead.

## Limits

- The history is not saved to disk.
- Lines wider than the console are not handled.
- There are no colors.
- The cancellation token is checked before an edited line is read, not while `Console.ReadKey` waits.

## Consequences

The interactive REPL gets the Up arrow and Tab. Behavior is unchanged when the input is redirected. The editor is reused across lines and is tested with simulated keys.
