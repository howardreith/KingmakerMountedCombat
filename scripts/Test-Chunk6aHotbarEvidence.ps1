param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHotbarEvidence.ps1')
$fixtures=Get-Content -Raw (Join-Path $repoRoot 'tests/Fixtures/Chunk6aHotbarSynthetic.json')|ConvertFrom-Json
$pass=0;$fail=0
foreach($f in $fixtures){$actual=$true;$reason='';try{Assert-KmcHotbarInput $f.input $f.command}catch{$actual=$false;$reason=$_.Exception.Message}
 if($actual -eq $f.expectPass){$pass++}else{$fail++;Write-Output ('FAIL '+$f.name+': '+$reason)}}
Write-Output "Hotbar synthetic checks PASS=$pass FAIL=$fail; no native qualification."
if($fail -ne 0){exit 1}
