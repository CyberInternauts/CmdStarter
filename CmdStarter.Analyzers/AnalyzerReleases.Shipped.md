; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CMDS0001 | Usage | Warning | Parent cycle
CMDS0002 | Usage | Warning | Multiple parent attributes
CMDS0003 | Usage | Warning | Auto-complete has no values
CMDS0004 | Usage | Warning | Unsupported auto-complete type
CMDS0005 | Usage | Warning | Empty command name
CMDS0006 | Usage | Warning | Command method is not public
CMDS0007 | Usage | Warning | Command method is generic
CMDS0008 | Usage | Warning | Command parameter is by reference
CMDS0009 | Usage | Warning | Mixed command models
CMDS0010 | Usage | Warning | Abstract instance command
CMDS0011 | Usage | Warning | Command class without a handler
CMDS0012 | Usage | Warning | Global option is hidden by a command option
CMDS0013 | Usage | Warning | Global options container is empty
CMDS0014 | Usage | Info | Type is not in the command catalog
CMDS0015 | Usage | Info | HandlingMethod cannot be read at compile time
CMDS0016 | Usage | Info | Value type cannot be registered in the command catalog
CMDS0017 | Usage | Warning | Invalid command handler
