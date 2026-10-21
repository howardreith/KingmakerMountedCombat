[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$SourceRunId,[string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$reader=Join-Path $RepositoryRoot 'scripts/runtime'
. (Join-Path $reader 'RuntimeHarness.Common.ps1')
. (Join-Path $reader 'Chunk7ManualDerivedFixtures.ps1')
$root=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $SourceRunId
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
$artifactPath=Join-Path $root 'chunk7-assisted-cold-inventory-evidence.json'
$artifact=Read-KmcJson $artifactPath
$prerequisites=$artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites
$candidate=[pscustomobject]@{commit=$request.commit;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid}
function Selection {
 $ready=[pscustomobject]@{mainActorId=$prerequisites.mainCharacterId;mainLevel=$prerequisites.mainCharacterLevel;
  mainExperience=$prerequisites.mainCharacterExperience;money=$prerequisites.nativeMoney;
  experienceRatePercent=$artifact.observations.fixtureReadiness.experienceRatePercent;
  nativeHireCost=$prerequisites.nativeHireCostNow;defaultHireExperience=$prerequisites.nativeDefaultHireExperienceNow;
  partyCharacterIds=@($prerequisites.nativePartyCharacterIds|Sort-Object)}
 [pscustomobject]@{case='existing-ranger-horse';sourceRunId=$SourceRunId;sourceArtifactSha256=(Get-KmcSha256 $artifactPath);
  sourceArchiveSha256=$request.assistedColdInventory.seedSha256;baseGameOnly=$true;expectedNativeLoadCount=1;
  expectedNativeWriteCount=1;nativeWriteLeaf='Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks';readiness=$ready;
  existingActorId=$ready.mainActorId;existingLevel=$ready.mainLevel;existingExperience=$ready.mainExperience;
  existingRangerLevel=3;smallRaceGuid=$null;smallPresetGuid=$null;skeletonName=$null;gender=$null;leftHanded=$null}
}
$passed=0
function Accept([string]$Name,[scriptblock]$Action){&$Action;$script:passed++;Write-Host ('PASS '+$Name)}
function Refuse([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action|Out-Null}catch{$refused=$true};if(-not$refused){throw ('FAIL accepted '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
Accept 'registered master and complete same-candidate cold receipt' {
 $source=Get-KmcChunk7ManualDerivedSource (Selection) $request.fixture $candidate
 if($source.descriptor.fileName-cne'Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks'-or
  $source.descriptor.sha256-cne$request.assistedColdInventory.seedSha256-or
  $source.coldInventoryRunId-cne$SourceRunId-or$source.ordinaryAcquisitionProven-ne$false){throw 'Owner-seed source changed role or archive identity.'}
}
Refuse 'candidate DLL hash drift' {$wrong=[pscustomobject]@{commit=$candidate.commit;dllSha256=('0'*64);dllMvid=$candidate.dllMvid};Get-KmcChunk7ManualDerivedSource (Selection) $request.fixture $wrong}
Refuse 'candidate MVID drift' {$wrong=[pscustomobject]@{commit=$candidate.commit;dllSha256=$candidate.dllSha256;dllMvid='11111111-1111-4111-8111-111111111111'};Get-KmcChunk7ManualDerivedSource (Selection) $request.fixture $wrong}
Refuse 'cold artifact hash drift' {$s=Selection;$s.sourceArtifactSha256='0'*64;Get-KmcChunk7ManualDerivedSource $s $request.fixture $candidate}
Refuse 'registered master hash drift' {$s=Selection;$s.sourceArchiveSha256='0'*64;Get-KmcChunk7ManualDerivedSource $s $request.fixture $candidate}
Refuse 'readiness money substitution' {$s=Selection;$s.readiness.money++;Get-KmcChunk7ManualDerivedSource $s $request.fixture $candidate}
Refuse 'bootstrap Manual output borrowed by owner-seed descriptor' {$s=Selection;$s.nativeWriteLeaf='Manual_2_KMC_C7_MANUAL_DERIVED_FIELD.zks';Get-KmcChunk7ManualDerivedSource $s $request.fixture $candidate}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
