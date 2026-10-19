[CmdletBinding()]
param([string]$RepositoryRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
$analysis=[IO.Path]::GetFullPath('C:/Dev/KingmakerMountedCombatLab/analysis-cache')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/f09-source-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private F09 source-check directory required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$paths=@('scripts/runtime/Chunk7SmallFixtureEvidence.ps1','scripts/runtime/Chunk7SmallFixtureFixtures.ps1','scripts/chunk7/Test-Chunk7SmallFixtureSource.ps1')
$inputPins=@($paths|ForEach-Object{$p=Join-Path $RepositoryRoot $_;@{path=$p;sha256=(Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant()}})
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureEvidence.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7SmallFixtureFixtures.ps1')
$watch=[Diagnostics.Stopwatch]::StartNew();$script:pass=0;$script:fail=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Action){$caught=$false;try{&$Action}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
$script:realLab=Get-KmcLabRoot;$script:selection=Get-KmcChunk7SmallFixturePins
$qualificationRoot=Join-Path $script:realLab ('runtime-evidence/'+$script:selection.seedQualificationRunId)
$script:originalRequest=[IO.File]::ReadAllText((Join-Path $qualificationRoot 'runtime-request.json'))|ConvertFrom-Json
$script:source=$null
Good 'read-only actual immutable seed/cold/catalogue receipt chains' {
 $script:source=Get-KmcChunk7SmallFixtureSource $script:selection $script:originalRequest.fixture $script:originalRequest
 if($script:source.nativeAuthorityGranted-ne$false-or$script:source.producerAncestry-ne$false-or$script:source.qualification.receiptPins.Count-ne5-or$script:source.inventory.receiptPins.Count-ne5){throw 'F09 source claimed authority or lost historical pins.'}
}
if($null-eq$script:source){throw 'Required real F09 provenance unavailable; preserve failed receipt output.'}
$script:immutablePins=@($script:source.qualification.receiptPins+$script:source.inventory.receiptPins+@([pscustomobject]@{path=$script:source.path;sha256=$script:source.descriptor.sha256;length=$script:source.descriptor.length;lastWriteTimeUtcTicks=$script:source.descriptor.lastWriteTimeUtcTicks}))
$script:realRead=(Get-Command Read-KmcSmallFixturePinnedJson).ScriptBlock;$script:mutation=$null
function Read-KmcSmallFixturePinnedJson([string]$Path,[string]$ExpectedHash,[int]$Maximum=16777216){$v=&$script:realRead $Path $ExpectedHash $Maximum;if($null-ne$script:mutation){&$script:mutation $Path $v.value};$v}
$cases=[ordered]@{
 'cold wrapper restoration absent'={param($p,$v)if($p-ceq(Join-Path $qualificationRoot 'runtime-result.json')){$v.modsRestored=$false}}
 'cold native extra load'={param($p,$v)if($p-ceq(Join-Path $qualificationRoot 'runtime-game-result.json')){$v.loadRequestCount=2}}
 'cold native unexpected save'={param($p,$v)if($p-ceq(Join-Path $qualificationRoot 'runtime-game-result.json')){$v.saveRequestCount=1}}
 'cold manifest artifact role absent'={param($p,$v)if($p-ceq(Join-Path $qualificationRoot 'runtime-artifacts.json')){$v.artifacts=@($v.artifacts|Where-Object kind -CNE 'chunk7-assisted-cold-inventory-evidence')}}
 'cold qualification incomplete'={param($p,$v)if($p-ceq(Join-Path $qualificationRoot 'chunk7-assisted-cold-inventory-evidence.json')){$v.observations.ownerSeedQualification.qualified=$false}}
 'catalogue wrong skeleton'={param($p,$v)if((Split-Path -Leaf $p)-ceq'chunk7-world-evidence.json'){($v.observations.nativeProfileInventory.smallRaces|Where-Object raceGuid -CEQ 'b0c3ef2729c498f47970bb50fa1acd30').presets[0].maleSkeleton.name='MaleHalfling_fat'}}
 'catalogue profile promoted'={param($p,$v)if((Split-Path -Leaf $p)-ceq'chunk7-world-evidence.json'){$v.observations.nativeProfileInventory.poseQualified=$true}}
}
foreach($case in $cases.GetEnumerator()){Bad $case.Key {$script:mutation=$case.Value;try{[void](Get-KmcChunk7SmallFixtureSource $script:selection $script:originalRequest.fixture $script:originalRequest)}finally{$script:mutation=$null}}}
# Execute the actual mutating copy helper only against fresh private analysis
# directories. The existing historical qualifier and runtime-lock owner are the
# two external facades below; real held artifact hashing and CREATE_NEW execute.
$script:virtualLab=Join-Path $OutputDirectory 'private-lab';$script:lockCalls=0;$script:refuseLockAt=0;$script:currentLock=$null
function Get-KmcLabRoot {$script:virtualLab}
function Get-KmcChunk7SmallFixtureSource($Selection,$Fixture,$Candidate){Assert-KmcChunk7SmallFixtureSelection ([pscustomobject]@{scenario='chunk7-small-fixture-preparation';runId='unassigned-f09-copy';smallFixture=$Selection});$script:source}
function Assert-KmcRuntimeLockOwner($Lock){$script:lockCalls++;if($Lock.RunId-cne$script:currentLock.RunId-or$Lock.Token-cne$script:currentLock.Token-or($script:refuseLockAt-gt0-and$script:lockCalls-ge$script:refuseLockAt)){throw 'External runtime-lock owner refused.'};return $true}
function New-CopyCase {
 $run='f09-copy-'+[Guid]::NewGuid().ToString('N');$profile=Join-Path $script:virtualLab ('runtime-staging/persistence-'+$run)
 [void][IO.Directory]::CreateDirectory((Join-Path $profile 'Saved Games'));[void][IO.Directory]::CreateDirectory((Join-Path $profile 'Areas'))
 $lock=[pscustomobject]@{RunId=$run;Token=('a'*64)};$script:currentLock=$lock;$script:lockCalls=0;$script:refuseLockAt=0
 $owner=[pscustomobject]@{runId=$run;transactionToken=$lock.Token;scenario='chunk7-small-fixture-preparation';persistenceCase=$null;sourceSha256=$script:source.descriptor.sha256;saveRoot=(Join-Path $profile 'Saved Games')}
 [IO.File]::WriteAllText((Join-Path $profile 'owner.json'),($owner|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
 [pscustomobject]@{profile=$profile;lock=$lock;owner=$owner}
}
function Copy-Case($C){Copy-KmcChunk7SmallFixtureSource $script:source $C.profile $C.lock $script:selection $script:originalRequest.fixture $script:originalRequest}
Good 'actual private CREATE_NEW provenance copies retain exact bytes and no authority' {$c=New-CopyCase;$copy=Copy-Case $c;foreach($pair in @(@('small-fixture-seed-qualification.json',$script:source.qualification.artifactSha256),@('small-fixture-native-inventory.json',$script:source.inventory.artifactSha256))){if((Get-KmcSha256 (Join-Path $c.profile $pair[0]))-cne$pair[1]){throw 'Private provenance copy differs.'}};if($copy.nativeAuthorityGranted-ne$false-or$copy.producerAncestry-ne$false){throw 'Copy granted authority.'}}
Bad 'existing owner record never overwritten' {$c=New-CopyCase;[IO.File]::WriteAllText((Join-Path $c.profile 'small-fixture-owner-seed.json'),'sentinel');[void](Copy-Case $c)}
Bad 'existing qualification never overwritten' {$c=New-CopyCase;[IO.File]::WriteAllText((Join-Path $c.profile 'small-fixture-seed-qualification.json'),'sentinel');[void](Copy-Case $c)}
Bad 'existing native catalogue never overwritten' {$c=New-CopyCase;[IO.File]::WriteAllText((Join-Path $c.profile 'small-fixture-native-inventory.json'),'sentinel');[void](Copy-Case $c)}
Bad 'foreign owner transaction token' {$c=New-CopyCase;$c.owner.transactionToken='b'*64;[IO.File]::WriteAllText((Join-Path $c.profile 'owner.json'),($c.owner|ConvertTo-Json));[void](Copy-Case $c)}
Bad 'foreign owner scenario' {$c=New-CopyCase;$c.owner.scenario='chunk7-native-progression';[IO.File]::WriteAllText((Join-Path $c.profile 'owner.json'),($c.owner|ConvertTo-Json));[void](Copy-Case $c)}
Bad 'foreign owner seed hash' {$c=New-CopyCase;$c.owner.sourceSha256='0'*64;[IO.File]::WriteAllText((Join-Path $c.profile 'owner.json'),($c.owner|ConvertTo-Json));[void](Copy-Case $c)}
Bad 'foreign profile path' {$c=New-CopyCase;$c.profile=Join-Path $script:virtualLab 'runtime-staging/persistence-other';[void](Copy-Case $c)}
Bad 'runtime lock refusal before copy' {$c=New-CopyCase;$script:refuseLockAt=1;[void](Copy-Case $c)}
Bad 'runtime lock loss after first CREATE_NEW is not hidden' {$c=New-CopyCase;$script:refuseLockAt=2;[void](Copy-Case $c)}
Good 'actual master and all original receipt files remained byte exact' {foreach($pin in $script:immutablePins){Assert-KmcAssistedSettledFile $pin.path $pin 'F09 immutable inputs after private checks'}}
$watch.Stop();$unchanged=$true;foreach($pin in $inputPins){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false}}
if(-not$unchanged){$script:fail++;Write-Host 'FAIL selected source/helper/test inputs changed during execution'}
@{status=$(if($script:fail){'FAIL'}else{'PASS'});pass=$script:pass;fail=$script:fail;elapsedSeconds=$watch.Elapsed.TotalSeconds;nativeExecution=$false;runtimeProfileTouched=$false;masterAndReceiptPins=$script:immutablePins;
 limits='Actual immutable historical receipt qualification; parsed-JSON mutations substitute external receipt content only. Actual provenance copy/held hash/CREATE_NEW uses fresh analysis-cache profiles; historical qualifier and runtime-lock owner are external facades during copy tests. No native authority, real runtime staging or game save writes.';
 inputsUnchanged=$unchanged;sourceHashes=$inputPins}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){exit 1}
