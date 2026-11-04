# 0014. Validation Attributes

Status: Accepted
Date: 2026-11-03

## Context

CmdStarter creates System.CommandLine options and arguments from class properties, handler parameters, and method parameters. Applications expect standard `System.ComponentModel.DataAnnotations.ValidationAttribute` annotations on those source members to validate values entered on the command line.

## Decision

CmdStarter applies every `ValidationAttribute` on a created option's or argument's source property or parameter.
Validation runs on the converted value during System.CommandLine parsing, using a `ValidationContext` whose `DisplayName` and `MemberName` are the option name without its prefix or the argument name.
An option or argument is validated only when the parse result contains an explicit value. An absent optional option or argument is not validated, including when it has a default value.
`[Required]` continues to make an option required and also runs as a validation attribute, so an explicitly supplied empty string is rejected.
Validation failures are added to System.CommandLine's parse errors. They produce a non-zero exit code, are written to the error output, and prevent the handler from running.
Applications can continue to add custom validators through `FindCommand<T>()` and the symbol's `Validators` collection.
DataAnnotations default error messages are in English. Set `ErrorMessage` on the attribute to replace the default message.

## Consequences

Standard DataAnnotations validation works consistently for property options, global options, class handler arguments, and method arguments and options. Applications retain full control to add custom System.CommandLine validators.
