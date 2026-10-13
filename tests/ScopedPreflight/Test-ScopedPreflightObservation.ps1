[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$SourcePath,[Parameter(Mandatory=$true)][string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$clock=[Diagnostics.Stopwatch]::StartNew();$private=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\');$out=[IO.Path]::GetFullPath($OutputRoot)
if(-not$out.StartsWith($private+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $out)){throw 'Observation check needs a fresh private output root.'}
[void][IO.Directory]::CreateDirectory($out);$temp=Join-Path $out 'temp';[void][IO.Directory]::CreateDirectory($temp);$env:TEMP=$temp;$env:TMP=$temp
$utf8=[Text.UTF8Encoding]::new($false);$sha=(Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
Add-Type -Path $SourcePath -ReferencedAssemblies System.Core
$rows=[Collections.Generic.List[object]]::new();$pass=0;$fail=0;$case=0
function Check([string]$Name,[bool]$Good){$script:rows.Add([ordered]@{name=$Name;pass=$Good});if($Good){$script:pass++;Write-Host ('PASS '+$Name)}else{$script:fail++;Write-Host ('FAIL '+$Name)}}
function Refuses([string]$Name,[scriptblock]$Action,[string]$Message){$errorText=$null;try{&$Action}catch{$errorText=$_.Exception.GetBaseException().Message};Check $Name ($null-ne$errorText-and$errorText.Contains($Message))}
function Fresh { $script:case++;$d=Join-Path $out ('case'+$script:case);[void][IO.Directory]::CreateDirectory($d);return $d }
function Put([string]$Path,[string]$Text){[IO.File]::WriteAllText($Path,$Text,$utf8)}
function NewLease([int]$Entries=100,[long]$Bytes=10000){return [KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new($Entries,$Bytes,10000)}
$d=Fresh;$f=Join-Path $d 'one.json';Put $f '{}';$l=NewLease
try{
 $l.AddTree($d);$l.Seal();Check 'one exact file and directory retained' ($l.HeldFileCount-eq1-and$l.HeldDirectoryCount-ge1-and$l.Before.Count-eq2)
 [void]$l.VerifyUnchanged();Check 'unchanged real filesystem verifies' $true
 $pin=(Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash.ToLowerInvariant();$l.RequirePinnedFile($f,2,$pin);Check 'exact immutable pin admitted' $true
 Refuses 'wrong pin refuses before consumer' {$l.RequirePinnedFile($f,2,('0'*64))} 'differs from its immutable pin'
 Refuses 'missing pin refuses' {$l.RequirePinnedFile((Join-Path $d 'absent.json'),2,$pin)} 'differs from its immutable pin'
 Refuses 'bad pin format refuses' {$l.RequirePinnedFile($f,2,'garbage')} 'Expected file pin is malformed'
 $writeFailed=$false;try{Put $f 'XX'}catch{$writeFailed=$true};Check 'held input cannot be overwritten during consumer' ($writeFailed-and[IO.File]::ReadAllText($f)-ceq'{}')
 $deleteFailed=$false;try{[IO.File]::Delete($f)}catch{$deleteFailed=$true};Check 'held input cannot be deleted during consumer' ($deleteFailed-and[IO.File]::Exists($f))
 $renamed=$false;try{[IO.Directory]::Move($d,$d+'-moved');$renamed=$true}catch{};Check 'held ancestor prevents namespace replacement' (-not$renamed-and[IO.Directory]::Exists($d))
 $copy=$l.Before;$copy[0]=$null;Check 'snapshot output cannot alter retained evidence' ($null-ne$l.Before[0])
 Refuses 'cannot add dependency after sealing' {$l.AddFile($f)} 'already sealed or disposed'
}finally{$l.Dispose()}
Check 'all file and directory handles released' ($l.HeldFileCount-eq0-and$l.HeldDirectoryCount-eq0);Put $f 'XX';Check 'native write works after release' ([IO.File]::ReadAllText($f)-ceq'XX');$l.Dispose();Check 'disposal is idempotent' $true
Refuses 'verification after release refuses' {$l.VerifyUnchanged()} 'No active sealed observation'
$d=Fresh;$f=Join-Path $d 'before.json';Put $f 'AA';$pin=(Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash.ToLowerInvariant();$stamp=[IO.File]::GetLastWriteTimeUtc($f);Put $f 'BB';[IO.File]::SetLastWriteTimeUtc($f,$stamp);$l=NewLease
try{$l.AddFile($f);$l.Seal();Refuses 'same-metadata planted mutation before import refuses pin' {$l.RequirePinnedFile($f,2,$pin)} 'differs from its immutable pin'}finally{$l.Dispose()}
$d=Fresh;$f=Join-Path $d 'source.json';Put $f '{}';$l=NewLease
try{$l.AddTree($d);$l.Seal();Put (Join-Path $d 'new-dependency.ps1') '#';Refuses 'new dependency during preflight refuses' {$l.VerifyUnchanged()} 'New observed file appeared'}finally{$l.Dispose()}
$d=Fresh;$f=Join-Path $d 'lock';$l=NewLease
try{$l.AddOptionalFile($f);$l.Seal();Put $f 'new';Refuses 'new transaction sentinel refuses' {$l.VerifyUnchanged()} 'New observed file appeared'}finally{$l.Dispose()}
$d=Fresh;$l=NewLease
try{$l.AddTree($d);$l.Seal();[void][IO.Directory]::CreateDirectory((Join-Path $d 'unexpected'));Refuses 'new empty directory refuses' {$l.VerifyUnchanged()} 'entry set changed'}finally{$l.Dispose()}
$d=Fresh;Put (Join-Path $d 'a') '1';Put (Join-Path $d 'b') '2';$l=NewLease 2 100
try{Refuses 'entry budget refuses with held custody retained' {$l.AddTree($d);$l.Seal()} 'entry bound exceeded';Check 'failed seal retains acquired files for caller cleanup' ($l.HeldFileCount-gt0)}finally{$l.Dispose()};Check 'failed admission cleanup releases all custody' ($l.HeldFileCount-eq0-and$l.HeldDirectoryCount-eq0)
$d=Fresh;Put (Join-Path $d 'big') '12345';$l=NewLease 100 4
try{Refuses 'byte budget refuses without relaxing limit' {$l.AddTree($d);$l.Seal()} 'byte bound exceeded'}finally{$l.Dispose()}
$d=Fresh;$f=Join-Path $d 'absent';$l=NewLease
try{Refuses 'required missing input refuses' {$l.AddFile($f);$l.Seal()} 'dependency is missing'}finally{$l.Dispose()}
$d=Fresh;$f=Join-Path $d 'input';Put $f '{}';$l=NewLease
try{$l.AddTree($d);$l.AddFile($f);$l.Seal();Check 'overlapping declared read scopes share one exact file' ($l.HeldFileCount-eq1);[void]$l.VerifyUnchanged()}finally{$l.Dispose()}
$d=Fresh;$f=Join-Path $d 'input';Put $f '{}';$link=Join-Path $d 'alias';[void](New-Item -ItemType HardLink -Path $link -Target $f);$l=NewLease
try{Refuses 'actual hardlinked input refuses' {$l.AddFile($f);$l.Seal()} 'hardlinked dependency refused'}finally{$l.Dispose()}
$d=Fresh;$target=Join-Path $d 'target';[void][IO.Directory]::CreateDirectory($target);Put (Join-Path $target 'input') '{}';$junction=Join-Path $d 'junction';[void](New-Item -ItemType Junction -Path $junction -Target $target);$l=NewLease
try{Refuses 'actual reparse ancestor refuses' {$l.AddFile((Join-Path $junction 'input'));$l.Seal()} 'Reparse dependency ancestor refused'}finally{$l.Dispose()}
$l=NewLease;try{Refuses 'UNC observation refuses' {$l.AddFile('\\localhost\share\source')} 'literal local file paths';Refuses 'alternate stream observation refuses' {$l.AddFile('C:\example:stream')} 'literal local file paths';Refuses 'relative observation refuses' {$l.AddFile('relative.txt')} 'must be absolute'}finally{$l.Dispose()}
if((Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash.ToLowerInvariant()-cne$sha){throw 'Observation source changed during test.'}
$clock.Stop();$receipt=[ordered]@{schemaVersion=1;status=$(if($fail-eq0){'PASS'}else{'FAIL'});pass=$pass;fail=$fail;elapsedSeconds=$clock.Elapsed.TotalSeconds;sourcePath=$SourcePath;sourceSha256=$sha;rows=@($rows.ToArray());gameOperations=0;archiveOperations=0;rootWrites=0;scope='Actual bounded file handles, directory handles and manifest reads under private analysis-cache; not a completed qualification command'}
[IO.File]::WriteAllText((Join-Path $out 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),$utf8);Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail);if($fail-ne0){exit 1}
