[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot,[string]$ClosurePath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve dynamic discovery tests.'}
[void][IO.Directory]::CreateDirectory($OutputRoot);$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
$ownedRoot=Split-Path $PSScriptRoot -Parent
if([string]::IsNullOrEmpty($ClosurePath)){$ClosurePath=Join-Path $ownedRoot 'overlays/scripts/runtime/ScopedPreflightClosure.ps1'}
Add-Type -Path (Join-Path $ownedRoot 'src/ScopedPreflightObservation.cs')
. (Join-Path $ownedRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1');. $ClosurePath
$script:pass=0;$script:fail=0;$script:index=0;$script:rows=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action){try{&$Action;$script:pass++;$script:rows.Add([pscustomobject]@{name=$Name;status='PASS'})}catch{$script:fail++;$script:rows.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()})}}
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Put([string]$Path,[string]$Text){[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path));[IO.File]::WriteAllText($Path,$Text,[Text.UTF8Encoding]::new($false))}
function Pin([string]$Path){[pscustomobject]@{path=$Path;sha256=(Get-FileHash $Path).Hash.ToLowerInvariant();bytes=[long](Get-Item $Path).Length}}
function Json([string]$Path,$Object){Put $Path ($Object|ConvertTo-Json -Depth 16);Pin $Path}
function Fixture([string]$Source='source-run',[string]$Destination='next-run',[bool]$Manual=$false){
 $script:index++;$lab=Join-Path $OutputRoot ('lab'+$script:index);$repo=Join-Path $lab 'repo/KingmakerMountedCombat';$analysis=Join-Path $lab 'analysis-cache';$state=Join-Path $lab 'runtime-state';$game=Join-Path $lab 'game';$save=Join-Path $lab 'save-root';$frozen=Join-Path $lab 'worktrees/frozen'
 foreach($directory in @($repo,$analysis,$state,(Join-Path $lab 'runtime-evidence'),(Join-Path $lab 'runtime-backups'),(Join-Path $lab 'runtime-staging'),(Join-Path $game 'Mods'),$save,(Join-Path $game 'Kingmaker_Data/Managed/UnityModManager'))){[void][IO.Directory]::CreateDirectory($directory)}
 Put (Join-Path $lab 'runtime-backups/historical-opaque.bin') 'unreferenced and must not be observed'
 Put (Join-Path $game 'Kingmaker_Data/Managed/Assembly-CSharp.dll') 'opaque-not-loaded'
 Put (Join-Path $game 'Kingmaker_Data/Managed/UnityModManager/Params.xml') 'read-only-loader-test-bytes'
 Put (Join-Path $game 'Mods/current.bin') 'current-mod';Put (Join-Path $save 'protected-opaque.bin') 'current-save-bytes'
 Put (Join-Path $state 'fixture-qualification.json') '{}'
 Put (Join-Path $frozen 'src/Original.cs') 'immutable-product';Put (Join-Path $repo 'src/Original.cs') 'immutable-product'
 $authority=Json (Join-Path $state 'protected-save-authorities/known.json') ([pscustomobject]@{kind='generated-test-authority'})
 $suite=Json (Join-Path $state 'qualification-suite-snapshots/suite.json') ([pscustomobject]@{suiteId='test-suite';historicalAuthorities=[pscustomobject]@{protectedSaveAuthorities=@([pscustomobject]@{path=$authority.path;sha256=$authority.sha256})}})
 $intake=Json (Join-Path $lab 'environment-intake.json') ([pscustomobject]@{requestedLayout=[pscustomobject]@{kingmakerInstallDir=$game;kingmakerModsRoot=(Join-Path $game 'Mods');kingmakerSaveRoot=$save}})
 $package=Json (Join-Path $lab 'artifacts/opaque-package-test.bin') ([pscustomobject]@{archiveNotCreated=$true})
 $manifest=Json (Join-Path $lab 'artifacts/manifest.json') ([pscustomobject]@{generated=$true})
 $freeze=Json (Join-Path $analysis 'freeze.json') ([pscustomobject]@{generated=$true})
 $selection=[pscustomobject]@{sourceRunId=$Source;sourceArtifactSha256=('a'*64);sourceArchiveSha256=('b'*64)}
 if($Manual){$selection|Add-Member NoteProperty inventoryRunId 'cold-run'}
 $selectionPin=Json (Join-Path $analysis 'selection.json') $selection
 $plan=[pscustomobject]@{schemaVersion=1;kind='closed-chunk7-preflight-case';runId=$Destination;scenario=$(if($Manual){'chunk7-bootstrap-manual-seed'}else{'chunk7-bootstrap-cold-inventory'});baseGameOnly=$true;saveAccessAllowed=$true;selection=$selectionPin;suiteId='test-suite';timeoutSeconds=600}
 $ids=@($Source);if($Manual){$ids+=('cold-run')}
 # Test setup models Win32 trailing-dot normalization explicitly; the production discovery still receives the original hostile ID.
 foreach($id in $ids){$profile=Join-Path $lab ('runtime-staging/'+('persistence-'+$id).TrimEnd('.'));$evidence=[IO.Path]::GetFullPath((Join-Path $lab ('runtime-evidence/'+$id)));Put (Join-Path $profile 'owner.json') '{}';Put (Join-Path $profile 'Saved Games/opaque-input.bin') 'source bytes';Put (Join-Path $evidence 'runtime-artifacts.json') '{}';Put (Join-Path $evidence 'extra-dependency.bin') 'must-be-observed'}
 $proof=[pscustomobject]@{documents=[pscustomobject]@{execution=[pscustomobject]@{repositoryRoot=$repo;frozenSourceRoot=$frozen;environmentIntake=$intake;suite=$suite;package=$package;manifest=$manifest;freeze=$freeze;saveRoot=$save;auditRoots=@($state,(Join-Path $lab 'runtime-backups'),(Join-Path $lab 'runtime-staging'),(Join-Path $lab 'runtime-evidence'),(Join-Path $game 'Mods'))};beforeInputs=[pscustomobject]@{tracked=@([pscustomobject]@{relativePath='src/Original.cs'})}}}
 [pscustomobject]@{lab=$lab;plan=$plan;proof=$proof;selection=$selection;source=$Source}
}
function Discover($Fixture,[scriptblock]$Action){$owned=[Collections.Generic.List[IDisposable]]::new();try{$context=Get-KmcScopedDynamicInputs $Fixture.proof $Fixture.plan $owned;if($Action){&$Action $context};foreach($l in $owned){[void]$l.VerifyUnchanged()}}finally{foreach($l in $owned){$l.Dispose()}}}
function Reject([string]$Name,[scriptblock]$FixtureFactory,[string]$Pattern){Check $Name {$f=&$FixtureFactory;$rejected=$false;try{Discover $f}catch{if($_.Exception.Message-cnotmatch$Pattern){throw};$rejected=$true};Need $rejected 'Invalid dynamic scope was admitted.'}}
$start=[DateTime]::UtcNow
Check 'cold discovery holds full selected source profile/evidence and current roots only' {$f=Fixture;Discover $f {param($c)Need (@($c.scopes|Where-Object role -CEQ 'complete-selected-source-evidence').Count-eq1) 'Source evidence omitted';Need (@($c.lease.Before|Where-Object{$_.Path.EndsWith('extra-dependency.bin')}).Count-eq1) 'Manifest-neighbor input omitted';Need (@($c.lease.Before|Where-Object{$_.Path.Contains('historical-opaque.bin')}).Count-eq0) 'Unreferenced history was traversed'}}
Check 'Manual discovery recursively holds bootstrap and cold source trees' {$f=Fixture -Manual $true;Discover $f {param($c)Need (@($c.scopes|Where-Object role -CEQ 'complete-selected-source-evidence').Count-eq2) 'Manual recursive evidence missing';Need (@($c.scopes|Where-Object role -CEQ 'complete-selected-source-profile').Count-eq2) 'Manual recursive profile missing'}}
Reject 'source dot cannot observe evidence parent tree' {Fixture -Source '.'} 'source identity|child|escaped|run identity'
Reject 'source dotdot cannot observe whole lab' {Fixture -Source '..'} 'source identity|child|escaped|run identity'
Reject 'destination dot is refused before acquisition' {Fixture -Destination '.'} 'run identity'
Reject 'destination dotdot is refused before acquisition' {Fixture -Destination '..'} 'run identity'
Reject 'source trailing-dot Windows alias is refused' {Fixture -Source 'source.'} 'source identity|run identity'
Reject 'destination trailing-dot Windows alias is refused' {Fixture -Destination 'next.'} 'run identity'
Reject 'same source and destination is refused' {Fixture -Source 'same-run' -Destination 'same-run'} 'source identity'
Reject 'active native transaction is refused' {$f=Fixture;Put (Join-Path $f.lab 'runtime-state/active-transaction.lock') 'active';$f} 'active or stale'
Reject 'existing requested evidence is refused' {$f=Fixture;Put (Join-Path $f.lab 'runtime-evidence/next-run/existing.bin') 'preserve';$f} 'already exists'
Reject 'existing requested isolated profile is refused' {$f=Fixture;Put (Join-Path $f.lab 'runtime-staging/persistence-next-run/existing.bin') 'preserve';$f} 'already exists'
Check 'new manifest neighbor during qualification is caught' {$f=Fixture;$refused=$false;try{Discover $f {param($c)Put (Join-Path $f.lab 'runtime-evidence/source-run/new-input.bin') 'new'}}catch{Need ($_.Exception.Message-cmatch'New observed file') 'Wrong dependency growth refusal';$refused=$true};Need $refused 'Dependency growth accepted'}
Check 'new destination during qualification is caught' {$f=Fixture;$refused=$false;try{Discover $f {param($c)[void][IO.Directory]::CreateDirectory((Join-Path $f.lab 'runtime-evidence/next-run'))}}catch{Need ($_.Exception.Message-cmatch'entry set changed|dependency changed') 'Wrong destination refusal';$refused=$true};Need $refused 'Destination mutation accepted'}
Check 'selected source bytes cannot mutate while qualifier runs' {$f=Fixture;Discover $f {param($c)$refused=$false;try{[IO.File]::AppendAllText((Join-Path $f.lab 'runtime-evidence/source-run/extra-dependency.bin'),'mutate')}catch{$refused=$true};Need $refused 'Source input write allowed'}}
Check 'unreferenced historical growth is explicitly outside this observation' {$f=Fixture;Discover $f {param($c)Put (Join-Path $f.lab 'runtime-backups/unreferenced-new.bin') 'not-in-scope';Need ($c.historicalRootsNotReevaluated.Count-eq4) 'Narrow scope not explicit'}}
$end=[DateTime]::UtcNow
[ordered]@{kind='actual-bounded-filesystem-dynamic-discovery-tests';status=$(if($script:fail-eq0){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;seconds=($end-$start).TotalSeconds;rows=@($script:rows);sourceSha256=(Get-FileHash $ClosurePath).Hash.ToLowerInvariant();externalBoundarySubstitutions=@();archivesCreatedOrOpened=0;nativeExecution=$false;fullCaseQualificationProven=$false;scope='Actual discovery and lease methods, generated JSON and opaque private files only; original semantic guards are a separate pending gate'}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Output ("Dynamic closure: {0} PASS / {1} FAIL"-f$script:pass,$script:fail)
if($script:fail-ne0){throw 'Private dynamic closure checks failed.'}
