Set-StrictMode -Version Latest
function Read-KmcChunk7BootstrapInventorySelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'bootstrap cold inventory selection'
 Assert-KmcNotReparsePoint $path 'bootstrap inventory selection';Assert-KmcNotHardLink $path 'bootstrap inventory selection'
 $file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt16KB){throw 'Bootstrap inventory selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'bootstrap inventory selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7BootstrapInventorySelection ([pscustomobject]@{scenario='chunk7-bootstrap-cold-inventory';runId=$RunId;bootstrapInventory=$selection})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Bootstrap inventory selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
function Assert-KmcChunk7BootstrapEmptyMetadata($Descriptor,$Header,$Archive){
 foreach($pair in @(@('Name','internalName'),@('GameId','gameId'),@('GameName','gameName'),@('Area','area'))){Assert-KmcBootstrapInventoryString $Header.($pair[0]) $Descriptor.($pair[1])}
 Assert-KmcBootstrapInventoryString $Header.Type 'Auto';Assert-KmcOrdinaryInteger $Header.CompatibilityVersion 1 1
 Assert-KmcChunk7Boolean $Header.IsAutoLevelupSave $false;Assert-KmcOrdinaryInteger $Archive.SchemaVersion 3 3
 Assert-KmcBootstrapInventoryString $Archive.CampaignId $Descriptor.gameId;Assert-KmcBootstrapInventoryString $Archive.AreaId $Descriptor.area
 foreach($key in @('Pairs','Participations','Slots')){if($Archive.$key-isnot[Array]-or$Archive.$key.Count-ne0){throw 'Bootstrap inventory source contains mounted or native-slot ownership.'}}
 if($null-ne$Archive.Combat){throw 'Bootstrap inventory source contains combat ownership.'}
}
function Assert-KmcChunk7BootstrapInventoryArchive($Request,$Artifact){
 # This helper is called only after the complete successful native bootstrap,
 # same-candidate, base-only and exact isolated-owner source gate below.
 Assert-KmcChunk7NativeBootstrapOutput $Request $Artifact
 $w=$Artifact.observations.nativeWrite;$path=$w.path
 Assert-KmcNotReparsePoint $path 'qualified native bootstrap Auto_1';Assert-KmcNotHardLink $path 'qualified native bootstrap Auto_1'
 $file=Get-Item -LiteralPath $path;$descriptor=[pscustomobject]@{internalName=$w.internalName;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Add-Type -AssemblyName System.IO.Compression
 $stream=$null;$zip=$null;$reader=$null
 try{
  $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  $headers=@($zip.Entries|Where-Object FullName -CEQ 'header.json');$states=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
  if($headers.Count-ne1-or$headers[0].Length-le0-or$headers[0].Length-gt1MB-or$states.Count-ne1-or$states[0].Length-le0-or$states[0].Length-gt128KB){throw 'Qualified bootstrap header/metadata is absent, duplicate or oversized.'}
  $reader=[IO.StreamReader]::new($headers[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd()
  Assert-KmcJsonObjectMembersUnique -Json $json -Description 'qualified bootstrap header';$header=$json|ConvertFrom-Json
  $reader.Dispose();$reader=[IO.StreamReader]::new($states[0].Open(),[Text.UTF8Encoding]::new($false,$true));$json=$reader.ReadToEnd()
  Assert-KmcJsonObjectMembersUnique -Json $json -Description 'qualified bootstrap mounted metadata';$archive=$json|ConvertFrom-Json
  Assert-KmcChunk7BootstrapEmptyMetadata $descriptor $header $archive
 }finally{if($reader){$reader.Dispose()};if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
 if((Get-KmcSha256 $path)-cne$w.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Qualified bootstrap archive changed during read-only metadata validation.'}
}
function Get-KmcChunk7BootstrapInventorySource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7BootstrapInventorySelection ([pscustomobject]@{scenario='chunk7-bootstrap-cold-inventory';runId='unassigned-cold-inventory';bootstrapInventory=$Selection})
 $lab=Get-KmcLabRoot
 $profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'bootstrap source profile'
 Assert-KmcDirectoryTreeCloneable $profile 'bootstrap source profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'bootstrap source evidence'
 Assert-KmcNotReparsePoint $root 'bootstrap source evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json')){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'bootstrap source receipt';Assert-KmcNotHardLink $path 'bootstrap source receipt'
  Assert-KmcManifestJsonMembersUnique $path 'bootstrap source receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotHardLink $ownerPath 'bootstrap profile owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'bootstrap profile owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json')
 $game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-native-main-bootstrap';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 Assert-KmcChunk7NativeBootstrapSelection $request
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $Fixture.working.sha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Bootstrap source owner unexpectedly selected a persistence case.'}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS'
  if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Bootstrap source is not an error-free completed native result.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $files[(Join-Path $root 'runtime-artifacts.json')]
 }
 if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Bootstrap source native transaction/restoration identity is missing.'}
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 $files[(Join-Path $root 'runtime-game-result.json')]
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $false
 foreach($key in @('loadRequestCount','workingLoadRequestCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 foreach($key in @('saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 1 1}
 Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
 Assert-KmcReadOnlyArtifactManifest $request $files[(Join-Path $root 'runtime-artifacts.json')] -GameResult
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7NativeBootstrapArtifact $request $manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-native-bootstrap-evidence.json' 'chunk7-native-bootstrap-evidence'
 $artifactPath=Join-Path $root 'chunk7-native-bootstrap-evidence.json';$artifactFile=Get-Item -LiteralPath $artifactPath
 if((Get-KmcSha256 $artifactPath)-cne$Selection.sourceArtifactSha256-or$artifactFile.Length-le0-or$artifactFile.Length-gt16MB){throw 'Caller-pinned successful bootstrap artifact differs.'}
 $write=$artifact.observations.nativeWrite;$path=Join-Path $profile 'Saved Games/Auto_1.zks'
 Assert-KmcBootstrapInventoryString $write.path $path;Assert-KmcBootstrapInventoryString $write.sha256 $Selection.sourceArchiveSha256
 if($write.sha256-ceq$Fixture.baseline.sha256-or$write.sha256-ceq$Fixture.working.sha256){throw 'Bootstrap source must be the native new-campaign output, not a protected anchor.'}
 # No other save is opened. Decoding these bounded metadata members only
 # qualifies immutable input; only observed native load grants runtime identity.
 Assert-KmcChunk7BootstrapInventoryArchive $request $artifact
 $file=Get-Item -LiteralPath $path
 foreach($p in $files.Keys){if((Get-KmcSha256 $p)-cne$files[$p]){throw 'Bootstrap source receipt changed during qualification.'}}
 if((Get-KmcSha256 $artifactPath)-cne$Selection.sourceArtifactSha256){throw 'Bootstrap source artifact changed during qualification.'}
 [pscustomobject]@{path=$path;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$artifactFile.Length;
  descriptor=[ordered]@{internalName=$write.internalName;fileName='Auto_1.zks';sha256=$Selection.sourceArchiveSha256;length=[long]$file.Length;
   lastWriteTimeUtcTicks=[long]$file.LastWriteTimeUtc.Ticks;gameId=$write.gameId;gameName=$write.gameName;area=$write.area}}
}
function Copy-KmcChunk7BootstrapInventorySourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'bootstrap cold profile'
 Assert-KmcDirectoryTreeCloneable $run 'bootstrap cold profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-bootstrap-cold-inventory'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Bootstrap cold copy lacks its exact locked isolated profile.'}
 $destination=Join-Path $run 'bootstrap-inventory-source.json'
 if(Test-Path -LiteralPath $destination){throw 'Bootstrap source artifact destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'bootstrap source artifact';Assert-KmcNotHardLink $Source.artifactPath 'bootstrap source artifact'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Bootstrap source changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Bootstrap source changed during locked copy.'}
}
