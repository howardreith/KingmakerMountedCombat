[CmdletBinding()]
param([string]$RepositoryRoot,[string]$TemplatePath,[string]$OutputDirectory)
# MMR-01 production seat attachment check (reviews R1-F12, R5-F3, R5-F4). Splices the ACTUAL KingmakerMountedPairRuntime attach/restore/
# replacement members, the ACTUAL MountedRiderPoseAdapter rig resolution, baseline lease, saddle calibration, frame, seat and
# settle, notice and deconfiguration members, and the ACTUAL SupportedMountedProfile settled-seat expression into
# tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs (the Unity hierarchy facade of the chunk7 attachment slot harness
# with translation-only world positions), compiles them with the real Domain sources and runs the attach/frame/dismount/
# remount/view-replacement sequence. Offline evidence only (nativeProof=false): the rendered seat stays an owner observation.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/mounted-alpha2-seat-attachment/run-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual source member.'}
function ExtractExpression([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$end=$Text.IndexOf(';',$start);if($end-lt0){throw 'Unterminated actual expression member.'};return $Text.Substring($start,$end-$start+1)}
function ExtractRegion([string]$Text,[string]$First,[string]$Next){$start=$Text.IndexOf($First,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source region absent: '+$First)};if($Text.IndexOf($First,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source region is ambiguous: '+$First)};$end=$Text.IndexOf($Next,$start,[StringComparison]::Ordinal);if($end-lt0){throw ('Actual source region end absent: '+$Next)};return $Text.Substring($start,$end-$start).TrimEnd()}
$runtimePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs';$runtime=[IO.File]::ReadAllText($runtimePath)
$adapterPath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/MountedRiderPoseAdapter.cs';$adapter=[IO.File]::ReadAllText($adapterPath)
$profilePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/SupportedMountedProfile.cs';$profile=[IO.File]::ReadAllText($profilePath)
$domain=@('ScopedTransformAttachmentLease.cs','ScopedPoseBaselineLease.cs','AnimatedSaddlePosition.cs','AnimatedSaddleCalibration.cs','MountedRiderPoseProfile.cs','PoseVector3.cs')|ForEach-Object{Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Domain/'+$_)}
$members=[ordered]@{
 'HORSE_GUID'=(ExtractExpression $profile 'internal const string HorseBlueprintGuid =')
 'SETTLED_SEAT'=(ExtractExpression $profile 'public bool SettlesSeatFromIdlePose =>')
 'ADAPTER_STATE'=(ExtractRegion $adapter 'private ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> baselineLease;' 'internal double? ObserveCurrentAnimatedSeatResidual()')
 'ADAPTER_RESTORE_STATE'=(ExtractRegion $adapter 'public bool BaselineRestoreVerified =>' 'public string BoneInventory =>')
 'ADAPTER_RESIDUE'=(ExtractExpression $adapter 'public bool HasPoseResidue =>')
 'ADAPTER_CONFIGURE'=(Extract $adapter 'public void Configure(UnitEntityView exactRiderView, MountedRiderPoseProfile exactProfile)')
 'ADAPTER_PRIME'=(Extract $adapter 'private void PrimeTimedPosePath()')
 'ADAPTER_SADDLE'=(Extract $adapter 'public void ConfigureAnimatedSaddle(')
 'ADAPTER_CALIBRATION'=(Extract $adapter 'private void AcquireSaddleCalibration(')
 'ADAPTER_SETTLE'=(Extract $adapter 'private void ObserveSettlingSeat()')
 'ADAPTER_NORMALIZED'=(ExtractExpression $adapter 'private PoseVector3 NormalizedSeatSample() =>')
 'ADAPTER_ADVANCE'=(Extract $adapter 'private float SeatAnimationAdvanceSeconds()')
 'ADAPTER_NOTIFY'=(Extract $adapter 'private void NotifySeatCalibration(')
 'ADAPTER_SCALE'=(Extract $adapter 'internal void CompensateMountScale(Transform exactAnchor)')
 'ADAPTER_DECONFIGURE'=(Extract $adapter 'public void Deconfigure()')
 'ADAPTER_UPDATE'=(Extract $adapter 'private void Update()')
 'ADAPTER_LATE_UPDATE'=(Extract $adapter 'private void LateUpdate()')
 'ADAPTER_SEAT'=(Extract $adapter 'private void ApplyAnimatedSeat()')
 'ADAPTER_VALIDATE'=(Extract $adapter 'private void ValidateLiveRig()')
 'ADAPTER_LATCH'=(Extract $adapter 'private void LatchFailure(string message, Exception exception)')
 'ADAPTER_RESOLVE'=(Extract $adapter 'private static bool TryResolveSupportedRig(')
 'ADAPTER_RESOLVE_LEG'=(Extract $adapter 'private static LegBinding ResolveLeg(')
 'ADAPTER_FIND_UNIQUE'=(Extract $adapter 'private static Transform FindUniqueTransform(Transform root, string exactName)')
 'ADAPTER_FIND'=(Extract $adapter 'private static void FindTransforms(Transform current, string exactName, ref Transform found, ref int count)')
 'ADAPTER_TO_POSE'=(Extract $adapter 'private static PoseVector3 ToPose(Vector3 value)')
 'ADAPTER_TO_UNITY'=(Extract $adapter 'private static Vector3 ToUnity(PoseVector3 value)')
 'ADAPTER_TICKS'=(Extract $adapter 'private static double TicksToMicroseconds(long ticks)')
 'ADAPTER_CREATE_LEASE'=(Extract $adapter 'private static ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> CreateBaselineLease()')
 'ADAPTER_LEG_BINDING'=(Extract $adapter 'private sealed class LegBinding')
 'ADAPTER_VECTOR_COMPARER'=(Extract $adapter 'private sealed class BoundedVector3Comparer : IEqualityComparer<Vector3>')
 'ADAPTER_QUATERNION_COMPARER'=(Extract $adapter 'private sealed class BoundedQuaternionComparer : IEqualityComparer<Quaternion>')
 'CONSTRUCTOR'=(Extract $runtime 'public KingmakerMountedPairRuntime(IModLogger logger, DiagnosticSettings settings)')
 'HAS_RESIDUE'=(ExtractExpression $runtime 'public bool HasPresentationAttachmentResidue =>')
 'NO_RESIDUE'=(ExtractExpression $runtime 'public bool NoPreparedPairResidue =>')
 'CUSTODY'=(ExtractExpression $runtime 'private bool AttachmentLeaseCustodyRetained =>')
 'ATTACH'=(Extract $runtime 'public void AttachPresentation(MountedPair pair)')
 'RESTORE'=(Extract $runtime 'public void RestorePresentation(MountedPair pair)')
 'OBSERVE'=(Extract $runtime 'private void ObserveAttachmentBoundary(string phase)')
 'RELEASE_REFERENCES'=(Extract $runtime 'private void TryReleasePreparedReferences()')
 'RELEASE_REPLACEMENT'=(Extract $runtime 'private void ReleaseReplacementRiderViewFromOwnedAnchor()')
 'REQUIRE_PAIR'=(Extract $runtime 'private void RequirePreparedPair(MountedPair pair)')
 'FIND_TRANSFORM'=(Extract $runtime 'private static Transform FindTransform(Transform current, string exactName)')
 'TO_UNITY'=(Extract $runtime 'private static Vector3 ToUnity(PoseVector3 value)')
 'SIBLING_CONTAINER'=(Extract $runtime 'private static object SiblingContainer(Transform transform)')
 'MOVE_TO_CONTAINER'=(Extract $runtime 'private static void MoveToSiblingContainer(Transform transform, object container)')
 'CREATE_RESERVATION'=(Extract $runtime 'private static Transform CreateSiblingReservation()')
 'RESERVATION_EXISTS'=(Extract $runtime 'private static bool SiblingReservationExists(Transform reservation)')
 'DESTROY_RESERVATION'=(Extract $runtime 'private static void DestroySiblingReservation(Transform reservation)')
 'VECTOR_COMPARER'=(Extract $runtime 'private sealed class BoundedVector3Comparer : IEqualityComparer<Vector3>')
 'QUATERNION_COMPARER'=(Extract $runtime 'private sealed class BoundedQuaternionComparer : IEqualityComparer<Quaternion>')
}
$text=[IO.File]::ReadAllText($TemplatePath);foreach($entry in $members.GetEnumerator()){$marker='/* '+$entry.Key+' */';if(-not$text.Contains($marker)){throw ('Template lacks '+$marker)};$text=$text.Replace($marker,$entry.Value)}
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$text)
$exe=Join-Path $OutputDirectory 'actual.exe'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe';if(Test-Path -LiteralPath $vswhere){$compiler=[string](@(& $vswhere -latest -products * -find 'MSBuild\**\Bin\Roslyn\csc.exe')|Select-Object -First 1)}}
if(-not$compiler -or -not(Test-Path -LiteralPath $compiler)){throw 'csc.exe not found.'}
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/out:'+$exe),$actual)+$domain
& $compiler @compile; if($LASTEXITCODE-ne0){throw 'Actual Horse seat attachment did not compile.'}
$timer=[Diagnostics.Stopwatch]::StartNew();$lines=@(& $exe);$code=$LASTEXITCODE;$timer.Stop();$lines|Write-Host
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($runtimePath,$adapterPath,$profilePath,$TemplatePath)+$domain|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
@{passes=$pass;failures=$fail;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false;exitCode=$code;inputs=$inputs;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0 -or $pass-eq0){throw 'Actual Horse seat attachment regressed.'}
Write-Host ('MMR-01 SEAT ATTACHMENT PASS '+$pass+'/0 (offline, not native or visual evidence)')
