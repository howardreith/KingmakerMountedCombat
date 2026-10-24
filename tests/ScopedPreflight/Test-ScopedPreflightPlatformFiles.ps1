[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve platform lease checks.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$source=Join-Path (Split-Path $PSScriptRoot -Parent) 'src/ScopedPreflightObservation.cs';Add-Type -Path $source -ReferencedAssemblies System.Core
$script:pass=0;$script:fail=0;$script:case=0;$script:rows=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;$script:rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$script:rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Need([bool]$Good,[string]$Message){if(-not$Good){throw $Message}}
function Refuse([scriptblock]$Action,[string]$Pattern){$refused=$false;try{&$Action}catch{if($_.Exception.GetBaseException().Message-cnotmatch$Pattern){throw};$refused=$true};Need $refused 'Expected exact refusal.'}
function Fixture {
 $script:case++;$d=Join-Path $OutputRoot ('fixture'+$script:case);[void][IO.Directory]::CreateDirectory($d)
 $p=Join-Path $d 'platform.bin';$a=Join-Path $d 'alias.bin';[IO.File]::WriteAllText($p,'original',[Text.UTF8Encoding]::new($false));[void](New-Item -ItemType HardLink -Path $a -Target $p)
 $native=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::ReadPlatformIdentity($p)
 [pscustomobject]@{root=$d;path=$p;alias=$a;native=$native;sha=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}
}
function Lease($f){$l=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(100,10000,10000);try{$l.AddPlatformFile($f.path,$f.native.NativeIdentity,$f.native.LinkCount,$f.native.Length,$f.sha);return $l}catch{$l.Dispose();throw}}
$start=[DateTime]::UtcNow
Check 'exact native ID/link count/hash platform file is held' {$f=Fixture;$l=Lease $f;try{$l.Seal();Need ($l.HeldFileCount-eq1) 'No owned file';[void]$l.VerifyUnchanged()}finally{$l.Dispose()}}
foreach($operation in @('write-original','write-alias','truncate-alias','delete-original','rename-original')){
 Check ('held platform native aliases refuse '+$operation){$f=Fixture;$l=Lease $f;try{$l.Seal();$refused=$false;try{switch($operation){'write-original'{[IO.File]::WriteAllText($f.path,'mutation')}'write-alias'{[IO.File]::WriteAllText($f.alias,'mutation')}'truncate-alias'{$s=[IO.File]::Open($f.alias,[IO.FileMode]::Truncate,[IO.FileAccess]::Write,[IO.FileShare]::Read);$s.Dispose()}'delete-original'{[IO.File]::Delete($f.path)}'delete-alias'{[IO.File]::Delete($f.alias)}'rename-original'{[IO.File]::Move($f.path,(Join-Path $f.root 'moved.bin'))}'rename-alias'{[IO.File]::Move($f.alias,(Join-Path $f.root 'moved.bin'))}}}catch{$refused=$true};Need $refused ('Native alias mutation succeeded: '+$operation);[void]$l.VerifyUnchanged()}finally{$l.Dispose()}}
}
Check 'deleting an unheld alias is detected by exact link count' {$f=Fixture;$l=Lease $f;try{$l.Seal();[IO.File]::Delete($f.alias);Refuse {$l.VerifyUnchanged()} 'hardlinked dependency refused';Need ([IO.File]::ReadAllText($f.path)-ceq'original') 'Admitted bytes changed'}finally{$l.Dispose()}}
Check 'unheld alias rename does not imply protection of every alias namespace' {$f=Fixture;$l=Lease $f;try{$l.Seal();[IO.File]::Move($f.alias,(Join-Path $f.root 'renamed-alias.bin'));[void]$l.VerifyUnchanged();Need ([IO.File]::ReadAllText($f.path)-ceq'original') 'Admitted bytes changed'}finally{$l.Dispose()}}
Check 'ordinary source/data hardlinks still refuse' {$f=Fixture;$l=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(100,10000,10000);try{$l.AddFile($f.path);Refuse {$l.Seal()} 'hardlinked dependency refused'}finally{$l.Dispose()}}
Check 'wrong platform link count refuses before consumption' {$f=Fixture;$f.native=[pscustomobject]@{NativeIdentity=$f.native.NativeIdentity;LinkCount=1;Length=$f.native.Length};$l=Lease $f;try{Refuse {$l.Seal()} 'hardlinked dependency refused'}finally{$l.Dispose()}}
Check 'wrong platform native file ID refuses' {$f=Fixture;$other=Fixture;$f.native=$other.native;$l=Lease $f;try{Refuse {$l.Seal()} 'object was replaced'}finally{$l.Dispose()}}
Check 'correct identity but wrong immutable platform bytes refuses' {$f=Fixture;$f.sha='a'*64;$l=Lease $f;try{Refuse {$l.Seal()} 'immutable pin'}finally{$l.Dispose()}}
Check 'same link count and content at replaced path refuses native ID' {$f=Fixture;$replacement=Fixture;[IO.File]::Delete($f.path);[void](New-Item -ItemType HardLink -Path $f.path -Target $replacement.path);[IO.File]::Delete($replacement.alias);$l=Lease $f;try{Refuse {$l.Seal()} 'object was replaced'}finally{$l.Dispose()}}
Check 'link-count change during read lease is denied or detected' {$f=Fixture;$l=Lease $f;try{$l.Seal();$added=$false;try{[void](New-Item -ItemType HardLink -Path (Join-Path $f.root 'new-alias.bin') -Target $f.path);$added=$true}catch{};if($added){Refuse {$l.VerifyUnchanged()} 'hardlinked dependency refused'}else{[void]$l.VerifyUnchanged()}}finally{$l.Dispose()}}
Check 'release ends ownership without changing file or link count' {$f=Fixture;$l=Lease $f;$l.Seal();$l.Dispose();Need ($l.HeldFileCount-eq0-and$l.HeldDirectoryCount-eq0) 'Custody retained';[IO.File]::WriteAllText($f.alias,'released');Need ([IO.File]::ReadAllText($f.path)-ceq'released') 'Alias fixture not native'}
Check 'actual suite aggregate fits explicitly bounded eight GiB cap' {$l=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(32768,8589934592,120000);try{Need (3180712448L-lt8589934592L) 'Recorded suite aggregate no longer fits'}finally{$l.Dispose()}}
Check 'larger than reviewed cap remains refused' {Refuse {[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(32768,8589934593,120000)} 'Bounds must be explicit'}
$end=[DateTime]::UtcNow
[ordered]@{kind='actual-read-only-platform-alias-lease-tests';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=($end-$start).TotalSeconds;rows=@($script:rows);sourceSha256=(Get-FileHash $source).Hash.ToLowerInvariant();actualPlatformFilesMutated=0;nativeExecution=$false;archiveOperations=0;scope='Only generated private non-executable files/hardlinks; no actual system file replacement'}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ('Platform leases: '+$script:pass+' PASS / '+$script:fail+' FAIL');if($script:fail-ne0){throw 'Platform alias checks failed.'}
