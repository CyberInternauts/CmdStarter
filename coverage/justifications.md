# Coverage justifications

Lines of `CmdStarter.Lib`, `CmdStarter.Hosting` and `CmdStarter.Analyzers` that no test can reach without changing the library. `measure-coverage.ps1` lists the uncovered lines in `coverage/report/uncovered.md`; a line only goes here when the lot's decision-maker has accepted that it cannot be covered. No line is excluded from the measure with `[ExcludeFromCodeCoverage]`: the measure stays honest and this file explains the gap.

## Entry format

Each entry is a level-3 heading `File:line`, then four items:

- **File**: path from the repository root.
- **Line**: line number at the time of the entry (the exact code below allows finding it again).
- **Code**: the exact code, in a code block.
- **Reason**: why the line is unreachable by a test.
- **Why no test can reach it without changing the library**: what a test would need to change (a visibility, a seam, a branch) and why that is not acceptable.

## Entries

None yet.
