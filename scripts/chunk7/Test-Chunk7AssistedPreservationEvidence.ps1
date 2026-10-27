[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$ReaderPath,[Parameter(Mandatory=$true)][string]$FixtureManifestPath,[Parameter(Mandatory=$true)][string]$OutputRoot,[string[]]$CaseNames=@())
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1');. $ReaderPath
$output=Assert-KmcChildPath $OutputRoot (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated assisted preservation checks'
if(Test-Path -LiteralPath $output){throw 'Preserve prior preservation test output.'};[void][IO.Directory]::CreateDirectory($output)
$clock=[Diagnostics.Stopwatch]::StartNew();$m=Read-KmcJson $FixtureManifestPath
if($m.kind-cne'actual-assisted-host-preservation-fixtures'-or$m.outputs-isnot[Array]-or$m.outputs.Count-ne2){throw 'Actual host formatter fixture manifest differs.'}
$fixtures=@{}
foreach($p in $m.outputs){
    $path=Assert-KmcChildPath $p.path (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'generated host fragment'
    Assert-KmcNotReparsePoint $path 'generated host fragment';Assert-KmcNotHardLink $path 'generated host fragment'
    if((Get-Item -LiteralPath $path).Length-ne$p.length-or(Get-KmcSha256 $path)-cne$p.sha256){throw 'Actual host fixture bytes changed.'}
    $fixtures.Add([IO.Path]::GetFileName($path),[IO.File]::ReadAllText($path))
}
$inputs=@($PSCommandPath,$ReaderPath,$FixtureManifestPath)+@($m.outputs|ForEach-Object path)+@(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'scripts/runtime') -Filter '*.ps1' -File|ForEach-Object FullName)
$pins=@($inputs|Sort-Object -Unique|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-KmcSha256 $_)}})
$script:passed=0;$script:failed=0;$cases=[Collections.Generic.List[object]]::new()
function Hash-Text([string]$Text){$sha=[Security.Cryptography.SHA256]::Create();try{[BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false,$true).GetBytes($Text))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}}
function Rehash-Rows($Value){foreach($r in $Value.preservation.rows){$r.canonicalJson=ConvertTo-Json -InputObject $r.state -Depth 64 -Compress;$r.sha256=Hash-Text $r.canonicalJson}}
function Fixture-Arguments($Value){
    # Only this helper's independently validated setup-envelope boundary is
    # projected. Preservation JSON itself comes from actual unchanged host
    # CapturePreservedStateSha256; it is never hand-authored for positives.
    $setup=[pscustomobject]@{experience=$null;hireFunds=$null}
    foreach($pair in @(@('experience','experience'),@('hire-funds','hireFunds'))){
        $rows=@($Value.preservation.rows|Where-Object{$_.purpose.StartsWith($pair[0]+'-',[StringComparison]::Ordinal)})
        $effect=[pscustomobject]@{noOp=($rows.Count-eq1);before=[pscustomobject]@{frame=$rows[0].frame;preservedStateSha256=$rows[0].sha256;experienceRatePercent=$rows[0].state.experienceRatePercent;level=($rows[0].state.actors|Where-Object id -CEQ $rows[0].state.mainActorId).characterLevel};after=[pscustomobject]@{frame=$rows[-1].frame;preservedStateSha256=$rows[-1].sha256;experienceRatePercent=$rows[-1].state.experienceRatePercent;level=($rows[-1].state.actors|Where-Object id -CEQ $rows[-1].state.mainActorId).characterLevel}}
        $setup.($pair[1])=$effect
    }
    $state=$Value.preservation.rows[0].state
    [pscustomobject]@{setup=$setup;expected=[pscustomobject]@{mainActorId=$state.mainActorId;gameId=$state.gameId;gameName=$state.gameName;area=$state.area};levelupWrites=$Value.cleanup.authority.levelupWrites}
}
function Check([string]$Name,[bool]$Accept,[scriptblock]$Mutation,[bool]$Zero=$false){
    if($CaseNames.Count-gt0-and$Name-cnotin$CaseNames){return}
    $leaf=if($Zero){'host-fragments-zero-xp.json'}else{'host-fragments-positive-xp.json'};$v=$fixtures[$leaf]|ConvertFrom-Json
    $fixtureArguments=Fixture-Arguments $v
    if($null-ne$Mutation){& $Mutation $v $fixtureArguments|Out-Null}
    $labels=[Collections.Generic.HashSet[int]]::new();foreach($id in $v.identityRegistry.retainedIds){[void]$labels.Add([int]$id)}
    $actual=$false;$error=$null
    try{Assert-KmcChunk7AssistedPreservation $v.preservation $fixtureArguments.setup $fixtureArguments.expected $labels $fixtureArguments.levelupWrites;$actual=$true}catch{$error=$_.Exception.Message}
    $ok=$actual-eq$Accept;if($ok){$script:passed++}else{$script:failed++}
    $cases.Add([pscustomobject]@{name=$Name;status=$(if($ok){'PASS'}else{'FAIL'});expectedAccepted=$Accept;actualAccepted=$actual;error=$error})
    Write-Host ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$error}else{''}))
}
function Reject([string]$Name,[scriptblock]$Mutation){Check $Name $false $Mutation}
function Rebind-Effects($Value,$fixtureArguments){$fresh=Fixture-Arguments $Value;foreach($kind in @('experience','hireFunds')){foreach($phase in @('before','after')){foreach($key in @('frame','preservedStateSha256')){$fixtureArguments.setup.$kind.$phase.$key=$fresh.setup.$kind.$phase.$key}}}}
Check 'actual positive XP and funds six reads' $true $null
Check 'actual zero XP and positive funds four reads' $true $null $true
Reject 'hash must cover exact retained canonical text' {param($v,$a)$v.preservation.rows[0].sha256=('f'*64)}
Reject 'canonical JSON must decode to observed state' {param($v,$a)$v.preservation.rows[0].state.gameTimeTicks++}
Reject 'unknown state fields refused despite consistent hash' {param($v,$a)foreach($r in $v.preservation.rows){$r.state|Add-Member NoteProperty synthetic $true};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'party money is outside preservation schema' {param($v,$a)foreach($r in $v.preservation.rows){$r.state|Add-Member NoteProperty money 0};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'main XP is excluded only by exact identity' {param($v,$a)foreach($r in $v.preservation.rows){($r.state.actors|Where-Object id -CEQ $r.state.mainActorId)|Add-Member NoteProperty experience 0};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'unrelated actor XP cannot be dropped' {param($v,$a)foreach($r in $v.preservation.rows){($r.state.actors|Where-Object id -CNE $r.state.mainActorId|Select-Object -First 1).PSObject.Properties.Remove('experience')};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native preparation cannot change across XP call' {param($v,$a)$v.preservation.rows[2].state.actors[0].actions.prepared=$false;Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native reaction debt cannot change across XP call' {param($v,$a)$v.preservation.rows[1].state.actors[0].actions.reactions++;Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native game clock cannot change within synchronous effect' {param($v,$a)$v.preservation.rows[2].state.gameTimeTicks++;Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native save cannot occur inside XP assistance' {param($v,$a)foreach($r in $v.preservation.rows|Select-Object -First 3){$r.state.completedSaves=2};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native restoration is unselected' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.semanticRestores=1};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'mounted custody is unselected' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.mountedOwners=1};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native reference outside retained registry refused' {param($v,$a)$v.identityRegistry.retainedIds=@()}
Reject 'stale main actor binding refused' {param($v,$a)$a.expected.mainActorId='ffffffff-ffff-ffff-ffff-ffffffffffff'}
Reject 'stale campaign binding refused' {param($v,$a)$a.expected.gameId='ffffffff-ffff-ffff-ffff-ffffffffffff'}
Reject 'native roles cannot alias' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.state=$r.state.game};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'duplicate roster character refused' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.partyCharacters+=,$r.state.partyCharacters[0]};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'field and remote memberships cannot overlap' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.remoteCompanions+=,$r.state.partyCharacters[0]};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native class totals must match character level' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.actors[0].characterLevel++};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'main is required in field character roster' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.partyCharacters=@($r.state.partyCharacters|Where-Object id -CNE $r.state.mainActorId)};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'sequence must retain actual read order' {param($v,$a)$v.preservation.rows[1].sequence=1}
Reject 'entry cannot be relabeled after' {param($v,$a)$v.preservation.rows[1].purpose='experience-after'}
Reject 'row cannot precede previous observed frame' {param($v,$a)$v.preservation.rows[1].frame=99}
Reject 'entry cannot follow effect after frame' {param($v,$a)$v.preservation.rows[1].frame=101}
Reject 'entry requires exact admitted frame even if after advances' {param($v,$a)$v.preservation.rows[1].frame=101;$v.preservation.rows[2].frame=101;for($i=3;$i-lt$v.preservation.rows.Count;$i++){$v.preservation.rows[$i].frame=102};Rebind-Effects $v $a}
Reject 'no extra read after native effects' {param($v,$a)$v.preservation.rows+=,$v.preservation.rows[-1]}
Reject 'bound cannot be expanded' {param($v,$a)$v.preservation.maximumSnapshots=13}
Reject 'exclusions cannot expand' {param($v,$a)$v.preservation.excluded+=,'nativeActions'}
Reject 'duplicate canonical JSON members refused' {param($v,$a)$r=$v.preservation.rows[0];$r.canonicalJson=$r.canonicalJson.Replace('{','{"game":1,');$r.sha256=Hash-Text $r.canonicalJson;Rebind-Effects $v $a}
Reject 'trailing canonical JSON refused' {param($v,$a)$r=$v.preservation.rows[0];$r.canonicalJson+=' {}';$r.sha256=Hash-Text $r.canonicalJson;Rebind-Effects $v $a}
Reject 'extra unrostered actor rejected even with consistent hashes' {param($v,$a)
    foreach($r in $v.preservation.rows){$extra=$r.state.actors[0]|ConvertTo-Json -Depth 32|ConvertFrom-Json;$extra.id='ffffffff-1111-1111-1111-111111111111';$extra.object=-1001;$extra.descriptor=-1002;$extra.view=-1003;$extra.masterId=$null;$extra.petId=$null;$extra.features=@();$extra.buffs=@();$extra|Add-Member NoteProperty experience 100 -Force;$r.state.actors+=,$extra}
    $v.identityRegistry.retainedIds+=@(-1001,-1002,-1003);Rehash-Rows $v;Rebind-Effects $v $a
}
Reject 'native field count cannot exceed twelve' {param($v,$a)
    foreach($r in $v.preservation.rows){for($i=1;$i-le12;$i++){$extra=$r.state.actors[0]|ConvertTo-Json -Depth 32|ConvertFrom-Json;$extra.id=('ffffffff-1111-1111-1111-'+$i.ToString('D12'));$extra.object=-1000-3*$i;$extra.descriptor=-1001-3*$i;$extra.view=-1002-3*$i;$extra.masterId=$null;$extra.petId=$null;$extra.features=@();$extra.buffs=@();$extra|Add-Member NoteProperty experience 100 -Force;$r.state.actors+=,$extra}}
    for($i=1;$i-le12;$i++){$v.identityRegistry.retainedIds+=@((-1000-3*$i),(-1001-3*$i),(-1002-3*$i))};Rehash-Rows $v;Rebind-Effects $v $a
}
Reject 'field character cannot have a master' {param($v,$a)foreach($r in $v.preservation.rows){$actor=$r.state.actors|Where-Object id -CEQ $r.state.partyCharacters[0].id;$actor.masterId=$r.state.partyCharacters[1].id};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'declared pet must exist in exact field graph' {param($v,$a)foreach($r in $v.preservation.rows){$actor=$r.state.actors|Where-Object id -CEQ $r.state.partyCharacters[0].id;$actor.petId='ffffffff-1111-1111-1111-111111111111'};Rehash-Rows $v;Rebind-Effects $v $a}

Reject 'preserved native rate must bind its effect snapshot' {param($v,$a)foreach($r in $v.preservation.rows|Select-Object -First 3){$r.state.experienceRatePercent=50};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'preserved native level must bind its effect snapshot' {param($v,$a)foreach($r in $v.preservation.rows|Select-Object -First 3){$main=$r.state.actors|Where-Object id -CEQ $r.state.mainActorId;$main.characterLevel++;$main.classes[0].level++};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native feature cannot alias an actor descriptor' {param($v,$a)foreach($r in $v.preservation.rows){$r.state.actors[0].features[0].object=$r.state.actors[0].descriptor};Rehash-Rows $v;Rebind-Effects $v $a}
Reject 'native actor cannot alias the Game object' {param($v,$a)foreach($r in $v.preservation.rows){$actor=$r.state.actors|Where-Object id -CEQ $r.state.partyCharacters[1].id;$actor.object=$r.state.game;$r.state.partyCharacters[1].object=$r.state.game};Rehash-Rows $v;Rebind-Effects $v $a}

if($script:passed+$script:failed-eq0){throw 'Preservation selection ran zero cases.'}
foreach($p in $pins){if((Get-KmcSha256 $p.path)-cne$p.sha256){throw ('Preservation test input changed: '+$p.path)}}
Write-KmcJsonAtomic (Join-Path $output 'receipt.json') ([ordered]@{schemaVersion=1;kind='actual-assisted-preservation-focused-checks';status=$(if($script:failed-eq0){'PASS'}else{'FAIL'});passed=$script:passed;failed=$script:failed;durationSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;installedOrProtectedArchiveReads=0;inputPinsVerifiedAfter=$true;inputPins=$pins;scope='Actual host preservation formatter fragments; setup-envelope scalar arguments projected, no complete/native artifact proof';cases=$cases.ToArray()})
Write-Host ('ASSISTED PRESERVATION: '+$script:passed+' PASS / '+$script:failed+' FAIL')
$global:LASTEXITCODE=$(if($script:failed-eq0){0}else{1})
