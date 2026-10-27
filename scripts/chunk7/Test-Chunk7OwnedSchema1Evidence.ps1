[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedSchema1EvidenceFixtures.json'}
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1')
$lab=Get-KmcLabRoot
if(-not$OutputDirectory){$OutputDirectory=Join-Path $lab ('analysis-cache/chunk7-continuation/owned-schema1-checks-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory (Join-Path $lab 'analysis-cache') 'generated historical fixture tests'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh generated output required.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$timer=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Copy-Case($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
function Check([string]$Name,[scriptblock]$Body,[bool]$Reject=$false,[string]$Pattern=''){
 $message=$null;$pass=$false
 try{&$Body;$pass=-not$Reject}catch{$message=$_.Exception.Message;$pass=$Reject-and(-not$Pattern-or$message-match$Pattern)}
 $results.Add([pscustomobject]@{name=$Name;pass=$pass;expectedRefusal=$Reject;error=$message});if(-not$pass){Write-Output ('FAIL '+$Name+': '+$message)}
}
function Require([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
$fixtures=(Read-KmcJson $FixturePath).fixtures;$source=$fixtures[0];$cold=$fixtures[1]
foreach($f in $fixtures){Check ('complete generated reader '+$f.name) {Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}}
foreach($f in $fixtures){Check ('six-capacity preserves generated historical reader '+$f.name) {$v=Copy-Case $f.artifact;$v.observations.nativeCapacity=6;Assert-KmcChunk7PersistenceObservation $f.request $v 'PASS'}}
function Mutate([string]$Name,[int]$Index,[scriptblock]$Change,[string]$Pattern=''){$v=Copy-Case $fixtures[$Index];&$Change $v;Check $Name {Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'} $true $Pattern}
Mutate 'source second mount is not historical one-owner proof' 0 {$args[0].artifact.observations.inputs+=@($args[0].artifact.observations.inputs[0])} 'exactly one'
Mutate 'source cannot discard second current owner' 0 {$args[0].artifact.observations.nativeWrite.archive.Pairs+=@($args[0].artifact.observations.nativeWrite.archive.Pairs[0])} 'another owner'
Mutate 'source cannot discard retained participation' 0 {$args[0].artifact.observations.nativeWrite.archive.Participations=@([pscustomobject]@{pending=$true})} 'participation'
Mutate 'source cannot discard combat' 0 {$args[0].artifact.observations.nativeWrite.archive.Combat=[pscustomobject]@{Round=1}} 'combat'
Mutate 'source cannot discard pair debt' 0 {$args[0].artifact.observations.nativeWrite.archive.Pairs[0].Rider.Standard=0.5} 'debt'
Mutate 'historical Charge control must refuse' 0 {$args[0].artifact.observations.nativeWrite.archive.Slots[2].Kind=5}
Mutate 'source unrelated resource change refused' 0 {$args[0].artifact.observations.ownedSchema1.fields[1].actors[2].native.ReactionsRemaining++} 'allocations'
Mutate 'source unrelated prepared change refused' 0 {$args[0].artifact.observations.ownedSchema1.fields[1].actors[2].prepared=$true} 'allocations'
Mutate 'cold omitted field actor refused' 1 {$args[0].artifact.observations.ownedSchema1.fields[0].actors=@($args[0].artifact.observations.ownedSchema1.fields[0].actors|Select-Object -Skip 1)} 'roster'
Mutate 'cold queued command refused' 1 {$args[0].artifact.observations.ownedSchema1.fields[0].actors[3].queuedCommands=1}
Mutate 'cold prepared state manufactured refused' 1 {$args[0].artifact.observations.ownedSchema1.fields[1].actors[3].prepared=$true} 'allocations'
Mutate 'cold derivative is explicitly synthetic' 1 {$args[0].artifact.observations.ownedSchema1.syntheticMetadata=$false}
Mutate 'cold receipt pin changed refused' 1 {$args[0].artifact.observations.schema1Migration.derivationReceiptSha256='b'*64}
Mutate 'cold original native source cannot equal derivative' 1 {$args[0].artifact.observations.ownedSchema1.originalNativeArchiveSha256=$args[0].request.persistenceLoad.sha256}
Mutate 'cold controls on unrelated actor may not disappear' 1 {$args[0].artifact.observations.nativeWrite.archive.Slots=@($args[0].artifact.observations.nativeWrite.archive.Slots|Select-Object -First 2)} 'migrated graph'
Mutate 'cold managed controls cannot be invented' 1 {$args[0].artifact.observations.schema1Migration.nativeInputMetadata.Slots[2].Kind=4} 'decoder changed'
Mutate 'cold native Prepare callback refused' 1 {$args[0].artifact.observations.coldRestoration.events+=@([pscustomobject]@{kind='prepare-before'})}
Mutate 'cold missing native restore callback refused' 1 {$args[0].artifact.observations.coldRestoration.events=@($args[0].artifact.observations.coldRestoration.events|Select-Object -Skip 1)}
Mutate 'cold replayed Mount refused' 1 {$args[0].artifact.observations.inputs=@([pscustomobject]@{clicked=$true})} 'replayed Mount'
Mutate 'cold exact worker settlement required' 1 {$args[0].artifact.observations.ownedOutputCompletion.nativeWorkerSettled=$false}
Mutate 'cold callback count exact' 1 {$args[0].artifact.observations.ownedOutputCompletion.callbackCount=2}
Mutate 'cold new Manual number exact' 1 {$args[0].artifact.observations.ownedOutputAdmission.number++}
Mutate 'inherited Horse facts cannot be removed' 1 {$args[0].artifact.observations.inheritedCleanup.ownedFactRemovalCount=1}
Mutate 'cold cleanup cannot retain owner' 1 {$args[0].artifact.observations.cleanup.world.owners=@($args[0].artifact.observations.snapshots[0].owners[0])}
foreach($key in @('native','prepared','queuedCommands')){Mutate ('strict field token '+$key) 0 {$row=$args[0].artifact.observations.ownedSchema1.fields[0].actors[0];$row.$key=@($row.$key)}}
Check 'complete representable unrelated slot array projected without loss' {$m=ConvertTo-KmcOwnedSchema1Metadata $source.artifact.observations.nativeWrite.archive $source.request;Require ((ConvertTo-KmcChunk7Canonical $m.Slots)-ceq(ConvertTo-KmcChunk7Canonical $source.artifact.observations.nativeWrite.archive.Slots)) 'Slots changed';Require ($m.Slots[2].ActorId-ceq$source.request.ownedPairs.riderBId-and$m.Slots[2].Kind-eq1) 'Unmounted rider slot lost'}
Check 'projection does not mutate current source or enable Writer1' {$a=Copy-Case $source.artifact.observations.nativeWrite.archive;$text=ConvertTo-KmcChunk7Canonical $a;[void](ConvertTo-KmcOwnedSchema1Metadata $a $source.request);Require ((ConvertTo-KmcChunk7Canonical $a)-ceq$text) 'Source object changed'}
foreach($kind in 1..4){Check ('unrelated representable kind '+$kind) {$a=Copy-Case $source.artifact.observations.nativeWrite.archive;$a.Slots[2].Kind=$kind;$m=ConvertTo-KmcOwnedSchema1Metadata $a $source.request;Require ($m.Slots[2].Kind-eq$kind) 'Representable slot kind lost'}}
foreach($direction in @('source-as-cold','cold-as-source','Working-alias','missing-receipt','extra-source-receipt')){Check ('direction refusal '+$direction) {$v=Copy-Case $(if($direction-cin@('source-as-cold','extra-source-receipt')){$source}else{$cold});switch($direction){'source-as-cold'{$v.request.ownedPairs.case='persistence-load'}'cold-as-source'{$v.request.ownedPairs.case='persistence-save'}'Working-alias'{$v.request.scenario='chunk7-persistence-load'}'missing-receipt'{$v.request.PSObject.Properties.Remove('schema1DerivationSha256')}'extra-source-receipt'{$v.request|Add-Member schema1DerivationSha256 ('a'*64)}};Assert-KmcOwnedSchema1Request $v.request} $true}
# Generated ZIP tests execute only the actual bounded member reader and exact
# preservation predicate. No qualified source, installed archive or native path
# is substituted into the public derivation entry point.
Add-Type -AssemblyName System.IO.Compression
function New-GeneratedZip([string]$Name,[object[]]$Entries){
 $path=Join-Path $OutputDirectory $Name;$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$zip=$null
 try{$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false);foreach($row in $Entries){$entry=$zip.CreateEntry($row.name);$inner=$entry.Open();try{$bytes=[Text.UTF8Encoding]::new($false,$true).GetBytes($row.text);$inner.Write($bytes,0,$bytes.Length)}finally{$inner.Dispose()}}}finally{if($zip){$zip.Dispose()};$stream.Dispose()};return $path
}
function Read-GeneratedZip([string]$Path){$stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);try{return Get-KmcOwnedSchema1Members $stream}finally{$stream.Dispose()}}
$entries=@([pscustomobject]@{name='header.json';text='{"generated":true}'},[pscustomobject]@{name='party.json';text='{"inert":"native bytes, never deserialized"}'},[pscustomobject]@{name='kmc-mounted-state';text=($source.artifact.observations.nativeWrite.archive|ConvertTo-Json -Depth 32 -Compress)})
$zipPath=New-GeneratedZip 'valid-generated.zks' $entries
Check 'actual bounded archive member reader preserves all generated native bytes' {$members=Read-GeneratedZip $zipPath;Require ($members.members.Count-eq3) 'Member omitted';Require ((ConvertTo-KmcChunk7Canonical $members.metadata)-ceq(ConvertTo-KmcChunk7Canonical $source.artifact.observations.nativeWrite.archive)) 'Metadata differs'}
foreach($bad in @('duplicate','case-collision','traversal','rooted','backslash','missing-header','duplicate-json','future-json')){
 Check ('generated archive refuses '+$bad) {$rows=Copy-Case $entries;switch($bad){'duplicate'{$rows+=@($rows[0])}'case-collision'{$rows+=@([pscustomobject]@{name='HEADER.json';text='{}'})}'traversal'{$rows[1].name='../party.json'}'rooted'{$rows[1].name='/party.json'}'backslash'{$rows[1].name='area\party.json'}'missing-header'{$rows[0].name='other.json'}'duplicate-json'{$rows[2].text='{"SchemaVersion":3,"SchemaVersion":3}'}'future-json'{$rows[2].text='{"SchemaVersion":99}'}};$p=New-GeneratedZip ($bad+'.zks') $rows;$v=Read-GeneratedZip $p;if($bad-ceq'future-json'){[void](ConvertTo-KmcOwnedSchema1Metadata $v.metadata $source.request)}} $true
}
$before=(Read-GeneratedZip $zipPath).members
Check 'complete metadata-only member set accepted' {$after=Copy-Case $before;@($after|Where-Object name -CEQ 'kmc-mounted-state')[0].sha256='b'*64;Assert-KmcOwnedSchema1MemberPreservation $before $after}
foreach($fault in @('missing-native','extra-native','native-content','native-length')){Check ('member preservation refuses '+$fault) {$after=Copy-Case $before;switch($fault){'missing-native'{$after=@($after|Where-Object name -CNE 'party.json')}'extra-native'{$after+=@([pscustomobject]@{name='extra';length=0;sha256='b'*64})}'native-content'{@($after|Where-Object name -CEQ 'party.json')[0].sha256='b'*64}'native-length'{@($after|Where-Object name -CEQ 'party.json')[0].length++}};Assert-KmcOwnedSchema1MemberPreservation $before $after} $true}
$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
$receipt=[ordered]@{schemaVersion=1;status=$(if($failed-eq0){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;elapsedSeconds=$timer.Elapsed.TotalSeconds;fixtureSha256=(Get-KmcSha256 $FixturePath);results=$results.ToArray();nativeInvoked=$false;actualArchivesRead=0;generatedArchivesOnly=$true;limits='Generated JSON reader/member fixtures; no native qualification and no public full-source qualification/copy dispatch executed.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 16),[Text.UTF8Encoding]::new($false))
Write-Output ('TOTAL PASS='+$passed+' FAIL='+$failed);if($failed-ne0){exit 1}
