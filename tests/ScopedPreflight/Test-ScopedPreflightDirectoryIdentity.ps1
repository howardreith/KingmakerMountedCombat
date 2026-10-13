[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot,[Parameter(Mandatory=$true)][string]$SourcePath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve directory-identity check evidence.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
Add-Type -Path $SourcePath
$type=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation];$flags=[Reflection.BindingFlags]'NonPublic,Instance'
$fixture=Join-Path $OutputRoot 'fixture';$other=Join-Path $OutputRoot 'different-object';[void][IO.Directory]::CreateDirectory($fixture);[void][IO.Directory]::CreateDirectory($other)
$lease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(64,4096,5000);$replacement=$null;$otherLease=$null;$original=$null;$rejected=$false;$refusal=$null
try{
 $lease.AddTree($fixture);$lease.Seal()
 $directoryMap=$type.GetField('directories',$flags).GetValue($lease);$original=$directoryMap[$fixture]
 $otherLease=[KingmakerMountedCombat.RuntimeTooling.ScopedPreflightObservation]::new(64,4096,5000);$otherLease.AddTree($other);$otherLease.Seal()
 $replacement=$type.GetField('directories',$flags).GetValue($otherLease)[$other]
 # Inject only the post-admission alias state hypothesized in review. Both native
 # directory objects/handles are real. This is not evidence an OS race occurred.
 $directoryMap[$fixture]=$replacement
 try{[void]$lease.VerifyUnchanged()}catch{$rejected=$true;$refusal=$_.Exception.ToString()}
 $directoryMap[$fixture]=$original;$replacement=$null
 [void]$lease.VerifyUnchanged()
}finally{if($original){$directoryMap[$fixture]=$original};$lease.Dispose();if($otherLease){$otherLease.Dispose()}}
[ordered]@{kind='actual-directory-identity-verification-with-owned-state-injection';status=$(if($rejected){'PASS'}else{'FAIL'});sourceSha256=(Get-FileHash $SourcePath).Hash.ToLowerInvariant();aliasRefused=$rejected;refusal=$refusal;nativeDirectoryObjects=$true;observedFilesystemRace=$false;testOnlySubstitution='Retained path dictionary points to a real different owned directory handle';originalStateRestoredAndVerified=$true}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ('Directory retained alias '+$(if($rejected){'PASS refusal'}else{'FAIL accepted'}))
if(-not$rejected){throw 'Retained directory alias was not checked.'}
