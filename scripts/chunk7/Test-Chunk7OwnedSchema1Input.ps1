[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
# Actual owned schema-1 input, locked CREATE_NEW copy and cold-source qualification
# over a synthetic one-owner native archive inside a private virtual lab root.
# Only the completed-run qualifier, lock owner and process checks are facades.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedSchema1EvidenceFixtures.json'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1')
$realLab=Get-KmcLabRoot
if(-not$OutputDirectory){$OutputDirectory=Join-Path $realLab ('analysis-cache/chunk7-continuation/owned-schema1-input-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory (Join-Path $realLab 'analysis-cache') 'generated historical input tests'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh generated output required.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$script:virtualLab=Join-Path $OutputDirectory 'lab';[void][IO.Directory]::CreateDirectory($script:virtualLab)
# Facades: the virtual lab root, the completed-run qualifier result, lock ownership and process absence.
function Get-KmcLabRoot{$script:virtualLab}
$script:qualifierResult=$null
function Get-KmcChunk7OwnedPairsSource($Selection,[AllowNull()][string]$PersistenceCase,$Fixture,$Candidate,$AreaTarget=$null){if($PersistenceCase-cne'owned-schema1-resave-manual'){throw 'Facade qualifier selected for another case.'};$script:qualifierResult}
function Assert-KmcRuntimeLockOwner($Lock){if($null-eq$Lock-or[string]::IsNullOrEmpty($Lock.RunId)){throw 'Facade lock requires a run.'};$true}
function Assert-KmcNoGameProcesses{}
$timer=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Copy-Case($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
function Check([string]$Name,[scriptblock]$Body,[bool]$Reject=$false,[string]$Pattern=''){
 $message=$null;$pass=$false
 try{&$Body;$pass=-not$Reject}catch{$message=$_.Exception.Message;$pass=$Reject-and(-not$Pattern-or$message-match$Pattern)}
 $results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Reject;error=$message});if(-not$pass){Write-Output ('FAIL '+$Name+': '+$message)}
}
function Require([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function New-SyntheticArchive([string]$Path,[hashtable]$Members){
 Add-Type -AssemblyName System.IO.Compression;Add-Type -AssemblyName System.IO.Compression.FileSystem
 $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$zip=$null
 try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
  foreach($name in @($Members.Keys|Sort-Object)){$entry=$zip.CreateEntry($name);$inner=$entry.Open();try{$bytes=$Members[$name];$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}}
 }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
}
function Read-Members([string]$Path){$s=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);try{Get-KmcOwnedSchema1Members $s}finally{$s.Dispose()}}
$fixtures=(Read-KmcJson $FixturePath).fixtures;$sourceCase=$fixtures[0];$coldCase=$fixtures[1]
$archive=$sourceCase.artifact.observations.nativeWrite.archive
$selection=Copy-Case $coldCase.request.ownedPairs
$checkpointRun=$selection.checkpointSourceRunId
$savedGames=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$checkpointRun+'/Saved Games');[void][IO.Directory]::CreateDirectory($savedGames)
$leaf=$coldCase.request.persistenceLoad.fileName;$zipPath=Join-Path $savedGames $leaf
$utf8=[Text.UTF8Encoding]::new($false)
$header=[ordered]@{Name='KMC_C7_WORLD';Type='Manual';GameId=$archive.CampaignId;GameName=$coldCase.request.persistenceLoad.gameName;Area=$archive.AreaId;CompatibilityVersion=1;IsAutoLevelupSave=$false}
$random=[byte[]]::new(4096);[Random]::new(7).NextBytes($random)
New-SyntheticArchive $zipPath @{'header.json'=$utf8.GetBytes(($header|ConvertTo-Json -Compress));'kmc-mounted-state'=$utf8.GetBytes(($archive|ConvertTo-Json -Depth 16 -Compress));'player.json'=$utf8.GetBytes('{"synthetic":"player","bytes":1}');'areas/area-a.json'=$random}
$zipFile=Get-Item -LiteralPath $zipPath
$descriptor=[pscustomobject][ordered]@{internalName='KMC_C7_WORLD';fileName=$leaf;sha256=(Get-KmcSha256 $zipPath);length=[long]$zipFile.Length;lastWriteTimeUtcTicks=[long]$zipFile.LastWriteTimeUtc.Ticks;gameId=$archive.CampaignId;gameName=$coldCase.request.persistenceLoad.gameName;area=$archive.AreaId}
$checkpointPath=Join-Path $OutputDirectory 'chunk7-persistence-evidence.json';[IO.File]::WriteAllText($checkpointPath,($sourceCase.artifact|ConvertTo-Json -Depth 90),$utf8)
$twoOwnerPath=Join-Path $OutputDirectory 'owned-two-pair-source.json';[IO.File]::WriteAllText($twoOwnerPath,'{"synthetic":true,"role":"two-owner source receipt"}',$utf8)
$selection.checkpointSourceArtifactSha256=Get-KmcSha256 $checkpointPath
$source=[pscustomobject][ordered]@{path=$zipPath;descriptor=$descriptor;artifactPath=$twoOwnerPath;artifactSha256=(Get-KmcSha256 $twoOwnerPath);checkpointArtifactPath=$checkpointPath;checkpointArtifactSha256=$selection.checkpointSourceArtifactSha256;sourceRunId=$selection.sourceRunId}
$fixture=Copy-Case $coldCase.request.fixture;$candidate=[pscustomobject]@{commit=$coldCase.request.commit;dllSha256=$coldCase.request.dllSha256;dllMvid=$coldCase.request.dllMvid}
$lock=[pscustomobject]@{RunId='synthetic-owned-schema1-input';Token=('1'*64)}
$script:qualifierResult=$source
$derivative=$null
Check 'actual input creates a metadata-only derivative preserving every other member' {
 $script:derivative=New-KmcOwnedSchema1Input $source $selection $fixture $candidate $lock;$d=$script:derivative
 Require (Test-Path -LiteralPath $d.path) 'Derivative archive absent.'
 Require ($d.descriptor.fileName-ceq$leaf-and$d.descriptor.sha256-cne$descriptor.sha256-and$d.descriptor.gameId-ceq$descriptor.gameId-and$d.descriptor.area-ceq$descriptor.area) 'Derivative descriptor differs from the metadata-only contract.'
 Require ($d.path.StartsWith((Join-Path $script:virtualLab 'analysis-cache\chunk7-owned-schema1-fixtures'),[StringComparison]::OrdinalIgnoreCase)) 'Derivative escaped its owned parent.'
 $before=Read-Members $zipPath;$after=Read-Members $d.path
 Assert-KmcOwnedSchema1MemberPreservation $before.members $after.members
 foreach($row in $before.members){$match=@($after.members|Where-Object name -CEQ $row.name)[0];if($row.name-cne'kmc-mounted-state'){Require ($match.sha256-ceq$row.sha256-and$match.length-eq$row.length) ('Member changed: '+$row.name)}}
 $request=[pscustomobject]@{scenario='chunk7-owned-two-pair-campaign';runId=$lock.RunId;ownedPairs=$selection;persistenceCase='owned-schema1-resave-manual';persistenceLoad=$descriptor;fixture=$fixture}
 $expected=ConvertTo-KmcOwnedSchema1Metadata $archive $request
 Require ((ConvertTo-KmcChunk7Canonical $after.metadata)-ceq(ConvertTo-KmcChunk7Canonical $expected)) 'Derivative metadata is not the exact one-owner projection.'
 Require ($after.metadata.SchemaVersion-eq1-and$after.metadata.Mounted-eq$true-and@($after.metadata.Slots).Count-eq@($archive.Slots).Count) 'Historical projection lost slots or writer identity.'
 Require ((Get-KmcSha256 $zipPath)-ceq$descriptor.sha256-and(Get-Item -LiteralPath $zipPath).LastWriteTimeUtc.Ticks-eq$descriptor.lastWriteTimeUtcTicks) 'Native source changed.'
 Require ($d.receiptSha256-ceq(Get-KmcSha256 $d.receiptPath)) 'Receipt hash differs.'
}
$request=Copy-Case $coldCase.request;$request.runId=$lock.RunId;$request.ownedPairs=$selection
if($null-ne$derivative){$request.persistenceLoad=$derivative.descriptor;$request.schema1DerivationSha256=$derivative.receiptSha256}
Check 'derivation receipt satisfies the cold reader contract' {
 Require ($null-ne$derivative) 'No derivative.'
 $receipt=Read-KmcOwnedSchema1PinnedJson $derivative.receiptPath $derivative.receiptSha256 256KB
 Assert-KmcOwnedSchema1Derivation $receipt $request $source
 Require ($receipt.nativeRestorationProven-eq$false-and$receipt.contract-ceq'owned-schema1-metadata-copy-v1') 'Receipt claims restoration or another contract.'
}
Check 'pinned receipt reader refuses a changed hash' {Require ($null-ne$derivative) 'No derivative.';[void](Read-KmcOwnedSchema1PinnedJson $derivative.receiptPath ('e'*64) 256KB)} $true 'unpinned|changed'
Check 'input refuses a source that changed after full qualification' {
 $changed=Copy-Case $source;$changed.descriptor.sha256='e'*64;$script:qualifierResult=$changed
 try{[void](New-KmcOwnedSchema1Input $source $selection $fixture $candidate $lock)}finally{$script:qualifierResult=$source}
} $true 'changed after full qualification'
Check 'input refuses qualified source bytes that differ from the descriptor' {
 $changed=Copy-Case $source;$changed.descriptor.sha256='f'*64;$script:qualifierResult=$changed
 try{[void](New-KmcOwnedSchema1Input $changed $selection $fixture $candidate $lock)}finally{$script:qualifierResult=$source}
} $true 'bytes or metadata changed'
Check 'input refuses a source archive without mounted metadata' {
 $bare=Join-Path $OutputDirectory 'bare.zks';New-SyntheticArchive $bare @{'header.json'=$utf8.GetBytes('{"Name":"x"}');'player.json'=$utf8.GetBytes('{}')}
 [void](Read-Members $bare)
} $true 'absent'
$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$lock.RunId);$profileSaves=Join-Path $profile 'Saved Games'
[void][IO.Directory]::CreateDirectory($profileSaves)
$owner=[ordered]@{runId=$lock.RunId;transactionToken=$lock.Token;scenario='chunk7-owned-two-pair-campaign';persistenceCase='owned-schema1-resave-manual';sourceSha256=$(if($null-ne$derivative){$derivative.descriptor.sha256}else{'0'*64});saveRoot=$profileSaves}
[IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),$utf8)
$copied=@('owned-two-pair-source.json','owned-schema1-checkpoint.json','owned-schema1-derivation.json')
Check 'locked copy stages exact provenance with CREATE_NEW' {
 Require ($null-ne$derivative) 'No derivative.'
 Copy-KmcOwnedSchema1Inputs $source $derivative $profile $lock $request
 $expectedHashes=@($source.artifactSha256,$source.checkpointArtifactSha256,$derivative.receiptSha256)
 for($i=0;$i-lt3;$i++){Require ((Get-KmcSha256 (Join-Path $profile $copied[$i]))-ceq$expectedHashes[$i]) ('Staged provenance differs: '+$copied[$i])}
}
Check 'locked copy refuses to overwrite already staged provenance' {
 Require ($null-ne$derivative) 'No derivative.'
 $before=@($copied|ForEach-Object{Get-KmcSha256 (Join-Path $profile $_)})
 try{Copy-KmcOwnedSchema1Inputs $source $derivative $profile $lock $request}
 finally{$after=@($copied|ForEach-Object{Get-KmcSha256 (Join-Path $profile $_)});if(($before-join',')-cne($after-join',')){throw 'Refused CREATE_NEW copy changed staged bytes.'}}
} $true 'exists'
Check 'locked copy refuses a profile owned by another transaction' {
 Require ($null-ne$derivative) 'No derivative.'
 $foreign=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$lock.RunId+'-foreign');[void][IO.Directory]::CreateDirectory((Join-Path $foreign 'Saved Games'))
 $other=[ordered]@{}+$owner;$other.transactionToken='2'*64;[IO.File]::WriteAllText((Join-Path $foreign 'owner.json'),($other|ConvertTo-Json),$utf8)
 Copy-KmcOwnedSchema1Inputs $source $derivative $foreign $lock $request
} $true
$stagedInput=Join-Path $profileSaves $leaf
if($null-ne$derivative){Copy-Item -LiteralPath $derivative.path -Destination $stagedInput;[IO.File]::SetLastWriteTimeUtc($stagedInput,[IO.File]::GetLastWriteTimeUtc($derivative.path))}
$artifact=Copy-Case $coldCase.artifact
$artifact.observations.ownedSchema1.originalNativeArchiveSha256=$descriptor.sha256
$artifact.observations.ownedSchema1.sourceField=$sourceCase.artifact.observations.ownedSchema1.fields[1]
Check 'cold source qualification accepts the exact staged derivative and provenance' {Require ($null-ne$derivative) 'No derivative.';Assert-KmcOwnedSchema1ColdSource $request $source $artifact}
Check 'cold source qualification refuses a tampered staged derivation receipt' {
 Require ($null-ne$derivative) 'No derivative.';$path=Join-Path $profile 'owned-schema1-derivation.json';$original=[IO.File]::ReadAllBytes($path)
 try{[IO.File]::WriteAllText($path,'{"schemaVersion":1,"contract":"owned-schema1-metadata-copy-v1","tampered":true}',$utf8);Assert-KmcOwnedSchema1ColdSource $request $source $artifact}
 finally{[IO.File]::WriteAllBytes($path,$original)}
} $true 'provenance changed'
Check 'cold source qualification refuses a changed staged input archive' {
 Require ($null-ne$derivative) 'No derivative.';$original=[IO.File]::ReadAllBytes($stagedInput)
 try{[IO.File]::WriteAllBytes($stagedInput,($original+[byte[]](1,2,3)));Assert-KmcOwnedSchema1ColdSource $request $source $artifact}
 finally{[IO.File]::WriteAllBytes($stagedInput,$original)}
} $true 'derivative changed'
Check 'cold source qualification refuses a foreign original archive claim' {
 Require ($null-ne$derivative) 'No derivative.';$foreignArtifact=Copy-Case $artifact;$foreignArtifact.observations.ownedSchema1.originalNativeArchiveSha256='d'*64
 Assert-KmcOwnedSchema1ColdSource $request $source $foreignArtifact
} $true
Check 'cold source qualification refuses a changed full-field source allocation' {
 Require ($null-ne$derivative) 'No derivative.';$changedField=Copy-Case $artifact;$changedField.observations.ownedSchema1.sourceField.actors[0].native.Move=1
 Assert-KmcOwnedSchema1ColdSource $request $source $changedField
} $true 'allocations differ'
$timer.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;elapsedSeconds=$timer.Elapsed.TotalSeconds;fixtureSha256=(Get-FileHash -LiteralPath $FixturePath).Hash.ToLowerInvariant();syntheticSourceSha256=$descriptor.sha256;derivativeSha256=$(if($null-ne$derivative){$derivative.descriptor.sha256}else{$null});virtualLab=$script:virtualLab;nativeInvoked=$false;installedArchivesRead=0;facades=@('Get-KmcLabRoot','Get-KmcChunk7OwnedPairsSource','Assert-KmcRuntimeLockOwner','Assert-KmcNoGameProcesses');results=@($results)}|ConvertTo-Json -Depth 12),$utf8)
Write-Output ('OWNED SCHEMA1 INPUT PASS='+$passed+' FAIL='+$failed+'; synthetic one-owner archive in a virtual lab root; no native, installed-archive or candidate claim.')
if($failed-ne0){throw 'Owned schema-1 input/copy regression failed; preserve outputs.'}
