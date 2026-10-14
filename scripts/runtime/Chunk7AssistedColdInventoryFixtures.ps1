Set-StrictMode -Version Latest
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
function Get-KmcChunk7AssistedColdInventorySource($Selection,$Fixture,$Candidate){
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
function Assert-KmcAssistedColdSourcePins($Source){
 if($Source.receiptPins-isnot[Array]-or$Source.receiptPins.Count-ne8){throw 'Assisted cold source lost its complete producer receipt pins.'}
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
 foreach($key in @('path','artifactPath','artifactSha256','artifactLength','sourceRunId','requestPath','requestSha256','manifestSha256','descriptor','receiptPins')){Assert-KmcAssistedSame $Source.$key $fresh.$key ('assisted cold qualified source '+$key)}
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
 Assert-KmcAssistedColdSourcePins $fresh
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
  Assert-KmcAssistedColdSourcePins $fresh
  if((Get-KmcSha256 $ownerPath)-cne$ownerHash){throw 'Assisted cold profile owner changed during copy.'}
  [void](Assert-KmcRuntimeLockOwner $Lock)
 }finally{
  try{if($algorithm){$algorithm.Dispose()}}finally{try{if($outputStream){$outputStream.Dispose()}}finally{if($inputStream){$inputStream.Dispose()}}}
 }
 [pscustomobject]@{path=$destination;sha256=$fresh.artifactSha256;length=$fresh.artifactLength;sourceRunId=$fresh.sourceRunId;fixtureSetupOnly=$true;nativeAuthorityGranted=$false}
}

function Read-KmcAssistedColdSourceArtifact($Request){
 Assert-KmcChunk7AssistedColdInventoryDescriptor $Request
 $base=Join-Path (Get-KmcLabRoot) 'runtime-evidence'
 $evidenceRoot=Assert-KmcChildPath $Request.evidenceRoot $base 'assisted cold evidence root'
 Assert-KmcBootstrapInventoryString $evidenceRoot (Join-Path $base $Request.runId)
 $path=Join-Path (Join-Path $base $Request.assistedColdInventory.sourceRunId) 'chunk7-assisted-fixture-evidence.json'
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
