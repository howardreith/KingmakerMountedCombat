[CmdletBinding()]
param([string]$RepositoryRoot,[string]$TemplatePath,[string]$OutputDirectory)
# Actual-method check of the mounted runtime's sibling slot custody: the real
# presentation attach/restore, replacement release, residue gates and Unity
# adapter operations, compiled with the real domain lease against a facade of
# Unity's hierarchy semantics. Offline evidence only; never native qualification.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AttachmentSlotRuntimeTemplate.cs'}
if(-not$OutputDirectory){$lab=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot));$OutputDirectory=Join-Path $lab ('analysis-cache/chunk7-continuation/attachment-slot-runtime-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};if($Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual source member is ambiguous: '+$Signature)};$brace=$Text.IndexOf('{',$start);$depth=0;for($i=$brace;$i-lt$Text.Length;$i++){if($Text[$i]-eq'{'){$depth++}elseif($Text[$i]-eq'}'){$depth--;if($depth-eq0){return $Text.Substring($start,$i-$start+1)}}};throw 'Unbalanced actual source member.'}
function ExtractExpression([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual source member absent: '+$Signature)};$end=$Text.IndexOf(';',$start);if($end-lt0){throw 'Unterminated actual expression member.'};return $Text.Substring($start,$end-$start+1)}
$runtimePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs';$runtime=[IO.File]::ReadAllText($runtimePath)
$leasePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain/ScopedTransformAttachmentLease.cs'
$members=[ordered]@{
 'CONSTRUCTOR'=(Extract $runtime 'public KingmakerMountedPairRuntime(IModLogger logger, DiagnosticSettings settings)')
 'HAS_RESIDUE'=(ExtractExpression $runtime 'public bool HasPresentationAttachmentResidue =>')
 'NO_RESIDUE'=(ExtractExpression $runtime 'public bool NoPreparedPairResidue =>')
 'CUSTODY'=(ExtractExpression $runtime 'private bool AttachmentLeaseCustodyRetained =>')
 'RISK_STATE'=(Extract $runtime 'public string PresentationAttachmentRiskState')
 'ATTACH'=(Extract $runtime 'public void AttachPresentation(MountedPair pair)')
 'RESTORE'=(Extract $runtime 'public void RestorePresentation(MountedPair pair)')
 'OBSERVE'=(Extract $runtime 'private void ObserveAttachmentBoundary(string phase)')
 'CLEAR_PREPARED'=(Extract $runtime 'public void ClearPreparedPairWhenUnmounted()')
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
$exe=Join-Path $OutputDirectory 'actual.exe';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$compile=@('/nologo','/langversion:7.3','/target:exe','/warn:0',('/out:'+$exe),$actual,$leasePath)
& $compiler @compile; if($LASTEXITCODE-ne0){throw 'Actual attachment slot runtime did not compile.'}
$timer=[Diagnostics.Stopwatch]::StartNew();$lines=@(& $exe);$code=$LASTEXITCODE;$timer.Stop();$lines|Write-Host
$pass=@($lines|Where-Object{$_-cmatch'^PASS '}).Count;$fail=@($lines|Where-Object{$_-cmatch'^FAIL '}).Count
$inputs=@($runtimePath,$leasePath,$TemplatePath)|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
@{passes=$pass;failures=$fail;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false;exitCode=$code;inputs=$inputs;actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($code-ne0 -or $pass-eq0){throw 'Actual attachment slot runtime custody regressed.'}
