[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve namespace evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$homeRoot=Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $homeRoot 'src/ScopedPreflightObservation.cs')
$pass=0;$fail=0;$rows=[Collections.Generic.List[object]]::new();$index=0
function Need([bool]$v,[string]$why){if(-not$v){throw $why}}
function Refuse([scriptblock]$body,[string]$pattern){$caught=$false;try{&$body}catch{if($_.Exception.Message-cnotmatch$pattern){throw};$caught=$true};Need $caught 'Expected refusal absent.'}
function Check($name,[scriptblock]$body){
 $script:index++;$dir=Join-Path $OutputRoot ('case'+$script:index);[void][IO.Directory]::CreateDirectory($dir);$file=Join-Path $dir 'opaque.bin';[IO.File]::WriteAllBytes($file,[byte[]]@(1,2,3))
 $lease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(64,1MB,10000)
 try{&$body $lease $dir $file;$script:pass++;$rows.Add([pscustomobject]@{name=$name;status='PASS'})}catch{$script:fail++;$rows.Add([pscustomobject]@{name=$name;status='FAIL';error=$_.Exception.ToString()})}finally{$lease.Dispose()}
}
$start=[DateTime]::UtcNow
Check 'metadata namespace records exact object/name without claiming byte hashes' {param($l,$d,$f)$l.AddNamespaceTree($d);$l.Seal();$row=@($l.Before|Where-Object Kind -eq 'file-metadata');Need ($row.Count-eq1-and$row[0].Length-eq3-and$null-eq$row[0].Sha256-and$l.BytesRead-eq0) 'Metadata falsely claimed content hash.';[void]$l.VerifyUnchanged()}
Check 'namespace lease blocks content replacement by exact path' {param($l,$d,$f)$l.AddNamespaceTree($d);$l.Seal();Refuse {[IO.File]::WriteAllBytes($f,[byte[]]@(3,2,1))} '(?i)process|access|sharing';[void]$l.VerifyUnchanged()}
Check 'untracked root addition is detected' {param($l,$d,$f)$l.AddNamespaceTree($d);$l.Seal();[IO.File]::WriteAllText((Join-Path $d 'planted.cs'),'x');Refuse {[void]$l.VerifyUnchanged()} 'New observed file|entry set'}
Check 'new ignored subtree is detected' {param($l,$d,$f)$l.AddNamespaceTree($d);$l.Seal();[void][IO.Directory]::CreateDirectory((Join-Path $d 'ignored'));Refuse {[void]$l.VerifyUnchanged()} 'entry set'}
Check 'exact namespace path cannot be renamed while held' {param($l,$d,$f)$l.AddNamespaceTree($d);$l.Seal();Refuse {[IO.Directory]::Move($d,$d+'-moved')} '(?i)process|access|sharing'}
Check 'declared scratch creation/removal allows no transient-no-write claim' {param($l,$d,$f)[IO.File]::Delete($f);$l.AddTree($d);$l.Seal();$child=Join-Path $d 'owned';[void][IO.Directory]::CreateDirectory($child);[IO.File]::WriteAllText((Join-Path $child 'owned.txt'),'temporary');[IO.File]::Delete((Join-Path $child 'owned.txt'));[IO.Directory]::Delete($child);[void]$l.VerifyUnchanged();Need ($l.Before.Count-eq1) 'Scratch final namespace changed.'}
Check 'declared scratch refuses a retained extraction file' {param($l,$d,$f)[IO.File]::Delete($f);$l.AddTree($d);$l.Seal();[IO.File]::WriteAllText((Join-Path $d 'leftover.dll'),'x');Refuse {[void]$l.VerifyUnchanged()} 'New observed file'}
Check 'namespace and full-byte observers remain distinct' {param($l,$d,$f)$l.AddFile($f);$l.Seal();Need ($l.Before[0].Kind-ceq'file'-and$l.Before[0].Sha256.Length-eq64-and$l.BytesRead-eq3) 'Full byte observation regressed.'}
Check 'namespace admission bounds include metadata file lengths' {param($l,$d,$f)$big=Join-Path $d 'too-large';$s=[IO.File]::OpenWrite($big);try{$s.SetLength(2MB)}finally{$s.Dispose()};$l.AddNamespaceTree($d);Refuse {$l.Seal()} 'byte bound'}
[ordered]@{kind='actual-bounded-namespace-and-scratch-observation';status=$(if($fail-eq0){'PASS'}else{'FAIL'});pass=$pass;fail=$fail;seconds=([DateTime]::UtcNow-$start).TotalSeconds;sourceSha256=(Get-FileHash (Join-Path $homeRoot 'src/ScopedPreflightObservation.cs')).Hash.ToLowerInvariant();rows=@($rows);nativeExecution=$false;archiveOperations=0;substitutions=@();claim='per-path before/after identity/names/metadata; explicit byte hashes only for content-reading scopes; no transient namespace or global-no-write proof'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ("Namespace: {0} PASS / {1} FAIL"-f$pass,$fail)
if($fail){throw 'Namespace checks failed.'}
