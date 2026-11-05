<#
.SYNOPSIS
    Measures the code coverage of CmdStarter.Lib, CmdStarter.Hosting and CmdStarter.Analyzers, and writes the reports in coverage/report/.

.DESCRIPTION
    1. deletes coverage/results/ and coverage/report/;
    2. runs the tests of CmdStarter.sln with the XPlat Code Coverage collector and coverage.runsettings;
    3. merges the Cobertura files with ReportGenerator (Html, MarkdownSummary, TextSummary and a merged Cobertura.xml);
    4. writes coverage/report/files.md (lines and branches per assembly and per file) and coverage/report/uncovered.md
       (every uncovered line with its number and its code, every line whose branches are partly covered).
    The script exits with a non-zero code when the tests fail.

.PARAMETER UncoveredFrom
    Runs only step 4 on an existing merged Cobertura file (used to check this step on a small example).

.PARAMETER UncoveredTo
    Folder where step 4 writes files.md and uncovered.md. Used with -UncoveredFrom.
#>
[CmdletBinding()]
param(
    [string] $UncoveredFrom,
    [string] $UncoveredTo
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = $PSScriptRoot

# Reads a merged Cobertura file and writes files.md and uncovered.md in $targetFolder.
function Write-CoverageFiles {
    param([string] $coberturaPath, [string] $targetFolder, [string] $rootFolder)

    [xml] $document = Get-Content -LiteralPath $coberturaPath -Raw
    $files = [ordered]@{}   # key: assembly|file

    foreach ($package in $document.SelectNodes('//package')) {
        foreach ($class in $package.SelectNodes('classes/class')) {
            $key = "$($package.name)|$($class.filename)"
            if (-not $files.Contains($key)) {
                $files[$key] = [pscustomobject]@{
                    Assembly = [string] $package.name
                    File     = [string] $class.filename
                    Lines    = @{}   # line number -> hits (the greatest of the classes that share the line)
                    Branches = @{}   # line number -> @(covered, total, text)
                }
            }

            $entry = $files[$key]
            foreach ($line in $class.SelectNodes('lines/line')) {
                $number = [int] $line.number
                $hits = [int] $line.hits
                if (-not $entry.Lines.ContainsKey($number) -or $entry.Lines[$number] -lt $hits) {
                    $entry.Lines[$number] = $hits
                }

                if ($line.branch -eq 'True' -and $line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                    $coveredBranches = [int] $Matches[1]
                    $totalBranches = [int] $Matches[2]
                    if ($entry.Branches.ContainsKey($number)) {
                        # Several classes can own branches of the same line (a lambda and its method): the branches add up.
                        $previous = $entry.Branches[$number]
                        $coveredBranches += $previous[0]
                        $totalBranches += $previous[1]
                    }

                    $entry.Branches[$number] = @($coveredBranches, $totalBranches)
                }
            }
        }
    }

    $rows = foreach ($entry in $files.Values) {
        $linesTotal = $entry.Lines.Count
        $linesCovered = @($entry.Lines.Values | Where-Object { $_ -gt 0 }).Count
        $branchesTotal = 0
        $branchesCovered = 0
        foreach ($branch in $entry.Branches.Values) {
            $coveredCount = $branch[0]
            $totalCount = $branch[1]
            $branchesCovered += $coveredCount
            $branchesTotal += $totalCount
        }

        [pscustomobject]@{
            Assembly        = $entry.Assembly
            File            = [IO.Path]::GetRelativePath($rootFolder, $entry.File).Replace('\', '/')
            FullPath        = $entry.File
            LinesCovered    = $linesCovered
            LinesTotal      = $linesTotal
            LinesUncovered  = $linesTotal - $linesCovered
            BranchesCovered = $branchesCovered
            BranchesTotal   = $branchesTotal
            Entry           = $entry
        }
    }

    function Format-Percent([int] $covered, [int] $total) {
        if ($total -eq 0) { return '-' }
        return ([double] $covered * 100.0 / [double] $total).ToString('0.0', [Globalization.CultureInfo]::InvariantCulture) + '%'
    }

    New-Item -ItemType Directory -Force -Path $targetFolder | Out-Null
    $text = New-Object System.Text.StringBuilder
    [void] $text.AppendLine('# Coverage by assembly and by file')
    [void] $text.AppendLine()
    [void] $text.AppendLine('Lines and branches are those of the merged Cobertura file. A line counts once for the file (the greatest hit count of the classes that share it).')

    foreach ($group in ($rows | Group-Object Assembly | Sort-Object Name)) {
        $assemblyLines = ($group.Group | Measure-Object LinesCovered -Sum).Sum
        $assemblyLinesTotal = ($group.Group | Measure-Object LinesTotal -Sum).Sum
        $assemblyBranches = ($group.Group | Measure-Object BranchesCovered -Sum).Sum
        $assemblyBranchesTotal = ($group.Group | Measure-Object BranchesTotal -Sum).Sum
        [void] $text.AppendLine()
        [void] $text.AppendLine("## $($group.Name)")
        [void] $text.AppendLine()
        [void] $text.AppendLine("Total: lines $assemblyLines/$assemblyLinesTotal ($(Format-Percent $assemblyLines $assemblyLinesTotal)), branches $assemblyBranches/$assemblyBranchesTotal ($(Format-Percent $assemblyBranches $assemblyBranchesTotal)).")
        [void] $text.AppendLine()
        [void] $text.AppendLine('| File | Lines | Lines % | Uncovered lines | Branches | Branches % |')
        [void] $text.AppendLine('| --- | ---: | ---: | ---: | ---: | ---: |')
        foreach ($row in ($group.Group | Sort-Object File)) {
            [void] $text.AppendLine("| $($row.File) | $($row.LinesCovered)/$($row.LinesTotal) | $(Format-Percent $row.LinesCovered $row.LinesTotal) | $($row.LinesUncovered) | $($row.BranchesCovered)/$($row.BranchesTotal) | $(Format-Percent $row.BranchesCovered $row.BranchesTotal) |")
        }
    }

    $allLines = ($rows | Measure-Object LinesCovered -Sum).Sum
    $allLinesTotal = ($rows | Measure-Object LinesTotal -Sum).Sum
    $allBranches = ($rows | Measure-Object BranchesCovered -Sum).Sum
    $allBranchesTotal = ($rows | Measure-Object BranchesTotal -Sum).Sum
    [void] $text.AppendLine()
    [void] $text.AppendLine("## All assemblies")
    [void] $text.AppendLine()
    [void] $text.AppendLine("Total: lines $allLines/$allLinesTotal ($(Format-Percent $allLines $allLinesTotal)), branches $allBranches/$allBranchesTotal ($(Format-Percent $allBranches $allBranchesTotal)).")
    [IO.File]::WriteAllText((Join-Path $targetFolder 'files.md'), $text.ToString(), (New-Object System.Text.UTF8Encoding($false)))

    # uncovered.md: for every incomplete file, the lines that no test reaches and the lines whose branches are partly covered.
    $uncovered = New-Object System.Text.StringBuilder
    [void] $uncovered.AppendLine('# Uncovered code')
    [void] $uncovered.AppendLine()
    [void] $uncovered.AppendLine('For each incomplete file: the lines no test reaches (with their code), then the lines whose branches are only partly covered. Generated by measure-coverage.ps1.')

    $incomplete = $rows | Where-Object { $_.LinesUncovered -gt 0 -or $_.BranchesCovered -lt $_.BranchesTotal } | Sort-Object Assembly, File
    foreach ($row in $incomplete) {
        $entry = $row.Entry
        $sourceLines = if (Test-Path -LiteralPath $row.FullPath) { Get-Content -LiteralPath $row.FullPath } else { $null }
        $partialLines = @($entry.Branches.Keys | Where-Object { $entry.Branches[$_][0] -lt $entry.Branches[$_][1] } | Sort-Object)
        $missingLines = @($entry.Lines.Keys | Where-Object { $entry.Lines[$_] -eq 0 } | Sort-Object)

        [void] $uncovered.AppendLine()
        [void] $uncovered.AppendLine("## $($row.File) ($($row.Assembly))")
        [void] $uncovered.AppendLine()
        [void] $uncovered.AppendLine("Lines $($row.LinesCovered)/$($row.LinesTotal), uncovered: $($row.LinesUncovered); branches $($row.BranchesCovered)/$($row.BranchesTotal).")

        $codeOf = {
            param([int] $number)
            if ($null -ne $sourceLines -and $number -ge 1 -and $number -le $sourceLines.Count) { return $sourceLines[$number - 1].Trim() }
            return '(source not found)'
        }

        if ($missingLines.Count -gt 0) {
            [void] $uncovered.AppendLine()
            [void] $uncovered.AppendLine('Uncovered lines:')
            [void] $uncovered.AppendLine()
            foreach ($number in $missingLines) {
                [void] $uncovered.AppendLine("- $number`: ``$(& $codeOf $number)``")
            }
        }

        if ($partialLines.Count -gt 0) {
            [void] $uncovered.AppendLine()
            [void] $uncovered.AppendLine('Lines with partly covered branches:')
            [void] $uncovered.AppendLine()
            foreach ($number in $partialLines) {
                $branch = $entry.Branches[$number]
                [void] $uncovered.AppendLine("- $number`: $($branch[0])/$($branch[1]) branches: ``$(& $codeOf $number)``")
            }
        }
    }

    if ($null -eq $incomplete -or @($incomplete).Count -eq 0) {
        [void] $uncovered.AppendLine()
        [void] $uncovered.AppendLine('Every line and every branch is covered.')
    }

    [IO.File]::WriteAllText((Join-Path $targetFolder 'uncovered.md'), $uncovered.ToString(), (New-Object System.Text.UTF8Encoding($false)))
    return [pscustomobject]@{ Lines = "$allLines/$allLinesTotal"; Branches = "$allBranches/$allBranchesTotal" }
}

if ($UncoveredFrom) {
    if (-not $UncoveredTo) { throw '-UncoveredTo is required with -UncoveredFrom.' }
    $summary = Write-CoverageFiles -coberturaPath $UncoveredFrom -targetFolder $UncoveredTo -rootFolder (Split-Path -Parent $UncoveredFrom)
    Write-Host "Lines $($summary.Lines), branches $($summary.Branches)"
    exit 0
}

Set-Location $repositoryRoot
$results = Join-Path $repositoryRoot 'coverage/results'
$report = Join-Path $repositoryRoot 'coverage/report'
Remove-Item -LiteralPath $results, $report -Recurse -Force -ErrorAction SilentlyContinue

dotnet test CmdStarter.sln -nologo --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory $results
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet test failed (exit code $LASTEXITCODE): no coverage report was written."
    exit $LASTEXITCODE
}

dotnet tool restore | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$reports = Join-Path $results '**/coverage.cobertura.xml'
dotnet reportgenerator "-reports:$reports" "-targetdir:$report" "-reporttypes:Html;MarkdownSummary;TextSummary;Cobertura" "-verbosity:Warning"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$summary = Write-CoverageFiles -coberturaPath (Join-Path $report 'Cobertura.xml') -targetFolder $report -rootFolder $repositoryRoot
Write-Host "Coverage: lines $($summary.Lines), branches $($summary.Branches). Reports in coverage/report/ (Summary.md, files.md, uncovered.md, index.html)."
exit 0
