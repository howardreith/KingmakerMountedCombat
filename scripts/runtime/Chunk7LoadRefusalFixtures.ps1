Set-StrictMode -Version Latest

# Generated, project-owned metadata-only probes. This never changes the original
# native Manual, grants another load, or proves a game refusal by itself.
function Get-KmcChunk7LoadRefusalMembers([IO.Stream]$Stream){
 Add-Type -AssemblyName System.IO.Compression
 $Stream.Position=0;$zip=$null
 try{
  $zip=[IO.Compression.ZipArchive]::new($Stream,[IO.Compression.ZipArchiveMode]::Read,$true)
  if($zip.Entries.Count-lt2-or$zip.Entries.Count-gt1024){throw 'Refusal archive member count is unbounded.'}
  $names=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase);$rows=[Collections.Generic.List[object]]::new();[long]$total=0;$metadataText=$null
  foreach($entry in $zip.Entries){
   $name=$entry.FullName
   if([string]::IsNullOrEmpty($name)-or$name.Length-gt256-or$name-match'[:\\\x00-\x1f\x7f]'-or$name.StartsWith('/')-or-not$names.Add($name)){throw 'Refusal archive member name is unsafe or ambiguous.'}
   if(@($name.TrimEnd('/').Split('/')|Where-Object{$_-cin@('','..','.')}).Count-ne0){throw 'Refusal member path traverses or aliases.'}
   $total+=$entry.Length;if($entry.Length-lt0-or$entry.Length-gt256MB-or$total-gt512MB){throw 'Refusal expanded archive bytes exceed their bound.'}
   $inner=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
   try{$digest=([BitConverter]::ToString($sha.ComputeHash($inner))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose();$inner.Dispose()}
   $rows.Add([pscustomobject]@{name=$name;length=[long]$entry.Length;sha256=$digest})
   if($name-ceq'kmc-mounted-state'){
    if($entry.Length-le0-or$entry.Length-gt128KB){throw 'Refusal metadata is empty or oversized.'}
    $reader=[IO.StreamReader]::new($entry.Open(),[Text.UTF8Encoding]::new($false,$true),$false)
    try{$metadataText=$reader.ReadToEnd()}finally{$reader.Dispose()}
   }
  }
  if(@($rows|Where-Object name -CEQ 'header.json').Count-ne1-or$null-eq$metadataText){throw 'Exact refusal native header or metadata member is absent.'}
  [pscustomobject]@{members=$rows.ToArray();metadataText=$metadataText}
 }finally{if($zip){$zip.Dispose()}}
}

function Get-KmcChunk7LoadRefusalMutation($Metadata,[string]$Checkpoint){
 if($Metadata.SchemaVersion-isnot[int]-and$Metadata.SchemaVersion-isnot[long]){throw 'Refusal source schema must be an integer.'}
 if($Metadata.SchemaVersion-ne3){throw 'Refusal probes derive only from the actual current native archive.'}
 if($Checkpoint-ceq'multi-malformed-refusal-manual'){return [pscustomobject]@{mutation='truncated-json';text='{"SchemaVersion":3,'}}
 if($Checkpoint-cne'multi-future-refusal-manual'){throw 'Unknown refusal metadata mutation.'}
 $copy=$Metadata|ConvertTo-Json -Depth 32|ConvertFrom-Json;$copy.SchemaVersion=99
 [pscustomobject]@{mutation='future-schema-99';text=($copy|ConvertTo-Json -Depth 32 -Compress)}
}

function New-KmcChunk7LoadRefusalInput($Source,$Selection,[string]$Checkpoint,$Fixture,$Candidate,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 $probe=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$Lock.RunId;ownedPairs=$Selection;persistenceCase=$Checkpoint}
 Assert-KmcChunk7OwnedPairsSelection $probe
 if(-not(Test-KmcChunk7LoadRefusal $probe)){throw 'Only the exact closed refusal selection may derive an alternate.'}
 $fresh=Get-KmcChunk7OwnedPairsSource $Selection $Checkpoint $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $fresh)-cne(ConvertTo-KmcChunk7Canonical $Source)){throw 'Refusal source changed after full qualification.'}
 $path=Assert-KmcChildPath $fresh.path (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Selection.sourceRunId+'/Saved Games')) 'qualified refusal source'
 Assert-KmcNotReparsePoint $path 'qualified refusal source';Assert-KmcNotHardLink $path 'qualified refusal source'
 $file=Get-Item -LiteralPath $path
 if($file.Length-le0-or$file.Length-gt256MB-or$file.Length-ne$Source.descriptor.length-or$file.LastWriteTimeUtc.Ticks-ne$Source.descriptor.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$Source.descriptor.sha256){throw 'Refusal source bytes or identity changed.'}
 $parent=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-load-refusal-fixtures';[void][IO.Directory]::CreateDirectory($parent);Assert-KmcDirectoryTreeCloneable $parent 'refusal derivative parent'
 $owned=Assert-KmcChildPath (Join-Path $parent ([Guid]::NewGuid().ToString('N'))) $parent 'fresh refusal derivative'
 if(Test-Path -LiteralPath $owned){throw 'Refusal derivative ownership is ambiguous.'};[void][IO.Directory]::CreateDirectory($owned)
 $destination=Join-Path $owned 'Manual_812_KMC_C7_SEED_REFUSAL.zks';$input=[IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{
  $before=Get-KmcChunk7LoadRefusalMembers $input
  Assert-KmcJsonObjectMembersUnique -Json $before.metadataText -Description 'refusal source current metadata'
  $metadata=$before.metadataText|ConvertFrom-Json
  $producer=Read-KmcOwnedSchema1PinnedJson $Source.artifactPath $Selection.sourceArtifactSha256 8MB
  if((ConvertTo-KmcChunk7Canonical $metadata)-cne(ConvertTo-KmcChunk7Canonical $producer.observations.nativeWrite.archive)){throw 'Refusal source metadata differs from its accepted producer readback.'}
  $mutation=Get-KmcChunk7LoadRefusalMutation $metadata $Checkpoint;$bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($mutation.text)
  if($bytes.Length-le0-or$bytes.Length-gt128KB){throw 'Refusal mutation exceeds the unchanged decoder bound.'}
  $output=[IO.File]::Open($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$input.Position=0;$input.CopyTo($output)}finally{$output.Dispose()}
  if((Get-KmcSha256 $destination)-cne$Source.descriptor.sha256){throw 'Refusal source changed during CREATE_NEW copy.'}
  $stream=[IO.File]::Open($destination,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$zip=$null
  try{
   $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Update,$false)
   $entries=@($zip.Entries|Where-Object FullName -CEQ 'kmc-mounted-state');if($entries.Count-ne1){throw 'Refusal metadata member lost uniqueness.'}
   $entries[0].Delete();$entry=$zip.CreateEntry('kmc-mounted-state');$inner=$entry.Open();try{$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}
  }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
  $check=[IO.File]::Open($destination,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  try{$after=Get-KmcChunk7LoadRefusalMembers $check}finally{$check.Dispose()}
  Assert-KmcOwnedSchema1MemberPreservation $before.members $after.members
  if($after.metadataText-cne$mutation.text-or(Get-KmcSha256 $path)-cne$Source.descriptor.sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Refusal mutation or immutable source changed.'}
  $out=Get-Item -LiteralPath $destination
  if($out.Length-le0-or$out.Length-gt256MB){throw 'Refusal derivative exceeds its archive byte bound.'}
  $descriptor=[pscustomobject][ordered]@{internalName=$Source.descriptor.internalName;fileName=$out.Name;sha256=(Get-KmcSha256 $destination);length=[long]$out.Length;lastWriteTimeUtcTicks=[long]$out.LastWriteTimeUtc.Ticks;gameId=$Source.descriptor.gameId;gameName=$Source.descriptor.gameName;area=$Source.descriptor.area}
  $meta=@($after.members|Where-Object name -CEQ 'kmc-mounted-state')[0]
  $receipt=[pscustomobject][ordered]@{schemaVersion=1;contract='owner-seed-load-refusal-metadata-copy-v1';checkpoint=$Checkpoint;sourceRunId=$Selection.sourceRunId;sourceArtifactSha256=$Selection.sourceArtifactSha256;sourceDescriptor=$Source.descriptor;descriptor=$descriptor;mutation=$mutation.mutation;metadataSha256=$meta.sha256;metadataLength=$meta.length;nativeMembersVerified=($after.members.Count-1);sourceMembers=$before.members;members=$after.members;fixtureSetupOnly=$true;nativeRestorationProven=$false}
  $receiptBytes=[Text.UTF8Encoding]::new($false,$true).GetBytes(($receipt|ConvertTo-Json -Depth 20));if($receiptBytes.Length-gt256KB){throw 'Refusal derivative receipt is oversized.'}
  $receiptPath=Join-Path $owned 'owner-seed-load-refusal-derivation.json';$receiptStream=[IO.File]::Open($receiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$receiptStream.Write($receiptBytes,0,$receiptBytes.Length)}finally{$receiptStream.Dispose()}
  [pscustomobject]@{path=$destination;descriptor=$descriptor;receiptPath=$receiptPath;receiptSha256=(Get-KmcSha256 $receiptPath)}
 }finally{$input.Dispose()}
}

function Copy-KmcChunk7LoadRefusalInputs($Derivative,[string]$ProfileRoot,$Lock,$Request){
 [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
 $Request=[pscustomobject]$Request;Assert-KmcChunk7LoadRefusalDescriptor $Request
 $profile=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'refusal target profile';Assert-KmcDirectoryTreeCloneable $profile 'refusal target profile'
 if([IO.Path]::GetFileName($profile)-cne('persistence-'+$Lock.RunId)-or$Request.runId-cne$Lock.RunId-or$Request.transactionToken-cne$Lock.Token){throw 'Refusal copy does not own its exact locked profile.'}
 if($Derivative.receiptSha256-cne$Request.loadRefusalDerivationSha256-or(ConvertTo-KmcChunk7Canonical $Derivative.descriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceAlternate)){throw 'Refusal copy substituted its alternate or derivation identity.'}
 $sourceFile=Get-Item -LiteralPath $Derivative.path
 if($sourceFile.Length-ne$Request.persistenceAlternate.length-or$sourceFile.LastWriteTimeUtc.Ticks-ne$Request.persistenceAlternate.lastWriteTimeUtcTicks){throw 'Refusal derivative length or timestamp changed before staging.'}
 $receipt=Read-KmcOwnedSchema1PinnedJson $Derivative.receiptPath $Request.loadRefusalDerivationSha256 256KB
 Assert-KmcChunk7LoadRefusalDerivation $receipt $Request
 foreach($pair in @(@($Derivative.path,(Join-Path $profile ('Saved Games/'+$Request.persistenceAlternate.fileName)),$Request.persistenceAlternate.sha256),@($Derivative.receiptPath,(Join-Path $profile 'owner-seed-load-refusal-derivation.json'),$Request.loadRefusalDerivationSha256))){
  Assert-KmcNotReparsePoint $pair[0] 'refusal copied input';Assert-KmcNotHardLink $pair[0] 'refusal copied input'
  $input=[IO.File]::Open($pair[0],[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$output=$null
  try{$output=[IO.File]::Open($pair[1],[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$input.CopyTo($output)}finally{if($output){$output.Dispose()};$input.Dispose()}
  [IO.File]::SetLastWriteTimeUtc($pair[1],[IO.File]::GetLastWriteTimeUtc($pair[0]))
  if((Get-KmcSha256 $pair[0])-cne$pair[2]-or(Get-KmcSha256 $pair[1])-cne$pair[2]){throw 'Refusal copy changed its immutable input bytes.'}
 }
 Assert-KmcChunk7LoadRefusalInputBytes $Request (Join-Path $profile 'Saved Games') (Join-Path $profile 'owner-seed-load-refusal-derivation.json')
}
