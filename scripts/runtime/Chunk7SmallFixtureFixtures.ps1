Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7AssistedFixtureFixtures.ps1')
function Assert-KmcChunk7SmallFixtureLaunchArguments($Bound,[string]$Scenario,[bool]$BaseGameOnly,[int]$TimeoutSeconds,[string]$SelectionPath){
 if(-not(Test-KmcChunk7SmallFixtureScenario $Scenario)){
  if($Bound.ContainsKey('SmallFixtureSelectionPath')){throw 'F09 fixture selection path is forbidden under another scenario.'};return
 }
 if(-not$BaseGameOnly-or$TimeoutSeconds-ne900-or[string]::IsNullOrEmpty($SelectionPath)){
  throw 'F09 fixture requires KMC-alone base-game staging, its explicit selection and exact outer timeout 900 seconds (engine 720, host 780).'
 }
 foreach($key in @('NativeBootstrapCase','OrdinarySelectionPath','OrdinaryFieldSelectionPath','BootstrapInventorySelectionPath','AssistedColdInventorySelectionPath','ManualSeedSelectionPath','AcquiredManualSelectionPath','AssistedFixtureSelectionPath','OwnedPairsSelectionPath','ManualDerivedSelectionPath','CumulativeAcquisitionSelectionPath','SmallCalibrationSelectionPath','ProgressionSelectionPath','PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','ObserverPackagePath','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas','ObserveWorkingDeserialization')){
  if($Bound.ContainsKey($key)){throw ('F09 fixture forbids foreign source/action/area input '+$key)}
 }
}
function Read-KmcChunk7SmallFixtureSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'F09 explicit selection';Assert-KmcNotReparsePoint $path 'F09 selection';Assert-KmcNotHardLink $path 'F09 selection'
 $file=Get-Item -LiteralPath $path;if($file.PSIsContainer-or$file.Length-lt1-or$file.Length-gt16384){throw 'F09 selection is empty or oversized.'}
 $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'F09 selection';$s=Read-KmcJson $path
 Assert-KmcChunk7SmallFixtureSelection ([pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId=$RunId;smallFixture=$s})
 $after=Get-Item -LiteralPath $path;if($after.Length-ne$file.Length-or$after.LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $path)-cne$hash){throw 'F09 selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$hash;selection=$s}
}
function Read-KmcSmallFixturePinnedJson([string]$Path,[string]$ExpectedHash,[int]$Maximum=16777216){
 Assert-KmcNotReparsePoint $Path 'F09 provenance';Assert-KmcNotHardLink $Path 'F09 provenance'
 $f=Get-Item -LiteralPath $Path;if($f.PSIsContainer-or$f.Length-lt1-or$f.Length-gt$Maximum){throw 'F09 provenance file is empty or oversized.'}
 $hash=Get-KmcSha256 $Path;if($ExpectedHash-and$hash-cne$ExpectedHash){throw 'F09 provenance SHA256 differs.'}
 Assert-KmcManifestJsonMembersUnique $Path 'F09 provenance';$value=Read-KmcJson $Path
 if($null-eq$value-or$value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'F09 provenance requires one JSON object.'}
 $after=Get-Item -LiteralPath $Path;if($after.Length-ne$f.Length-or$after.LastWriteTimeUtc.Ticks-ne$f.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $Path)-cne$hash){throw 'F09 provenance changed during read.'}
 [pscustomobject]@{path=$Path;sha256=$hash;length=[long]$f.Length;lastWriteTimeUtcTicks=[long]$f.LastWriteTimeUtc.Ticks;value=$value}
}
function Get-KmcSmallFixtureHistoricalArtifact([string]$RunId,[string]$Leaf,[string]$Kind,[string]$Hash,[int]$Writes){
 $root=Assert-KmcChildPath (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$RunId)) (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'F09 historical provenance'
 Assert-KmcNotReparsePoint $root 'F09 historical provenance';$files=@{}
 foreach($name in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json',$Leaf)){$files[$name]=Read-KmcSmallFixturePinnedJson (Join-Path $root $name) $(if($name-ceq$Leaf){$Hash}else{$null})}
 $r=$files['runtime-request.json'].value;$result=$files['runtime-result.json'].value;$game=$files['runtime-game-result.json'].value;$manifest=$files['runtime-artifacts.json'].value;$artifact=$files[$Leaf].value
 foreach($object in @($r,$result,$game,$manifest,$artifact)){Assert-KmcBootstrapInventoryString $object.runId $RunId;Assert-KmcBootstrapInventoryString $object.scenario $r.scenario}
 Assert-KmcBootstrapInventoryString $r.evidenceRoot $root
 foreach($object in @($result,$game,$artifact)){Assert-KmcBootstrapInventoryString $object.status 'PASS';Assert-KmcAssistedArray $object.errors 0 0 'F09 historical PASS errors'}
 foreach($key in @('branch','commit','productVersion','dllSha256','dllMvid')){foreach($object in @($result,$game,$artifact)){Assert-KmcBootstrapInventoryString $object.$key $r.$key}}
 Assert-KmcBootstrapInventoryString $result.transactionToken $r.transactionToken;Assert-KmcBootstrapInventoryString $game.transactionToken $r.transactionToken
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $result.$key 0 0;Assert-KmcOrdinaryInteger $game.$key 0 0}
 Assert-KmcOrdinaryInteger $game.loadRequestCount 1 1;Assert-KmcOrdinaryInteger $game.workingLoadRequestCount 1 1
 Assert-KmcOrdinaryInteger $game.saveRequestCount $Writes $Writes;Assert-KmcOrdinaryInteger $game.workingSaveRequestCount $Writes $Writes
 foreach($key in @('unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','baselineLoadRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 Assert-KmcBootstrapInventoryString $result.evidenceManifestSha256 $files['runtime-artifacts.json'].sha256
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 $files['runtime-game-result.json'].sha256
 $records=@($manifest.artifacts|Where-Object{$_.relativePath-ceq$Leaf-or$_.kind-ceq$Kind})
 if($records.Count-ne1){throw 'F09 historical manifest must contain one exact native artifact role.'}
 Assert-KmcBootstrapInventoryString $records[0].relativePath $Leaf;Assert-KmcBootstrapInventoryString $records[0].kind $Kind
 Assert-KmcBootstrapInventoryString $records[0].sha256 $Hash;Assert-KmcOrdinaryInteger $records[0].length $files[$Leaf].length $files[$Leaf].length
 Assert-KmcAssistedColdArtifactEnvelope $r $artifact $Kind
 [pscustomobject]@{artifact=$artifact;request=$r;artifactPath=$files[$Leaf].path;artifactSha256=$Hash;artifactLength=$files[$Leaf].length;sourceRunId=$RunId;
  receiptPins=@($files.Values|ForEach-Object{[pscustomobject]@{path=$_.path;sha256=$_.sha256;length=$_.length;lastWriteTimeUtcTicks=$_.lastWriteTimeUtcTicks}})}
}
function Get-KmcChunk7SmallFixtureSource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7SmallFixtureSelection ([pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId='unassigned-f09-fixture';smallFixture=$Selection})
 $cold=[pscustomobject]@{case='owner-seed-readonly-inventory';baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0;seedId=$Selection.seedId;seedSha256=$Selection.seedSha256}
 $seed=Get-KmcChunk7OwnerSeedSource $cold $Fixture $Candidate
 Assert-KmcChunk7SmallFixtureDescriptor ([pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId='unassigned-f09-fixture';smallFixture=$Selection;persistenceLoad=$seed.descriptor;fixture=$Fixture})
 $q=Get-KmcSmallFixtureHistoricalArtifact $Selection.seedQualificationRunId 'chunk7-assisted-cold-inventory-evidence.json' 'chunk7-assisted-cold-inventory-evidence' $Selection.seedQualificationArtifactSha256 0
 Assert-KmcBootstrapInventoryString $q.request.scenario 'chunk7-assisted-cold-inventory';Assert-KmcAssistedSame $q.request.assistedColdInventory $cold 'F09 original owner cold selection'
 Assert-KmcAssistedSame $q.request.persistenceLoad $seed.descriptor 'F09 qualified clean master descriptor'
 $v=$q.artifact.observations;Assert-KmcChunk7Boolean $v.ownerSeedQualification.qualified $true
 Assert-KmcBootstrapInventoryString $v.ownerSeedQualification.identity.mainActorId '73681ef2-b281-4bc6-bc05-2de68c1616cb'
 foreach($key in @('gameId','gameName','area')){Assert-KmcBootstrapInventoryString $v.ownerSeedQualification.identity.$key $seed.descriptor.$key}
 foreach($key in @('custodySettled','nativeWorkerSettled','observersReleased','sourceUnchanged')){Assert-KmcChunk7Boolean $v.cleanup.$key $true}
 $i=Get-KmcSmallFixtureHistoricalArtifact $Selection.nativeInventoryRunId 'chunk7-world-evidence.json' 'chunk7-world-evidence' $Selection.nativeInventoryArtifactSha256 1
 $inv=$i.artifact.observations.nativeProfileInventory
 Assert-KmcBootstrapInventoryString $inv.captureStatus 'PASS';Assert-KmcBootstrapInventoryString $inv.captureKind 'installed-native-small-body-catalogue';Assert-KmcBootstrapInventoryString $inv.gameplayAssemblyMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 foreach($key in @('profileAdmissionChanged','nativeFixtureCreated','poseQualified','ordinaryAcquisitionProven')){Assert-KmcChunk7Boolean $inv.$key $false}
 $race=@($inv.smallRaces|Where-Object raceGuid -CEQ 'b0c3ef2729c498f47970bb50fa1acd30');if($race.Count-ne1){throw 'F09 native Halfling race is absent or ambiguous.'};Assert-KmcBootstrapInventoryString $race[0].size 'Small'
 $preset=@($race[0].presets|Where-Object presetGuid -CEQ '639cebe8cbc098345af429d62fd79578');if($preset.Count-ne1){throw 'F09 native default Halfling preset is absent or ambiguous.'}
 Assert-KmcChunk7Boolean $preset[0].present $true;Assert-KmcBootstrapInventoryString $preset[0].maleSkeleton.name 'MaleHalfling';Assert-KmcChunk7Boolean $preset[0].maleSkeleton.present $true
 foreach($p in @($q,$i)){foreach($pin in $p.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 original immutable provenance'}}
 [pscustomobject]@{path=$seed.path;descriptor=$seed.descriptor;seedId=$seed.seedId;registryPath=$seed.registryPath;registrySha256=$seed.registrySha256;registryEntry=$seed.registryEntry;intakePath=$seed.intakePath;intakeSha256=$seed.intakeSha256;
  qualification=$q;inventory=$i;fixtureSetupOnly=$true;producerAncestry=$false;nativeAuthorityGranted=$false}
}
function Copy-KmcChunk7SmallFixtureSource($Source,[string]$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcChunk7SmallFixtureSelection ([pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId=$Lock.RunId;smallFixture=$Selection})
 $fresh=Get-KmcChunk7SmallFixtureSource $Selection $Fixture $Candidate
 foreach($key in @('path','descriptor','seedId','registrySha256','registryEntry','intakePath','intakeSha256','qualification','inventory')){Assert-KmcAssistedSame $Source.$key $fresh.$key ('F09 qualified provenance '+$key)}
 $lab=Get-KmcLabRoot;$run=Assert-KmcChildPath $ProfileRoot (Join-Path $lab 'runtime-staging') 'F09 isolated profile'
 Assert-KmcBootstrapInventoryString $run (Join-Path $lab ('runtime-staging/persistence-'+$Lock.RunId));Assert-KmcDirectoryTreeCloneable $run 'F09 isolated profile'
 $ownerPath=Join-Path $run 'owner.json';$ownerPin=Read-KmcSmallFixturePinnedJson $ownerPath $null 16384;$owner=$ownerPin.value
 Assert-KmcAssistedObjectValue $owner @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'F09 profile owner'
 foreach($pair in @(@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('scenario','chunk7-small-fixture-preparation'),@('sourceSha256',$fresh.descriptor.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($null-ne$owner.persistenceCase-and$owner.persistenceCase-cne''){throw 'F09 profile contains a foreign persistence case.'}
 [void](Write-KmcChunk7OwnerSeedSourceRecord $fresh (Join-Path $run 'small-fixture-owner-seed.json') $ownerPath $ownerPin.sha256 $Lock)
 foreach($pair in @(@($fresh.qualification,'small-fixture-seed-qualification.json'),@($fresh.inventory,'small-fixture-native-inventory.json'))){
  $source=$pair[0];$input=$null;$output=$null;$algorithm=$null;$destination=Join-Path $run $pair[1]
  try{
   $input=[IO.FileStream]::new($source.artifactPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$algorithm=[Security.Cryptography.SHA256]::Create()
   $held=[BitConverter]::ToString($algorithm.ComputeHash($input)).Replace('-','').ToLowerInvariant()
   if($held-cne$source.artifactSha256-or$input.Length-ne$source.artifactLength){throw 'Held F09 native provenance changed.'};$input.Position=0
   $output=[IO.FileStream]::new($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read);$input.CopyTo($output);$output.Flush();$output.Position=0
   if(([BitConverter]::ToString($algorithm.ComputeHash($output)).Replace('-','').ToLowerInvariant())-cne$source.artifactSha256-or$output.Length-ne$source.artifactLength){throw 'F09 staged native provenance differs.'}
   foreach($pin in $source.receiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 held provenance receipt'}
   Assert-KmcAssistedSettledFile $ownerPath $ownerPin 'F09 profile owner';[void](Assert-KmcRuntimeLockOwner $Lock)
  }finally{try{if($algorithm){$algorithm.Dispose()}}finally{try{if($output){$output.Dispose()}}finally{if($input){$input.Dispose()}}}}
 }
 [void](Get-KmcChunk7SmallFixtureSource $Selection $Fixture $Candidate);[void](Assert-KmcRuntimeLockOwner $Lock)
 [pscustomobject]@{path=$run;seedId=$fresh.seedId;fixtureSetupOnly=$true;producerAncestry=$false;nativeAuthorityGranted=$false}
}
function Assert-KmcChunk7SmallFixtureOutput($Request,$Artifact){
 Assert-KmcChunk7SmallFixtureEvidence $Request $Artifact
 $root=Get-KmcAssistedSaveRoot $Request;Assert-KmcDirectoryTreeCloneable $root 'F09 settled save root';$w=$Artifact.observations.nativeWrite
 Assert-KmcAssistedSettledFile $w.path $w 'F09 native Manual';Assert-KmcAssistedSettledFile (Join-Path $root $Request.persistenceLoad.fileName) $Request.persistenceLoad 'F09 immutable seed copy'
 $actual=@(Get-ChildItem -LiteralPath $root -Force);$names=@($actual|ForEach-Object Name|Sort-Object -CaseSensitive)
 if(@($actual|Where-Object PSIsContainer).Count-ne0-or(ConvertTo-KmcChunk7Canonical $names)-cne(ConvertTo-KmcChunk7Canonical @($Request.persistenceLoad.fileName,$w.fileName|Sort-Object -CaseSensitive))){throw 'F09 output save root contains an unowned entry.'}
 Add-Type -AssemblyName System.IO.Compression;$stream=$null;$zip=$null
 try{
  $stream=[IO.FileStream]::new($w.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  if($zip.Entries.Count-gt4096){throw 'F09 native archive entry count is unbounded.'}
  $header=Read-KmcChunk7AssistedMetadataEntry $zip 'header.json' 1048576;$archive=Read-KmcChunk7AssistedMetadataEntry $zip 'kmc-mounted-state' 131072
  foreach($pair in @(@('Name','internalName'),@('GameId','gameId'),@('GameName','gameName'),@('Area','area'),@('Type','type'))){Assert-KmcBootstrapInventoryString $header.($pair[0]) $w.($pair[1])}
  Assert-KmcOrdinaryInteger $header.CompatibilityVersion 1 1;Assert-KmcChunk7Boolean $header.IsAutoLevelupSave $false
  Assert-KmcSmallFixtureEmptyArchive $archive $Request.persistenceLoad;Assert-KmcAssistedSame $archive $w.archive 'F09 actual ZIP metadata versus native readback'
 }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
 Assert-KmcAssistedSettledFile $w.path $w 'F09 native Manual after readback'
}
