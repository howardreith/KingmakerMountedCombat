Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7ThirdHorseColdSource.ps1')
. (Join-Path $PSScriptRoot 'Chunk7FourthHorseColdSource.ps1')
. (Join-Path $PSScriptRoot 'Chunk7FifthHorseColdSource.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SixthHorseColdSource.ps1')
function Read-KmcChunk7AssistedColdInventorySelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'assisted cold selection'
 Assert-KmcNotReparsePoint $path 'assisted cold selection';Assert-KmcNotHardLink $path 'assisted cold selection'
 $file=Get-Item -LiteralPath $path
 if($file.PSIsContainer-or$file.Length-lt1-or$file.Length-gt16384){throw 'Assisted cold selection is empty or oversized.'}
 $length=$file.Length;$ticks=$file.LastWriteTimeUtc.Ticks;$hash=Get-KmcSha256 $path
 Assert-KmcManifestJsonMembersUnique $path 'assisted cold selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$RunId;assistedColdInventory=$selection})
 $after=Get-Item -LiteralPath $path
 if($after.Length-ne$length-or$after.LastWriteTimeUtc.Ticks-ne$ticks-or(Get-KmcSha256 $path)-cne$hash){throw 'Assisted cold selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}
# Owner-approved development seeds are named by a reviewed, committed registry
# (data, so a future seed needs no product rebuild). The registry replaces
# producer ancestry only for its own entries; nothing here grants native authority.
function Get-KmcChunk7OwnerSeedRegistryPath {Join-Path $PSScriptRoot 'Chunk7OwnerSeedRegistry.json'}
function Read-KmcChunk7OwnerSeedRegistry {
 $path=[IO.Path]::GetFullPath((Get-KmcChunk7OwnerSeedRegistryPath))
 Assert-KmcNotReparsePoint $path 'owner seed registry';Assert-KmcNotHardLink $path 'owner seed registry'
 $file=Get-Item -LiteralPath $path
 if($file.PSIsContainer-or$file.Length-lt1-or$file.Length-gt65536){throw 'Owner seed registry is empty or oversized.'}
 $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'owner seed registry';$registry=Read-KmcJson $path
 Assert-KmcAssistedObjectValue $registry @('schemaVersion','kind','authority','seeds') 'owner seed registry'
 Assert-KmcOrdinaryInteger $registry.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $registry.kind 'kmc-owner-seed-registry'
 Assert-KmcAssistedArray $registry.seeds 1 64 'owner seed registry entries';$ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($entry in $registry.seeds){Assert-KmcChunk7OwnerSeedEntry $entry;if(-not$ids.Add($entry.seedId)){throw 'Owner seed registry repeats a seed identity.'}}
 if((Get-KmcSha256 $path)-cne$hash){throw 'Owner seed registry changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$hash;registry=$registry}
}
function Read-KmcChunk7OwnerSeedHeader([string]$Path){
 $stream=$null;$zip=$null
 try{
  Add-Type -AssemblyName System.IO.Compression
  $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  if($zip.Entries.Count-gt4096){throw 'Owner seed archive entry inventory is unbounded.'}
  Read-KmcChunk7AssistedMetadataEntry $zip 'header.json' 1MB
 }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
}
function Get-KmcChunk7OwnerSeedSource($Selection,$Fixture,$Candidate){
 $run='unassigned-owner-seed';if($Selection.seedId-ceq$run){$run='unassigned-owner-seed-2'}
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection})
 if(-not(Test-KmcChunk7OwnerSeedSelection $Selection)){throw 'Owner seed source requires the owner-seed selection kind.'}
 $registry=Read-KmcChunk7OwnerSeedRegistry
 $entries=@($registry.registry.seeds|Where-Object{$_.seedId-ceq$Selection.seedId})
 if($entries.Count-ne1){throw 'Owner seed is not in the reviewed owner-seed registry.'};$entry=$entries[0]
 if($entry.sha256-cne$Selection.seedSha256){throw 'Owner seed SHA256 differs from its registry entry.'}
 $lab=Get-KmcLabRoot;$ownerRoot=Join-Path $lab 'fixtures/owner-supplied'
 Assert-KmcNotReparsePoint $ownerRoot 'owner-supplied fixture root'
 $master=Assert-KmcChildPath (Join-Path $lab $entry.masterPath) $ownerRoot 'owner seed master'
 $intake=Assert-KmcChildPath (Join-Path $lab $entry.intakePath) $ownerRoot 'owner seed intake record'
 foreach($pair in @(@($master,'owner seed master'),@($intake,'owner seed intake record'))){
  if(-not(Test-Path -LiteralPath $pair[0] -PathType Leaf)){throw ($pair[1]+' is missing.')}
  Assert-KmcNotReparsePoint $pair[0] $pair[1];Assert-KmcNotHardLink $pair[0] $pair[1]
 }
 if((Get-KmcSha256 $intake)-cne$entry.intakeSha256){throw 'Owner seed intake record differs from its registered SHA256.'}
 $file=Get-Item -LiteralPath $master
 if($file.Length-ne$entry.length-or(Get-KmcSha256 $master)-cne$entry.sha256){throw 'Owner seed master differs from its registered length/SHA256.'}
 # Bounded read-only header qualification; the master itself is never loaded.
 $header=Read-KmcChunk7OwnerSeedHeader $master
 foreach($pair in @(@('Name','internalName'),@('GameId','gameId'),@('GameName','gameName'),@('Area','area'),@('Type','type'),@('CampaignDLC','campaignDlc'))){
  if($null-eq$header.PSObject.Properties[$pair[0]]){throw ('Owner seed header lacks '+$pair[0]+'.')}
  Assert-KmcBootstrapInventoryString $header.($pair[0]) $entry.($pair[1])
 }
 Assert-KmcOrdinaryInteger $header.CompatibilityVersion 1 1;Assert-KmcChunk7Boolean $header.IsAutoLevelupSave $false
 if($header.DLC-isnot[Array]){throw 'Owner seed header DLC list is missing.'};Assert-KmcAssistedSame $header.DLC $entry.dlc 'owner seed header DLC list'
 $after=Get-Item -LiteralPath $master
 if($after.Length-ne$file.Length-or$after.LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $master)-cne$entry.sha256){throw 'Owner seed master changed during read-only qualification.'}
 $descriptor=[pscustomobject]@{internalName=$entry.internalName;fileName=$entry.fileName;sha256=$entry.sha256;length=[long]$entry.length;lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$entry.gameId;gameName=$entry.gameName;area=$entry.area}
 # Reuses the cold family's no-alias checks against the Working/Baseline fixtures.
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 [pscustomobject]@{path=$master;descriptor=$descriptor;seedId=$entry.seedId;registryPath=$registry.path;registrySha256=$registry.sha256;registryEntry=$entry;
  intakePath=$intake;intakeSha256=$entry.intakeSha256;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
function Get-KmcChunk7AssistedColdInventorySource($Selection,$Fixture,$Candidate){
 if(Test-KmcChunk7OwnerSeedSelection $Selection){return Get-KmcChunk7OwnerSeedSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7ThirdHorseColdSelection $Selection){return Get-KmcChunk7ThirdHorseColdSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7FourthHorseSourceSelection $Selection){return Get-KmcChunk7FourthHorseColdSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7FifthHorseDepartureSelection $Selection){return Get-KmcChunk7FifthHorseDepartureSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7FifthHorseColdSelection $Selection){return Get-KmcChunk7FifthHorseColdSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7SixthHorseColdSelection $Selection){return Get-KmcChunk7SixthHorseColdSource $Selection $Fixture $Candidate}
 if(Test-KmcChunk7PaidFixtureColdSelection $Selection){
  $producerCase=if(Test-KmcChunk7MediumFixtureColdSelection $Selection){'owner-seed-default-human-horse-manual'}else{'owner-seed-default-halfling-horse-manual'}
  return Get-KmcChunk7SmallFixtureColdSource $Selection.sourceRunId $Selection.sourceArtifactSha256 $Selection.sourceArchiveSha256 $Fixture $Candidate $producerCase
 }
 # The producer gate still verifies its complete native completion/restoration,
 # final Manual bytes and bounded empty schema3 metadata. No new source alias.
 $run='unassigned-assisted-cold';if($Selection.sourceRunId-ceq$run){$run='unassigned-assisted-cold-2'}
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection})
 $keys=[pscustomobject]@{sourceRunId=$Selection.sourceRunId;sourceArtifactSha256=$Selection.sourceArtifactSha256;sourceArchiveSha256=$Selection.sourceArchiveSha256}
 $source=Get-KmcChunk7AssistedFixtureSource $keys $Fixture $Candidate
 Assert-KmcChunk7AssistedColdInventoryDescriptor ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$run;assistedColdInventory=$Selection;persistenceLoad=$source.descriptor;fixture=$Fixture})
 Assert-KmcBootstrapInventoryString $source.sourceRunId $Selection.sourceRunId
 Assert-KmcBootstrapInventoryString $source.artifactSha256 $Selection.sourceArtifactSha256
 Assert-KmcChunk7Boolean $source.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $source.nativeAuthorityGranted $false
 # Return the actual gate's descriptor and pins without granting runtime identity.
 $source
}
function Assert-KmcAssistedColdSourcePins($Source,[bool]$SmallFixture=$false,[bool]$FourthHorse=$false,[bool]$FifthDeparture=$false,[bool]$SixthHorse=$false){
 if($SixthHorse){
  if(-not$SmallFixture-or$FourthHorse-or$FifthDeparture){throw 'Sixth-Horse receipt shape cannot be borrowed by another source mode.'}
  Assert-KmcSixthHorseColdReceiptPins $Source
 }
 if($FifthDeparture-and(-not$SmallFixture-or$FourthHorse)){throw 'Native departure receipt shape cannot be borrowed by another source mode.'}
 if($Source.receiptPins-isnot[Array]-or$Source.receiptPins.Count-ne$(if($SixthHorse){41}elseif($FifthDeparture){16}elseif($FourthHorse){10}elseif($SmallFixture){9}else{8})){throw 'Producer cold source lost its complete receipt pins.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 foreach($pin in $Source.receiptPins){
  if($pin.path-isnot[string]-or-not$seen.Add([IO.Path]::GetFullPath($pin.path))){throw 'Assisted cold receipt path is malformed or duplicated.'}
  Assert-KmcAssistedSettledFile $pin.path $pin 'assisted cold producer receipt'
 }
 Assert-KmcAssistedSettledFile $Source.path $Source.descriptor 'assisted cold source Manual'
 Assert-KmcNotReparsePoint $Source.artifactPath 'assisted cold source artifact';Assert-KmcNotHardLink $Source.artifactPath 'assisted cold source artifact'
 if($Source.artifactLength-isnot[int]-and$Source.artifactLength-isnot[long]){throw 'Assisted cold source artifact length is not an integer.'}
 $info=Get-Item -LiteralPath $Source.artifactPath
 if($Source.artifactLength-lt1-or$Source.artifactLength-gt16777216-or$info.Length-ne$Source.artifactLength-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Assisted cold source artifact changed.'}
}
function Copy-KmcChunk7AssistedColdInventorySourceArtifact($Source,[string]$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 Assert-KmcChunk7AssistedColdInventorySelection ([pscustomobject]@{scenario='chunk7-assisted-cold-inventory';runId=$Lock.RunId;assistedColdInventory=$Selection})
 $fresh=Get-KmcChunk7AssistedColdInventorySource $Selection $Fixture $Candidate
 $isSeed=Test-KmcChunk7OwnerSeedSelection $Selection
 $isFourth=Test-KmcChunk7FourthHorseSourceSelection $Selection
 $isDeparture=Test-KmcChunk7FifthHorseDepartureSelection $Selection
 $isSixth=Test-KmcChunk7SixthHorseColdSelection $Selection
 $isSmall=(Test-KmcChunk7PaidFixtureColdSelection $Selection)-or(Test-KmcChunk7HorseWorldColdSelection $Selection)
 $keys=if($isSeed){@('path','descriptor','seedId','registrySha256','registryEntry','intakePath','intakeSha256')}
  elseif($isSmall){@('path','artifactPath','artifactSha256','artifactLength','sourceRunId','sourceProcessId','descriptor','receiptPins')}
  else{@('path','artifactPath','artifactSha256','artifactLength','sourceRunId','requestPath','requestSha256','manifestSha256','descriptor','receiptPins')}
 foreach($key in $keys){Assert-KmcAssistedSame $Source.$key $fresh.$key ('assisted cold qualified source '+$key)}
 $lab=Get-KmcLabRoot;$run=Assert-KmcChildPath $ProfileRoot (Join-Path $lab 'runtime-staging') 'assisted cold isolated profile'
 $expected=Join-Path $lab ('runtime-staging/persistence-'+$Lock.RunId)
 Assert-KmcBootstrapInventoryString $run $expected
 Assert-KmcDirectoryTreeCloneable $run 'assisted cold isolated profile'
 $ownerPath=Join-Path $run 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'assisted cold profile owner';Assert-KmcNotHardLink $ownerPath 'assisted cold profile owner'
 $ownerInfo=Get-Item -LiteralPath $ownerPath;if($ownerInfo.Length-lt1-or$ownerInfo.Length-gt16384){throw 'Assisted cold profile owner is empty or oversized.'}
 $ownerHash=Get-KmcSha256 $ownerPath;Assert-KmcManifestJsonMembersUnique $ownerPath 'assisted cold profile owner';$owner=Read-KmcJson $ownerPath
 Assert-KmcAssistedObjectValue $owner @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'assisted cold profile owner'
 foreach($pair in @(@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('scenario','chunk7-assisted-cold-inventory'),@('sourceSha256',$fresh.descriptor.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($null-ne$owner.persistenceCase-and($owner.persistenceCase-isnot[string]-or$owner.persistenceCase-cne'')){throw 'Assisted cold profile owner contains a foreign persistence case.'}
 $destination=Join-Path $run 'assisted-cold-source.json'
 if(Test-Path -LiteralPath $destination){throw 'Assisted cold source destination already exists.'}
 if($isSeed){return Write-KmcChunk7OwnerSeedSourceRecord $fresh $destination $ownerPath $ownerHash $Lock}
 Assert-KmcAssistedColdSourcePins $fresh $isSmall $isFourth $isDeparture $isSixth
 $inputStream=$null;$outputStream=$null;$algorithm=$null
 try{
  $inputStream=[IO.FileStream]::new($fresh.artifactPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $algorithm=[Security.Cryptography.SHA256]::Create()
  $heldHash=[BitConverter]::ToString($algorithm.ComputeHash($inputStream)).Replace('-','').ToLowerInvariant()
  if($heldHash-cne$fresh.artifactSha256-or$inputStream.Length-ne$fresh.artifactLength){throw 'Held assisted cold source bytes differ.'}
  $inputStream.Position=0
  $outputStream=[IO.FileStream]::new($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read)
  $inputStream.CopyTo($outputStream);$outputStream.Flush();$outputStream.Position=0
  $copiedHash=[BitConverter]::ToString($algorithm.ComputeHash($outputStream)).Replace('-','').ToLowerInvariant()
  if($copiedHash-cne$fresh.artifactSha256-or$outputStream.Length-ne$fresh.artifactLength){throw 'Assisted cold copied bytes differ.'}
  # The retained source handle denies concurrent writing/deletion of its file;
  # producer/Manual/owner pins and the transaction owner are checked again.
  Assert-KmcAssistedColdSourcePins $fresh $isSmall $isFourth $isDeparture $isSixth
  if((Get-KmcSha256 $ownerPath)-cne$ownerHash){throw 'Assisted cold profile owner changed during copy.'}
  [void](Assert-KmcRuntimeLockOwner $Lock)
 }finally{
  try{if($algorithm){$algorithm.Dispose()}}finally{try{if($outputStream){$outputStream.Dispose()}}finally{if($inputStream){$inputStream.Dispose()}}}
 }
 [pscustomobject]@{path=$destination;sha256=$fresh.artifactSha256;length=$fresh.artifactLength;sourceRunId=$fresh.sourceRunId;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}

function Read-KmcAssistedColdSourceArtifact($Request){
 if(Test-KmcChunk7OwnerSeedSelection $Request.assistedColdInventory){return Read-KmcChunk7OwnerSeedSourceRecord $Request}
 Assert-KmcChunk7AssistedColdInventoryDescriptor $Request
 $base=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
 $evidenceRoot=Assert-KmcChildPath $Request.evidenceRoot $base 'assisted cold evidence root'
 Assert-KmcBootstrapInventoryString $evidenceRoot (Join-Path $base $Request.runId)
 $leaf=if((Test-KmcChunk7FifthHorseSourceSelection $Request.assistedColdInventory)-or(Test-KmcChunk7SixthHorseColdSelection $Request.assistedColdInventory)){'chunk7-manual-derived-evidence.json'}elseif(Test-KmcChunk7HorseWorldColdSelection $Request.assistedColdInventory){'chunk7-world-evidence.json'}elseif(Test-KmcChunk7PaidFixtureColdSelection $Request.assistedColdInventory){'chunk7-small-fixture-evidence.json'}else{'chunk7-assisted-fixture-evidence.json'}
 $path=Join-Path (Join-Path $base $Request.assistedColdInventory.sourceRunId) $leaf
 $path=Assert-KmcChildPath $path $base 'assisted cold producer artifact'
 Assert-KmcNotReparsePoint $path 'assisted cold producer artifact';Assert-KmcNotHardLink $path 'assisted cold producer artifact'
 $before=Get-Item -LiteralPath $path
 if($before.PSIsContainer-or$before.Length-lt1-or$before.Length-gt16777216){throw 'Assisted cold producer artifact is empty or oversized.'}
 $stream=$null;$algorithm=$null
 try{
  $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $algorithm=[Security.Cryptography.SHA256]::Create()
  $hash=[BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()
  if($stream.Length-ne$before.Length-or$hash-cne$Request.assistedColdInventory.sourceArtifactSha256){throw 'Assisted cold producer artifact differs from the qualified source hash.'}
  Assert-KmcManifestJsonMembersUnique $path 'assisted cold producer artifact'
  $value=Read-KmcJson $path
  if($null-eq$value-or$value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw 'Assisted cold producer artifact must be one JSON object.'}
  $stream.Position=0;$afterHash=[BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()
  $after=Get-Item -LiteralPath $path
  if($after.Length-ne$before.Length-or$after.LastWriteTimeUtc.Ticks-ne$before.LastWriteTimeUtc.Ticks-or$afterHash-cne$hash){throw 'Assisted cold producer artifact changed during read.'}
 }finally{try{if($algorithm){$algorithm.Dispose()}}finally{if($stream){$stream.Dispose()}}}
 # This is the exact pinned raw artifact, not a replacement for the complete
 # producer gate performed before admission and again under the copy lock.
 $value
}

# The honest owner-seed source record: the registry entry and intake reference,
# never a fabricated producer artifact. Written once (CreateNew) under the lock.
function Write-KmcChunk7OwnerSeedSourceRecord($Fresh,[string]$Destination,[string]$OwnerPath,[string]$OwnerHash,$Lock){
 $record=[ordered]@{schemaVersion=1;kind='kmc-owner-seed-source';seedId=$Fresh.seedId;registrySha256=$Fresh.registrySha256;registryEntry=$Fresh.registryEntry
  intake=[ordered]@{path=$Fresh.registryEntry.intakePath;sha256=$Fresh.intakeSha256};ownerAuthorized=$true;producerAncestry=$false;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
 $bytes=[Text.UTF8Encoding]::new($false).GetBytes((ConvertTo-Json -InputObject $record -Depth 8))
 $stream=$null
 try{$stream=[IO.FileStream]::new($Destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read);$stream.Write($bytes,0,$bytes.Length);$stream.Flush()}
 finally{if($stream){$stream.Dispose()}}
 # The master, intake and transaction owner are checked again after the write.
 if((Get-KmcSha256 $Fresh.path)-cne$Fresh.descriptor.sha256-or(Get-KmcSha256 $Fresh.intakePath)-cne$Fresh.intakeSha256){throw 'Owner seed master or intake changed during record placement.'}
 if((Get-KmcSha256 $OwnerPath)-cne$OwnerHash){throw 'Assisted cold profile owner changed during copy.'}
 [void](Assert-KmcRuntimeLockOwner $Lock)
 [pscustomobject]@{path=$Destination;sha256=(Get-KmcSha256 $Destination);length=[long]$bytes.Length;seedId=$Fresh.seedId;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}
function Read-KmcChunk7OwnerSeedSourceRecord($Request){
 Assert-KmcChunk7AssistedColdInventoryDescriptor $Request
 $root=Get-KmcAssistedSaveRoot $Request
 $path=Join-Path (Split-Path $root -Parent) 'assisted-cold-source.json'
 Assert-KmcNotReparsePoint $path 'owner seed source record';Assert-KmcNotHardLink $path 'owner seed source record'
 $before=Get-Item -LiteralPath $path
 if($before.PSIsContainer-or$before.Length-lt1-or$before.Length-gt65536){throw 'Owner seed source record is empty or oversized.'}
 $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'owner seed source record';$value=Read-KmcJson $path
 $after=Get-Item -LiteralPath $path
 if($after.Length-ne$before.Length-or$after.LastWriteTimeUtc.Ticks-ne$before.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $path)-cne$hash){throw 'Owner seed source record changed during read.'}
 [pscustomobject]@{path=$path;sha256=$hash;value=$value}
}
