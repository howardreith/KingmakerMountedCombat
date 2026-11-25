[CmdletBinding()]
param([string]$RepositoryRoot,[string]$TemplatePath,[string]$OutputDirectory)
# Production-path check of the mounted pair attack's approach and start (reviews R3-F1, R4-F2). Splices the ACTUAL source
# of: the whole MountedPairSingleAttack class (strike admission, position admission, start admission) and its admission enum;
# the MountedPairAttackCommand members on the approach/start paths (own-move registry, pre-start approach for the rider's
# carrier and the horse's native approach, automatic perception guard, OnStart, OnTick, target invalidation, OnEnded,
# TickApproach, Repath, BeginDelegatedMove, StopDelegatedMove, StartChildAttack, end-cause recording, fault ending);
# MountedPatchController.MountedAttackApproachPrefix
# into tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs, compiles them with every real Domain source and runs the
# reviewers' counterexamples with controls on a facade of the native command surface. Offline evidence only (nativeProof=false).
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/mounted-alpha2-attack-path/run-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual source member.'}
function ExtractExpression([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$end=$Text.IndexOf(';',$start);if($end-lt0){throw 'Unterminated actual expression member.'};return $Text.Substring($start,$end-$start+1)}
function ExtractRegion([string]$Text,[string]$First,[string]$Next){$start=$Text.IndexOf($First,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source region absent: '+$First)};if($Text.IndexOf($First,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source region is ambiguous: '+$First)};$end=$Text.IndexOf($Next,$start,[StringComparison]::Ordinal);if($end-lt0){throw ('Actual source region end absent: '+$Next)};return $Text.Substring($start,$end-$start).TrimEnd()}
$integration=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration'
$commandPath=Join-Path $integration 'MountedPairAttackCommand.cs';$command=[IO.File]::ReadAllText($commandPath)
$childPath=Join-Path $integration 'MountedPairSingleAttack.cs';$child=[IO.File]::ReadAllText($childPath)
$patchPath=Join-Path $integration 'MountedPatchController.cs';$patch=[IO.File]::ReadAllText($patchPath)
$domain=@(Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
$members=[ordered]@{
 'ADMISSION_ENUM'=(Extract $child 'internal enum MountedPairNativeAdmissionState')
 'CHILD_ATTACK_CLASS'=(Extract $child 'internal class MountedPairSingleAttack : UnitAttack')
 'APPROACH_PREFIX'=(Extract $patch 'internal static bool MountedAttackApproachPrefix(UnitCommand __instance)')
 'APPROACH_FAILURE'=(Extract $command 'private sealed class ApproachFailure : InvalidOperationException')
 'RECORD_END_CAUSE'=(Extract $command 'internal void RecordEndCause(MountedAttackEndCause cause, string detail,')
 'END_AFTER_FAULT'=(Extract $command 'private void EndAfterFault(Exception exception, string phase)')
 'TARGET_LIFE'=(Extract $command 'private bool TargetLifeAdmits(UnitEntityData unit)')
 'OWN_MOVES_AND_PRE_START'=(ExtractRegion $command 'internal UnitMoveTo DelegatedMove => delegatedMove;' '        // A Hold that arrived while this command waited in the native queue')
 'CAN_MOVE_AFTER_START'=(ExtractExpression $command 'public override bool CanMoveAfterStart =>')
 'ON_START'=(Extract $command 'protected override void OnStart()')
 'ON_TICK'=(Extract $command 'protected override void OnTick()')
 'TARGET_INVALIDATION'=(Extract $command 'private bool TryEndExpectedTargetInvalidation()')
 'ON_ENDED'=(Extract $command 'protected override void OnEnded(bool raiseEvent = true)')
 'TICK_APPROACH'=(Extract $command 'private void TickApproach()')
 'REPATH'=(Extract $command 'private bool Repath(string cause, bool targetDrift = false)')
 'BEGIN_DELEGATED'=(Extract $command 'private bool BeginDelegatedMove()')
 'STOP_DELEGATED'=(Extract $command 'private void StopDelegatedMove(bool requireSuccess)')
 'FORBID_APPROACH'=(Extract $command 'internal void ForbidApproach()')
 'SUCCESSOR_STEERS'=(Extract $command 'private bool SuccessorSteersMount(UnitCommands commands, UnitMoveTo exactMove)')
 'DEFERRED_CARRIER'=(ExtractRegion $command '        // PR-1 review R1-F4: a carrier waiting behind older work.' '        private void TickApproach()')
 'APPROACHING_BEFORE_ATTACK'=(ExtractRegion $command '        // PR-1 review R1-F8: also before native start' '        internal bool ValidateNativeSequenceTarget()')
 'START_CHILD'=(Extract $command 'private void StartChildAttack()')
 'OBSERVE_ADMISSION'=(Extract $command 'private void ObserveNativeAdmission(MountedPairNativeAdmissionState state)')
}
$text=[IO.File]::ReadAllText($TemplatePath);foreach($entry in $members.GetEnumerator()){$marker='/* '+$entry.Key+' */';if(-not$text.Contains($marker)){throw ('Template lacks '+$marker)};$text=$text.Replace($marker,$entry.Value)}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$text)
$exe=Join-Path $OutputDirectory 'actual.exe'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe';if(Test-Path -LiteralPath $vswhere){$compiler=[string](@(& $vswhere -latest -products * -find 'MSBuild\**\Bin\Roslyn\csc.exe')|Select-Object -First 1)}}
if(-not$compiler -or -not(Test-Path -LiteralPath $compiler)){throw 'csc.exe not found.'}
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/out:'+$exe),$actual)+$domain
& $compiler @compile; if($LASTEXITCODE-ne0){throw 'Actual attack path members did not compile.'}
$timer=[Diagnostics.Stopwatch]::StartNew();$lines=@(& $exe);$code=$LASTEXITCODE;$timer.Stop();$lines|Write-Host
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($commandPath,$childPath,$patchPath,$TemplatePath)+$domain|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
@{passes=$pass;failures=$fail;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false;exitCode=$code;inputs=$inputs;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0 -or $pass-eq0 -or $fail-ne0){throw 'Actual attack path harness regressed.'}
Write-Host ('MOUNTED ATTACK PATH HARNESS PASS '+$pass+'/0 (offline, not native evidence)')
