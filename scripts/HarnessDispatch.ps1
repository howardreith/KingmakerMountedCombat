# PR-5 review R4-F1: the harness case dispatcher, shared by scripts/Test-Harness.ps1 and its fixture
# (scripts/Test-HarnessSelection.ps1). Single-case selection is scoped to one invocation (Test-Harness.ps1 -Only <exact name>);
# the inherited KMC_HARNESS_ONLY environment variable is not read, so ordinary Candidate and Full callers, which pass no -Only,
# always run every case. An explicit selection must match exactly one case, or the invocation fails.
$script:harnessOnly = $null
$script:harnessMatched = 0

# PR-5 review R5-F3: omission (not -Supplied) selects every case; a supplied but empty name is refused before any case runs.
function Initialize-HarnessSelection([string]$Only, [switch]$Supplied) {
    if ($Supplied -and [string]::IsNullOrEmpty($Only)) { throw '-Only was supplied without a case name (omit it to run every case)' }
    $script:harnessOnly = if ($Supplied) { $Only } else { $null }
    $script:harnessMatched = 0
}

# PR-5 review R5-F2: the legacy Horse case's compiled binding check reads the product its caller selected.
function Get-HarnessBindingContractArguments([string]$Contract, [string]$Configuration) {
    , @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Contract, '-Configuration', $Configuration)
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
