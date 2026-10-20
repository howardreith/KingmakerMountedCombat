[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot
$intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
$managed=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data/Managed'
Add-Type -Path (Join-Path $managed 'Newtonsoft.Json.dll')
[void][Reflection.Assembly]::LoadFrom((Join-Path (Get-KmcRepositoryRoot) 'bin/Observer/Release/KmcRemovalObserver.dll'))
$r=[KmcRemovalObserver.ObserverRequest]::new()
$r.SchemaVersion=1;$r.RunId='offline-owner-cold';$r.TransactionToken='a'*64
$r.EvidenceRoot=$lab;$r.ProfileRoot=$lab;$r.ModsRoot=$lab
$r.KmcModId='KingmakerMountedCombat';$r.ObserverModId='KmcRemovalObserver'
$r.KmcHarmonyIds=[string[]]@('KingmakerMountedCombat.Feasibility')
$r.KmcBlueprintGuids=[string[]]@('4016c7db400ab721ff125aef9e65e202')
$r.MammothBlueprintGuid='e7aa96d15a45238438ae4cfb476f6bb9';$r.TimeoutSeconds=300
$r.Candidate=[KmcRemovalObserver.ObserverCandidate]::new()
$r.Candidate.Commit='a'*40;$r.Candidate.ProductVersion='preview.82';$r.Candidate.DllSha256='b'*64
$r.Candidate.DllMvid='141c76d8-bef6-4d87-9062-530ef5c4aff5'
$r.Observer=[KmcRemovalObserver.ObserverPackage]::new()
$r.Observer.Version='0.1.0-observer.4';$r.Observer.PackageSha256='c'*64
$r.Observer.DllSha256='d'*64;$r.Observer.DllMvid='141c76d8-bef6-4d87-9062-530ef5c4aff5'
$r.Archive=[KmcRemovalObserver.ObserverArchive]::new()
$r.Archive.InternalName='KMC_CLEANUP';$r.Archive.FileName='Manual_306_KMC_CLEANUP.zks'
$r.Archive.Sha256='2c46c2700f117b3f0d50f688d2c89bd879de79d4b0b40eaf3b2502c1015bdfc2'
$r.Archive.Length=1263594;$r.Archive.GameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180'
$r.Archive.GameName='Testy the Modless';$r.Archive.Area='b10e86516b6437d45a629931c30936fe'
if(@($r.Validate()).Count-ne0){throw 'Exact batch65 cleanup observer request was rejected.'}
$r.Archive.Sha256='e'*64
if(@($r.Validate()).Count-eq0){throw 'Observer accepted another Manual 306 hash.'}
$r.Archive.Sha256='2c46c2700f117b3f0d50f688d2c89bd879de79d4b0b40eaf3b2502c1015bdfc2'
$r.Archive.GameId='00000000-0000-0000-0000-000000000001'
if(@($r.Validate()).Count-eq0){throw 'Observer accepted another Manual 306 campaign.'}
$r.Archive.FileName='Manual_301_KMC_CLEANUP.zks';$r.Archive.Sha256='e'*64
$r.Archive.GameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180'
if(@($r.Validate()).Count-ne0){throw 'Legacy Manual 301 observer path changed.'}
Write-Host 'OWNER STOCK OBSERVER REQUEST PASS=4 FAIL=0 (compiled observer, offline only)'
