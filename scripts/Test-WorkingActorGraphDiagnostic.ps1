[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Split-Path -Parent $PSScriptRoot}
if(-not$PSBoundParameters.ContainsKey('DiagnosticDirectory')){$DiagnosticDirectory=Join-Path $PSScriptRoot 'runtime'}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$analysis=[IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'analysis-cache')).TrimEnd('\')
if([string]::IsNullOrEmpty($OutputDirectory)){$OutputDirectory=Join-Path $analysis ('working-actor-graph-tests-'+[Guid]::NewGuid().ToString('N'))}
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$out.StartsWith($analysis+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){
    throw 'Generated graph tests require a fresh private analysis-cache directory.'
}
[void][IO.Directory]::CreateDirectory($out)
. (Join-Path $DiagnosticDirectory 'WorkingActorGraphDiagnostic.ps1')
$watch=[Diagnostics.Stopwatch]::StartNew();$script:rows=[Collections.Generic.List[object]]::new()
$readerHash=Initialize-KmcWorkingActorGraphReader
$utf8=[Text.UTF8Encoding]::new($false,$true)
function Check([string]$Name,[scriptblock]$Action){
    try{& $Action;$script:rows.Add([pscustomobject]@{name=$Name;status='PASS';error=$null})}
    catch{$script:rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}
}
function Require([bool]$Condition,[string]$Reason){if(-not$Condition){throw $Reason}}
function Refuse([scriptblock]$Action,[string]$Pattern){
    $caught=$null;try{& $Action|Out-Null}catch{$caught=$_.Exception.ToString()}
    if($null-eq$caught-or$caught-notmatch$Pattern){throw "Expected refusal '$Pattern', observed '$caught'"}
}
function Graph([string]$Player,[string]$Party){return [KingmakerMountedCombat.Harness.WorkingActorGraphReader]::Analyze($utf8.GetBytes($Player),$utf8.GetBytes($Party))}
$player='{"m_MainCharacter":{"m_UniqueId":"main-id"}}'
$party='{"m_EntityData":[{"UniqueId":"main-id","Descriptor":{"Blueprint":"uninterpreted"}},{"UniqueId":"other-id"}]}'
function GraphRefuse([string]$Player,[string]$Party,[string]$Pattern){
    $r=Graph $Player $Party;Require ($r.status-ceq'FAIL'-and-not$r.shapeSupported-and$r.refusal-match$Pattern) ('Wrong graph refusal: '+($r|ConvertTo-Json -Compress))
    Require ($null-eq$r.mainReference-and-not$r.nativeLoadProven-and-not$r.restorationAuthorized-and-not$r.eligibilityAuthorized) 'Refusal leaked value/authority'
}
Check 'object-reference unique match reports facts only' { $r=Graph $player $party;Require ($r.status-ceq'PASS'-and$r.comparison-ceq'unique'-and$r.matchingRecordCount-eq1-and$r.entityCount-eq2-and$r.mainReference-ceq'main-id') 'Unique fact differs';Require (-not$r.nativeDeserialization-and-not$r.nativeLoadProven-and-not$r.eligibilityAuthorized-and-not$r.restorationAuthorized-and-not$r.rawBytesRewritten) 'Decoded data granted authority' }
Check 'direct scalar reference supported only as logical data' { $r=Graph '{"m_MainCharacter":"main-id"}' $party;Require ($r.comparison-ceq'unique'-and$r.mainReferenceShape-ceq'string') 'Scalar fact differs' }
Check 'missing ID is a fact, not unsupported format' { $r=Graph '{"m_MainCharacter":"absent"}' $party;Require ($r.status-ceq'PASS'-and$r.comparison-ceq'missing'-and$r.matchingRecordCount-eq0) 'Missing fact differs' }
Check 'duplicate main IDs observed, never resolved arbitrarily' { $r=Graph $player '{"m_EntityData":[{"UniqueId":"main-id"},{"UniqueId":"main-id"}]}';Require ($r.comparison-ceq'duplicate'-and$r.matchingRecordCount-eq2-and$r.duplicateEntityIdCount-eq1-and($r.matchingRecordIndices-join',')-ceq'0,1') 'Duplicate fact differs' }
Check 'empty entity list reports missing' { $r=Graph $player '{"m_EntityData":[]}';Require ($r.comparison-ceq'missing'-and$r.entityCount-eq0) 'Empty list differs' }
Check 'IDs compare ordinally' { $r=Graph $player '{"m_EntityData":[{"UniqueId":"MAIN-ID"}]}';Require ($r.comparison-ceq'missing') 'IDs were coerced' }
Check 'unrelated duplicate ID counted independently' { $r=Graph $player '{"m_EntityData":[{"UniqueId":"other"},{"UniqueId":"other"},{"UniqueId":"main-id"}]}';Require ($r.comparison-ceq'unique'-and$r.duplicateEntityIdCount-eq1) 'Duplicate neighbor differs' }
Check 'native type names remain inert strings' { $r=Graph $player '{"m_EntityData":[{"$type":"No.Such.Native.Type, Missing.Assembly","UniqueId":"main-id"}]}';Require ($r.comparison-ceq'unique'-and-not$r.nativeDeserialization) 'Type metadata was activated' }
Check 'raw bytes and UTF8 BOM hashes preserved' { $p=$utf8.GetBytes($player);$b=[byte[]](@(239,187,191)+@($p));$r=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::Analyze($b,$utf8.GetBytes($party));$s=[Security.Cryptography.SHA256]::Create();try{$h=([BitConverter]::ToString($s.ComputeHash($b))).Replace('-','').ToLowerInvariant()}finally{$s.Dispose()};Require ($r.status-ceq'PASS'-and$r.playerSha256-ceq$h) 'Raw BOM identity changed' }
$mutations=@(
 @('player root array','[]',$party,'unsupported-player-root'),
 @('party root array',$player,'[]','unsupported-party-root'),
 @('missing main field','{}',$party,'unsupported-main-reference-field'),
 @('wrong-case main alias','{"M_MainCharacter":"main-id"}',$party,'unsupported-main-reference-field'),
 @('main singleton array','{"m_MainCharacter":["main-id"]}',$party,'unsupported-main-reference-kind'),
 @('main numeric','{"m_MainCharacter":1}',$party,'unsupported-main-reference-kind'),
 @('main null','{"m_MainCharacter":null}',$party,'unsupported-main-reference-kind'),
 @('main id singleton array','{"m_MainCharacter":{"m_UniqueId":["main-id"]}}',$party,'unsupported-identity-scalar'),
 @('main object ref','{"m_MainCharacter":{"$ref":"1"}}',$party,'unsupported-main-reference-object'),
 @('main alias mixture','{"m_MainCharacter":{"m_UniqueId":"main-id","UniqueId":"main-id"}}',$party,'unsupported-main-reference-object'),
 @('empty ID','{"m_MainCharacter":""}',$party,'unsupported-identity-length'),
 @('control ID','{"m_MainCharacter":"x\u0000y"}',$party,'unsupported-identity-control'),
 @('oversized ID',('{"m_MainCharacter":"'+('x'*257)+'"}'),$party,'unsupported-identity-length'),
 @('duplicate property','{"m_MainCharacter":"main-id","m_MainCharacter":"main-id"}',$party,'duplicate-or-case-ambiguous'),
 @('case collision property','{"m_MainCharacter":"main-id","M_MainCharacter":"main-id"}',$party,'duplicate-or-case-ambiguous'),
 @('nested duplicate property',$player,'{"m_EntityData":[{"UniqueId":"main-id","Descriptor":{"x":1,"X":2}}]}','duplicate-or-case-ambiguous'),
 @('unsupported list wrapper',$player,'{"m_EntityData":{"$values":[{"UniqueId":"main-id"}]}}','unsupported-entity-list'),
 @('missing entity field',$player,'{}','unsupported-entity-list'),
 @('entity scalar',$player,'{"m_EntityData":["main-id"]}','unsupported-entity-record'),
 @('entity reference',$player,'{"m_EntityData":[{"$ref":"1","UniqueId":"main-id"}]}','unsupported-entity-reference'),
 @('entity id array',$player,'{"m_EntityData":[{"UniqueId":["main-id"]}]}','unsupported-identity-scalar'),
 @('entity id numeric',$player,'{"m_EntityData":[{"UniqueId":1}]}','unsupported-identity-scalar'),
 @('missing entity id',$player,'{"m_EntityData":[{}]}','unsupported-identity-scalar'),
 @('malformed JSON','{"m_MainCharacter":',$party,'malformed-'),
 @('trailing document',($player+' {}'),$party,'trailing-json-content'),
 @('trailing array',($player+' []'),$party,'trailing-json-content'),
 @('trailing string',($player+' "extra"'),$party,'trailing-json-content'),
 @('trailing number',($player+' 1'),$party,'trailing-json-content'),
 @('trailing boolean',($player+' true'),$party,'trailing-json-content'),
 @('trailing null',($player+' null'),$party,'trailing-json-content'),
 @('trailing garbage',($player+' junk'),$party,'trailing-json-content'),
 @('trailing party document',$player,($party+' {}'),'trailing-json-content'),
 @('mismatched bracket','{"m_MainCharacter":"main-id"]',$party,'malformed-'),
 @('invalid string escape','{"m_MainCharacter":"main-id","x":"\q"}',$party,'malformed-'),
 @('inert metadata duplicate','{"__type":"A","__type":"B","m_MainCharacter":"main-id"}',$party,'duplicate-or-case-ambiguous'),
 @('inert metadata case collision','{"__type":"A","__TYPE":"B","m_MainCharacter":"main-id"}',$party,'duplicate-or-case-ambiguous'),
 @('nested inert metadata duplicate','{"m_MainCharacter":"main-id","x":{"__type":"A","__type":"B"}}',$party,'duplicate-or-case-ambiguous'),
 @('nested inert metadata case collision','{"m_MainCharacter":"main-id","x":{"__type":"A","__TYPE":"B"}}',$party,'duplicate-or-case-ambiguous'),
 @('main reference metadata alias','{"m_MainCharacter":{"__type":"A","m_UniqueId":"main-id"}}',$party,'unsupported-main-reference-object')
)
foreach($case in $mutations){
 $current=$case;Check $case[0] {
  Require ($current-is[object[]]-and$current.Count-eq4) 'Malformed graph mutation row: expected four scalar fields'
  foreach($value in $current){Require ($value-is[string]) 'Malformed graph mutation row: all fields must be strings'}
  GraphRefuse $current[1] $current[2] $current[3]
 }
}
Check 'complete JSON permits only whitespace suffix' { $r=Graph (" `t`r`n"+$player+" `t`r`n") ($party+" `r`n");Require ($r.status-ceq'PASS'-and$r.comparison-ceq'unique') 'Whitespace-only document refused' }
Check 'string braces are not document delimiters' { $r=Graph '{"m_MainCharacter":"main-id","extra":"}][{"}' $party;Require ($r.status-ceq'PASS') 'String brace delimiter confused' }
Check 'escaped quotes and slashes do not end document' { $r=Graph '{"m_MainCharacter":"main-id","extra":"quote\"} slash\\[\/"}' $party;Require ($r.status-ceq'PASS') 'Escaped quote delimiter confused' }
Check 'unicode quote escape does not end document' { $r=Graph '{"m_MainCharacter":"main-id","extra":"\u0022}\u005b"}' $party;Require ($r.status-ceq'PASS') 'Unicode escape delimiter confused' }
Check 'leading framework type metadata stays inert' { $r=Graph '{"__type":"No.Such.Type, Missing","m_MainCharacter":"main-id"}' $party;Require ($r.status-ceq'PASS'-and-not$r.nativeDeserialization) 'Inert metadata was activated or refused' }
Check 'unsupported shape retains only bounded structure' { $r=Graph $player '{"m_EntityData":{"$ref":"private-secret"}}';Require ($r.status-ceq'FAIL'-and$r.mainReferencePresent-and$r.entityListPresent-and$r.mainReferenceShape-ceq'object:m_UniqueId'-and$r.entityListShape-ceq'object'-and$null-eq$r.mainReference-and($r|ConvertTo-Json)-notmatch'private-secret') 'Unsupported diagnostic leaked values or lost shape' }
Check 'unsupported main still reports independent party structure' { $r=Graph '{"m_MainCharacter":{"$ref":"unresolved"}}' $party;Require ($r.status-ceq'FAIL'-and$r.refusal-ceq'unsupported-main-reference-object'-and$r.entityListPresent-and$r.entityListShape-ceq'array'-and$r.entityCount-eq2-and$null-eq$r.mainReference-and$null-eq$r.comparison-and-not$r.nativeLoadProven) 'Unsupported main hid already parsed party facts or granted authority' }
Check 'invalid UTF8 refuses' { $r=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::Analyze([byte[]]@(255,255),$utf8.GetBytes($party));Require ($r.refusal-ceq'invalid-utf8') 'UTF8 refusal differs' }
Check 'entity count boundary accepts4096' { $p='{"m_EntityData":['+((0..4095|ForEach-Object{'{"UniqueId":"id'+$_+'"}'})-join',')+']}';$r=Graph $player $p;Require ($r.status-ceq'PASS'-and$r.entityCount-eq4096) '4096 boundary refused' }
Check 'entity count4097 refuses' { $p='{"m_EntityData":['+((0..4096|ForEach-Object{'{"UniqueId":"id'+$_+'"}'})-join',')+']}';GraphRefuse $player $p 'entity-count-limit' }
Check 'depth bound refuses' { $p='{"m_MainCharacter":"main-id","extra":'+('['*65)+'0'+(']'*65)+'}';GraphRefuse $p $party 'malformed-or-quota' }
Check 'token bound refuses' { $p='{"m_MainCharacter":"main-id","extra":['+(('0,'*250000)+'0')+']}';GraphRefuse $p $party 'json-token-limit|malformed-or-quota' }
Check 'scalar string bound refuses' { $p='{"m_MainCharacter":"main-id","extra":"'+('x'*1048577)+'"}';GraphRefuse $p $party 'json-string-limit|malformed-or-quota' }
Check 'member byte bound refuses before parsing' { $r=[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::Analyze([byte[]]::new(16777217),$utf8.GetBytes($party));Require ($r.refusal-ceq'selected-member-byte-limit') 'Byte cap failed' }
Check 'archive maximum is a diagnostic policy bound' { [KingmakerMountedCombat.Harness.WorkingActorGraphReader]::CheckArchiveLength(268435456);Refuse { [KingmakerMountedCombat.Harness.WorkingActorGraphReader]::CheckArchiveLength(268435457) } 'archive-byte-limit';Refuse { [KingmakerMountedCombat.Harness.WorkingActorGraphReader]::CheckArchiveLength(0) } 'archive-byte-limit' }
Check 'declared expanded length cannot hide extra bytes' { $s=[IO.MemoryStream]::new([byte[]]@(1,2,3));try{Refuse {[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::ReadMember($s,2)} 'selected-member-expanded-length'}finally{$s.Dispose()} }
Check 'declared expanded length cannot be truncated' { $s=[IO.MemoryStream]::new([byte[]]@(1));try{Refuse {[KingmakerMountedCombat.Harness.WorkingActorGraphReader]::ReadMember($s,2)} 'selected-member-expanded-length'}finally{$s.Dispose()} }
# Actual existing fixture/header/qualification guards below. Only OS process
# discovery and repository-owned lab location are substituted; all archives are generated.
$script:fakeLab=$null;$script:processChecks=0;$script:processMode='normal'
function Get-KmcLabRoot { return $script:fakeLab }
function Assert-KmcNoGameProcesses {
    $script:processChecks++
    if($script:processMode-ceq'game'){throw 'Synthetic external game-process refusal'}
    if($script:processChecks-eq2){
        if($script:processMode-ceq'late-lock'){[IO.File]::WriteAllText((Join-Path $script:fakeLab 'runtime-state/active-transaction.lock'),'synthetic lock')}
        if($script:processMode-ceq'late-working'){[IO.File]::AppendAllText($script:workingPath,'changed')}
        if($script:processMode-ceq'late-qualification'){[IO.File]::AppendAllText((Join-Path $script:fakeLab 'runtime-state/fixture-qualification.json'),' ')}
        if($script:processMode-ceq'late-intake'){[IO.File]::AppendAllText((Join-Path $script:fakeLab 'environment-intake.json'),' ')}
    }
}
Add-Type -AssemblyName System.IO.Compression
function New-Zip([string]$Path,[object[]]$Entries){
 $f=[IO.FileStream]::new($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 $z=[IO.Compression.ZipArchive]::new($f,[IO.Compression.ZipArchiveMode]::Create,$false)
 try{foreach($e in $Entries){$s=$z.CreateEntry([string]$e[0]).Open();try{$b=$utf8.GetBytes([string]$e[1]);$s.Write($b,0,$b.Length)}finally{$s.Dispose()}}}finally{$z.Dispose();$f.Dispose()}
}
function New-Lab([string]$Name,[object[]]$Extra=@(),[string]$Player=$script:player,[string]$Party=$script:party){
 $script:fakeLab=Join-Path $out $Name;$saves=Join-Path $script:fakeLab 'saves';$state=Join-Path $script:fakeLab 'runtime-state'
 [void][IO.Directory]::CreateDirectory($saves);[void][IO.Directory]::CreateDirectory($state)
 $header=[ordered]@{Name='KMC_AUTOMATION_BASELINE';GameName='synthetic diagnostic fixture';GameId='11111111-2222-4333-8444-555555555555';Area=('a'*32);Type='Manual';CompatibilityVersion=1}
 New-Zip (Join-Path $saves 'Manual_1_KMC_AUTOMATION_BASELINE.zks') (,@('header.json',($header|ConvertTo-Json -Compress)))
 $header.Name='KMC_AUTOMATION_WORKING';$script:workingPath=Join-Path $saves 'Manual_2_KMC_AUTOMATION_WORKING.zks'
 $entries=@(@('header.json',($header|ConvertTo-Json -Compress)),@('player.json',$Player),@('party.json',$Party))
 New-Zip $script:workingPath @($entries+$Extra)
 [IO.File]::WriteAllText((Join-Path $script:fakeLab 'environment-intake.json'),([ordered]@{requestedLayout=@{kingmakerSaveRoot=$saves}}|ConvertTo-Json),$utf8)
 $qualification=Join-Path $state 'fixture-qualification.json'
 $pair=Assert-KmcFixturePair -SaveRoot $saves -QualificationPath $qualification -InitializeQualification
 $script:processChecks=0;$script:processMode='normal'
 return [pscustomobject]@{pair=$pair;q=$qualification;qhash=(Get-KmcSha256 $qualification);whash=(Get-KmcSha256 $script:workingPath);saveRoot=$saves}
}
function Invoke-Lab($Fixture){Invoke-KmcQualifiedWorkingActorGraphDiagnostic -ExpectedQualificationSha256 $Fixture.qhash -ExpectedWorkingSha256 $Fixture.whash}
Check 'actual qualified generated Working route preserves every input' { $f=New-Lab 'guard-positive';$before=Get-KmcSha256 $script:workingPath;$r=Invoke-Lab $f;Require ($r.status-ceq'PASS'-and$r.archive.observation.comparison-ceq'unique'-and$r.sourceBytesPreserved-and-not$r.nativeAdmission-and$r.archive.beforeSha256-ceq$before-and$r.archive.afterSha256-ceq$before-and(Get-KmcSha256 $script:workingPath)-ceq$before-and$script:processChecks-eq2) 'Guarded positive differs' }
Check 'guarded unsupported shape emits structural FAIL' { $f=New-Lab 'guard-unsupported' -Party '{"m_EntityData":{"$values":[]}}';$r=Invoke-Lab $f;Require ($r.status-ceq'FAIL'-and$r.sourceBytesPreserved-and$r.archive.observation.refusal-ceq'unsupported-entity-list'-and$null-eq$r.archive.observation.mainReference) 'Unsupported archive incorrectly qualified' }
foreach($case in @(@('duplicate-member','party.json'),@('case-member','PARTY.JSON'),@('traversal','../extra'),@('absolute-member','C:/extra'),@('nested-member','folder/extra'),@('backslash-member','folder\extra'))){
 $current=$case;Check ('archive '+$case[0]) { $f=New-Lab $current[0] -Extra (,@($current[1],'{}'));Refuse {Invoke-Lab $f} 'traversal, nested, duplicate or case-colliding' }
}
Check '1025 archive entries refuse' { $extra=@(1..1022|ForEach-Object{,@(('extra'+$_),'')});$f=New-Lab 'member-count' -Extra $extra;Refuse {Invoke-Lab $f} 'member-count limit' }
Check 'selected member over16MiB refuses' { $f=New-Lab 'member-size' -Party ('x'*16777217);Refuse {Invoke-Lab $f} 'selected-member byte limit' }
Check 'wrong qualification pin refuses' { $f=New-Lab 'qualification-pin';$f.qhash='0'*64;Refuse {Invoke-Lab $f} 'qualification hash differs' }
Check 'wrong Working pin refuses' { $f=New-Lab 'working-pin';$f.whash='0'*64;Refuse {Invoke-Lab $f} 'qualified Working hash differs' }
Check 'changed Working bytes refuse original qualification' { $f=New-Lab 'changed-working';[IO.File]::AppendAllText($script:workingPath,'x');Refuse {Invoke-Lab $f} 'qualification|fingerprint|identity|length|metadata|hash|changed' }
Check 'active transaction refuses' { $f=New-Lab 'active-lock';[IO.File]::WriteAllText((Join-Path $script:fakeLab 'runtime-state/active-transaction.lock'),'synthetic');Refuse {Invoke-Lab $f} 'Active runtime transaction' }
Check 'active game external boundary refuses' { $f=New-Lab 'active-game';$script:processMode='game';Refuse {Invoke-Lab $f} 'Synthetic external game-process refusal' }
foreach($mode in @('late-lock','late-working','late-qualification','late-intake')){ $current=$mode;Check $mode { $f=New-Lab $current;$script:processMode=$current;Refuse {Invoke-Lab $f} 'appeared|changed|qualification|hash|fingerprint' } }
Check 'internal archive boundary cannot substitute Baseline' { $f=New-Lab 'baseline-refusal';Refuse {Read-KmcQualifiedWorkingActorGraphArchive -Working $f.pair.baseline -SaveRoot $f.saveRoot} 'exact qualified Working leaf' }
Check 'internal archive boundary refuses stale metadata' { $f=New-Lab 'metadata-refusal';$f.pair.working.lastWriteTimeUtcTicks++;Refuse {Read-KmcQualifiedWorkingActorGraphArchive -Working $f.pair.working -SaveRoot $f.saveRoot} 'metadata changed before' }
Check 'internal archive boundary refuses stale bytes' { $f=New-Lab 'hash-refusal';$f.pair.working.sha256='0'*64;Refuse {Read-KmcQualifiedWorkingActorGraphArchive -Working $f.pair.working -SaveRoot $f.saveRoot} 'bytes changed before' }
Check 'internal archive boundary refuses outside-root path' { $f=New-Lab 'outside-refusal';$f.pair.working.path=Join-Path $out 'outside.zks';Refuse {Read-KmcQualifiedWorkingActorGraphArchive -Working $f.pair.working -SaveRoot $f.saveRoot} 'outside|child|escape' }
Check 'save-root junction refuses before archive observation' { $f=New-Lab 'junction-refusal';$link=Join-Path $script:fakeLab 'save-link';[void](New-Item -ItemType Junction -Path $link -Target $f.saveRoot);[IO.File]::WriteAllText((Join-Path $script:fakeLab 'environment-intake.json'),([ordered]@{requestedLayout=@{kingmakerSaveRoot=$link}}|ConvertTo-Json),$utf8);Refuse {Invoke-Lab $f} 'reparse' }
Check 'Working hardlink refuses via unchanged guard' { $f=New-Lab 'hardlink-refusal';$links=Join-Path $script:fakeLab 'links';[void][IO.Directory]::CreateDirectory($links);$link=Join-Path $links $f.pair.working.fileName;[void](New-Item -ItemType HardLink -Path $link -Target $script:workingPath);$f.pair.working.path=$link;Refuse {Read-KmcQualifiedWorkingActorGraphArchive -Working $f.pair.working -SaveRoot $links} 'hard link' }
Check 'public command has only two hash inputs and no archive/output options' { $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $DiagnosticDirectory 'Test-KmcWorkingActorGraph.ps1'),[ref]$tokens,[ref]$errors);Require ($errors.Count-eq0) 'Command does not parse';$names=@($ast.ParamBlock.Parameters|ForEach-Object{$_.Name.VariablePath.UserPath});Require (($names-join',')-ceq'ExpectedQualificationSha256,ExpectedWorkingSha256') 'Command exposed a path or native option' }
$watch.Stop();$failed=@($script:rows|Where-Object status -CEQ 'FAIL')
$receipt=[ordered]@{schemaVersion=1;status=$(if($failed.Count){'FAIL'}else{'PASS'});passCount=$script:rows.Count-$failed.Count;failCount=$failed.Count;elapsedSeconds=$watch.Elapsed.TotalSeconds;
 readerSha256=$readerHash;guardSha256=(Get-KmcSha256 (Join-Path $DiagnosticDirectory 'WorkingActorGraphDiagnostic.ps1'));commandSha256=(Get-KmcSha256 (Join-Path $DiagnosticDirectory 'Test-KmcWorkingActorGraph.ps1'));
 fixtureGuardSha256=(Get-KmcSha256 (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1'));scriptSha256=(Get-KmcSha256 $PSCommandPath);
 generatedArchivesOnly=$true;actualSaveArchiveReads=0;nativeInvocations=0;rootWrites=0;externalSubstitutions=@('repository lab location','OS game-process discovery');cases=@($script:rows)}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 10),$utf8)
Write-Host ('Working graph diagnostic: PASS='+$receipt.passCount+' FAIL='+$receipt.failCount+' seconds='+$receipt.elapsedSeconds)
foreach($failure in $failed){Write-Host ($failure.name+': '+$failure.error)}
if($failed.Count){exit 1}
