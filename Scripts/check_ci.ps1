#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks the latest CI status of a Git repository and all of its submodules.

.DESCRIPTION
    PowerShell port of check_ci.sh. For the root repository and every submodule,
    it queries the latest GitHub Actions run with `gh` and reports its status.
    When a run failed, the first failing step and its timestamp are shown.

.PARAMETER TargetDir
    Path to the target Git repository (default: current directory).

.PARAMETER FailedOnly
    Display only failed CI checks.

.PARAMETER Help
    Show usage information.

.EXAMPLE
    ./check_ci.ps1
.EXAMPLE
    ./check_ci.ps1 ./path/to/repo -FailedOnly
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$TargetDir = ".",

    [Alias('f')]
    [switch]$FailedOnly,

    [Alias('h')]
    [switch]$Help
)

# Colors (ANSI escape sequences, matching the bash script)
$ESC = [char]27
$GREEN = "$ESC[0;32m"
$RED = "$ESC[0;31m"
$YELLOW = "$ESC[0;33m"
$BLUE = "$ESC[0;34m"
$GRAY = "$ESC[0;90m"
$NC = "$ESC[0m"

if ($Help) {
    Write-Host "Usage: $($MyInvocation.MyCommand.Name) [path/to/repo] [-FailedOnly]"
    Write-Host "Options:"
    Write-Host "  path/to/repo       Path to the target Git repository (default: .)"
    Write-Host "  -f, -FailedOnly    Display only failed CI checks"
    exit 0
}

# Resolve absolute path and navigate to the target directory
if (-not (Test-Path -LiteralPath $TargetDir -PathType Container)) {
    Write-Host "${RED}Error:${NC} Directory '$TargetDir' not found."
    exit 1
}

Push-Location -LiteralPath $TargetDir
$TargetDir = (Get-Location).Path

# Helper function to check CI status for any repository path.
# Returns the formatted output lines as a single string (empty when filtered out).
# NOTE: kept self-contained (own colors + params) so it can be injected and run
# inside a worker runspace.
function Get-RepoCi {
    param(
        [string]$Path,
        [string]$Label,
        [switch]$FailedOnly
    )

    # Colors (ANSI escape sequences, matching the bash script)
    $ESC = [char]27
    $GREEN = "$ESC[0;32m"
    $RED = "$ESC[0;31m"
    $YELLOW = "$ESC[0;33m"
    $BLUE = "$ESC[0;34m"
    $GRAY = "$ESC[0;90m"
    $NC = "$ESC[0m"

    # Get repo remote URL
    $repoUrl = $null
    if ($Path -eq ".") {
        $repoUrl = (git config --get remote.origin.url 2>$null)
    }
    else {
        $repoUrl = (git config --file .gitmodules --get "submodule.$Path.url" 2>$null)
    }

    # Fetch run details via gh
    $jsonRaw = $null
    Push-Location -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($?) {
        $jsonRaw = (gh run list --limit 1 --json databaseId,workflowName,status,conclusion,url 2>$null)
        Pop-Location
    }
    else {
        Pop-Location -ErrorAction SilentlyContinue
    }

    $runs = $null
    if (-not [string]::IsNullOrWhiteSpace($jsonRaw)) {
        try { $runs = $jsonRaw | ConvertFrom-Json } catch { $runs = $null }
    }

    if ($null -eq $runs -or $runs.Count -eq 0) {
        if (-not $FailedOnly) {
            $repoDisplay = if ($repoUrl) { $repoUrl } else { "Unknown" }
            return @(
                ""
                "${BLUE}${Label}:${NC} $Path"
                "  ${GRAY}Repo:${NC} $repoDisplay"
                "  ${GRAY}CI Status:${NC} ${YELLOW}UNKNOWN / NO RUNS${NC}"
            ) -join "`n"
        }
        return $null
    }

    # Parse main JSON (take the first run)
    $run = $runs[0]
    $runId = $run.databaseId
    $wfName = if ($run.workflowName) { $run.workflowName } else { "Workflow" }
    $status = $run.status
    $conclusion = $run.conclusion
    $runUrl = $run.url

    # Filter out non-failing runs when -FailedOnly is set
    if ($FailedOnly -and $conclusion -ne "failure") {
        return $null
    }

    $failedStep = ""
    $failedTime = ""

    if ($status -eq "completed") {
        if ($conclusion -eq "success") {
            $statusDisplay = "${GREEN}SUCCESS${NC}"
        }
        elseif ($conclusion -eq "failure") {
            $statusDisplay = "${RED}FAILED${NC}"

            # Fetch detailed view for failed step and timestamp
            $viewJson = $null
            Push-Location -LiteralPath $Path -ErrorAction SilentlyContinue
            $viewJson = (gh run view $runId --json jobs 2>$null)
            Pop-Location -ErrorAction SilentlyContinue

            $view = $null
            if (-not [string]::IsNullOrWhiteSpace($viewJson)) {
                try { $view = $viewJson | ConvertFrom-Json } catch { $view = $null }
            }

            if ($view) {
                # Extract first failing step name and completion time
                $failedSteps = @($view.jobs | ForEach-Object { $_.steps } |
                        Where-Object { $_.conclusion -eq "failure" })

                if ($failedSteps.Count -gt 0) {
                    $failedStep = $failedSteps[0].name

                    $rawTime = $failedSteps[0].completedAt
                    if (-not [string]::IsNullOrWhiteSpace($rawTime) -and $rawTime -ne "null") {
                        try {
                            $failedTime = ([datetime]$rawTime).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                        }
                        catch {
                            $failedTime = $rawTime
                        }
                    }
                }
            }
        }
        else {
            $statusDisplay = "${YELLOW}$($conclusion.ToUpper())${NC}"
        }
    }
    elseif ($status -eq "in_progress") {
        $statusDisplay = "${YELLOW}IN PROGRESS${NC}"
    }
    else {
        $statusDisplay = "${YELLOW}$($status.ToUpper())${NC}"
    }

    $repoDisplay = if ($repoUrl) { $repoUrl } else { "Unknown" }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("")
    $lines.Add("${BLUE}${Label}:${NC} $Path")
    $lines.Add("  ${GRAY}Repo:${NC} $repoDisplay")
    $lines.Add("  ${GRAY}Workflow:${NC} $($wfName -replace '^$', 'Unknown')")
    $lines.Add("  ${GRAY}CI Status:${NC} $statusDisplay")

    if ($conclusion -eq "failure") {
        if (-not [string]::IsNullOrWhiteSpace($failedStep) -and $failedStep -ne "null") {
            $lines.Add("  ${GRAY}Failed Step:${NC} ${RED}${failedStep}${NC}")
        }
        if (-not [string]::IsNullOrWhiteSpace($failedTime) -and $failedTime -ne "null") {
            $lines.Add("  ${GRAY}Failed At:${NC} $failedTime")
        }
        $lines.Add("  ${GRAY}Logs URL:${NC} $runUrl")
    }
    else {
        $lines.Add("  ${GRAY}Run URL:${NC} $(if ($runUrl) { $runUrl } else { 'N/A' })")
    }

    return ($lines -join "`n")
}

Write-Host "Checking CI status for '$TargetDir'..."

# Build the task list: root repository first, then each submodule.
$submodules = @()
git submodule status 2>$null | ForEach-Object {
    # Format: " <sha> <path> (<description>)"
    $fields = ($_ -replace '^[+\-U ]', '').Trim() -split '\s+'
    if ($fields.Count -ge 2) { $submodules += $fields[1] }
}

$tasks = [System.Collections.Generic.List[object]]::new()
$tasks.Add([pscustomobject]@{ Path = "."; Label = "Root Repository"; Index = $tasks.Count })
foreach ($sub in $submodules) {
    if (-not [string]::IsNullOrWhiteSpace($sub)) {
        $tasks.Add([pscustomobject]@{ Path = $sub; Label = "Submodule"; Index = $tasks.Count })
    }
}

$total = $tasks.Count

# Make Get-RepoCi self-contained so it can run inside worker runspaces.
$funcDef = "function Get-RepoCi {`n" + (Get-Command Get-RepoCi).Definition + "`n}"
$workerScript = {
    param($Path, $Label, $FailedOnly, $FuncDef)
    . ([scriptblock]::Create($FuncDef))
    Get-RepoCi -Path $Path -Label $Label -FailedOnly:$FailedOnly
}

# Throttled runspace pool (network-bound gh calls -> allow a few more than cores).
$maxThreads = [Math]::Max(4, [Math]::Min($total, 8))
$pool = [runspacefactory]::CreateRunspacePool(1, $maxThreads)
$pool.Open()

$running = [System.Collections.Generic.List[object]]::new()
foreach ($task in $tasks) {
    $ps = [powershell]::Create()
    $ps.RunspacePool = $pool
    [void]$ps.AddScript($workerScript.ToString()).
        AddArgument($task.Path).
        AddArgument($task.Label).
        AddArgument([bool]$FailedOnly).
        AddArgument($funcDef)
    $running.Add([pscustomobject]@{
            PS     = $ps
            Handle = $ps.BeginInvoke()
            Task   = $task
            Done   = $false
        })
}

# Wait for completion, updating a progress bar as each check finishes.
$results = @{}
$done = 0
while ($done -lt $total) {
    foreach ($job in $running) {
        if ($job.Done -or -not $job.Handle.IsCompleted) { continue }

        $job.Done = $true
        $out = $job.PS.EndInvoke($job.Handle)
        if ($out -and $out.Count -gt 0) {
            $results[$job.Task.Index] = ($out -join "`n").TrimEnd()
        }
        $job.PS.Dispose()
        $done++

        Write-Progress -Activity "Checking CI status" `
            -Status "[$done/$total] $($job.Task.Label): $($job.Task.Path)" `
            -PercentComplete ([int](($done / $total) * 100))
    }
    if ($done -lt $total) { Start-Sleep -Milliseconds 80 }
}

Write-Progress -Activity "Checking CI status" -Completed

$pool.Close()
$pool.Dispose()

# Print collected results in original order (root first, then submodules).
foreach ($task in $tasks) {
    if ($results.ContainsKey($task.Index)) {
        Write-Host $results[$task.Index]
    }
}

Pop-Location

Write-Host ""
Write-Host "Done!"
