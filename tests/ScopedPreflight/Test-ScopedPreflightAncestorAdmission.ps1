[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot,[Parameter(Mandatory=$true)][string]$SourcePath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve ancestor admission evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$source=[IO.File]::ReadAllText($SourcePath)
$anchor='directories.Add(current, handle);'
if(([regex]::Matches($source,[regex]::Escape($anchor))).Count-ne1){throw 'Exact owned admission interleave anchor changed.'}
# Test-only scheduling seam immediately after actual lower-directory acquisition.
# Every FileShare/Win32/acquisition/Seal/Dispose method remains actual source.
$instrumented=$source.Replace('public sealed class ScopedPreflightObservation : IDisposable','public sealed class ScopedPreflightObservation : IDisposable').Replace('private readonly Dictionary<string, SafeFileHandle> directories','public static Action<string> AdmissionInterleave;'+[Environment]::NewLine+'        private readonly Dictionary<string, SafeFileHandle> directories').Replace($anchor,$anchor+' if (AdmissionInterleave != null) AdmissionInterleave(current);')
$instrumentedPath=Join-Path $OutputRoot 'actual-source-with-admission-interleave.cs';[IO.File]::WriteAllText($instrumentedPath,$instrumented,[Text.UTF8Encoding]::new($false));Add-Type -Path $instrumentedPath
$outer=Join-Path $OutputRoot 'outer';$parent=Join-Path $outer 'parent';$child=Join-Path $parent 'child';$old=Join-Path $outer 'old-parent'
[void][IO.Directory]::CreateDirectory($child)
$script:attempted=$false;$script:renamed=$false;$script:renameError=$null;$script:accepted=$false;$script:refusal=$null;$script:replacementEscaped=$false
[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::AdmissionInterleave=[Action[string]]{param($path)
 if($path-ceq$child-and-not$script:attempted){
  $script:attempted=$true
  try{[IO.Directory]::Move($parent,$old);$script:renamed=$true;[void][IO.Directory]::CreateDirectory($child)}catch{$script:renameError=$_.Exception.ToString()}
 }
}
$lease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(64,4096,5000)
try{
 $lease.AddTree($child)
 try{$lease.Seal();$script:accepted=$true}catch{$script:refusal=$_.Exception.ToString()}
 if($script:accepted-and$script:renamed){try{[IO.Directory]::Move($child,(Join-Path $parent 'replacement-renamed'));$script:replacementEscaped=$true}catch{}}
}finally{[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::AdmissionInterleave=$null;$lease.Dispose()}
$ok=$script:attempted-and(-not$script:renamed-or-not$script:accepted)
[ordered]@{kind='actual-Win32-ancestor-admission-interleaving';status=$(if($ok){'PASS'}else{'FAIL'});sourcePath=$SourcePath;sourceSha256=(Get-FileHash $SourcePath).Hash.ToLowerInvariant();instrumentedSourceSha256=(Get-FileHash $instrumentedPath).Hash.ToLowerInvariant();testOnlySeam='Schedule an ancestor rename after actual descendant handle acquisition, before its parent handle';attempted=$script:attempted;nativeAncestorRenameSucceeded=$script:renamed;nativeAncestorRenameError=$script:renameError;sealAccepted=$script:accepted;sealRefusal=$script:refusal;replacementChildCouldBeRenamedAfterSeal=$script:replacementEscaped;noNativeOrArchiveOperation=$true}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ('Ancestor interleave '+$(if($ok){'PASS'}else{'FAIL'})+'; actual rename='+$script:renamed+'; Seal='+$script:accepted+'; replacement rename='+$script:replacementEscaped)
if(-not$ok){throw 'The published observation retained a different directory object at the same path.'}
