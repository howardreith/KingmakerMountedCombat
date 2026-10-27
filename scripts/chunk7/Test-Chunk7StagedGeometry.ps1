[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
# 7E F04 staged leg geometry terms and the up-front whole-plan selection, compiled alone (no native or Unity
# reference) with the real b202/b203 st proposals; then the external reader replays the plans the C# planner
# recorded (the committed reader fixture must equal the regenerated one) and refuses tampered records.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/staged-geometry-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$sources=@((Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7StagedGeometry.cs'),(Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/StagedGeometryTests.cs'))
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'staged-geometry.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Tests.StagedGeometryTests',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+$sources)|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Staged geometry compile failed.'}
&$exe $OutputDirectory;if($LASTEXITCODE-ne0){throw 'Staged geometry regression failed.'}

# The external reader over the recorded plans.
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7CombatEvidence.ps1')
$passed=0;$failed=0
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:passed++}catch{$script:failed++;Write-Output ('FAIL '+$Name+': '+$_.Exception.Message)}}
function Reject([string]$Name,[scriptblock]$Body,[string]$Pattern){$m=$null;try{&$Body}catch{$m=$_.Exception.Message};if($null-ne$m-and$m-match$Pattern){$script:passed++}else{$script:failed++;Write-Output ('FAIL refuses '+$Name+': '+$m)}}
function Read-Plan([string]$Name){Get-Content -LiteralPath (Join-Path $OutputDirectory $Name) -Raw|ConvertFrom-Json}
$committed=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/StagedGeometryPlanFixture.json'
Check 'the committed reader fixture equals the plan the C# planner regenerates' {
 if(-not[IO.File]::Exists($committed)-or(Get-KmcSha256 $committed)-cne(Get-KmcSha256 (Join-Path $OutputDirectory 'fixture-plan.json'))){throw 'The committed staged plan fixture differs from the regenerated plan.'}}
Check 'the reader replays the fixture plan and selects the ninth placement' {$c=Get-KmcChunk7StagedPlanSelection (Read-Plan 'fixture-plan.json').plan;if($null-eq$c-or[double]$c.angle-ne-90.0-or[double]$c.distance-ne8.0){throw 'Wrong selection.'}}
Check 'the reader replays the fixture confirmation as feasible' {$f=Read-Plan 'fixture-plan.json';$c=$f.confirmation
 if(-not(Test-KmcChunk7StagedSimulation $c.simulation @($c.mountStart|ForEach-Object{[double]$_}) @($c.target|ForEach-Object{[double]$_}) ([double]$c.envelope) ([double]$c.speed) 'fixture')){throw 'Infeasible.'}}
Check 'the reader replays b203: every candidate refused, the old placement at its round-3 beyond-range leg' {$p=(Read-Plan 'b203-plan.json').plan
 if($null-ne(Get-KmcChunk7StagedPlanSelection $p)){throw 'b203 selected a placement.'}
 $r=$p.candidates[0].simulation.refusal;if($r.step-ne5-or$r.kind-cne'move-beyond-range'-or$r.term-cne'no-destination'-or@($p.candidates).Count-ne64){throw 'b203 refusal differs.'}}
Check 'the reader replays the disc floor and selects the first feasible (9 m) placement' {$c=Get-KmcChunk7StagedPlanSelection (Read-Plan 'disc-plan.json').plan;if($null-eq$c-or[double]$c.distance-ne9.0){throw 'Wrong selection.'}}
function Get-FixtureLeg($p,[string]$Kind){@($p.candidates[8].simulation.legs|Where-Object kind -CEQ $Kind)[0]}
$mutations=@(
 @('a refused proposal recorded eligible',{param($p)$hit=$null;foreach($l in @($p.candidates[8].simulation.legs)){foreach($q in @($l.proposals)){if($null-eq$hit-and$null-ne$q[12]){$hit=$q}}};if($null-eq$hit){throw 'no refused proposal in the fixture'};$hit[12]=$null},'records  but recomputes'),
 @('a chosen proposal recorded refused',{param($p)$l=Get-FixtureLeg $p 'move-exhaust';$l.proposals[$l.chosen][12]='below-band'},'records below-band but recomputes'),
 @('a moved requested point',{param($p)$l=Get-FixtureLeg $p 'move-two-moves';$l.proposals[0][2]=[double]$l.proposals[0][2]+0.01},'requested point differs'),
 @('a reordered candidate',{param($p)$p.candidates[1].angle=45.0},'candidate order differs'),
 @('a selection past the first feasible',{param($p)$p.selected=7},'first feasible'),
 @('a feasible candidate recorded infeasible',{param($p)$p.candidates[8].refusal='plan-infeasible'},'refusal differs'),
 @('a moved simulated end',{param($p)$l=Get-FixtureLeg $p 'move-beyond-range';$l.end[0]=[double]$l.end[0]+0.01},'end or movement time differs'),
 @('a changed stop-short margin',{param($p)$p.stopShortMargin=0.4},'declared parameters differ'),
 @('a changed planned target footprint',{param($p)$p.plannedTargetCorpulence=0.5},'declared parameters differ'),
 @('a chosen goal recorded unwalkable',{param($p)$l=Get-FixtureLeg $p 'move-short';$l.proposals[$l.chosen][5]=$false},'unwalkable proposal records a trace|recomputes'),
 @('an inexact trace on the chosen goal',{param($p)$l=Get-FixtureLeg $p 'move-exhaust';$q=$l.proposals[$l.chosen];$q[9]=[double]$q[9]+0.31},'recomputes trace-inexact'),
 @('a dropped leg',{param($p)$s=$p.candidates[8].simulation;$s.legs=@($s.legs|Select-Object -First 5)},'missing'),
 @('a candidate recorded past the selection',{param($p)$p.candidates=@($p.candidates)+@($p.candidates[8])},'past the first feasible'),
 @('a missing double-move leg',{param($p)$s=$p.candidates[8].simulation;$legs=@($s.legs);$i=[array]::IndexOf($legs,(Get-FixtureLeg $p 'move-two-moves'));$s.legs=@($legs[0..$i]+$legs[($i+2)..($legs.Count-1)])},'out of place|differs')
)
foreach($m in $mutations){$p=(Read-Plan 'fixture-plan.json').plan;&$m[1] $p;Reject $m[0] {[void](Get-KmcChunk7StagedPlanSelection $p)} $m[2]}
Reject 'a refused plan missing a declared candidate' {$p=(Read-Plan 'b203-plan.json').plan;$p.candidates=@($p.candidates|Select-Object -First 63);[void](Get-KmcChunk7StagedPlanSelection $p)} 'every declared candidate'
Reject 'a refused plan claiming a selection' {$p=(Read-Plan 'b203-plan.json').plan;$p.selected=0;[void](Get-KmcChunk7StagedPlanSelection $p)} 'every declared candidate'
Reject 'a b203 beyond-range proposal recorded eligible' {$p=(Read-Plan 'b203-plan.json').plan;$l=@($p.candidates[0].simulation.legs|Where-Object kind -CEQ 'move-beyond-range')[0];$l.proposals[2][12]=$null;[void](Get-KmcChunk7StagedPlanSelection $p)} 'recomputes below-band'
# Geometry revision 2 (owner-seed-staged-geometry-plan-v2, F6): centre-aimed beyond-range leg with a recorded spare.
$committed2=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/StagedGeometryPlanFixtureV2.json'
Check 'the committed revision 2 reader fixture equals the plan the C# planner regenerates' {
 if(-not[IO.File]::Exists($committed2)-or(Get-KmcSha256 $committed2)-cne(Get-KmcSha256 (Join-Path $OutputDirectory 'fixture-plan-v2.json'))){throw 'The committed revision 2 staged plan fixture differs from the regenerated plan.'}}
Check 'the reader replays the revision 2 fixture and selects the ninth placement' {$c=Get-KmcChunk7StagedPlanSelection (Read-Plan 'fixture-plan-v2.json').plan;if($null-eq$c-or[double]$c.angle-ne-90.0-or[double]$c.distance-ne8.0){throw 'Wrong selection.'}}
Check 'the reader replays a revision 2 plan and its confirmation' {$f=Read-Plan 'open-plan-v2.json';$c=Get-KmcChunk7StagedPlanSelection $f.plan;if($null-eq$c){throw 'No selection.'}
 if(-not(Test-KmcChunk7StagedSimulation $f.confirmation.simulation @($f.confirmation.mountStart|ForEach-Object{[double]$_}) @($f.confirmation.target|ForEach-Object{[double]$_}) ([double]$f.confirmation.envelope) ([double]$f.confirmation.speed) 'v2 confirmation' 2)){throw 'Infeasible.'}}
function Get-V2BeyondLeg($p){@($p.candidates[$p.selected].simulation.legs|Where-Object kind -CEQ 'move-beyond-range')[0]}
$v2mutations=@(
 @('a revision 2 plan replayed under the v1 contract',{param($p)$p.contract='owner-seed-staged-geometry-plan-v1'},'spare field does not match'),
 @('a revision 2 leg without its spare field',{param($p)$l=Get-V2BeyondLeg $p;$l.PSObject.Properties.Remove('spare')},'spare field does not match'),
 @('a moved spare',{param($p)$l=Get-V2BeyondLeg $p;$l.spare=[int]$l.spare+1},'spare is not the next eligible'),
 @('a dropped spare proposal',{param($p)$l=Get-V2BeyondLeg $p;$l.proposals=@($l.proposals|Select-Object -First ([int]$l.spare));$l.spare=$null},'stopped before its first eligible|spare'),
 @('a floor-aimed request under revision 2',{param($p)$l=Get-V2BeyondLeg $p;$l.proposals[0][1]=[double]$l.proposals[0][1]-0.75},'order differs'),
 @('an unknown plan contract',{param($p)$p.contract='owner-seed-staged-geometry-plan-v3'},'lack their up-front geometry plan')
)
foreach($m in $v2mutations){$p=(Read-Plan 'open-plan-v2.json').plan;&$m[1] $p;Reject $m[0] {[void](Get-KmcChunk7StagedPlanSelection $p)} $m[2]}
Write-Output ('Staged geometry reader: '+$passed+' PASS / '+$failed+' FAIL')
if($failed-ne0){throw 'Staged geometry reader regression failed.'}
