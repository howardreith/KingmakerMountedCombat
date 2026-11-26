# PR-5 review R4-F1: the harness case dispatcher, shared by scripts/Test-Harness.ps1 and its fixture
# (scripts/Test-HarnessSelection.ps1). Single-case selection is scoped to one invocation (Test-Harness.ps1 -Only <exact name>);
# the inherited KMC_HARNESS_ONLY environment variable is not read, so ordinary Candidate and Full callers, which pass no -Only,
# always run every case. An explicit selection must match exactly one case, or the invocation fails.
$script:harnessOnly = $null
$script:harnessMatched = 0

function Initialize-HarnessSelection([string]$Only) {
    $script:harnessOnly = if ([string]::IsNullOrEmpty($Only)) { $null } else { $Only }
    $script:harnessMatched = 0
}

function Test-HarnessSelected([string]$Name) {
    if ($null -eq $script:harnessOnly) { return $true }
    if ($Name -cne $script:harnessOnly) { return $false }
    $script:harnessMatched++
    return $true
}

# Null when the invocation's selection is satisfied; otherwise the reason it is not.
function Get-HarnessSelectionFailure {
    if ($null -ne $script:harnessOnly -and $script:harnessMatched -ne 1) {
        return ("-Only '" + $script:harnessOnly + "' matched " + $script:harnessMatched + ' harness cases (exactly one is required)')
    }
    return $null
}

function Invoke-HarnessTest {
    param([string]$Name, [scriptblock]$Body)
    if (-not (Test-HarnessSelected $Name)) { return }
    try {
        & $Body
        $script:passed++
        Write-Host "PASS $Name"
    }
    catch {
        $script:failed++
        Write-Host "FAIL $Name`: $($_.Exception.Message)"
    }
}
