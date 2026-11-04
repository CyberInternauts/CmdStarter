<#
.SYNOPSIS
    Measures CmdStarter under trimming and Native AOT, and writes the table in coverage/trimming/trimming.md.

.DESCRIPTION
    1. builds CmdStarter.sln in Release, which writes the package cints.CmdStarter in artefacts/;
    2. recreates, outside the repository, $env:TEMP\CmdStarter-trimming\HelloWorld and ...\DevOpsTool: an application that references the package
       (RestoreSources = artefacts/ and nuget.org, RestorePackagesPath = $env:TEMP\CmdStarter-trimming\packages, so the global NuGet cache of the user is never read or written)
       and holds a copy of the sources of the sample;
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
# Expect = 'Output' (exit code 0 and the first line of the output), 'AnyOutput' (exit code 0 and a non-empty output), 'ParseError' (an exit code from 1 to 255, and no unhandled exception).
$applications = @(
    [pscustomobject]@{
        Name   = 'HelloWorld'
        Sample = 'samples/CmdStarter.Samples.HelloWorld'
        Runs   = @(
            [pscustomobject]@{ Arguments = '--to Bob'; Expect = 'Output'; Line = 'Hello, Bob!' }
        )
    },
    [pscustomobject]@{
        Name   = 'DevOpsTool'
        Sample = 'samples/CmdStarter.Samples.DevOpsTool'
        Runs   = @(
            [pscustomobject]@{ Arguments = 'service deploy api --version 1.2.3'; Expect = 'Output'; Line = 'Deploying api 1.2.3 to dev' },
            [pscustomobject]@{ Arguments = '--help'; Expect = 'AnyOutput'; Line = '' },
            [pscustomobject]@{ Arguments = 'service deploy'; Expect = 'ParseError'; Line = '' }
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

    $sampleFolder = Join-Path $repositoryRoot $application.Sample
    foreach ($file in Get-ChildItem -LiteralPath $sampleFolder -Recurse -File -Filter '*.cs') {
        $relative = [IO.Path]::GetRelativePath($sampleFolder, $file.FullName)
        if ($relative -match '^(bin|obj)[\\/]') { continue }

        $destination = Join-Path $directory $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
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
                    'AnyOutput' { ($result.ExitCode -eq 0) -and ($firstOutput.Length -gt 0) }
                    'ParseError' { ($null -ne $result.ExitCode) -and ($result.ExitCode -ge 1) -and ($result.ExitCode -le 255) -and (-not $crashed) }
                }

                $expected = switch ($run.Expect) {
                    'Output' { "0, first line '$($run.Line)'" }
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
