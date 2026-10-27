param([string]$ReaderDirectory,[string]$FixtureDirectory=$PSScriptRoot)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $FixtureDirectory 'Initialize-Chunk7Tests.ps1')
if([string]::IsNullOrEmpty($ReaderDirectory)){$ReaderDirectory=Join-Path $repo 'scripts/runtime'}
. (Join-Path $FixtureDirectory 'Test-Chunk7CombatPersistenceObservation.ps1')
. (Join-Path $ReaderDirectory 'Chunk7PersistenceEvidence.ps1')
$typedPassed=0;$typedFailures=[Collections.Generic.List[string]]::new()
function Read-ObservationPath($Value,[string]$Path){foreach($part in $Path.Split('.')){if($part-match'^[0-9]+$'){$Value=$Value[[int]$part]}else{$Value=$Value.$part}};return ,$Value}
function Write-ObservationPath($Value,[string]$Path,$Replacement){$parts=$Path.Split('.');for($i=0;$i-lt$parts.Length-1;$i++){if($parts[$i]-match'^[0-9]+$'){$Value=$Value[[int]$parts[$i]]}else{$Value=$Value.($parts[$i])}};$Value.($parts[-1])=$Replacement}
$typedSpecs=@([pscustomobject]@{name='exploration-source';request=$request;artifact=$good},[pscustomobject]@{name='exploration-cold';request=$coldRequest;artifact=$cold},[pscustomobject]@{name='combat-source';request=$sourceTb.request;artifact=$sourceTb.artifact},[pscustomobject]@{name='combat-cold';request=$coldTb.request;artifact=$coldTb.artifact})
foreach($spec in $typedSpecs){Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS';$typedPassed++;Write-Host ('PASS unchanged exact numeric fixture '+$spec.name)}
$typedCases=@()
foreach($spec in $typedSpecs){
 foreach($path in @('schemaVersion','observations.nativeSchema','observations.nativeCapacity','observations.snapshots.0.owners.0.generation','observations.snapshots.0.controls.DuplicateFactCount','observations.snapshots.0.controls.ManagedHotbarSlotCount','observations.snapshots.0.semanticRestores','observations.snapshots.0.presentationRestores','observations.snapshots.0.nativePostLoads','observations.snapshots.0.snapshots','observations.snapshots.0.nativeCastRequests')){$typedCases+= [pscustomobject]@{spec=$spec;path=$path}}
 if($spec.name.EndsWith('cold')){foreach($path in @('observations.coldRestoration.maximumEvents','observations.coldRestoration.dropped','observations.coldRestoration.errors')){$typedCases+=[pscustomobject]@{spec=$spec;path=$path}}}else{foreach($path in @('observations.nativeWrite.snapshotCount','observations.saveRequested.snapshotCount','observations.inputs.0.nativeCastDelta','observations.inputs.0.nativeRefusalDelta','observations.acquisition.nativeRangerCommitCount','observations.acquisition.horseFactRankAtCommit')){$typedCases+=[pscustomobject]@{spec=$spec;path=$path}}}
}
$typedCases+=[pscustomobject]@{spec=$typedSpecs[0];path='observations.acquisition.huntersBondSelectionLevel'};$typedCases+=[pscustomobject]@{spec=$typedSpecs[0];path='observations.acquisition.rangerCompanionSelectionLevel'}
$typedCases+=[pscustomobject]@{spec=$typedSpecs[3];path='observations.loadedSourceSchema'}
foreach($case in $typedCases){foreach($form in @('numeric-text','floating-point','boolean','single-array')){$copy=Copy-Test $case.spec.artifact;$value=Read-ObservationPath $copy $case.path;$replacement=switch($form){'numeric-text'{[string]$value};'floating-point'{[double]$value};'boolean'{[bool]$value};'single-array'{,@($value)}};Write-ObservationPath $copy $case.path $replacement;$rejected=$false;try{Assert-KmcChunk7PersistenceObservation $case.spec.request $copy 'PASS'}catch{$rejected=$true};$name=$case.spec.name+' '+$case.path+' '+$form;if($rejected){$typedPassed++;Write-Host ('PASS refuses '+$name)}else{$typedFailures.Add($name);Write-Host ('FAIL accepted '+$name)}}}
foreach($spec in $typedSpecs){foreach($value in @('false',0,$null)){$copy=Copy-Test $spec.artifact;Write-ObservationPath $copy 'observations.snapshots.0.controls.SerializationSuspended' $value;$rejected=$false;try{Assert-KmcChunk7PersistenceObservation $spec.request $copy 'PASS'}catch{$rejected=$true};$name=$spec.name+' serialization bool type '+$(if($null-eq$value){'null'}else{$value.GetType().Name});if($rejected){$typedPassed++;Write-Host ('PASS refuses '+$name)}else{$typedFailures.Add($name);Write-Host ('FAIL accepted '+$name)}}}
Write-Host ('CHUNK7 SNAPSHOT TYPE CONTRACT PASS='+$typedPassed+' FAIL='+$typedFailures.Count+'; numeric identity and native counter coercion only')
if($typedFailures.Count-ne0){throw ('Type coercion admitted: '+($typedFailures-join'; '))}
