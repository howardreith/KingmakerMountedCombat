[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$repo=Get-KmcRepositoryRoot
$intake=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'environment-intake.json')
$installed=Join-Path $intake.requestedLayout.kingmakerModsRoot 'KingmakerMountedCombat'
$testParent=Assert-KmcChildPath (Join-Path $repo ('obj/starting-installation-tests/'+[Guid]::NewGuid().ToString('N'))) (Join-Path $repo 'obj') 'test parent'
$clone=Join-Path $testParent 'KingmakerMountedCombat'
[void][IO.Directory]::CreateDirectory($clone)
[void](Assert-KmcPhase3fStartingInstallation $installed)
$leaves=@(Get-ChildItem -LiteralPath $installed -File -Force | ForEach-Object Name)
foreach($leaf in $leaves){[IO.File]::Copy((Join-Path $installed $leaf),(Join-Path $clone $leaf),$false)}
[void](Assert-KmcPhase3fStartingInstallation $clone)
$passes=1
foreach($leaf in $leaves) {
    $path=Join-Path $clone $leaf; $bytes=[IO.File]::ReadAllBytes($path)
    try {
        $changed=[byte[]]$bytes.Clone();$changed[0]=$changed[0] -bxor 1
        [IO.File]::WriteAllBytes($path,$changed)
        $rejected=$false;try{[void](Assert-KmcPhase3fStartingInstallation $clone)}catch{$rejected=$true}
        if(!$rejected){throw "Changed starting-installation bytes were accepted: $leaf"};$passes++
    } finally {[IO.File]::WriteAllBytes($path,$bytes)}
}
$extra=Join-Path $clone 'unregistered.cache'
[IO.File]::WriteAllText($extra,'unregistered')
$rejected=$false;try{[void](Assert-KmcPhase3fStartingInstallation $clone)}catch{$rejected=$true}
if(!$rejected){throw 'Extra starting-installation file was accepted.'};$passes++
[IO.File]::Delete($extra)
[void](Assert-KmcPhase3fStartingInstallation $installed);$passes++
Write-Host "REGISTERED STARTING INSTALLATION PASS=$passes FAIL=0; live bytes were read only."
