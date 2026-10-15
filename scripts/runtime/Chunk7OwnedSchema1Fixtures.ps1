# Project-owned derivative only; no native or installed archive is ever edited.
Set-StrictMode -Version Latest
function Read-KmcOwnedSchema1PinnedJson([string]$Path,[string]$Sha256,[long]$MaximumBytes){
 Assert-KmcNotReparsePoint $Path 'owned schema-1 receipt';Assert-KmcNotHardLink $Path 'owned schema-1 receipt'
 $file=Get-Item -LiteralPath $Path;if($file.Length-le0-or$file.Length-gt$MaximumBytes-or$Sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owned historical JSON is empty, oversized or unpinned.'}
 $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  if((Get-KmcSha256 $Path)-cne$Sha256){throw 'Owned historical JSON changed before read.'}
  $reader=[IO.StreamReader]::new($stream,[Text.UTF8Encoding]::new($false,$true),$true,4096,$true)
  try{$json=$reader.ReadToEnd()}finally{$reader.Dispose()}
  Assert-KmcJsonObjectMembersUnique -Json $json -Description 'owned historical receipt';$value=$json|ConvertFrom-Json
  if($null-eq$value-or$value-is[Array]-or$value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'-or(Get-KmcSha256 $Path)-cne$Sha256-or(Get-Item -LiteralPath $Path).Length-ne$file.Length-or(Get-Item -LiteralPath $Path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Owned historical JSON identity changed while held.'}
  return $value
 }finally{$stream.Dispose()}
}
function Get-KmcOwnedSchema1Members([IO.Stream]$Stream){
 Add-Type -AssemblyName System.IO.Compression
 $Stream.Position=0;$zip=$null
 try{
  $zip=[IO.Compression.ZipArchive]::new($Stream,[IO.Compression.ZipArchiveMode]::Read,$true)
  if($zip.Entries.Count-lt2-or$zip.Entries.Count-gt1024){throw 'Owned schema-1 archive member count is unbounded.'}
  $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase);$rows=[Collections.Generic.List[object]]::new();[long]$total=0;$metadata=$null
  foreach($entry in $zip.Entries){
   $name=$entry.FullName
   if([string]::IsNullOrEmpty($name)-or$name.Length-gt256-or$name-match'[:\\\x00-\x1f\x7f]'-or$name.StartsWith('/')-or-not$names.Add($name)){throw 'Archive member name is unsafe, duplicated or case-colliding.'}
   $parts=$name.TrimEnd('/').Split('/');if(@($parts|Where-Object{$_-cin@('','..','.')}).Count-ne0){throw 'Archive member path traverses or aliases.'}
   $total+=$entry.Length;if($entry.Length-lt0-or$entry.Length-gt256MB-or$total-gt512MB){throw 'Archive expanded bytes are unbounded.'}
   $inner=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
   try{$digest=([BitConverter]::ToString($sha.ComputeHash($inner))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose();$inner.Dispose()}
   $rows.Add([pscustomobject]@{name=$name;length=[long]$entry.Length;sha256=$digest})
   if($name-ceq'kmc-mounted-state'){
    if($entry.Length-le0-or$entry.Length-gt128KB){throw 'Owned source metadata is empty or oversized.'}
    $reader=[IO.StreamReader]::new($entry.Open(),[Text.UTF8Encoding]::new($false,$true),$false)
    try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()}
    Assert-KmcJsonObjectMembersUnique -Json $text -Description 'owned source mounted metadata';$metadata=$text|ConvertFrom-Json
   }
  }
  if(-not$names.Contains('header.json')-or@($rows|Where-Object name -CEQ 'header.json').Count-ne1-or$null-eq$metadata){throw 'Exact native header or mounted metadata is absent.'}
  return [pscustomobject]@{members=$rows.ToArray();metadata=$metadata}
 }finally{if($zip){$zip.Dispose()}}
}
function Assert-KmcOwnedSchema1MemberPreservation($Before,$After){
 if($Before-isnot[Array]-or$After-isnot[Array]-or$Before.Count-lt2-or$Before.Count-gt1024-or$After.Count-ne$Before.Count){throw 'Metadata projection changed the native member set.'}
 foreach($row in $Before){$match=@($After|Where-Object name -CEQ $row.name)
  if($match.Count-ne1){throw 'Metadata projection omitted or duplicated a member.'}
  if($row.name-cne'kmc-mounted-state'-and($match[0].length-ne$row.length-or$match[0].sha256-cne$row.sha256)){throw 'Metadata projection changed a native archive member.'}
 }
}
function Assert-KmcOwnedSchema1Derivation($Receipt,$Request,$Source){
 Assert-KmcOwnedSchema1Request $Request
 if(-not(Test-KmcOwnedSchema1ColdRequest $Request)){throw 'Metadata-copy receipt is cold-only.'}
 Assert-KmcOwnedSchema1Object $Receipt @('schemaVersion','contract','sourceRunId','sourceArtifactSha256','sourceDescriptor','descriptor','metadata','nativeMembersVerified','sourceMembers','members','nativeRestorationProven') 'owned schema-1 derivation'
 Assert-KmcOrdinaryInteger $Receipt.schemaVersion 1 1;Assert-KmcBootstrapInventoryString $Receipt.contract 'owned-schema1-metadata-copy-v1';Assert-KmcChunk7Boolean $Receipt.nativeRestorationProven $false
 Assert-KmcBootstrapInventoryString $Receipt.sourceRunId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $Receipt.sourceArtifactSha256 $Request.ownedPairs.checkpointSourceArtifactSha256
 if((ConvertTo-KmcChunk7Canonical $Receipt.sourceDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Source.descriptor)-or(ConvertTo-KmcChunk7Canonical $Receipt.descriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Historical copy substituted its original or derivative descriptor.'}
 foreach($key in @('fileName','internalName','gameId','gameName','area')){Assert-KmcBootstrapInventoryString $Receipt.descriptor.$key $Source.descriptor.$key}
 if($Receipt.descriptor.sha256-ceq$Source.descriptor.sha256){throw 'Schema-1 derivative was not distinct from native schema-3 source bytes.'}
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 $projection=ConvertTo-KmcOwnedSchema1Metadata $checkpoint.observations.nativeWrite.archive $Request
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
function New-KmcOwnedSchema1Input($Source,$Selection,$Fixture,$Candidate,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 $fresh=Get-KmcChunk7OwnedPairsSource $Selection 'owned-schema1-resave-manual' $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $fresh)-cne(ConvertTo-KmcChunk7Canonical $Source)){throw 'Owned historical source changed after full qualification.'}
 # The qualified native source path is derived by the existing completed-run
 # qualifier. No arbitrary/human archive path is admitted by this entry point.
 $path=Assert-KmcChildPath $fresh.path (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Selection.checkpointSourceRunId+'/Saved Games')) 'qualified owned source'
 Assert-KmcNotReparsePoint $path 'qualified owned source';Assert-KmcNotHardLink $path 'qualified owned source'
 $file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt256MB-or$file.Length-ne$Source.descriptor.length-or$file.LastWriteTimeUtc.Ticks-ne$Source.descriptor.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$Source.descriptor.sha256){throw 'Qualified historical source bytes or metadata changed.'}
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 $request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$Lock.RunId;ownedPairs=$Selection;persistenceCase='owned-schema1-resave-manual';persistenceLoad=$Source.descriptor;fixture=$Fixture}
 $parent=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-owned-schema1-fixtures';[void][IO.Directory]::CreateDirectory($parent);Assert-KmcDirectoryTreeCloneable $parent 'owned schema-1 derivative parent'
 $owned=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'fresh owned schema-1 derivative'
 if(Test-Path -LiteralPath $owned){throw 'Historical derivative directory already exists.'};[void][IO.Directory]::CreateDirectory($owned)
 $destination=Join-Path $owned $Source.descriptor.fileName;$input=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  $before=Get-KmcOwnedSchema1Members $input
  if((ConvertTo-KmcChunk7Canonical $before.metadata)-cne(ConvertTo-KmcChunk7Canonical $checkpoint.observations.nativeWrite.archive)){throw 'Native source metadata differs from its completed one-owner observation.'}
  $metadata=ConvertTo-KmcOwnedSchema1Metadata $before.metadata $request;$json=$metadata|ConvertTo-Json -Depth 16 -Compress;$bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($json)
  if($bytes.Length-le0-or$bytes.Length-gt32KB-or$json.Length-gt32768){throw 'Historical metadata cannot fit the unchanged schema-1 decoder bound.'}
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
  $receipt=[pscustomobject][ordered]@{schemaVersion=1;contract='owned-schema1-metadata-copy-v1';sourceRunId=$Selection.checkpointSourceRunId;sourceArtifactSha256=$Selection.checkpointSourceArtifactSha256;sourceDescriptor=$Source.descriptor;descriptor=$descriptor;metadata=$metadata;nativeMembersVerified=($after.members.Count-1);sourceMembers=$before.members;members=$after.members;nativeRestorationProven=$false}
  $receiptBytes=[Text.UTF8Encoding]::new($false,$true).GetBytes(($receipt|ConvertTo-Json -Depth 20));if($receiptBytes.Length-gt256KB){throw 'Historical derivative receipt exceeds its bounded observation size.'}
  $receiptPath=Join-Path $owned 'owned-schema1-derivation.json';$receiptStream=[IO.File]::Open($receiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$receiptStream.Write($receiptBytes,0,$receiptBytes.Length)}finally{$receiptStream.Dispose()}
  return [pscustomobject]@{path=$destination;descriptor=$descriptor;receiptPath=$receiptPath;receiptSha256=(Get-KmcSha256 $receiptPath)}
 }finally{$input.Dispose()}
}
function Copy-KmcOwnedSchema1Inputs($Source,$Derivative,[string]$ProfileRoot,$Lock,$Request){
 $Request=[pscustomobject]$Request;Assert-KmcOwnedSchema1Request $Request;[void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'owned historical profile';Assert-KmcDirectoryTreeCloneable $run 'owned historical profile'
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Lock.RunId))){throw 'Historical profile differs from exact current lock.'}
 $owner=Read-KmcJson (Join-Path $run 'owner.json');foreach($p in @(@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('scenario','chunk7-owned-two-pair-campaign'),@('persistenceCase','owned-schema1-resave-manual'),@('sourceSha256',$Request.persistenceLoad.sha256),@('saveRoot',(Join-Path $run 'Saved Games')))){Assert-KmcBootstrapInventoryString $owner.($p[0]) $p[1]}
 $receipt=Read-KmcOwnedSchema1PinnedJson (Assert-KmcChildPath $Derivative.receiptPath (Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-owned-schema1-fixtures') 'owned historical derivation') $Request.schema1DerivationSha256 256KB;Assert-KmcOwnedSchema1Derivation $receipt $Request $Source
 $rows=@(@($Source.artifactPath,$Source.artifactSha256,'owned-two-pair-source.json',8MB),@($Source.checkpointArtifactPath,$Source.checkpointArtifactSha256,'owned-schema1-checkpoint.json',16MB),@($Derivative.receiptPath,$Request.schema1DerivationSha256,'owned-schema1-derivation.json',256KB))
 foreach($row in $rows){
  $inputPath=$row[0];Assert-KmcNotReparsePoint $inputPath 'owned historical receipt';Assert-KmcNotHardLink $inputPath 'owned historical receipt';Assert-KmcManifestJsonMembersUnique $inputPath 'owned historical receipt'
  $file=Get-Item -LiteralPath $inputPath;if($file.Length-le0-or$file.Length-gt$row[3]-or(Get-KmcSha256 $inputPath)-cne$row[1]){throw 'Historical provenance changed before locked copy.'}
  $input=[IO.File]::Open($inputPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$output=$null
  try{$output=[IO.File]::Open((Join-Path $run $row[2]),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$input.CopyTo($output)}finally{if($output){$output.Dispose()};$input.Dispose()}
  if((Get-KmcSha256 (Join-Path $run $row[2]))-cne$row[1]-or(Get-KmcSha256 $inputPath)-cne$row[1]){throw 'Historical provenance changed during locked copy.'}
 }
}
function Assert-KmcOwnedSchema1ColdSource($Request,$Source,$Artifact){
 $Request=[pscustomobject]$Request;Assert-KmcOwnedSchema1Request $Request
 $profile=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId)
 foreach($row in @(@('owned-schema1-checkpoint.json',$Request.ownedPairs.checkpointSourceArtifactSha256,16MB),@('owned-schema1-derivation.json',$Request.schema1DerivationSha256,256KB))){
  $path=Join-Path $profile $row[0];Assert-KmcNotReparsePoint $path 'owned historical provenance';Assert-KmcNotHardLink $path 'owned historical provenance';Assert-KmcManifestJsonMembersUnique $path 'owned historical provenance'
  $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt$row[2]-or(Get-KmcSha256 $path)-cne$row[1]){throw 'Historical staged provenance changed during native load/resave.'}
 }
 $receipt=Read-KmcOwnedSchema1PinnedJson (Join-Path $profile 'owned-schema1-derivation.json') $Request.schema1DerivationSha256 256KB;Assert-KmcOwnedSchema1Derivation $receipt $Request $Source
 Assert-KmcBootstrapInventoryString $Artifact.observations.ownedSchema1.originalNativeArchiveSha256 $Source.descriptor.sha256
 $checkpoint=Read-KmcOwnedSchema1PinnedJson $Source.checkpointArtifactPath $Source.checkpointArtifactSha256 16MB
 if((ConvertTo-KmcChunk7Canonical $Artifact.observations.ownedSchema1.sourceField)-cne(ConvertTo-KmcChunk7Canonical $checkpoint.observations.ownedSchema1.fields[1])){throw 'Native full-field source allocations differ from the immutable original checkpoint.'}
 $input=Join-Path (Join-Path $profile 'Saved Games') $Request.persistenceLoad.fileName;Assert-KmcNotReparsePoint $input 'owned historical input';Assert-KmcNotHardLink $input 'owned historical input'
 if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length){throw 'Immutable historical derivative changed during native restoration.'}
}
