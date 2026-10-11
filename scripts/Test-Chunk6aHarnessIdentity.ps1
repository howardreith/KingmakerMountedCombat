param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# The qualification harness identity (reader revision and per-file hashes) is bound
# separately from the product candidate, and a pure external-reader change is recognised by a
# strict path classification. Repository-local checks only; no native qualification.
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHarnessIdentity.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checks=0
function Reject([scriptblock]$Action,[string]$Expected){
 $rejected=$false
 try{& $Action}catch{if($_.Exception.Message.IndexOf($Expected,[StringComparison]::Ordinal)-lt0){throw};$rejected=$true}
 if(-not$rejected){throw ('Accepted invalid harness identity: '+$Expected)};$script:checks++
}
function Copy-Json($v){$v|ConvertTo-Json -Depth 20|ConvertFrom-Json}
$files=@(Get-KmcChunk6aReaderFiles $repo)
foreach($required in @('scripts/runtime/RuntimeHarness.Common.ps1','scripts/runtime/Chunk6aSupportingEvidence.ps1','scripts/runtime/NativeActionEconomyEvidence.ps1','scripts/runtime/ChildEntryPreambleEvidence.ps1','scripts/runtime/Chunk6aHarnessIdentity.ps1','scripts/Test-Chunk6aLedger.ps1')){
 if($required -cnotin $files){throw ('Reader file set omits '+$required)};$checks++
}
if(@($files|Select-Object -Unique).Count-ne$files.Count-or(($files|Sort-Object)-join'|')-cne($files-join'|')){throw 'Reader file set is not sorted and unique'};$checks++
$identity=Get-KmcChunk6aHarnessIdentity $repo
if($identity.schemaVersion-ne1-or$identity.revision-cnotmatch'^[0-9a-f]{40}$'-or$identity.readerDigest-cnotmatch'^[0-9a-f]{64}$'){throw 'Harness identity shape differs'};$checks++
$again=Get-KmcChunk6aHarnessIdentity $repo
if($again.readerDigest-cne$identity.readerDigest){throw 'Harness digest is not deterministic'};$checks++
$record=Copy-Json $identity
Assert-KmcChunk6aHarnessIdentity $record $repo;$checks++
$bad=Copy-Json $record;$bad.readers.'scripts/Test-Chunk6aLedger.ps1'='0'*64;$bad.readerDigest=Get-KmcChunk6aReaderDigest $bad.readers
Reject {Assert-KmcChunk6aHarnessIdentity $bad $repo} 'reader revision differs'
$bad=Copy-Json $record;$bad.readerDigest='0'*64
Reject {Assert-KmcChunk6aHarnessIdentity $bad $repo} 'digest does not match'
$bad=Copy-Json $record;$bad.readers.PSObject.Properties.Remove('scripts/runtime/NativeActionEconomyEvidence.ps1');$bad.readerDigest=Get-KmcChunk6aReaderDigest $bad.readers
Reject {Assert-KmcChunk6aHarnessIdentity $bad $repo} 'unrecorded scripts/runtime/NativeActionEconomyEvidence.ps1'
$bad=Copy-Json $record;$bad.readers|Add-Member -NotePropertyName 'scripts/runtime/Removed.ps1' -NotePropertyValue ('1'*64);$bad.readerDigest=Get-KmcChunk6aReaderDigest $bad.readers
Reject {Assert-KmcChunk6aHarnessIdentity $bad $repo} 'removed scripts/runtime/Removed.ps1'
$bad=Copy-Json $record;$bad.revision='HEAD'
Reject {Assert-KmcChunk6aHarnessIdentity $bad $repo} 'exact reader revision'
Reject {Assert-KmcChunk6aHarnessIdentity $null $repo} 'records no qualification harness identity'
# Pure reader paths versus native-side paths.
foreach($pure in @('scripts/runtime/NativeActionEconomyEvidence.ps1','scripts/runtime/Chunk4CoreEvidence.ps1','scripts/Test-Chunk6aLedger.ps1','scripts/Test-NativeActionEconomy.ps1','docs/CHUNK6A-COMBAT-MOUNT.md','planning/RUNTIME-SCENARIO-MATRIX.md','scripts\runtime\ChildEntryPreambleEvidence.ps1')){
 if(-not(Test-KmcChunk6aPureReaderPath $pure)){throw ('Pure reader path refused: '+$pure)};$checks++
}
foreach($native in @('src/KingmakerMountedCombat/Diagnostics/Chunk6aActionEconomyScenario.cs','Info.json','version.json','src/KingmakerMountedCombat/BuildIdentity.cs','scripts/runtime/RuntimeHarness.Common.ps1','scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1','scripts/fixtures/chunk6a-auto-use/case-dismount.json','scripts/Package.ps1','scripts/Build-Local.ps1','src/KingmakerMountedCombat/KingmakerMountedCombat.csproj','LocalGamePaths.props')){
 if(Test-KmcChunk6aPureReaderPath $native){throw ('Native-side path accepted as pure reader: '+$native)};$checks++
}
$head=(& git -C $repo rev-parse HEAD).Trim();$parent=(& git -C $repo rev-parse 'HEAD~1').Trim()
$digest=Get-KmcChunk6aSourceTreeDigest $repo $head
if($digest-cnotmatch'^[0-9a-f]{40}$'-or$digest-cne(& git -C $repo rev-parse ($head+':src')).Trim()){throw 'Source tree digest differs from git'};$checks++
Reject {Get-KmcChunk6aSourceTreeDigest $repo 'HEAD'} 'exact commit'
$change=Test-KmcChunk6aPureReaderChange $repo $parent $head
if($change.changed.Count-lt1-or($change.pure-and$change.violations.Count-ne0)-or(-not$change.pure-and$change.violations.Count-eq0-and$change.changed.Count-gt0)){throw 'Pure reader classification is inconsistent'};$checks++
Reject {Test-KmcChunk6aPureReaderChange $repo 'HEAD~1' $head} 'exact commits'
Write-Output ('HARNESS IDENTITY PASS='+$checks+' FAIL=0; repository-local identity checks only, no native qualification')
