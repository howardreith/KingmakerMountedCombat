[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory)
# Reader vocabulary for the assisted story driver's selected native dialog kinds:
# common PC dialogues (pc-cached-rows), book pages (book-rows) and interchapter pages
# (interchapter-rows). Synthetic snapshots only; no native route, archive or runner.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
$kindsRuntime=[IO.Path]::GetFullPath($RuntimeDirectory)
. (Join-Path $kindsRuntime 'RuntimeHarness.Common.ps1')
. (Join-Path $kindsRuntime 'Chunk7AssistedFixtureEvidence.ps1')
$script:kindsPasses=0
function Good([string]$Name,[scriptblock]$Action){&$Action;$script:kindsPasses++;Write-Host ('PASS '+$Name)}
function Bad([string]$Name,[scriptblock]$Action){$refused=$false;try{&$Action}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:kindsPasses++;Write-Host ('PASS rejects '+$Name)}
$setup=[pscustomobject]@{runId='story-kinds';gameId='11111111-1111-4111-8111-111111111111';gameName='KMC owned';mainActorId=('1'*32);authorityGeneration=1}
function Snapshot([int]$Kind,[string]$Status,[bool]$InterchapterRows){
 $identity=[pscustomobject]@{runId=$setup.runId;gameId=$setup.gameId;gameName=$setup.gameName;mainActorId=$setup.mainActorId;generation=1;gameMatches=$true;playerMatches=$true;saveManagerMatches=$true;mainMatches=$true;descriptorMatches=$true;authorityMatches=$true;exactIsolatedFreshCampaign=$true;noForeignOrMountedCustody=$true}
 [pscustomobject]@{frame=10;elapsedMilliseconds=5;mode='Dialog';uiStatus=$Status;dialogGuid=('a'*32);cueGuid=('b'*32);managerKind=$Kind;managerShown=$true;pcActive=$true;identity=$identity;
  controller=1;dialog=2;cue=3;ui=4;common=5;manager=6;pcDialog=7;part=8;offersList=9;rowsList=10;
  nativeBusy=$false;transitionPending=$false;dialogActive=$true;unexpectedPopup=$false;mouseController=$true;cutscene=$false;mainViewReady=$true;
  offerObjects=@(11);offerSelectionObjects=@(12);offerGuids=@(('c'*32));offerSelectionTypes=@(3);
  rowObjects=@(13);rowAnswerObjects=@(11);rowNativeIndices=@(1);rowInitialized=@($true);rowDisabled=@($false);rowChoosingCharacter=@($false);rowInterchapter=@($InterchapterRows);rowActiveAndEnabled=@($true);rowActiveInHierarchy=@($true)}
}
Good 'common selected snapshot' {Assert-KmcAssistedStorySnapshot (Snapshot 0 'pc-cached-rows' $false) $setup $true}
Good 'book selected snapshot' {Assert-KmcAssistedStorySnapshot (Snapshot 1 'book-rows' $false) $setup $true}
Good 'interchapter selected snapshot with interchapter rows' {Assert-KmcAssistedStorySnapshot (Snapshot 2 'interchapter-rows' $true) $setup $true}
Good 'interchapter selected snapshot with plain rows' {Assert-KmcAssistedStorySnapshot (Snapshot 2 'interchapter-rows' $false) $setup $true}
Good 'unselected snapshot keeps an unsupported kind' {Assert-KmcAssistedStorySnapshot (Snapshot 3 'unsupported-dialog-kind' $false) $setup $false}
Bad 'common status under the interchapter kind' {Assert-KmcAssistedStorySnapshot (Snapshot 2 'pc-cached-rows' $true) $setup $true}
Bad 'interchapter status under the common kind' {Assert-KmcAssistedStorySnapshot (Snapshot 0 'interchapter-rows' $false) $setup $true}
Bad 'book status under the common kind' {Assert-KmcAssistedStorySnapshot (Snapshot 0 'book-rows' $false) $setup $true}
Bad 'unsupported kind three selected' {Assert-KmcAssistedStorySnapshot (Snapshot 3 'unsupported-dialog-kind' $false) $setup $true}
Bad 'missing kind selected' {$s=Snapshot 0 'pc-cached-rows' $false;$s.managerKind=$null;Assert-KmcAssistedStorySnapshot $s $setup $true}
Bad 'interchapter rows under the common kind' {Assert-KmcAssistedStorySnapshot (Snapshot 0 'pc-cached-rows' $true) $setup $true}
Bad 'interchapter rows under the book kind' {Assert-KmcAssistedStorySnapshot (Snapshot 1 'book-rows' $true) $setup $true}
Bad 'chooser rows under the interchapter kind' {$s=Snapshot 2 'interchapter-rows' $true;$s.rowChoosingCharacter=@($true);Assert-KmcAssistedStorySnapshot $s $setup $true}
Write-Host ('ASSISTED STORY KINDS PASS='+$script:kindsPasses+' FAIL=0; reader vocabulary only, no native route')
