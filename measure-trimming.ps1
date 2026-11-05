<#
.SYNOPSIS
    Measures CmdStarter under trimming and Native AOT, and writes the table in coverage/trimming/trimming.md.

.DESCRIPTION
    1. builds CmdStarter.sln in Release, which writes the package cints.CmdStarter in artefacts/;
    2. recreates, outside the repository, $env:TEMP\CmdStarter-trimming\HelloWorld, ...\DevOpsTool and ...\Lists: an application that references the package
       (RestoreSources = artefacts/ and nuget.org, RestorePackagesPath = $env:TEMP\CmdStarter-trimming\packages, so the global NuGet cache of the user is never read or written)
       and holds a copy of the sources of the sample (Lists, which has the collection options, is written by the script);
    3. publishes each application with -p:PublishTrimmed=true and with -p:PublishAot=true (-c Release -r win-x64 --self-contained);
    4. runs each executable and records the exit code, the first line of the output and the first line of the error output;
    5. writes coverage/trimming/trimming.md (coverage/trimming/ is ignored by git).
    The script exits with a non-zero code when a publication or a run does not give the expected result. It is not part of dotnet test.

.PARAMETER SkipBuild
    Does not build the solution (the package of artefacts/ must be the one to measure). For tries of the script only.
#>
[CmdletBinding()]
param(
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = $PSScriptRoot
$artefacts = Join-Path $repositoryRoot 'artefacts'
$workRoot = Join-Path $env:TEMP 'CmdStarter-trimming'
$packagesPath = Join-Path $workRoot 'packages'
$reportFolder = Join-Path $repositoryRoot 'coverage/trimming'
$reportPath = Join-Path $reportFolder 'trimming.md'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$stopwatch = [Diagnostics.Stopwatch]::StartNew()

Set-Location $repositoryRoot

$versionMatch = [regex]::Match((Get-Content -LiteralPath (Join-Path $repositoryRoot 'ProjectsCommon.props') -Raw), '<Version>([^<]+)</Version>')
if (-not $versionMatch.Success) { throw 'The version was not found in ProjectsCommon.props.' }
$packageVersion = $versionMatch.Groups[1].Value

# The applications: the sample to copy, the commands to run, and what each must give.
# Expect = 'Output' (exit code 0 and the first line of the output), 'OutputAndError' (the same, and the first line of the error output is ErrorLine), 'AnyOutput' (exit code 0 and a non-empty output),
# 'ParseError' (an exit code from 1 to 255, and no unhandled exception).
# The third application is written by the script itself. It has what the two samples do not have:
# - the collection options (a List<string> option, a string[] argument, an int[] option), which are read through GetInterfaces (command `show`);
# - a service provider set on the starter with one service, IGreeter, and no command registered in it. The command `greet` takes IGreeter in its constructor, so it is created by ActivatorUtilities;
# - a filter registered by type with AddFilter<T>, which has two constructors, one that takes IGreeter: it is created by ActivatorUtilities, which has to choose between them, and writes a line on the error
#   output that tells which constructor served;
# - a command `made` that declares its own static GetInstance, so it is created through FindGetInstanceMethod (MakeGenericMethod on a class, then Invoke), and shows that this method made it. Its execution
#   still has the scope of the service provider (ADR 0006, amended on 2026-11-04), so its filter is created with the constructor that takes IGreeter, like the one of the other commands;
# - a class with a method marked [Command] that has an [Option] List<string> parameter and an int parameter (command `tag`), created by ActivatorUtilities too;
# - a plain command (command `reporter`): a class marked [Command] that has no base class or interface of CmdStarter, whose handler is named by Handler = nameof(Report) and INHERITED from a base class
#   (GetMethods without DeclaredOnly), with an option property that is inherited too (the properties of the base classes), an argument, and a global options container as a parameter.
# The commands are not rooted, so that the command name is on the command line.
# The fourth application, Tree, places its commands with Starter.UseTree, under a group; with the mode `ct` it sets Starter.CommandsTypes to a type of the catalog instead.
$listsProgram = @'
using CyberInternauts.CmdStarter;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.Lists;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter, Greeter>();
        var starter = new Starter { IsRootingLonelyCommand = false, ServiceProvider = services.BuildServiceProvider() };
        starter.AddFilter<MarkFilter>();
        return starter.Start(args);
    }
}
'@
$listsServices = @'
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.Lists;

public interface IGreeter
{
    string Greet(string name);
}

public sealed class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello, {name}!";
}

public sealed class MarkFilter : ICommandFilter
{
    private readonly string message;

    // Not reached by the measure since ADR 0006 was amended (2026-11-04): every command of this application has the service provider,
    // made included. It stays so that ActivatorUtilities still has two constructors to choose from.
    public MarkFilter() => message = "filter: no container";

    public MarkFilter(IGreeter greeter) => message = $"filter: {greeter.Greet("filter")}";

    public Task<int> InvokeAsync(CommandContext context, Func<Task<int>> next)
    {
        context.Console.Error.WriteLine(message);
        return next();
    }
}
'@
$listsShow = @'
namespace CyberInternauts.CmdStarter.Samples.Lists;

public sealed class Show : StarterCommand
{
    public List<string> Names { get; set; } = new();

    public int[] Numbers { get; set; } = [];

    public override Delegate HandlingMethod => (string[] values) =>
        Console.WriteLine($"names={string.Join(",", Names)} values={string.Join(",", values)} numbers={string.Join(",", Numbers)}");
}
'@
$listsGreet = @'
namespace CyberInternauts.CmdStarter.Samples.Lists;

public sealed class Greet(IGreeter greeter) : StarterCommand
{
    public string Who { get; set; } = "World";

    public override Delegate HandlingMethod => () => Console.WriteLine(greeter.Greet(Who));
}
'@
$listsMade = @'
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.Lists;

public sealed class Made : StarterCommand
{
    private string origin = "the constructor";

    public new static IStarterCommand GetInstance<TCommand>() where TCommand : IStarterCommand => new Made { origin = "GetInstance" };

    public override Delegate HandlingMethod => () => Console.WriteLine($"made by {origin}");
}
'@

$listsTagger = @'
using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.Lists;

public sealed class Tagger
{
    [Command]
    public int Tag(string label, [Option] List<string> tags, int count)
    {
        Console.WriteLine($"label={label} tags={string.Join(",", tags)} count={count}");
        return 0;
    }
}
'@

$listsReporter = @'
using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Samples.Lists;

public sealed class ListsGlobals : IGlobalOptionsContainer
{
    public int Level { get; set; } = 3;
}

public abstract class ReporterBase
{
    public string Prefix { get; set; } = "report";

    public int Report(string text, ListsGlobals globals)
    {
        Console.WriteLine($"{Prefix}: {text} level={globals.Level}");
        return 0;
    }
}

[Command(Handler = nameof(Report))]
public sealed class Reporter : ReporterBase
{
}
'@

# The fourth application places its commands with Starter.UseTree, under a group.
$treeProgram = @'
using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Tree;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        // The mode "ct" sets CommandsTypes to a type of the catalog, instead of describing a tree; the mode is not passed to the starter.
        if (args.Length > 0 && args[0] == "ct")
        {
            return new Starter { CommandsTypes = [typeof(Show)], IsRootingLonelyCommand = false }.Start(args[1..]);
        }

        return new Starter { IsRootingLonelyCommand = false }
            .UseTree(tree => tree.Group("tools", tools => tools.Add<Show>()))
            .Start(args);
    }
}
'@
$treeShow = @'
namespace CyberInternauts.CmdStarter.Samples.Tree;

public sealed class Show : StarterCommand
{
    public string Label { get; set; } = "none";

    public override Delegate HandlingMethod => () => Console.WriteLine($"label={Label}");
}
'@

$applications = @(
    [pscustomobject]@{
        Name   = 'HelloWorld'
        Sample = 'samples/CmdStarter.Samples.HelloWorld'
        Files  = $null
        Runs   = @(
            [pscustomobject]@{ Arguments = '--to Bob'; Expect = 'Output'; Line = 'Hello, Bob!' }
        )
    },
    [pscustomobject]@{
        Name   = 'DevOpsTool'
        Sample = 'samples/CmdStarter.Samples.DevOpsTool'
        Files  = $null
        Runs   = @(
            [pscustomobject]@{ Arguments = 'service deploy api --version 1.2.3'; Expect = 'Output'; Line = 'Deploying api 1.2.3 to dev' },
            [pscustomobject]@{ Arguments = '--help'; Expect = 'AnyOutput'; Line = '' },
            [pscustomobject]@{ Arguments = 'service deploy'; Expect = 'ParseError'; Line = '' }
        )
    },
    [pscustomobject]@{
        Name   = 'Lists'
        Sample = $null
        Files  = [ordered]@{ 'Program.cs' = $listsProgram; 'Services.cs' = $listsServices; 'Show.cs' = $listsShow; 'Greet.cs' = $listsGreet; 'Made.cs' = $listsMade; 'Tagger.cs' = $listsTagger; 'Reporter.cs' = $listsReporter }
        Runs   = @(
            [pscustomobject]@{ Arguments = 'show a b --names x --names y --numbers 1 2'; Expect = 'Output'; Line = 'names=x,y values=a,b numbers=1,2' },
            [pscustomobject]@{ Arguments = 'greet --who Bob'; Expect = 'Output'; Line = 'Hello, Bob!' },
            [pscustomobject]@{ Arguments = 'made'; Expect = 'OutputAndError'; Line = 'made by GetInstance'; ErrorLine = 'filter: Hello, filter!' },
            [pscustomobject]@{ Arguments = 'tag red 3 --tags a --tags b'; Expect = 'Output'; Line = 'label=red tags=a,b count=3' },
            [pscustomobject]@{ Arguments = 'show c --names z --numbers 3'; Expect = 'OutputAndError'; Line = 'names=z values=c numbers=3'; ErrorLine = 'filter: Hello, filter!' },
            [pscustomobject]@{ Arguments = 'reporter hello --prefix note --level 5'; Expect = 'OutputAndError'; Line = 'note: hello level=5'; ErrorLine = 'filter: Hello, filter!' }
        )
    },
    [pscustomobject]@{
        Name   = 'Tree'
        Sample = $null
        Files  = [ordered]@{ 'Program.cs' = $treeProgram; 'Show.cs' = $treeShow }
        Runs   = @(
            [pscustomobject]@{ Arguments = 'tools show --label x'; Expect = 'Output'; Line = 'label=x' },
            [pscustomobject]@{ Arguments = 'tools --help'; Expect = 'AnyOutput'; Line = '' },
            [pscustomobject]@{ Arguments = 'ct show --label y'; Expect = 'Output'; Line = 'label=y' }
        )
    }
)
$modes = @(
    [pscustomobject]@{ Name = 'Trimmed'; Property = '-p:PublishTrimmed=true' },
    [pscustomobject]@{ Name = 'Native AOT'; Property = '-p:PublishAot=true' }
)

# Makes a text safe for a cell of a Markdown table (one line, no pipe, at most 220 characters).
function ConvertTo-Cell {
    param([string] $text)

    if ([string]::IsNullOrEmpty($text)) { return '' }
    $oneLine = ($text -replace '\s+', ' ').Trim().Replace('|', '\|')
    if ($oneLine.Length -gt 220) { $oneLine = $oneLine.Substring(0, 220) + '...' }
    return $oneLine
}

function Get-FolderSize {
    param([string] $path)

    $total = (Get-ChildItem -LiteralPath $path -Recurse -File -Force | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $total) { return 0 }
    return [long] $total
}

# Runs an executable, waits at most $timeoutSeconds, and returns its exit code and its two outputs.
function Invoke-Executable {
    param([string] $path, [string] $arguments, [int] $timeoutSeconds = 120)

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $path
    $info.Arguments = $arguments
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.RedirectStandardInput = $true
    $info.StandardOutputEncoding = $utf8
    $info.StandardErrorEncoding = $utf8
    $process = [Diagnostics.Process]::Start($info)
    $process.StandardInput.Close()
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($timeoutSeconds * 1000)) {
        $process.Kill($true)
        return [pscustomobject]@{ ExitCode = $null; Output = $outputTask.Result; Error = "Timeout after $timeoutSeconds seconds." }
    }

    $process.WaitForExit()
    return [pscustomobject]@{ ExitCode = $process.ExitCode; Output = $outputTask.Result; Error = $errorTask.Result }
}

function Get-FirstLine {
    param([string] $text)

    foreach ($line in ($text -split "`r?`n")) {
        if ($line.Trim().Length -gt 0) { return $line.Trim() }
    }

    return ''
}

# --- 1. The package ---------------------------------------------------------------------------------------------------------------------------------------------

if (-not $SkipBuild) {
    Write-Host 'Building CmdStarter.sln in Release...'
    dotnet build CmdStarter.sln -c Release -nologo -v:q
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed (exit code $LASTEXITCODE): nothing was measured."
        exit $LASTEXITCODE
    }
}

$packageFile = Join-Path $artefacts "cints.CmdStarter.$packageVersion.nupkg"
if (-not (Test-Path -LiteralPath $packageFile)) {
    Write-Error "The package $packageFile does not exist."
    exit 1
}

$builtHash = (Get-FileHash -LiteralPath $packageFile -Algorithm SHA256).Hash

# --- 2. The applications ----------------------------------------------------------------------------------------------------------------------------------------

Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $workRoot, $packagesPath | Out-Null

$sources = (@($artefacts, 'https://api.nuget.org/v3/index.json')) -join ';'
foreach ($application in $applications) {
    $directory = Join-Path $workRoot $application.Name
    New-Item -ItemType Directory -Path $directory | Out-Null

    if ($null -ne $application.Sample) {
        $sampleFolder = Join-Path $repositoryRoot $application.Sample
        foreach ($file in Get-ChildItem -LiteralPath $sampleFolder -Recurse -File -Filter '*.cs') {
            $relative = [IO.Path]::GetRelativePath($sampleFolder, $file.FullName)
            if ($relative -match '^(bin|obj)[\\/]') { continue }

            $destination = Join-Path $directory $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination
        }
    }
    else {
        foreach ($name in $application.Files.Keys) {
            [IO.File]::WriteAllText((Join-Path $directory $name), $application.Files[$name], $utf8)
        }
    }

    # Same settings as ProjectsCommon.props, but without TreatWarningsAsErrors: the IL warnings of the publication must not stop the measure.
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <RestoreSources>$sources</RestoreSources>
    <RestorePackagesPath>$packagesPath</RestorePackagesPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="cints.CmdStarter" Version="$packageVersion" />
  </ItemGroup>

</Project>
"@
    [IO.File]::WriteAllText((Join-Path $directory "$($application.Name).csproj"), $project, $utf8)
}

# --- 3. The publications and the runs ---------------------------------------------------------------------------------------------------------------------------

# Native AOT needs vswhere.exe on the PATH of the publishing process (it is in the folder of the Visual Studio installer).
$vswhereFolder = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
$originalPath = $env:PATH
if (Test-Path -LiteralPath (Join-Path $vswhereFolder 'vswhere.exe')) {
    $env:PATH = "$vswhereFolder;$env:PATH"
}
else {
    Write-Warning "vswhere.exe was not found in ${vswhereFolder}: the Native AOT publications may fail."
}

$publications = New-Object System.Collections.Generic.List[object]
$runs = New-Object System.Collections.Generic.List[object]
$failures = New-Object System.Collections.Generic.List[string]

try {
    foreach ($application in $applications) {
        $directory = Join-Path $workRoot $application.Name
        $projectFile = Join-Path $directory "$($application.Name).csproj"

        foreach ($mode in $modes) {
            Write-Host "Publishing $($application.Name), $($mode.Name)..."
            Remove-Item -LiteralPath (Join-Path $directory 'bin'), (Join-Path $directory 'obj') -Recurse -Force -ErrorAction SilentlyContinue
            $outputFolder = Join-Path $directory ("publish-" + ($mode.Name -replace '\s', ''))
            $logFile = Join-Path $directory ("publish-" + ($mode.Name -replace '\s', '') + '.log')
            dotnet publish $projectFile -c Release -r win-x64 --self-contained $mode.Property -o $outputFolder -nologo *> $logFile
            $publishExit = $LASTEXITCODE

            $log = Get-Content -LiteralPath $logFile
            # The distinct warnings of the publication (code and text), counted by code.
            $warningLines = $log | Where-Object { $_ -match ': warning ([A-Z]+\d+)' } | Sort-Object -Unique
            $codes = @($warningLines | ForEach-Object { [regex]::Match($_, ': warning ([A-Z]+\d+)').Groups[1].Value } | Group-Object | Sort-Object Name | ForEach-Object { "$($_.Name) x$($_.Count)" })
            $executable = Join-Path $outputFolder "$($application.Name).exe"
            $hasOutput = ($publishExit -eq 0) -and (Test-Path -LiteralPath $executable)

            $publications.Add([pscustomobject]@{
                    Application = $application.Name
                    Mode        = $mode.Name
                    ExitCode    = $publishExit
                    Warnings    = if ($codes.Count -gt 0) { $codes -join ', ' } else { 'none' }
                    FolderBytes = if (Test-Path -LiteralPath $outputFolder) { Get-FolderSize $outputFolder } else { 0 }
                    ExeBytes    = if ($hasOutput) { (Get-Item -LiteralPath $executable).Length } else { 0 }
                    Errors      = @($log | Where-Object { $_ -match ': error ' } | Select-Object -First 5)
                })

            if (-not $hasOutput) {
                $failures.Add("$($application.Name), $($mode.Name): the publication failed (exit code $publishExit, see $logFile).")
                continue
            }

            foreach ($run in $application.Runs) {
                $result = Invoke-Executable -path $executable -arguments $run.Arguments
                $firstOutput = Get-FirstLine $result.Output
                $firstError = Get-FirstLine $result.Error
                $crashed = $result.Error -match 'Unhandled exception'

                $ok = switch ($run.Expect) {
                    'Output' { ($result.ExitCode -eq 0) -and ($firstOutput -ceq $run.Line) }
                    'OutputAndError' { ($result.ExitCode -eq 0) -and ($firstOutput -ceq $run.Line) -and ($firstError -ceq $run.ErrorLine) }
                    'AnyOutput' { ($result.ExitCode -eq 0) -and ($firstOutput.Length -gt 0) }
                    'ParseError' { ($null -ne $result.ExitCode) -and ($result.ExitCode -ge 1) -and ($result.ExitCode -le 255) -and (-not $crashed) }
                }

                $expected = switch ($run.Expect) {
                    'Output' { "0, first line '$($run.Line)'" }
                    'OutputAndError' { "0, first line '$($run.Line)', first error line '$($run.ErrorLine)'" }
                    'AnyOutput' { '0, non-empty output' }
                    'ParseError' { 'a parse error (1 to 255)' }
                }

                $runs.Add([pscustomobject]@{
                        Application = $application.Name
                        Mode        = $mode.Name
                        Command     = "$($application.Name).exe $($run.Arguments)"
                        ExitCode    = $result.ExitCode
                        Expected    = $expected
                        Passed      = [bool] $ok
                        FirstOutput = $firstOutput
                        FirstError  = $firstError
                        ErrorText   = $result.Error
                        OutputText  = $result.Output
                    })

                if (-not $ok) {
                    $failures.Add("$($application.Name), $($mode.Name), '$($run.Arguments)': exit code $($result.ExitCode), expected $expected.")
                }
            }
        }
    }
}
finally {
    $env:PATH = $originalPath
}

# --- 4. The package that was restored ---------------------------------------------------------------------------------------------------------------------------

$restoredFile = Join-Path $packagesPath "cints.cmdstarter/$($packageVersion.ToLowerInvariant())/cints.cmdstarter.$($packageVersion.ToLowerInvariant()).nupkg"
$restoredHash = if (Test-Path -LiteralPath $restoredFile) { (Get-FileHash -LiteralPath $restoredFile -Algorithm SHA256).Hash } else { 'not restored' }
$sameHash = $builtHash -eq $restoredHash
if (-not $sameHash) {
    $failures.Add("The restored package is not the one of artefacts/ (SHA-256 $restoredHash instead of $builtHash).")
}

# --- 5. The report ----------------------------------------------------------------------------------------------------------------------------------------------

$report = New-Object System.Text.StringBuilder
[void] $report.AppendLine('# Trimming and Native AOT measures')
[void] $report.AppendLine()
[void] $report.AppendLine("Generated by measure-trimming.ps1 on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'). SDK $(dotnet --version), $([Runtime.InteropServices.RuntimeInformation]::OSDescription), win-x64, Release, self-contained.")
[void] $report.AppendLine()
[void] $report.AppendLine('## Package')
[void] $report.AppendLine()
[void] $report.AppendLine('| Package | SHA-256 | Same |')
[void] $report.AppendLine('| --- | --- | --- |')
[void] $report.AppendLine("| artefacts/cints.CmdStarter.$packageVersion.nupkg | $builtHash | |")
[void] $report.AppendLine("| restored in the private packages folder | $restoredHash | $(if ($sameHash) { 'yes' } else { 'NO' }) |")
[void] $report.AppendLine()
[void] $report.AppendLine('## Publications')
[void] $report.AppendLine()
[void] $report.AppendLine('| Application | Mode | Exit code | Distinct warnings | Folder (bytes) | Executable (bytes) |')
[void] $report.AppendLine('| --- | --- | ---: | --- | ---: | ---: |')
foreach ($publication in $publications) {
    [void] $report.AppendLine("| $($publication.Application) | $($publication.Mode) | $($publication.ExitCode) | $(ConvertTo-Cell $publication.Warnings) | $($publication.FolderBytes) | $($publication.ExeBytes) |")
}

[void] $report.AppendLine()
[void] $report.AppendLine('## Runs')
[void] $report.AppendLine()
[void] $report.AppendLine('| Application | Mode | Command | Exit code | Expected | Result | First line of the output | First line of the error output |')
[void] $report.AppendLine('| --- | --- | --- | ---: | --- | --- | --- | --- |')
foreach ($run in $runs) {
    $verdict = if ($run.Passed) { 'ok' } else { 'FAILED' }
    [void] $report.AppendLine("| $($run.Application) | $($run.Mode) | ``$(ConvertTo-Cell $run.Command)`` | $($run.ExitCode) | $(ConvertTo-Cell $run.Expected) | $verdict | $(ConvertTo-Cell $run.FirstOutput) | $(ConvertTo-Cell $run.FirstError) |")
}

$failedRuns = @($runs | Where-Object { -not $_.Passed })
$failedPublications = @($publications | Where-Object { $_.ExitCode -ne 0 -or $_.ExeBytes -eq 0 })
if ($failedPublications.Count -gt 0 -or $failedRuns.Count -gt 0) {
    [void] $report.AppendLine()
    [void] $report.AppendLine('## Details of the failures')
    foreach ($publication in $failedPublications) {
        [void] $report.AppendLine()
        [void] $report.AppendLine("### Publication: $($publication.Application), $($publication.Mode)")
        [void] $report.AppendLine()
        [void] $report.AppendLine('```text')
        foreach ($line in $publication.Errors) { [void] $report.AppendLine($line) }
        [void] $report.AppendLine('```')
    }

    foreach ($run in $failedRuns) {
        [void] $report.AppendLine()
        [void] $report.AppendLine("### Run: $($run.Application), $($run.Mode), $($run.Command)")
        [void] $report.AppendLine()
        [void] $report.AppendLine('Error output (first 40 lines):')
        [void] $report.AppendLine()
        [void] $report.AppendLine('```text')
        foreach ($line in (($run.ErrorText -split "`r?`n") | Select-Object -First 40)) { [void] $report.AppendLine($line) }
        [void] $report.AppendLine('```')
        if ($run.OutputText.Trim().Length -gt 0) {
            [void] $report.AppendLine()
            [void] $report.AppendLine('Output (first 10 lines):')
            [void] $report.AppendLine()
            [void] $report.AppendLine('```text')
            foreach ($line in (($run.OutputText -split "`r?`n") | Select-Object -First 10)) { [void] $report.AppendLine($line) }
            [void] $report.AppendLine('```')
        }
    }
}

[void] $report.AppendLine()
[void] $report.AppendLine("Duration of the script: $([int] $stopwatch.Elapsed.TotalSeconds) seconds.")

New-Item -ItemType Directory -Path $reportFolder -Force | Out-Null
[IO.File]::WriteAllText($reportPath, $report.ToString(), $utf8)

Write-Host "Report written in coverage/trimming/trimming.md ($([int] $stopwatch.Elapsed.TotalSeconds) seconds)."
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAILED: $failure" }
    exit 1
}

exit 0
