[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/ordinary-save-descriptor-evidence-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh output required'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$descriptorOutput=$OutputDirectory
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixturesOnly
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ManualDerivedEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory (Join-Path $RepositoryRoot 'scripts/runtime') -FixtureDirectory (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics') -FixturesOnly
$script:pass=0;$script:fail=0;$script:lines=[Collections.Generic.List[string]]::new()
function Check-Descriptor([string]$CaseLabel,[scriptblock]$Action,[bool]$Reject=$false){$errorValue=$null;try{&$Action}catch{$errorValue=$_};$ok=if($Reject){$null-ne$errorValue}else{$null-eq$errorValue};if($ok){$script:pass++;$text='PASS '+$CaseLabel}else{$script:fail++;$text='FAIL '+$CaseLabel+': '+$(if($null-eq$errorValue){'unexpected acceptance'}else{$errorValue.ToString()+' '+$errorValue.ScriptStackTrace})};$script:lines.Add($text);Write-Host $text}
function New-DescriptorFixture([string]$Family){switch -CaseSensitive($Family){'ordinary'{New-OrdinaryFixture}'manual'{New-ManualDerivedFixture}'progression'{New-ProgressionFixture}}}
function Probe-Descriptor($Fixture,[string]$Family){switch -CaseSensitive($Family){'ordinary'{$Fixture.artifact.observations.nativeObserver}'manual'{$Fixture.artifact.observations.nativeAcquisition}'progression'{$Fixture.artifact.observations.session.nativeCompletion}}}
function Write-Descriptor($Fixture,[string]$Family){if($Family-ceq'progression'){$Fixture.artifact.observations.session.nativeWrite}else{$Fixture.artifact.observations.nativeWrite}}
function Validate-Descriptor($Fixture,[string]$Family){switch -CaseSensitive($Family){'ordinary'{Assert-KmcChunk7OrdinaryEvidence $Fixture.request $Fixture.artifact}'manual'{Assert-KmcChunk7ManualDerivedEvidence $Fixture.request $Fixture.artifact $Fixture.source}'progression'{Assert-KmcChunk7ProgressionEvidence $Fixture.request $Fixture.artifact}}}
$mutations=[ordered]@{
 'same descriptor'={param($p,$w)$r=@($p.events|Where-Object kind -CEQ 'native-level-autosave-request')[0];$s=@($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared')[0];$s.saveObject=$r.saveObject;$w.writtenObject=$r.saveObject}
 'prepared request alias'={param($p,$w)($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared').requestedSaveObject++}
 'completed requested alias'={param($p,$w)$w.requestedObject++}
 'completed written alias'={param($p,$w)$w.writtenObject++}
 'wrong sequence'={param($p,$w)$w.completionSequence++}
 'wrong prior completion count'={param($p,$w)$w.completedWritesBefore++}
 'prepared name changed'={param($p,$w)($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared').saveName='other'}
 'completed name changed'={param($p,$w)$w.name='other'}
 'copy in native callback'={param($p,$w)($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared').insideObservedCompletion=$true}
 'prepared retains callback actor'={param($p,$w)$s=($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared');$s.completionActorId=$s.actorId}
 'prepared before exit'={param($p,$w)$rows=@($p.events);$temp=$rows[-1];$rows[-1]=$rows[-2];$rows[-2]=$temp;$p.events=$rows}
 'rejected copy'={param($p,$w)($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared').accepted=$false}
}
foreach($family in @('ordinary','manual','progression')){
 Check-Descriptor ($family+' actual request/copy/completion relation') {$f=New-DescriptorFixture $family;Validate-Descriptor $f $family}
 foreach($mutation in $mutations.GetEnumerator()){Check-Descriptor ($family+' refuses '+$mutation.Key) {$f=New-DescriptorFixture $family;$p=Probe-Descriptor $f $family;$w=Write-Descriptor $f $family;&$mutation.Value $p $w;Validate-Descriptor $f $family} $true}
 foreach($field in @('requestedObject','writtenObject','completionSequence','completedWritesBefore')){foreach($form in @('string','array','boolean','double')){Check-Descriptor ($family+' refuses '+$field+' '+$form) {$f=New-DescriptorFixture $family;$w=Write-Descriptor $f $family;$v=$w.$field;$w.$field=switch($form){'string'{[string]$v}'array'{,@($v)}'boolean'{$true}'double'{[double]$v}};Validate-Descriptor $f $family} $true}}
 Check-Descriptor ($family+' refuses name singleton array') {$f=New-DescriptorFixture $family;$p=Probe-Descriptor $f $family;$s=($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared');$s.saveName=@($s.saveName);Validate-Descriptor $f $family} $true
 Check-Descriptor ($family+' refuses prepared request singleton array') {$f=New-DescriptorFixture $family;$p=Probe-Descriptor $f $family;$s=($p.events|Where-Object kind -CEQ 'native-level-autosave-prepared');$s.requestedSaveObject=@($s.requestedSaveObject);Validate-Descriptor $f $family} $true
}
Check-Descriptor 'ordinary existing natural advancement neighbor' {$f=New-OrdinaryFixture $false;Validate-Descriptor $f 'ordinary'}
Check-Descriptor 'Manual-derived existing natural advancement neighbor' {$f=New-ManualDerivedFixture $false;Validate-Descriptor $f 'manual'}
foreach($case in @('horse-native-retrain-horse','horse-native-retrain-mammoth','horse-native-dismiss')){Check-Descriptor ('progression '+$case+' neighbor') {$f=New-ProgressionFixture $case;Validate-Descriptor $f 'progression'}}
Check-Descriptor 'Manual refuses a correlated second completion sequence' {$f=New-ManualDerivedFixture;$f.artifact.observations.nativeWrite.completedWritesBefore=1;$f.artifact.observations.nativeWrite.completionSequence=2;Validate-Descriptor $f 'manual'} $true
Check-Descriptor 'ordinary refuses a singleton-array final callback kind' {$f=New-OrdinaryFixture;$r=$f.artifact.observations.nativeObserver.events[-2];$r.kind=@($r.kind);Validate-Descriptor $f 'ordinary'} $true
$terminal='ORDINARY SAVE DESCRIPTOR READER PASS='+$script:pass+' FAIL='+$script:fail;$script:lines.Add($terminal);Write-Host $terminal
[IO.File]::WriteAllLines((Join-Path $descriptorOutput 'output.log'),$script:lines,[Text.UTF8Encoding]::new($false));$r=[ordered]@{schemaVersion=1;pass=$script:pass;fail=$script:fail;testPath=$PSCommandPath;testSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash.ToLowerInvariant();repositoryRoot=$RepositoryRoot;nativeInvoked=$false;archiveRead=$false;scope='Actual full strict evidence readers with generated JSON only; no qualifier/runner/native admission.'};[IO.File]::WriteAllText((Join-Path $descriptorOutput 'receipt.json'),($r|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false));if($script:fail-ne0){throw 'Descriptor evidence regressions failed; preserved.'}
