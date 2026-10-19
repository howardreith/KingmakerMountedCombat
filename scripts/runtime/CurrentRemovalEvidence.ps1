# Current P07 readback is schema3. The singular schema2-shaped view is explicitly
# an observation projection and never supplies native restoration or save input.
Set-StrictMode -Version Latest
function Assert-KmcCurrentRemovalGraph($Archive,[string]$GameId,[string]$Area,[bool]$Mounted,[string]$RiderId,[string]$MountId){
 Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'current P07 actual native archive'
 Assert-KmcOrdinaryInteger $Archive.SchemaVersion 3 3;Assert-KmcOrdinaryInteger $Archive.GameTimeTicks 0 ([long]::MaxValue)
 foreach($kv in @(@('CampaignId',$GameId),@('AreaId',$Area),@('Policy','rider-principal-distinct-native-v1'),@('RulesId','crpg-transport-v1'))){Assert-KmcBootstrapInventoryString $Archive.($kv[0]) $kv[1]}
 if($Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne$(if($Mounted){1}else{0})-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$Archive.Slots-isnot[Array]-or$Archive.Slots.Count-gt128-or$null-ne$Archive.Combat-or(-not$Mounted-and$Archive.Slots.Count-ne0)){throw 'Current P07 archive has extra mounted/retained owners, combat or cleanup controls.'}
 if($Mounted){$p=$Archive.Pairs[0]
  Assert-KmcExactProperties $p @('PairId','Generation','ProfileId','Rider','Mount') 'current P07 native Mammoth pair'
  if($p.PairId-isnot[string]-or$p.PairId-cnotmatch'\A[a-f0-9]{32}\z'-or$p.PairId-ceq('0'*32)){throw 'Current P07 native pair identity is not its stable archive identity.'}
  Assert-KmcOrdinaryInteger $p.Generation 1 ([long]::MaxValue);Assert-KmcBootstrapInventoryString $p.ProfileId 'medium-humanoid-mammoth-v1'
  Assert-KmcChunk7PersistenceNativeActor $p.Rider;Assert-KmcChunk7PersistenceNativeActor $p.Mount
  Assert-KmcBootstrapInventoryString $p.Rider.Id $RiderId;Assert-KmcBootstrapInventoryString $p.Mount.Id $MountId
  if($RiderId-ceq$MountId){throw 'Current P07 pair roles collide.'}
  # The existing read-only DTO projection validates kinds1..4; the actual
  # default-off-Charge P07 fixture cannot project a kind5 slot.
  $slots=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);foreach($s in $Archive.Slots){Assert-KmcExactProperties $s @('ActorId','Index','Kind') 'current P07 native control binding';Assert-KmcOrdinaryGuid $s.ActorId;Assert-KmcOrdinaryInteger $s.Index 0 127;Assert-KmcOrdinaryInteger $s.Kind 1 4;if(-not$slots.Add($s.ActorId+':'+$s.Index)){throw 'Current P07 repeats a native control slot.'}}
 }
}
function Assert-KmcCurrentRemovalProjection($Recorded,[string]$GameId,[string]$Area,[bool]$Mounted,[string]$RiderId,[string]$MountId){
 Assert-KmcOrdinaryInteger $Recorded.nativeSourceSchema 3 3
 Assert-KmcBootstrapInventoryString $Recorded.snapshotProjection 'schema3-single-pair-observation-v1'
 $a=$Recorded.nativeArchive;Assert-KmcCurrentRemovalGraph $a $GameId $Area $Mounted $RiderId $MountId
 $s=$Recorded.snapshot;Assert-KmcExactProperties $s @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots','Combat') 'current P07 explicitly projected observation'
 Assert-KmcOrdinaryInteger $s.SchemaVersion 2 2;Assert-KmcChunk7Boolean $s.Mounted $Mounted
 foreach($key in @('CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Slots','Combat')){if((ConvertTo-KmcChunk7Canonical $s.$key)-cne(ConvertTo-KmcChunk7Canonical $a.$key)){throw 'Current P07 observation projection differs from its full native archive.'}}
 foreach($key in @('ProfileId','Rider','Mount')){$expected=if($Mounted){$a.Pairs[0].$key}else{$null};if((ConvertTo-KmcChunk7Canonical $s.$key)-cne(ConvertTo-KmcChunk7Canonical $expected)){throw 'Current P07 projection substituted a pair, profile or native actor debt.'}}
}
function Get-KmcCurrentRemovalReferenceScan([string]$Path){
 $guids=@('4016c7db400ab721ff125aef9e65e202','7db7c50677e39f09feef56f3831fc723','98e651899e6278d938de77af1d69bd32','6874a165bf8bda3531ee4e2abc10c899','f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
 Add-Type -AssemblyName System.IO.Compression;$stream=$null;$zip=$null;$count=0;$bytes=0L;$hits=[Collections.Generic.List[string]]::new();$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 try{$stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  foreach($entry in $zip.Entries){if([string]::IsNullOrEmpty($entry.FullName)-or-not$seen.Add($entry.FullName)){throw 'Current P07 archive has unnamed or duplicate members.'}
   if(-not$entry.FullName.EndsWith('.json',[StringComparison]::OrdinalIgnoreCase)-and$entry.FullName-cne'kmc-mounted-state'){continue}
   if($entry.Length-lt0-or$entry.Length-gt64MB-or++$count-gt1024){throw 'Current P07 reference member exceeds the native scan bound.'};$bytes+=$entry.Length;if($bytes-gt512MB){throw 'Current P07 reference scan exceeds the native total bound.'}
   $reader=[IO.StreamReader]::new($entry.Open(),[Text.UTF8Encoding]::new($false,$false));try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()}
   foreach($guid in $guids){if($text.IndexOf($guid,[StringComparison]::OrdinalIgnoreCase)-ge0){$hits.Add($entry.FullName+':'+$guid)}}
  }
 }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
 if($count-eq0){throw 'Current P07 reference scan read no native member.'}
 [pscustomobject]@{scannedMembers=$count;scannedBytes=$bytes;referenceHits=$hits.ToArray()}
}
function Assert-KmcCurrentRemovalArchive($Recorded,[string]$Path,[string]$GameId,[string]$GameName,[string]$Area,[bool]$Mounted,[string]$RiderId,[string]$MountId){
 Assert-KmcCurrentRemovalProjection $Recorded $GameId $Area $Mounted $RiderId $MountId
 Assert-KmcNotReparsePoint $Path 'current P07 archive';Assert-KmcNotHardLink $Path 'current P07 archive'
 if($Recorded.path-cne$Path-or$Recorded.sha256-isnot[string]-or$Recorded.sha256-cnotmatch'\A[a-f0-9]{64}\z'){throw 'Current P07 native archive path/hash is not its declared output.'}
 Assert-KmcOrdinaryInteger $Recorded.length 1 268435456
 $info=Get-Item -LiteralPath $Path;if($info.Length-ne$Recorded.length-or(Get-KmcSha256 $Path)-cne$Recorded.sha256){throw 'Current P07 native archive bytes changed.'}
 $members=Read-KmcCampaignArchiveMembers $Path;$h=$members.header;$name=if($Mounted){'KMC_P01'}else{'KMC_CLEANUP'}
 foreach($kv in @(@('Name',$name),@('Type','Manual'),@('GameId',$GameId),@('GameName',$GameName),@('Area',$Area))){Assert-KmcBootstrapInventoryString $h.($kv[0]) $kv[1]};Assert-KmcOrdinaryInteger $h.CompatibilityVersion 1 1
 if((ConvertTo-KmcChunk7Canonical $members.kmc)-cne(ConvertTo-KmcChunk7Canonical $Recorded.nativeArchive)){throw 'Current P07 recorded full native readback differs from actual ZIP metadata.'}
 if((Get-KmcSha256 $Path)-cne$Recorded.sha256-or(Get-Item -LiteralPath $Path).LastWriteTimeUtc.Ticks-ne$info.LastWriteTimeUtc.Ticks){throw 'Current P07 archive changed while reading.'}
}
function Assert-KmcCurrentRemovalEvidence($Request,$Rows){
 if($Request.scenario-cne'persistence-p07-save'-or$Request.persistenceCase-cne'prepare-removal'){throw 'Current P07 removal evidence belongs only to its selected native save case.'}
 $open=@($Rows|Where-Object kind -CEQ 'native-write-complete');$prepared=@($Rows|Where-Object kind -CEQ 'removal-prepared');$initial=@($Rows|Where-Object kind -CEQ 'initial')
 if($open.Count-ne1-or$prepared.Count-ne1-or$initial.Count-ne1){throw 'Current P07 removal requires exact opening/cleanup observations.'}
 $w=$open[0].detail;$p=$prepared[0].detail;$f=$Request.fixture.working;$rider=$initial[0].rider.Id;$mount=$initial[0].mount.Id
 $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
 Assert-KmcCurrentRemovalArchive $w (Join-Path $root 'Manual_300_KMC_P01.zks') $f.gameId $f.gameName $f.area $true $rider $mount
 Assert-KmcCurrentRemovalArchive $p.cleanup (Join-Path $root 'Manual_301_KMC_CLEANUP.zks') $f.gameId $f.gameName $f.area $false $rider $mount
 $scan=Get-KmcCurrentRemovalReferenceScan $p.cleanup.path
 if($scan.referenceHits.Count-ne0-or$p.referenceHits-isnot[Array]-or$p.referenceHits.Count-ne0-or$p.scannedMembers-ne$scan.scannedMembers-or$p.scannedBytes-ne$scan.scannedBytes){throw 'Current P07 cleanup scan differs from actual members or retains a registered KMC reference.'}
 $expected=@($f.fileName,'Manual_300_KMC_P01.zks','Manual_301_KMC_CLEANUP.zks')|Sort-Object -CaseSensitive
 $actual=@(Get-ChildItem -LiteralPath $root -File -Filter '*.zks'|ForEach-Object Name|Sort-Object -CaseSensitive)
 if(($actual-join'|')-cne($expected-join'|')){throw 'Current P07 output inventory contains an unmeasured archive or omitted its protected input.'}
 $input=Join-Path $root $f.fileName;Assert-KmcNotReparsePoint $input 'current P07 immutable Working input';Assert-KmcNotHardLink $input 'current P07 immutable Working input'
 if((Get-KmcSha256 $input)-cne$f.sha256-or(Get-Item -LiteralPath $input).Length-ne$f.length){throw 'Current P07 changed its exact qualified disposable input.'}
}
function Assert-KmcCurrentRemovalSource([string]$SourceRunId,[string]$ArchiveSha,$Fixture,[string]$ArtifactRole){
 $root=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$SourceRunId)
 Assert-KmcDirectoryTreeCloneable $root 'current P07 source evidence'
 $request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
 if($request.runId-cne$SourceRunId-or$request.scenario-cne'persistence-p07-save'-or$request.persistenceCase-cne'prepare-removal'-or$result.status-cne'PASS'-or$result.runId-cne$SourceRunId-or$result.scenario-cne$request.scenario-or$result.transactionToken-cne$request.transactionToken){throw 'Current P07 source is not its exact completed selected process.'}
 foreach($key in @('commit','dllSha256','dllMvid','branch','productVersion')){Assert-KmcBootstrapInventoryString $result.$key $request.$key}
 foreach($key in @('modsRestored','workingRestored','saveProtectionPassed','baselineImmutable','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 if(@($result.errors).Count-ne0-or(ConvertTo-KmcChunk7Canonical $request.fixture.working)-cne(ConvertTo-KmcChunk7Canonical $Fixture.working)){throw 'Current P07 source fixture or terminal errors differ.'}
 $gamePath=Join-Path $root 'runtime-game-result.json';$manifestPath=Join-Path $root 'runtime-artifacts.json';$path=Join-Path $root 'persistence-observations.jsonl'
 $game=Read-KmcJson $gamePath;$manifest=Read-KmcJson $manifestPath;$hash=Get-KmcSha256 $path
 if((Get-KmcSha256 $gamePath)-cne$result.gameResultSha256-or$game.status-cne'PASS'-or$game.runId-cne$SourceRunId-or$game.scenario-cne$request.scenario-or
    (Get-KmcSha256 $manifestPath)-cne$game.evidenceManifestSha256-or(Get-KmcSha256 $manifestPath)-cne$result.evidenceManifestSha256-or$manifest.runId-cne$SourceRunId){throw 'Current P07 source result/manifest bytes are not bound to the completed process.'}
 $artifact=@($manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
 if($artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'-or$artifact[0].sha256-cne$hash-or$artifact[0].length-ne(Get-Item -LiteralPath $path).Length){throw 'Current P07 observations changed after their native result.'}
 Assert-KmcPersistenceScenarioEvidence $request $manifest 'PASS' $game
 $rows=@(Get-Content -LiteralPath $path -Encoding UTF8|ForEach-Object{$_|ConvertFrom-Json})
 $write=if($ArtifactRole-ceq'cleanup-manual'){@($rows|Where-Object kind -CEQ 'removal-prepared')[0].detail.cleanup}else{@($rows|Where-Object kind -CEQ 'native-write-complete')[0].detail}
 if($write.sha256-cne$ArchiveSha){throw 'Current P07 source selected a different native output.'}
 if((Get-KmcSha256 $path)-cne$hash){throw 'Current P07 observations changed during qualification.'}
}
