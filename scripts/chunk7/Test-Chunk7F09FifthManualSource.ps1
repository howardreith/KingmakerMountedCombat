[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SelectionOutputPath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ManualDerivedFixtures.ps1')
$run='20261022-c7b-world180-b180s1-fourthhorse-selector'
$root=Join-Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') $run
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
$artifact=Read-KmcJson (Join-Path $root 'chunk7-assisted-cold-inventory-evidence.json')
$native=$artifact.observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites
[string[]]$characters=@($native.nativePartyCharacterIds);[Array]::Sort($characters,[StringComparer]::Ordinal)
function New-Selection {
 [pscustomobject]@{case='paid-small-ranger-horse';sourceRunId=$run
  sourceArtifactSha256='985101587995147c4a548b6f81ed6c88837cc54ac39f3ca22b8352a32afb2e96'
  sourceArchiveSha256='e82f7bb0775535ac2d9b782c6e53fa92a0893ab71bdf375a1e7cc06d50188759'
  baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1
  nativeWriteLeaf='Manual_307_KMC_C7_MANUAL_DERIVED_FIELD.zks'
  readiness=[pscustomobject]@{mainActorId=$native.mainCharacterId;mainLevel=$native.mainCharacterLevel
   mainExperience=$native.mainCharacterExperience;money=$native.nativeMoney
   experienceRatePercent=$artifact.observations.fixtureReadiness.experienceRatePercent
   nativeHireCost=$native.nativeHireCostNow;defaultHireExperience=$native.nativeDefaultHireExperienceNow
   partyCharacterIds=$characters}
  existingActorId=$null;existingLevel=$null;existingExperience=$null;existingRangerLevel=$null
  smallRaceGuid='b0c3ef2729c498f47970bb50fa1acd30'
  smallPresetGuid='639cebe8cbc098345af429d62fd79578'
  skeletonName='MaleHalfling';gender='Male';leftHanded=$false}
}
$passed=0
function Accept([string]$Name,[scriptblock]$Action){&$Action;$script:passed++;Write-Host ('PASS '+$Name)}
function Refuse([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action|Out-Null}catch{$refused=$true};if(-not$refused){throw ('FAIL accepted '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
Accept 'exact selector plus four-Horse Manual admits source with no purchase authority' {
 $s=New-Selection
 $source=Get-KmcChunk7ManualDerivedSource $s $request.fixture ([pscustomobject]@{})
 if($source.descriptor.fileName-cne'Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'-or
  $source.descriptor.sha256-cne$s.sourceArchiveSha256-or
  $source.artifactSha256-cne$s.sourceArtifactSha256-or
  $source.nativeAuthorityGranted-ne$false-or$source.ordinaryAcquisitionProven-ne$false){throw 'F09 source gained authority or changed identity.'}
}
Refuse 'wrong paid case at source gate' {$s=New-Selection;$s.case='existing-ranger-horse';Get-KmcChunk7ManualDerivedSource $s $request.fixture ([pscustomobject]@{})}
Refuse 'wrong archive pin at source gate' {$s=New-Selection;$s.sourceArchiveSha256='0'*64;Get-KmcChunk7ManualDerivedSource $s $request.fixture ([pscustomobject]@{})}
Refuse 'changed actor readiness' {$s=New-Selection;$s.readiness.money++;Assert-KmcManualDerivedSourcePins $s (Get-KmcChunk7OwnerSeedAcquisitionProjection $artifact.observations)}
Refuse 'fifth paid output cannot borrow owner-seed leaf' {$s=New-Selection;$s.nativeWriteLeaf='Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks';Assert-KmcChunk7ManualDerivedDescriptor ([pscustomobject]@{scenario='chunk7-manual-derived-acquisition';runId='test-f09-fifth';manualDerived=$s;persistenceLoad=$request.persistenceLoad;fixture=$request.fixture})}
$world=$artifact.observations.loadedWorld;$facts=@($artifact.observations.fourthHorseFacts)
$before=@(foreach($fact in $facts){
 $r=@($world.party|Where-Object id -CEQ $fact.riderId)[0]
 $h=@($world.party|Where-Object id -CEQ $fact.mountId)[0]
 [pscustomobject]@{riderId=$fact.riderId;mountId=$fact.mountId;riderObject=$r.object;mountObject=$h.object
  riderDescriptor=$r.descriptor;mountDescriptor=$h.descriptor;riderView=$r.view;mountView=$h.view
  featureObject=123;sourceObject=124;rankObject=125;featureGuid=$fact.featureGuid
  sourceGuid=$fact.sourceGuid;rank=$fact.rank;reciprocal=$true}
})
$saved=@($world.party|ForEach-Object{[pscustomobject]@{id=$_.id;blueprint=$_.blueprint
 object=$_.object;descriptor=$_.descriptor;view=$_.view}})
$observed=[pscustomobject]@{f09PriorHorsesBefore=$before;f09PriorHorsesAfter=$before
 savedWorld=[pscustomobject]@{party=$saved}}
Accept 'reader binds all four retained Horse proofs to saved actor identities' {Assert-KmcF09PriorHorseRetention $world $facts $observed}
Refuse 'reader rejects changed Horse identity after native save' {
 $copy=$observed|ConvertTo-Json -Depth 12|ConvertFrom-Json
 $copy.f09PriorHorsesAfter[0].mountObject++
 Assert-KmcF09PriorHorseRetention $world $facts $copy
}
Refuse 'reader rejects a saved actor view substituted after proof' {
 $copy=$observed|ConvertTo-Json -Depth 12|ConvertFrom-Json
 $row=@($copy.savedWorld.party|Where-Object id -CEQ $facts[0].riderId)[0];$row.view++
 Assert-KmcF09PriorHorseRetention $world $facts $copy
}
Refuse 'reader rejects a broken source reciprocal Horse link' {
 $copy=$world|ConvertTo-Json -Depth 12|ConvertFrom-Json
 $row=@($copy.party|Where-Object id -CEQ $facts[0].riderId)[0];$row.petId=$facts[1].mountId
 Assert-KmcF09PriorHorseRetention $copy $facts $observed
}
if($SelectionOutputPath){
 $target=[IO.Path]::GetFullPath($SelectionOutputPath)
 if([IO.File]::Exists($target)){throw 'Refusing to overwrite an existing selection.'}
 [IO.File]::WriteAllText($target,((New-Selection)|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
}
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
