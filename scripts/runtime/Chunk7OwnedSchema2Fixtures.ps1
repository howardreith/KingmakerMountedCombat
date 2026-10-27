# Reuses the bounded held-source member inventory and pinned JSON readers from schema1.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7OwnedSchema1Fixtures.ps1')
function Assert-KmcOwnedSchema2Derivation($Receipt,$Request,$Source){
 Assert-KmcOwnedSchema2Request $Request
 if(-not(Test-KmcOwnedSchema2ColdRequest $Request)){throw 'Metadata-copy receipt is cold-only.'}
 Assert-KmcOwnedSchema1Object $Receipt @('schemaVersion','contract','sourceRunId','sourceArtifactSha256','sourceDescriptor','descriptor','metadata','nativeMembersVerified','sourceMembers','members','nativeRestorationProven') 'owned schema-2 derivation'
 Assert-KmcOrdinaryInteger $Receipt.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $Receipt.contract 'owned-schema2-metadata-copy-v1';Assert-KmcChunk7Boolean $Receipt.nativeRestorationProven $false
 Assert-KmcBootstrapInventoryString $Receipt.sourceRunId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $Receipt.sourceArtifactSha256 $Request.ownedPairs.checkpointSourceArtifactSha256
 if((ConvertTo-KmcChunk7Canonical $Receipt.sourceDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Source.descriptor)-or(ConvertTo-KmcChunk7Canonical $Receipt.descriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Historical copy substituted its original or derivative descriptor.'}
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Receipt.descriptor.$key $Source.descriptor.$key}
 if($Receipt.descriptor.sha256-ceq$Source.descriptor.sha256){throw 'Schema-1 derivative was not distinct from native schema-3 source bytes.'}
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 $projection=ConvertTo-KmcOwnedSchema2Metadata $checkpoint.observations.nativeWrite.archive $Request
 if((ConvertTo-KmcChunk7Canonical $Receipt.metadata)-cne(ConvertTo-KmcChunk7Canonical $projection)){throw 'Historical metadata is not the complete exact one-owner projection.'}
 foreach($list in @('sourceMembers','members')){
  if($Receipt.$list-isnot[Array]){throw 'Member receipt must be an array.'};$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  foreach($row in $Receipt.$list){Assert-KmcOwnedSchema1Object $row @('name','length','sha256') 'archive member receipt'
   if($row.name-isnot[string]-or[string]::IsNullOrEmpty($row.name)-or$row.name.Length-gt256-or-not$seen.Add($row.name)-or$row.sha256-isnot[string]-or$row.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Member receipt name or hash is invalid.'};Assert-KmcOrdinaryInteger $row.length 0 268435456
  }
 }
 Assert-KmcOwnedSchema1MemberPreservation $Receipt.sourceMembers $Receipt.members
 Assert-KmcOrdinaryInteger $Receipt.nativeMembersVerified ($Receipt.members.Count-1) ($Receipt.members.Count-1)
 foreach($leaf in @('header.json','kmc-mounted-state')){if(@($Receipt.members|Where-Object name -CEQ $leaf).Count-ne1){throw 'Required exact member receipt is absent.'}}
}
function New-KmcOwnedSchema2Input($Source,$Selection,$Fixture,$Candidate,$Lock){
 if($Selection.case-cne'owner-seed-checkpoint-load'){throw 'Schema2 metadata copy is admitted only for its owner-seed historical cold consumer.'}
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 $fresh=Get-KmcChunk7OwnedPairsSource $Selection 'legacy-mounted-pending-tb-manual' $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $fresh)-cne(ConvertTo-KmcChunk7Canonical $Source)){throw 'Owned historical source changed after full qualification.'}
 # The qualified native source path is derived by the existing completed-run
 # qualifier. No arbitrary/human archive path is admitted by this entry point.
 $path=Assert-KmcChildPath $fresh.path (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Selection.checkpointSourceRunId+'/Saved Games')) 'qualified owned source'
 Assert-KmcNotReparsePoint $path 'qualified owned source';Assert-KmcNotHardLink $path 'qualified owned source'
 $file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt256MB-or$file.Length-ne$Source.descriptor.length-or$file.LastWriteTimeUtc.Ticks-ne$Source.descriptor.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$Source.descriptor.sha256){throw 'Qualified historical source bytes or metadata changed.'}
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 $request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$Lock.RunId;ownedPairs=$Selection;persistenceCase='legacy-mounted-pending-tb-manual';persistenceLoad=$Source.descriptor;fixture=$Fixture}
 $parent=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-owned-schema2-fixtures';[void][IO.Directory]::CreateDirectory($parent);Assert-KmcDirectoryTreeCloneable $parent 'owned schema-2 derivative parent'
 $owned=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'fresh owned schema-2 derivative'
 if(Test-Path -LiteralPath $owned){throw 'Historical derivative directory already exists.'};[void][IO.Directory]::CreateDirectory($owned)
 $destination=Join-Path $owned $Source.descriptor.fileName;$input=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  $before=Get-KmcOwnedSchema1Members $input
  if((ConvertTo-KmcChunk7Canonical $before.metadata)-cne(ConvertTo-KmcChunk7Canonical $checkpoint.observations.nativeWrite.archive)){throw 'Native source metadata differs from its completed one-owner observation.'}
  $metadata=ConvertTo-KmcOwnedSchema2Metadata $before.metadata $request;$json=$metadata|ConvertTo-Json -Depth 64 -Compress;$bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($json)
  if($bytes.Length-le0-or$bytes.Length-gt32KB-or$json.Length-gt32768){throw 'Historical metadata cannot fit the unchanged schema-2 decoder bound.'}
  $output=[IO.File]::Open($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$input.Position=0;$input.CopyTo($output)}finally{$output.Dispose()}
  if((Get-KmcSha256 $destination)-cne$Source.descriptor.sha256){throw 'Native source changed during CREATE_NEW copy.'}
  $stream=[IO.File]::Open($destination,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$zip=$null
  try{
   $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Update,$false)
   $entries=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state');if($entries.Count-ne1){throw 'Historical metadata member lost uniqueness.'}
   $entries[0].Delete();$entry=$zip.CreateEntry('kmc-mounted-state');$inner=$entry.Open();try{$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}
  }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
  $check=[IO.File]::Open($destination,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  try{$after=Get-KmcOwnedSchema1Members $check}finally{$check.Dispose()}
  Assert-KmcOwnedSchema1MemberPreservation $before.members $after.members
  if((ConvertTo-KmcChunk7Canonical $after.metadata)-cne(ConvertTo-KmcChunk7Canonical $metadata)-or(Get-KmcSha256 $path)-cne$Source.descriptor.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Historical copy or immutable source changed during projection.'}
  $out=Get-Item -LiteralPath $destination
  if($out.Length-le0-or$out.Length-gt256MB){throw 'Historical derivative archive exceeds its original guarded byte bound.'}
  $descriptor=[pscustomobject][ordered]@{internalName=$Source.descriptor.internalName;fileName=$out.Name;sha256=(Get-KmcSha256 $destination);length=[long]$out.Length;lastWriteTimeUtcTicks=[long]$out.LastWriteTimeUtc.Ticks;gameId=$Source.descriptor.gameId;gameName=$Source.descriptor.gameName;area=$Source.descriptor.area}
  $receipt=[pscustomobject][ordered]@{schemaVersion=1;contract='owned-schema2-metadata-copy-v1';sourceRunId=$Selection.checkpointSourceRunId;sourceArtifactSha256=$Selection.checkpointSourceArtifactSha256;sourceDescriptor=$Source.descriptor;descriptor=$descriptor;metadata=$metadata;nativeMembersVerified=($after.members.Count-1);sourceMembers=$before.members;members=$after.members;nativeRestorationProven=$false}
  $receiptBytes=[Text.UTF8Encoding]::new($false,$true).GetBytes(($receipt|ConvertTo-Json -Depth 64));if($receiptBytes.Length-gt256KB){throw 'Historical derivative receipt exceeds its bounded observation size.'}
  $receiptPath=Join-Path $owned 'owned-schema2-derivation.json';$receiptStream=[IO.File]::Open($receiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$receiptStream.Write($receiptBytes,0,$receiptBytes.Length)}finally{$receiptStream.Dispose()}
  return [pscustomobject]@{path=$destination;descriptor=$descriptor;receiptPath=$receiptPath;receiptSha256=(Get-KmcSha256 $receiptPath)}
 }finally{$input.Dispose()}
}
function Copy-KmcOwnedSchema2Inputs($Source,$Derivative,[string]$ProfileRoot,$Lock,$Request){
 $Request=[pscustomobject]$Request;Assert-KmcOwnedSchema2Request $Request;[void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'owned historical profile';Assert-KmcDirectoryTreeCloneable $run 'owned historical profile'
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Lock.RunId))){throw 'Historical profile differs from exact current lock.'}
 $owner=Read-KmcJson (Join-Path $run 'owner.json');foreach($p in @(@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('scenario','chunk7-owned-two-pair-campaign'),@('persistenceCase','legacy-mounted-pending-tb-manual'),@('sourceSha256',$Request.persistenceLoad.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($p[0]) $p[1]}
 $receipt=Read-KmcOwnedSchema1PinnedJson (Assert-KmcChildPath $Derivative.receiptPath (Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-owned-schema2-fixtures') 'owned historical derivation') $Request.schema2DerivationSha256 256KB;Assert-KmcOwnedSchema2Derivation $receipt $Request $Source
 $rows=@(@($Source.artifactPath,$Source.artifactSha256,'owned-two-pair-source.json',8MB),@($Source.checkpointArtifactPath,$Source.checkpointArtifactSha256,'owned-schema2-checkpoint.json',16MB),@($Derivative.receiptPath,$Request.schema2DerivationSha256,'owned-schema2-derivation.json',256KB))
 foreach($row in $rows){
  $inputPath=$row[0];Assert-KmcNotReparsePoint $inputPath 'owned historical receipt';Assert-KmcNotHardLink $inputPath 'owned historical receipt';Assert-KmcManifestJsonMembersUnique $inputPath 'owned historical receipt'
  $file=Get-Item -LiteralPath $inputPath;if($file.Length-le0-or$file.Length-gt$row[3]-or(Get-KmcSha256 $inputPath)-cne$row[1]){throw 'Historical provenance changed before locked copy.'}
  $input=[IO.File]::Open($inputPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$output=$null
  try{$output=[IO.File]::Open((Join-Path $run $row[2]),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$input.CopyTo($output)}finally{if($output){$output.Dispose()};$input.Dispose()}
  if((Get-KmcSha256 (Join-Path $run $row[2]))-cne$row[1]-or(Get-KmcSha256 $inputPath)-cne$row[1]){throw 'Historical provenance changed during locked copy.'}
 }
}
function Assert-KmcOwnedSchema2ColdSource($Request,$Source,$Artifact){
 $Request=[pscustomobject]$Request;Assert-KmcOwnedSchema2Request $Request
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId)
 foreach($row in @(@('owned-schema2-checkpoint.json',$Request.ownedPairs.checkpointSourceArtifactSha256,16MB),@('owned-schema2-derivation.json',$Request.schema2DerivationSha256,256KB))){
  $path=Join-Path $profile $row[0];Assert-KmcNotReparsePoint $path 'owned historical provenance';Assert-KmcNotHardLink $path 'owned historical provenance';Assert-KmcManifestJsonMembersUnique $path 'owned historical provenance'
  $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt$row[2]-or(Get-KmcSha256 $path)-cne$row[1]){throw 'Historical staged provenance changed during native load/resave.'}
 }
 $receipt=Read-KmcOwnedSchema1PinnedJson (Join-Path $profile 'owned-schema2-derivation.json') $Request.schema2DerivationSha256 256KB;Assert-KmcOwnedSchema2Derivation $receipt $Request $Source
 Assert-KmcOwnedSchema2Migration $Request $Artifact.observations
 if((ConvertTo-KmcChunk7Canonical $receipt.metadata)-cne(ConvertTo-KmcChunk7Canonical $Artifact.observations.schema2Migration.nativeInputMetadata)){throw 'Native schema2 decoder input differs from the immutable receipted projection.'}
 Assert-KmcBootstrapInventoryString $Artifact.observations.schema2Migration.originalNativeArchiveSha256 $Source.descriptor.sha256
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 $input=Join-Path (Join-Path $profile 'Saved Games') $Request.persistenceLoad.fileName;Assert-KmcNotReparsePoint $input 'owned historical input';Assert-KmcNotHardLink $input 'owned historical input'
 if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length){throw 'Immutable historical derivative changed during native restoration.'}
 foreach($spec in @(@($Source.path,$Source.descriptor,$receipt.sourceMembers,$checkpoint.observations.nativeWrite.archive),@($input,$Request.persistenceLoad,$receipt.members,$receipt.metadata))){
  $path=$spec[0];$descriptor=$spec[1];$members=$spec[2];$metadata=$spec[3]
  Assert-KmcNotReparsePoint $path 'requalified historical archive';Assert-KmcNotHardLink $path 'requalified historical archive';$file=Get-Item -LiteralPath $path
  if($file.Length-ne$descriptor.length-or(Get-KmcSha256 $path)-cne$descriptor.sha256){throw 'Historical derivation archive bytes differ during requalification.'}
  $stream=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  try{$actual=Get-KmcOwnedSchema1Members $stream}finally{$stream.Dispose()}
  if((ConvertTo-KmcChunk7Canonical $actual.metadata)-cne(ConvertTo-KmcChunk7Canonical $metadata)-or(ConvertTo-KmcChunk7Canonical @($actual.members|Sort-Object name -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical @($members|Sort-Object name -CaseSensitive))-or(Get-KmcSha256 $path)-cne$descriptor.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Historical derivation metadata or native member inventory differs from its exact receipt.'}
 }
}
