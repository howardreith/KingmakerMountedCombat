[CmdletBinding()] param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '..\..'))
$source=Join-Path $lab 'runtime-evidence\20261104-finalize-f1b-creator-scroll\runtime-request.json'
$testRoot=Join-Path $repo 'obj\f1-branch-test'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$path=Join-Path $testRoot 'request.json'
$request=Get-Content -Raw $source | ConvertFrom-Json
foreach($case in @(
    @{branch='codex/kmc-finalize-f1-creation-ui-icons';valid=$true},
    @{branch='codex/mounted-combat-feasibility';valid=$true},
    @{branch='codex/kmc-finalize-f1-creation-ui-icons-extra';valid=$false},
    @{branch='codex/foreign-project';valid=$false},
    @{branch='Codex/kmc-finalize-f1-creation-ui-icons';valid=$false})) {
    $request.branch=$case.branch
    [IO.File]::WriteAllText($path,($request | ConvertTo-Json -Depth 30),(New-Object Text.UTF8Encoding($false)))
    $errorText=$null
    try { & (Join-Path $repo 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $path | Out-Null } catch { $errorText=$_.Exception.Message }
    if($case.valid -and $null -ne $errorText){throw "Valid request refused: $errorText"}
    if(!$case.valid -and $errorText -notlike '*branch is outside*'){throw "Foreign branch was not refused at branch boundary: $errorText"}
    Write-Host "PASS external request branch $($case.branch) admitted=$($case.valid)"
}
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo 'bin\Release\KingmakerMountedCombat.dll'))
$type=$assembly.GetType('KingmakerMountedCombat.Diagnostics.RuntimeRequest',$true)
foreach($case in @(
    @{branch='codex/kmc-finalize-f1-creation-ui-icons';valid=$true},
    @{branch='codex/mounted-combat-feasibility';valid=$true},
    @{branch='codex/kmc-finalize-f1-creation-ui-icons-extra';valid=$false},
    @{branch='codex/foreign-project';valid=$false},
    @{branch='Codex/kmc-finalize-f1-creation-ui-icons';valid=$false})) {
    $native=[Activator]::CreateInstance($type,$true)
    $type.GetProperty('Branch').SetValue($native,$case.branch,$null)
    $errors=@($type.GetMethod('Validate').Invoke($native,@()))
    $branchErrors=@($errors | Where-Object {$_ -like 'branch must*'})
    if(($branchErrors.Count-eq0) -ne $case.valid){throw "Compiled branch disposition wrong: $($case.branch)"}
    Write-Host "PASS compiled request branch $($case.branch) admitted=$($case.valid)"
}
Write-Host 'TOTAL PASS=10 FAIL=0'
