[CmdletBinding()]
param()
# Round 19 (R18-F4): the UPG-01 resave reader, Assert-KmcUpgradeResaveEvidence (the handler PersistenceSaveFixtures.ps1 dispatches for the
# upgrade-chain persistence-p05-load quick producer), over generated ZIP resaves beside a retained older Quick. The reader decodes the
# hash-verified resave's kmc-mounted-state member with the candidate's own product codec (shared helper Read-KmcChunk7MountedArchiveMember)
# BEFORE any lossy parse, compares the complete decoded member with the reported snapshot (explicit version view only), keeps the actual
# source schema separate, and applies the area, campaign, pair, slot and step-travel expectations to the member itself.
# Fixtures: schema-3 members as the previous writer wrote them; schema-4 members written by the candidate's production serializer
# (MountedArchiveCodec.Encode over the codec's decoding). The combat graph is the inherited owned-pairs combat fixture reduced to its first
# pair. Every refusal keeps the recorded SHA-256, length and pair ids equal to the bytes on disk. Offline generated observations only
# (nativeInvoked=false): reader behavior, never a native save or load.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$runtime=Join-Path $repo 'scripts/runtime'
. (Join-Path $runtime 'RuntimeHarness.Common.ps1')
. (Join-Path $runtime 'PersistenceSaveFixtures.ps1')
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action){$why=$null;try{&$Action;$ok=$true}catch{$ok=$false;$why=$_.Exception.Message};$results.Add([pscustomobject]@{name=$Name;pass=$ok;refusal=$false});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if($ok){''}else{': '+$why}))}
# A refusal counts only when its setup succeeded and the reader refused for the named reason.
function Reject([string]$Name,[scriptblock]$Setup,[string]$Expect){$c=$null;try{$c=& $Setup}catch{$results.Add([pscustomobject]@{name=$Name;pass=$false;refusal=$true});Write-Output ('FAIL refuses '+$Name+': setup failed: '+$_.Exception.Message);return}
 $why=$null;try{Invoke-Upgrade $c}catch{$why=$_.Exception.Message};$ok=$null-ne$why-and$why-match$Expect
 $results.Add([pscustomobject]@{name=$Name;pass=$ok;refusal=$true});Write-Output ($(if($ok){'PASS refuses '}else{'FAIL refuses '})+$Name+' ('+$(if($null-eq$why){'accepted'}else{$why})+')')}

$codec=Get-KmcChunk7ProductArchiveCodec
function Read-Member([string]$Json){$r=$codec.decode.Invoke($null,@([string]$Json));if($codec.kind.GetValue($r).ToString()-cne'Current'){throw ('fixture member refused by the product codec: '+$codec.feedback.GetValue($r))};$r}
# The current writer's member for a graph: the production serializer over the codec's own decoding (optionally edited, then re-encoded).
function ConvertTo-Current([string]$Json,[scriptblock]$Edit={}){$j=[string]$codec.encode.Invoke($null,@($codec.data.GetValue((Read-Member $Json))))
 $o=$j|ConvertFrom-Json;& $Edit $o;[string]$codec.encode.Invoke($null,@($codec.data.GetValue((Read-Member ($o|ConvertTo-Json -Depth 40 -Compress)))))}
function Copy-Graph($Value){$Value|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json}

# The inherited owned combat fixture (schema 3) reduced to its first pair: one mounted pair, its participation, combat actors, roster and
# movement allocation (the mount's), and the rider's control rows.
$fixture=@((Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json')).fixtures|Where-Object name -CEQ 'persistence-save-multi-tb-debt-manual')[0]
$combat3=Copy-Graph $fixture.artifact.observations.nativeWrite.archive;$keep=@($combat3.Pairs[0].Rider.Id,$combat3.Pairs[0].Mount.Id)
$combat3.Pairs=@($combat3.Pairs[0]);$combat3.Participations=@($combat3.Participations[0]);$combat3.Slots=@($combat3.Slots|Where-Object{$_.ActorId-cin$keep})
$combat3.Combat.Actors=@($combat3.Combat.Actors|Where-Object{$_.Native.Id-cin$keep});$combat3.Combat.Roster=@($combat3.Combat.Roster|Where-Object{$_.ActorId-cin$keep})
$combat3.Combat.Allocations=@($combat3.Combat.Allocations|Where-Object{$_.ActorId-cin$keep});$combat3.Combat.Current=$null
$explore3=Copy-Graph $combat3;$explore3.Participations=@();$explore3.Combat=$null
$rider=[string]$combat3.Pairs[0].Rider.Id;$mount=[string]$combat3.Pairs[0].Mount.Id;$area=[string]$combat3.AreaId;$campaign=[string]$combat3.CampaignId
function Json($Graph){$Graph|ConvertTo-Json -Depth 40 -Compress}
$explore3Json=Json $explore3;$combat3Json=Json $combat3;[void](Read-Member $explore3Json);[void](Read-Member $combat3Json)
$explore4Json=ConvertTo-Current $explore3Json
# Independent step travel: the mount's written travel (0.25) differs from its native MetresStepped counter (0).
$combat4Json=ConvertTo-Current $combat3Json {param($g)$g.StepTravel[0].MetresStepTravelled=0.25}
$combat4=$combat4Json|ConvertFrom-Json

$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-upg01-member-'+[Guid]::NewGuid().ToString('N'))
Add-Type -AssemblyName System.IO.Compression
# One UPG-01 producer root: the retained older Quick and the resave Manual_1_KMC_P01.zks. Shape: zip (header and one mounted member),
# plain (a text file), none (a ZIP without the mounted member) or twice (two kmc-mounted-state members). The rows record the bytes on disk.
function New-Case([string]$Name,[string]$Member,$Snapshot,[string]$Shape='zip'){
 $dir=Join-Path $root $Name;[void][IO.Directory]::CreateDirectory($dir);[IO.File]::WriteAllBytes((Join-Path $dir 'Quick_1.zks'),[Text.Encoding]::ASCII.GetBytes('retained older quick '+$Name))
 $quick=Get-KmcSha256 (Join-Path $dir 'Quick_1.zks');$path=Join-Path $dir 'Manual_1_KMC_P01.zks'
 if($Shape-ceq'plain'){[IO.File]::WriteAllBytes($path,[Text.Encoding]::ASCII.GetBytes($Member))}
 else{$entries=@(,@('header.json','{"Name":"KMC_P01","Type":"Manual"}'));if($Shape-cne'none'){$entries+=,@('kmc-mounted-state',$Member)};if($Shape-ceq'twice'){$entries+=,@('kmc-mounted-state',$Member)}
  $s=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$z=$null
  try{$z=[IO.Compression.ZipArchive]::new($s,[IO.Compression.ZipArchiveMode]::Create,$false);foreach($e in $entries){$w=[IO.StreamWriter]::new($z.CreateEntry($e[0]).Open(),[Text.UTF8Encoding]::new($false));try{$w.Write($e[1])}finally{$w.Dispose()}}}finally{if($z){$z.Dispose()};$s.Dispose()}}
 [pscustomobject]@{root=$dir;request=[pscustomobject]@{persistenceLoad=[pscustomobject]@{sha256=$quick;area=$area;gameId=$campaign}}
  rows=@([pscustomobject]@{kind='initial';rider=[pscustomobject]@{Id=$script:rider};mount=[pscustomobject]@{Id=$script:mount}},[pscustomobject]@{kind='native-write-complete';detail=[pscustomobject]@{ordinal=1;path=$path;sha256=(Get-KmcSha256 $path);length=(Get-Item -LiteralPath $path).Length;nativeType='Manual';nativeCallback=$true;operation='None';sourceSha256=$quick;snapshot=(Copy-Graph $Snapshot)}})}}
function Invoke-Upgrade($c){Assert-KmcUpgradeResaveEvidence $c.request $c.rows $c.root}
function Edit([string]$Json,[scriptblock]$Change){$o=$Json|ConvertFrom-Json;& $Change $o;Json $o}

try{
 # --- matching positives: the snapshot is the producer's normalized read of its own write (the decoded member at the written version). ---
 Check 'UPG-01: current schema-4 exploration resave (production serializer member, decoded snapshot) accepted' {Invoke-Upgrade (New-Case 'current-explore' $explore4Json ($explore4Json|ConvertFrom-Json))}
 Check 'UPG-01: current schema-4 combat resave with independent step travel (0.25 written, MetresStepped 0) accepted' {
  $a=@($combat4.Combat.Allocations|Where-Object ActorId -CEQ $combat4.StepTravel[0].ActorId)[0];if([double]$combat4.StepTravel[0].MetresStepTravelled-eq[double]$a.Movement.MetresStepped){throw 'The fixture step travel is not independent of MetresStepped.'}
  Invoke-Upgrade (New-Case 'current-combat' $combat4Json $combat4)}
 Check 'UPG-01: historical (earlier build) schema-3 exploration resave accepted' {Invoke-Upgrade (New-Case 'historical-explore' $explore3Json $explore3)}
 Check 'UPG-01: historical (earlier build) schema-3 combat resave accepted' {Invoke-Upgrade (New-Case 'historical-combat' $combat3Json $combat3)}

 # --- same-version content mismatches: a valid member that is not the reported/restored state. ---
 Reject 'UPG-01: valid schema-4 member for a different area under the expected snapshot' {New-Case 'area-4' (ConvertTo-Current (Edit $explore3Json {param($g)$g.AreaId='b'*32})) ($explore4Json|ConvertFrom-Json)} 'not in the retained area and campaign'
 Reject 'UPG-01: valid schema-3 member for a different area under the expected snapshot' {New-Case 'area-3' (Edit $explore3Json {param($g)$g.AreaId='b'*32}) $explore3} 'not in the retained area and campaign'
 Reject 'UPG-01: valid schema-4 member for a different campaign' {New-Case 'campaign-4' (ConvertTo-Current (Edit $explore3Json {param($g)$g.CampaignId='99999999-9999-4999-8999-999999999999'})) ($explore4Json|ConvertFrom-Json)} 'not in the retained area and campaign'
 Reject 'UPG-01: valid schema-4 member for another pair (snapshot and initial name the restored pair)' {New-Case 'pair-4' (ConvertTo-Current (Edit $explore3Json {param($g)$g.Pairs[0].Rider.Id='55555555-5555-4555-8555-555555555555';foreach($s in $g.Slots){$s.ActorId='55555555-5555-4555-8555-555555555555'}})) ($explore4Json|ConvertFrom-Json)} 'exactly the restored mounted pair'
 Reject 'UPG-01: valid schema-4 member with different step travel (0.5 written, 0.25 reported)' {New-Case 'step-4' (ConvertTo-Current $combat3Json {param($g)$g.StepTravel[0].MetresStepTravelled=0.5}) $combat4} 'step travel differs'
 Reject 'UPG-01: valid schema-4 member with a different slot row' {New-Case 'slot-4' (ConvertTo-Current (Edit $explore3Json {param($g)$g.Slots[0].Index=20})) ($explore4Json|ConvertFrom-Json)} 'slot rows'
 Reject 'UPG-01: valid schema-4 member differing from the snapshot only in GameTimeTicks (complete comparison)' {New-Case 'ticks-4' (ConvertTo-Current (Edit $explore3Json {param($g)$g.GameTimeTicks=20000000})) ($explore4Json|ConvertFrom-Json)} 'differs from the decoded archive member'

 # --- duplicate JSON names: refused by the product codec before any parse that keeps one of the two values. ---
 $dupVersion='{"SchemaVersion":3,'+$explore4Json.Substring(1);$dupStep='{"StepTravel":[],'+$combat4Json.Substring(1)
 Check 'control: the pre-round-19 lossy parse keeps one SchemaVersion (4) and its shape check accepts the duplicate-name member' {$o=$dupVersion|ConvertFrom-Json;if($o.SchemaVersion-ne4-or(Assert-KmcChunk7WriterArchive $o 'lossy parse')-ne4){throw 'ConvertFrom-Json did not collapse the duplicate SchemaVersion to the last value.'}}
 Reject 'UPG-01: schema-4 member with a duplicate SchemaVersion property' {New-Case 'dup-version' $dupVersion ($explore4Json|ConvertFrom-Json)} 'refused by the product codec \(Invalid\)'
 Reject 'UPG-01: schema-4 member with a duplicate StepTravel property' {New-Case 'dup-step' $dupStep $combat4} 'refused by the product codec \(Invalid\)'

 # --- the other malformed-format controls (R17-F4 retained), with the file identity unchanged. ---
 Reject 'UPG-01: schema-3 bytes beneath a schema-4 snapshot (hash, length and pair ids match)' {New-Case 'crossed' $explore3Json ($explore4Json|ConvertFrom-Json)} 'written as schema 3'
 Reject 'UPG-01: schema-4 member missing StepTravel' {New-Case 'missing-step' (Edit $explore4Json {param($g)$g.PSObject.Properties.Remove('StepTravel')}) ($explore4Json|ConvertFrom-Json)} 'refused by the product codec \(Invalid\)'
 Reject 'UPG-01: schema-3 member carrying StepTravel' {New-Case 'step-3' (Edit $explore3Json {param($g)$g|Add-Member NoteProperty StepTravel @()}) $explore3} 'refused by the product codec \(Invalid\)'
 Reject 'UPG-01: future schema-5 member' {New-Case 'future' (Edit $explore4Json {param($g)$g.SchemaVersion=5}) ($explore4Json|ConvertFrom-Json)} 'refused by the product codec \(Future\)'
 Reject 'UPG-01: plain-text resave with matching recorded hash and length' {New-Case 'plain' 'kept resave as plain text' ($explore4Json|ConvertFrom-Json) 'plain'} 'not a ZIP save archive'
 Reject 'UPG-01: ZIP resave without the mounted member' {New-Case 'no-member' $explore4Json ($explore4Json|ConvertFrom-Json) 'none'} 'lacks exactly one bounded kmc-mounted-state member'
 Reject 'UPG-01: ZIP resave with two mounted members' {New-Case 'two-members' $explore4Json ($explore4Json|ConvertFrom-Json) 'twice'} 'lacks exactly one bounded kmc-mounted-state member'
 Reject 'UPG-01: recorded sourceSchema 3 for schema-4 bytes' {$c=New-Case 'labelled' $explore4Json ($explore4Json|ConvertFrom-Json);$c.rows[1].detail|Add-Member NoteProperty sourceSchema 3;$c} 'recorded source schema differs'
}finally{if(Test-Path -LiteralPath $root){Remove-Item -LiteralPath $root -Recurse -Force}}

$clock.Stop();$passed=@($results|Where-Object pass -EQ $true).Count;$failed=@($results|Where-Object pass -EQ $false).Count;$refusals=@($results|Where-Object{$_.pass-and$_.refusal}).Count
Write-Output ('UPG-01 RESAVE MEMBER RESULT '+$passed+' PASS / '+$failed+' FAIL ('+$refusals+' expected refusals; '+[Math]::Round($clock.Elapsed.TotalSeconds,1)+' s; offline generated observations, nativeInvoked=false, no native save/load claim)')
if($failed-or$passed-eq0){exit 1}
