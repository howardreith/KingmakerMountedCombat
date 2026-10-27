[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-progression-source-'+[Guid]::NewGuid().ToString('N'))}
$script:ownedSourceLab=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$script:ownedSourceLab.StartsWith('C:\Dev\KingmakerMountedCombatLab\analysis-cache\',[StringComparison]::OrdinalIgnoreCase)){throw 'Source adapter tests must remain private.'}
[void][IO.Directory]::CreateDirectory($script:ownedSourceLab)
. (Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
function Get-KmcLabRoot {return $script:ownedSourceLab}
$script:originalOwnedRead=(Get-Command Read-KmcJson).ScriptBlock
function Read-KmcJson([string]$Path){$value=&$script:originalOwnedRead $Path;if($script:mutateDuringRead-and$Path-ceq$script:currentSource.source.artifactPath){[IO.File]::AppendAllText($script:currentSource.source.checkpointArtifactPath,' ');$script:mutateDuringRead=$false};return $value}
# The previously qualified owned source is the declared external boundary.
# This test neither reads an archive nor claims to reproduce its full ancestry proof.
function Get-KmcChunk7OwnedPairsSource($Selection,[string]$Case,$Fixture,$Candidate){
 $script:delegated++
 if($script:rejectPrior){throw 'Earlier complete owned lineage refuses.'}
 if(-not[object]::ReferenceEquals($Selection,$script:currentSource.selection.ownedSource)-or$Case-cne'multi-exploration-manual'-or
    -not[object]::ReferenceEquals($Fixture,$script:currentSource.fixture)-or-not[object]::ReferenceEquals($Candidate,$script:currentSource.candidate)){throw 'Caller changed the exact inherited source boundary.'}
 return $script:currentSource.source
}
function Write-OS([string]$Path,$Value){[IO.File]::WriteAllText($Path,(ConvertTo-KmcChunk7Canonical $Value),[Text.UTF8Encoding]::new($false))}
function Pin-OwnedSource($F){
 Write-OS $F.source.checkpointArtifactPath $F.checkpoint;Write-OS $F.source.artifactPath $F.inventory
 $F.selection.sourceArtifactSha256=Get-KmcSha256 $F.source.checkpointArtifactPath;$F.selection.ownedSource.checkpointSourceArtifactSha256=$F.selection.sourceArtifactSha256
 $F.selection.inventoryArtifactSha256=Get-KmcSha256 $F.source.artifactPath;$F.selection.ownedSource.sourceArtifactSha256=$F.selection.inventoryArtifactSha256
 $F.source.checkpointArtifactLength=[long](Get-Item -LiteralPath $F.source.checkpointArtifactPath).Length;$F.source.artifactLength=[long](Get-Item -LiteralPath $F.source.artifactPath).Length
}
function New-OwnedSource([string]$Case='owned-horse-native-level'){
 $f=New-ProgressionFixture $Case;$s=$f.request.progression;$o=$f.artifact.observations;$dir=Join-Path $script:ownedSourceLab ([Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($dir)
 $candidate=[pscustomobject]@{commit=$f.artifact.commit;dllSha256=$f.artifact.dllSha256;dllMvid=$f.artifact.dllMvid}
 $checkpoint=[pscustomobject]@{status='PASS';scenario='chunk7-owned-two-pair-campaign';evidenceKind='chunk7-persistence-evidence';runId=$s.sourceRunId;commit=$candidate.commit;dllSha256=$candidate.dllSha256;dllMvid=$candidate.dllMvid;observations=[pscustomobject]@{processId=123;checkpoint='multi-exploration-manual';nativeWrite=[pscustomobject]@{sha256=$s.sourceArchiveSha256;archive=(Copy-O $o.loadedArchive)}}}
 $target=Copy-O $o.source.inventoryActor
 foreach($item in @(@('raceGuid',('1'*32)),@('dollPresetGuid',('2'*32)),@('dollGender','Male'),@('dollLeftHanded',$false),@('character',[pscustomobject]@{skeleton=[pscustomobject]@{name='ObservedMediumRig';present=$true}}))){$target|Add-Member NoteProperty $item[0] $item[1]}
 $control=Copy-O $target;$control.id=$s.controlActorId
 $inventory=[pscustomobject]@{status='PASS';scenario='chunk7-acquired-manual-derivative';evidenceKind='chunk7-acquired-manual-evidence';runId=$s.inventoryRunId;commit=$candidate.commit;dllSha256=$candidate.dllSha256;dllMvid=$candidate.dllMvid;observations=[pscustomobject]@{selection=[pscustomobject]@{case='preserve-two-acquired-field'};nativeWrite=[pscustomobject]@{sha256=$s.ownedSource.sourceArchiveSha256};fieldWorld=(Copy-O $o.source.inventoryWorld);nativeProfileInventory=[pscustomobject]@{party=@($target,$control)}}}
 $source=[pscustomobject]@{path=(Join-Path $dir 'qualified-native-Manual-not-opened.zks');descriptor=(Copy-O $f.request.persistenceLoad);checkpointArtifactPath=(Join-Path $dir 'checkpoint.json');checkpointArtifactLength=0L;artifactPath=(Join-Path $dir 'acquired.json');artifactLength=0L}
 $result=[pscustomobject]@{selection=$s;candidate=$candidate;fixture=$f.request.fixture;source=$source;checkpoint=$checkpoint;inventory=$inventory}
 Pin-OwnedSource $result;$script:currentSource=$result;$script:delegated=0;$script:rejectPrior=$false;$script:mutateDuringRead=$false;return $result
}
function Read-OwnedSource($F){Get-KmcChunk7ProgressionSource $F.selection $F.fixture $F.candidate}
$script:passes=0
foreach($case in @(Get-KmcChunk7OwnedProgressionCases|Where-Object{-not(Test-KmcChunk7PaidProgressionCase $_)})){Good ($case+' exact inherited delegate and two immutable input roles'){$f=New-OwnedSource $case;$before=@(Get-KmcSha256 $f.source.checkpointArtifactPath;Get-KmcSha256 $f.source.artifactPath);$r=Read-OwnedSource $f;if($script:delegated-ne1-or$r.path-cne$f.source.path-or$r.artifactPath-cne$f.source.checkpointArtifactPath-or$r.inventory.path-cne$f.source.artifactPath-or$r.descriptor.fileName-cne'Manual_404_KMC_C7_WORLD.zks'-or$r.sourceProcessId-ne123){throw 'Exact qualified role projection differs.'};if((ConvertTo-KmcChunk7Canonical $before)-cne(ConvertTo-KmcChunk7Canonical @(Get-KmcSha256 $f.source.checkpointArtifactPath;Get-KmcSha256 $f.source.artifactPath))){throw 'Read-only qualification changed bytes.'}}}
Bad 'complete prior lineage refusal cannot be bypassed'{$f=New-OwnedSource;$script:rejectPrior=$true;[void](Read-OwnedSource $f)}
$mutations=[ordered]@{
 'checkpoint FAIL'={param($f)$f.checkpoint.status='FAIL'}
 'foreign candidate MVID'={param($f)$f.candidate.dllMvid='aaaaaaaa-1111-4111-8111-111111111111'}
 'foreign checkpoint run'={param($f)$f.checkpoint.runId='foreign-run'}
 'checkpoint case singleton'={param($f)$f.checkpoint.observations.checkpoint=@('multi-exploration-manual')}
 'checkpoint native hash replaced'={param($f)$f.checkpoint.observations.nativeWrite.sha256='9'*64}
 'one-owner ancestry substituted'={param($f)$f.inventory.observations.selection.case='preserve-acquired-field'}
 'acquired native hash replaced'={param($f)$f.inventory.observations.nativeWrite.sha256='9'*64}
 'ancestry and checkpoint roles swapped'={param($f)$f.checkpoint.scenario='chunk7-acquired-manual-derivative'}
 'foreign profile'={param($f)$f.checkpoint.observations.nativeWrite.archive.Pairs[0].ProfileId='medium-humanoid-mammoth-v1'}
 'replaced target pet'={param($f)$f.checkpoint.observations.nativeWrite.archive.Pairs[1].Mount.Id='aaaaaaaa-1111-4111-8111-111111111111'}
 'Small target body'={param($f)$f.inventory.observations.nativeProfileInventory.party[0].effectiveSize='Small'}
 'Small control body'={param($f)$f.inventory.observations.nativeProfileInventory.party[1].effectiveSize='Small'}
 'text native XP'={param($f)$f.inventory.observations.nativeProfileInventory.party[0].nativeLevelUpReadiness.experience='20000'}
 'missing native target inventory'={param($f)$f.inventory.observations.nativeProfileInventory.party=@($f.inventory.observations.nativeProfileInventory.party[1])}
 'duplicate native target inventory'={param($f)$f.inventory.observations.nativeProfileInventory.party+=@((Copy-O $f.inventory.observations.nativeProfileInventory.party[0]))}
 'lost field actor'={param($f)$f.inventory.observations.fieldWorld.party=@($f.inventory.observations.fieldWorld.party|Select-Object -Skip 1)}
 'target is main'={param($f)$f.inventory.observations.fieldWorld.mainActorId=$f.selection.actorId}
 'foreign body rig'={param($f)$f.inventory.observations.nativeProfileInventory.party[0].character.skeleton.name='DifferentRig'}
 'body gender singleton'={param($f)$f.inventory.observations.nativeProfileInventory.party[0].dollGender=@('Male')}
}
foreach($entry in $mutations.GetEnumerator()){Bad $entry.Key {$f=New-OwnedSource 'owned-horse-native-retrain-horse';&$entry.Value $f;Pin-OwnedSource $f;[void](Read-OwnedSource $f)}}
Bad 'changed pinned checkpoint bytes' {$f=New-OwnedSource;[IO.File]::AppendAllText($f.source.checkpointArtifactPath,' ');[void](Read-OwnedSource $f)}
Bad 'changed input after first read' {$f=New-OwnedSource;$script:mutateDuringRead=$true;[void](Read-OwnedSource $f)}
Bad 'changed pinned artifact length' {$f=New-OwnedSource;$f.source.artifactLength++;[void](Read-OwnedSource $f)}
Good 'exact owned selection file retains original hash' {$f=New-OwnedSource;$p=Join-Path $script:ownedSourceLab 'selection.json';Write-OS $p $f.selection;$h=Get-KmcSha256 $p;$got=Read-KmcChunk7ProgressionSelectionFile $p 'fresh-progression';if($got.sha256-cne$h-or(Get-KmcSha256 $p)-cne$h){throw 'Immutable selection changed.'}}
# Only lock ownership is substituted. Actual profile-owner, copy/no-overwrite and byte guards execute.
function Assert-KmcRuntimeLockOwner($Lock){if(-not$Lock.Valid){throw 'Synthetic external lock refused.'};return $Lock}
Good 'locked two-input copy preserves exact checkpoint and inventory roles'{
 $f=New-OwnedSource;$source=Read-OwnedSource $f;$run='synthetic-owned-copy';$profile=Join-Path $script:ownedSourceLab ('runtime-staging/persistence-'+$run);[void][IO.Directory]::CreateDirectory($profile)
 $lock=[pscustomobject]@{RunId=$run;Token=('a'*64);Valid=$true};Write-OS (Join-Path $profile 'owner.json') ([pscustomobject]@{scenario='chunk7-native-progression';runId=$run;transactionToken=$lock.Token;sourceSha256=$source.descriptor.sha256})
 Copy-KmcChunk7ProgressionInputs $source $profile $lock
 if((Get-KmcSha256 (Join-Path $profile 'progression-source.json'))-cne$source.artifactSha256-or(Get-KmcSha256 (Join-Path $profile 'progression-inventory.json'))-cne$source.inventory.sha256){throw 'Copied exact evidence roles differ.'}
 Bad 'immutable input copy cannot be overwritten'{Copy-KmcChunk7ProgressionInputs $source $profile $lock}
 $lock.Valid=$false;Bad 'lost lock refuses further copy'{Copy-KmcChunk7ProgressionInputs $source $profile $lock}
}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual caller and JSON/whole-file boundaries, earlier full qualifier and lock admission external; no archive/native')
