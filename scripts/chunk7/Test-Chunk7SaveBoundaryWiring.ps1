$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){&$Body;$script:passed++;Write-Output ('PASS '+$Name)}
function Refuse([string]$Name,[scriptblock]$Body){$failed=$false;try{&$Body}catch{$failed=$true};if(-not$failed){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Output ('PASS refuses '+$Name)}
$path=Join-Path $repo 'scripts/runtime/Test-RuntimeGameResult.ps1';$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors);if($errors.Count){throw 'Result reader parse failed'}
$quota=@($ast.FindAll({param($x)$x-is[Management.Automation.Language.AssignmentStatementAst]-and$x.Left.Extent.Text-ceq'$expectedNativeWrites'},$true));if($quota.Count-ne1){throw 'Exact result quota assignment ambiguous'}
$apply=[scriptblock]::Create($quota[0].Extent.Text)
foreach($owned in @($false,$true)){foreach($cold in @($false,$true)){foreach($worker in @($false,$true)){
 $game=[pscustomobject]@{scenario=if($owned){'chunk7-owned-two-pair-campaign'}elseif($cold){'chunk7-persistence-load'}else{'chunk7-persistence-save'}}
 $request=[pscustomobject]@{persistenceCase=if($worker){'multi-owned-worker-cancel-manual'}else{'multi-nonselected-mount-unsettled-manual'};ownedPairs=[pscustomobject]@{case=if($cold){'persistence-load'}else{'persistence-save'}}}
 $nativeBootstrap=$false;$assistedFixture=$false;$assistedWriteCount=0;$absentKmc=$false;$campaignB=$false;$campaignBWrites=0;$alternating=$false;$expectedNativeWrites=-1
 Check ('actual completion quota '+$owned+'/'+$cold+'/'+$worker){. $apply;$expect=if($cold){0}elseif($worker){2}else{1};if($expectedNativeWrites-ne$expect){throw 'Selected native write quota differs.'}}
}}}
foreach($pair in @(@('chunk7-persistence-load','legacy-mounted-pending-tb-manual',1),@('persistence-p07-save','worker-drain',3),@('persistence-p04-save','combat-mount-unsettled-save',1),@('chunk7-persistence-save','multi-exploration-manual',1),@('chunk7-cumulative-acquisition','',1))){$game=[pscustomobject]@{scenario=$pair[0]};$request=[pscustomobject]@{persistenceCase=$pair[1]};$nativeBootstrap=$false;$assistedFixture=$false;$assistedWriteCount=0;$absentKmc=$false;$campaignB=$false;$campaignBWrites=0;$alternating=$false;$expectedNativeWrites=-1;Check ('unchanged neighboring quota '+$pair[0]+'/'+$pair[1]){. $apply;if($expectedNativeWrites-ne$pair[2]){throw 'Historical quota changed.'}}}
$sourcePath=Join-Path $repo 'scripts/runtime/Chunk7PersistenceSaveFixtures.ps1';$sourceTokens=$null;$sourceErrors=$null;$sourceAst=[Management.Automation.Language.Parser]::ParseFile($sourcePath,[ref]$sourceTokens,[ref]$sourceErrors);if($sourceErrors.Count){throw 'Source qualifier parse failed'}
$countBody=@($sourceAst.FindAll({param($x)$x-is[Management.Automation.Language.FunctionDefinitionAst]-and$x.Name-ceq'Assert-KmcChunk7WorkerSourceCounts'},$true));if($countBody.Count-ne1){throw 'Exact Worker source-count function absent or ambiguous'}
. ([scriptblock]::Create($countBody[0].Extent.Text))
$game=[pscustomobject]@{saveRequestCount=2;workingSaveRequestCount=2;loadRequestCount=1;workingLoadRequestCount=1;unauthorizedSaveRequestCount=0;unauthorizedLoadRequestCount=0;suppressedWorkingSaveRequestCount=0}
Check 'source qualifier actual two-request total'{Assert-KmcChunk7WorkerSourceCounts $game}
foreach($key in @('saveRequestCount','workingSaveRequestCount','loadRequestCount','workingLoadRequestCount','unauthorizedSaveRequestCount','unauthorizedLoadRequestCount','suppressedWorkingSaveRequestCount')){foreach($text in @($false,$true)){$bad=$game|ConvertTo-Json|ConvertFrom-Json;if($text){$bad.$key=[string]$bad.$key}else{$bad.$key++};Refuse ('source count '+$key+' text='+$text){Assert-KmcChunk7WorkerSourceCounts $bad}}}
# Execute the actual extracted qualifier-count function; source/archive APIs are never called.
Write-Host ('SAVE BOUNDARY WIRING PASS='+$passed+' FAIL=0; actual result AST and pure source counters, no native/archive calls.')
