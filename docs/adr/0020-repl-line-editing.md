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

Drawing uses `\r`, the prompt, the buffer, spaces to clear any leftover from a longer line, then backspaces to put the cursor back. No ANSI escape sequence is written.

## Completion and Command States

A command that is unavailable in the current state is hidden ([ADR 0012](0012-command-states.md)). System.CommandLine does not propose hidden commands, so they are not proposed either, and they are proposed again after the state changes.

## Redirected Input

When `Console.IsInputRedirected` is true, or `LineEditing` is false, the provider writes the prompt and reads with `Console.In.ReadLineAsync`, exactly as before. Integration tests and pipes keep this behavior.

## Limits

- The history is not saved to disk.
- Lines wider than the console are not handled.
- There are no colors.
- The cancellation token is checked before an edited line is read, not while `Console.ReadKey` waits.

## Consequences

The interactive REPL gets the Up arrow and Tab. Behavior is unchanged when the input is redirected. The editor is reused across lines and is tested with simulated keys.
