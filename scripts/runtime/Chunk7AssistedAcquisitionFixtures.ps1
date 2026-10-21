Set-StrictMode -Version Latest
function Read-KmcAssistedConnectionJson([string]$Path,$ExpectedHash=$null){
 if($null-ne$ExpectedHash-and($ExpectedHash-isnot[string]-or$ExpectedHash-cnotmatch'^[a-f0-9]{64}$')){throw 'Assisted connection expected hash must be null or one exact SHA256 string.'}
 $path=Assert-KmcChildPath $Path (Join-Path (Get-KmcLabRoot) 'runtime-evidence') 'assisted connection receipt'
 Assert-KmcNotReparsePoint $path 'assisted connection receipt';Assert-KmcNotHardLink $path 'assisted connection receipt'
 $info=Get-Item -LiteralPath $path;if($info.PSIsContainer-or$info.Length-lt1-or$info.Length-gt16777216){throw 'Assisted connection receipt is empty or oversized.'}
 $stream=$null;$hashAlgorithm=$null
 try{
  $stream=[IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$hashAlgorithm=[Security.Cryptography.SHA256]::Create()
  $hash=[BitConverter]::ToString($hashAlgorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()
  if($null-ne$ExpectedHash-and$hash-cne$ExpectedHash){throw 'Assisted connection receipt hash differs.'}
  Assert-KmcManifestJsonMembersUnique $path 'assisted connection receipt';$value=Read-KmcJson $path;Assert-KmcConnectionObject $value 'receipt'
  $stream.Position=0;$afterHash=[BitConverter]::ToString($hashAlgorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant();$after=Get-Item -LiteralPath $path
  if($stream.Length-ne$info.Length-or$after.Length-ne$info.Length-or$after.LastWriteTimeUtc.Ticks-ne$info.LastWriteTimeUtc.Ticks-or$afterHash-cne$hash){throw 'Assisted connection receipt changed during read.'}
 }finally{try{if($hashAlgorithm){$hashAlgorithm.Dispose()}}finally{if($stream){$stream.Dispose()}}}
 [pscustomobject]@{value=$value;pin=[pscustomobject]@{path=$path;sha256=$hash;length=[long]$info.Length;lastWriteTimeUtcTicks=$info.LastWriteTimeUtc.Ticks}}
}
function Read-KmcAssistedCumulativeArtifacts($Request){
 Assert-KmcChunk7CumulativeDescriptor $Request
 if(-not(Test-KmcAssistedCumulativeMode $Request.cumulativeAcquisition)){throw 'Assisted artifact reader cannot accept legacy source mode.'}
 $base=Join-Path (Get-KmcLabRoot) 'runtime-evidence';Assert-KmcBootstrapInventoryString $Request.evidenceRoot (Join-Path $base $Request.runId)
 $s=$Request.cumulativeAcquisition
 $p=Read-KmcAssistedConnectionJson (Join-Path (Join-Path $base $s.sourceRunId) 'chunk7-assisted-fixture-evidence.json') $s.sourceArtifactSha256
 $c=Read-KmcAssistedConnectionJson (Join-Path (Join-Path $base $s.assistedColdRunId) 'chunk7-assisted-cold-inventory-evidence.json') $s.assistedColdArtifactSha256
 [pscustomobject]@{producer=$p.value;cold=$c.value;producerPin=$p.pin;coldPin=$c.pin}
}
function Get-KmcAssistedCumulativeSource($Selection,$Fixture,$Candidate){
 $consumer='unassigned-assisted-cumulative';if($Selection.sourceRunId-ceq$consumer-or$Selection.assistedColdRunId-ceq$consumer){$consumer='unassigned-assisted-cumulative-2'}
 Assert-KmcChunk7CumulativeSelection ([pscustomobject]@{scenario='chunk7-cumulative-acquisition';runId=$consumer;cumulativeAcquisition=$Selection})
 if(-not(Test-KmcAssistedCumulativeMode $Selection)){throw 'Assisted qualifier accepts only its exact cumulative source mode.'}
 $source=Get-KmcChunk7AssistedFixtureSource ([pscustomobject]@{sourceRunId=$Selection.sourceRunId;sourceArtifactSha256=$Selection.sourceArtifactSha256;sourceArchiveSha256=$Selection.sourceArchiveSha256}) $Fixture $Candidate
 $lab=Get-KmcLabRoot;$root=Join-Path (Join-Path $lab 'runtime-evidence') $Selection.assistedColdRunId
 $profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.assistedColdRunId)) (Join-Path $lab 'runtime-staging') 'completed assisted cold profile';Assert-KmcDirectoryTreeCloneable $profile 'completed assisted cold profile'
 $records=@{};$pins=@()
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','profile-cache-changes.json','chunk7-assisted-cold-inventory-evidence.json')){
  $expected=if($leaf-ceq'chunk7-assisted-cold-inventory-evidence.json'){$Selection.assistedColdArtifactSha256}else{$null}
  $record=Read-KmcAssistedConnectionJson (Join-Path $root $leaf) $expected;$records[$leaf]=$record.value;$pins+=,$record.pin
 }
 $request=$records['runtime-request.json'];$result=$records['runtime-result.json'];$game=$records['runtime-game-result.json'];$manifest=$records['runtime-artifacts.json'];$isolation=$records['base-game-isolation-result.json'];$cold=$records['chunk7-assisted-cold-inventory-evidence.json'];$profileReceipt=$records['profile-cache-changes.json']
 Assert-KmcChunk7AssistedColdInventoryDescriptor $request
 Assert-KmcBootstrapInventoryString $request.runId $Selection.assistedColdRunId;Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $request.assistedColdInventory.$key $Selection.$key}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 foreach($key in @('working','baseline')){Assert-KmcAssistedSame $request.fixture.$key $Fixture.$key ('assisted cold anchor '+$key)}
 Assert-KmcAssistedSame $request.persistenceLoad $source.descriptor 'completed cold exact original producer Manual'
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'assisted cold owner';Assert-KmcNotHardLink $ownerPath 'assisted cold owner'
 $ownerInfo=Get-Item -LiteralPath $ownerPath;if($ownerInfo.Length-lt1-or$ownerInfo.Length-gt16384){throw 'Assisted cold owner is empty or oversized.'}
 $ownerHash=Get-KmcSha256 $ownerPath;Assert-KmcManifestJsonMembersUnique $ownerPath 'assisted cold owner';$owner=Read-KmcJson $ownerPath
 Assert-KmcAssistedObjectValue $owner @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'completed assisted cold owner'
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $Selection.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase-and($owner.persistenceCase-isnot[string]-or$owner.persistenceCase-cne'')){throw 'Completed assisted cold owner selects another persistence case.'}
 $pins+=,[pscustomobject]@{path=$ownerPath;sha256=$ownerHash;length=[long]$ownerInfo.Length;lastWriteTimeUtcTicks=$ownerInfo.LastWriteTimeUtc.Ticks}
 $manifestHash=@($pins|Where-Object path -CEQ (Join-Path $root 'runtime-artifacts.json'))[0].sha256
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcOrdinaryInteger $receipt.schemaVersion 2 2;foreach($key in @('assertionFailCount','subscenarioFailCount')){Assert-KmcOrdinaryInteger $receipt.$key 0 0}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Assisted cumulative source requires error-free completed cold inventory.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $manifestHash
 }
 if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Completed cold restoration identity is absent.'}
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 (@($pins|Where-Object path -CEQ (Join-Path $root 'runtime-game-result.json'))[0].sha256)
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount')){Assert-KmcOrdinaryInteger $game.$key 1 1}
 Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $true;Assert-KmcChunk7Boolean $game.movementExperimentEnabled $false;Assert-KmcBootstrapInventoryString $game.currentGameMode 'Default';Assert-KmcChunk7Boolean $game.loadedAreaPresent $true
 Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
 Assert-KmcAssistedProfileReceipt $profileReceipt $request.runId
 Assert-KmcReadOnlyArtifactManifest $request $manifestHash -GameResult
 Assert-KmcChunk7AssistedColdInventoryArtifact $request $manifest 'PASS'
 $producer=(Read-KmcAssistedConnectionJson $source.artifactPath $Selection.sourceArtifactSha256).value
 Assert-KmcChunk7AssistedColdInventoryEvidence $request $cold $producer 'PASS'
 $consumerRequest=[pscustomobject]@{scenario='chunk7-cumulative-acquisition';runId=$consumer;cumulativeAcquisition=$Selection;persistenceLoad=$source.descriptor;fixture=$Fixture;commit=$Candidate.commit;dllSha256=$Candidate.dllSha256;dllMvid=$Candidate.dllMvid}
 $projection=Assert-KmcAssistedCumulativeSource $consumerRequest $producer $cold
 foreach($pin in $pins){Assert-KmcAssistedSettledFile $pin.path $pin 'completed assisted cold receipt'};Assert-KmcAssistedColdSourcePins $source
 $coldPin=@($pins|Where-Object path -CEQ (Join-Path $root 'chunk7-assisted-cold-inventory-evidence.json'))[0]
 [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;artifactPath=$source.artifactPath;artifactSha256=$source.artifactSha256;artifactLength=$source.artifactLength;
  sourceMode='assisted-producer-and-cold';assistedProducerRunId=$Selection.sourceRunId;assistedColdRunId=$Selection.assistedColdRunId;
  coldArtifactPath=$coldPin.path;coldArtifactSha256=$coldPin.sha256;coldArtifactLength=$coldPin.length;producer=$source;coldReceiptPins=$pins;
  bootstrapRunId=$null;coldInventoryRunId=$null;manualRunId=$null;fixtureSetupWasAssisted=$true;nativeAuthorityGranted=$false}
}
function Copy-KmcAssistedCumulativeSourceArtifacts($Source,[string]$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 Assert-KmcChunk7CumulativeSelection ([pscustomobject]@{scenario='chunk7-cumulative-acquisition';runId=$Lock.RunId;cumulativeAcquisition=$Selection})
 $fresh=Get-KmcAssistedCumulativeSource $Selection $Fixture $Candidate;Assert-KmcAssistedSame $Source $fresh 'locked assisted cumulative lineage'
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'assisted cumulative profile';Assert-KmcBootstrapInventoryString $run (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Lock.RunId));Assert-KmcDirectoryTreeCloneable $run 'assisted cumulative profile'
 $ownerPath=Join-Path $run 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'assisted cumulative owner';Assert-KmcNotHardLink $ownerPath 'assisted cumulative owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'assisted cumulative owner';$ownerHash=Get-KmcSha256 $ownerPath;$owner=Read-KmcJson $ownerPath
 Assert-KmcAssistedObjectValue $owner @('runId','transactionToken','scenario','persistenceCase','sourceSha256','saveRoot') 'assisted cumulative owner'
 foreach($pair in @(@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('scenario','chunk7-cumulative-acquisition'),@('sourceSha256',$fresh.descriptor.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($null-ne$owner.persistenceCase-and($owner.persistenceCase-isnot[string]-or$owner.persistenceCase-cne'')){throw 'Assisted cumulative profile selects another persistence case.'}
 $rows=@([pscustomobject]@{path=$fresh.artifactPath;sha256=$fresh.artifactSha256;length=$fresh.artifactLength;leaf='cumulative-acquisition-source.json'},[pscustomobject]@{path=$fresh.coldArtifactPath;sha256=$fresh.coldArtifactSha256;length=$fresh.coldArtifactLength;leaf='cumulative-assisted-cold-source.json'})
 $held=[Collections.Generic.List[IO.FileStream]]::new();$algorithm=[Security.Cryptography.SHA256]::Create()
 try{
  foreach($row in $rows){$dest=Join-Path $run $row.leaf;if(Test-Path -LiteralPath $dest){throw 'Assisted cumulative source destination exists.'};Assert-KmcNotReparsePoint $row.path 'assisted cumulative source';Assert-KmcNotHardLink $row.path 'assisted cumulative source'
   $input=[IO.FileStream]::new($row.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$held.Add($input)
   $sha=[BitConverter]::ToString($algorithm.ComputeHash($input)).Replace('-','').ToLowerInvariant();if($sha-cne$row.sha256-or$input.Length-ne$row.length){throw 'Held assisted cumulative source changed.'}
  }
  for($i=0;$i-lt2;$i++){$row=$rows[$i];$input=$held[$i];$input.Position=0;$output=$null
   try{$output=[IO.FileStream]::new((Join-Path $run $row.leaf),[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Read);$input.CopyTo($output);$output.Flush();$output.Position=0
    $sha=[BitConverter]::ToString($algorithm.ComputeHash($output)).Replace('-','').ToLowerInvariant();if($sha-cne$row.sha256-or$output.Length-ne$row.length){throw 'Copied assisted cumulative source changed.'}
   }finally{if($output){$output.Dispose()}}
  }
  Assert-KmcAssistedColdSourcePins $fresh.producer;foreach($pin in $fresh.coldReceiptPins){Assert-KmcAssistedSettledFile $pin.path $pin 'completed assisted cold receipt'}
  if((Get-KmcSha256 $ownerPath)-cne$ownerHash){throw 'Assisted cumulative owner changed during copy.'};[void](Assert-KmcRuntimeLockOwner $Lock)
 }finally{try{$algorithm.Dispose()}finally{foreach($stream in $held){$stream.Dispose()}}}
}
