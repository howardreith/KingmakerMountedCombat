[CmdletBinding()]
param()
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot '../runtime/RuntimeHarness.Common.ps1')
$script:passed=0;$script:failed=0
function Fixture {
 $entries=@('80c140c44a9dfd54ebecd0829d2dd1e8','0fbacd33cce6908409a8d38f92dec7c9','00becd5302e6d1d4a8ad93b6c612b927')
 [pscustomobject]@{contract='owner-seed-stock-area-route-observation-v1';loadedAreaGuid='b10e86516b6437d45a629931c30936fe';loadedAreaName='SpiderCave';enumeration='loaded active game objects; no inactive or unloaded scene claim';maximumTransitions=64;observedTransitions=1;complete=$true;interactionRequests=0;travelRequests=0;saveRequests=0;candidates=@($entries|ForEach-Object{[pscustomobject]@{requestedGuid=$_;resolved=$true;entryGuid=$_;entryName='GeneratedEntry';areaGuid='093f2a5b5f4195a41a73ca7a6fa2f8bc';areaName='SpiderCaveIndoor'}});transitions=@([pscustomobject]@{instanceId=-123;name='GeneratedTransition';enabled=$true;activeAndEnabled=$true;activeInHierarchy=$true;position=[pscustomobject]@{x=1.0;y=2.0;z=3.0};blueprintGuid=('a'*32);entryGuid=$entries[0];destinationAreaGuid='093f2a5b5f4195a41a73ca7a6fa2f8bc';destinationAreaName='SpiderCaveIndoor';autoSaveMode='AfterEntry'})}
}
function Check([string]$Name,[bool]$Expected,[scriptblock]$Change){
 $r=Fixture;$accepted=$true;$detail=''
 try{& $Change $r;Assert-KmcChunk7StockAreaRouteObservation $r 'b10e86516b6437d45a629931c30936fe'}catch{$accepted=$false;$detail=$_.Exception.Message}
 if($accepted-eq$Expected){$script:passed++;Write-Host "PASS $Name"}else{$script:failed++;Write-Host "FAIL $Name ($detail)"}
}
Check 'bounded read-only active authored transition' $true {param($r)}
Check 'zero active transitions is discovery without a route claim' $true {param($r)$r.transitions=@();$r.observedTransitions=0}
Check 'unresolved candidate records no fabricated destination' $true {param($r)$c=$r.candidates[0];$c.resolved=$false;foreach($k in @('entryGuid','entryName','areaGuid','areaName')){$c.$k=$null}}
Check 'foreign loaded area refused' $false {param($r)$r.loadedAreaGuid='b'*32}
Check 'incomplete scan refused' $false {param($r)$r.complete=$false}
Check 'overflow refused' $false {param($r)$r.observedTransitions=65}
Check 'truncated array refused' $false {param($r)$r.observedTransitions=0}
Check 'duplicate transition identity refused' $false {param($r)$r.transitions+=,$r.transitions[0];$r.observedTransitions=2}
Check 'inactive flag mismatch refused' $false {param($r)$r.transitions[0].activeInHierarchy=$false}
Check 'nonfinite position refused' $false {param($r)$r.transitions[0].position.x=[double]::NaN}
Check 'text coordinate refused' $false {param($r)$r.transitions[0].position.x='1.0'}
Check 'transition without entry cannot claim destination' $false {param($r)$r.transitions[0].entryGuid=$null}
Check 'candidate identity substitution refused' $false {param($r)$r.candidates[0].entryGuid=$r.candidates[1].entryGuid}
Check 'unresolved candidate cannot claim metadata' $false {param($r)$r.candidates[0].resolved=$false}
foreach($k in @('interactionRequests','travelRequests','saveRequests')){Check "native action claim $k refused" $false ([scriptblock]::Create('param($r)$r.'+$k+'=1'))}
Check 'unknown observation property refused' $false {param($r)$r|Add-Member NoteProperty invented $true}
Check 'array owner identity refused' $false {param($r)$r.transitions[0].entryGuid=@($r.transitions[0].entryGuid)}
Write-Host "TOTAL PASS=$script:passed FAIL=$script:failed"
if($script:failed){exit 1}
