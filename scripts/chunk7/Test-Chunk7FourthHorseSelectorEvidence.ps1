[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$run='20261022-c7b-world179-b179s1-fourthhorse-cold'
$root=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$run)
$request=Read-KmcJson (Join-Path $root 'runtime-request.json')
$artifact=Read-KmcJson (Join-Path $root 'chunk7-assisted-cold-inventory-evidence.json')
$source=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world178-b178s1-fourthhorse-manual/chunk7-world-evidence.json')
function CloneJson($value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $value -Depth 90 -Compress)}
function Case([bool]$required){
 $r=CloneJson $request;$a=CloneJson $artifact
 $r.assistedColdInventory.case='fourth-horse-open-selector-inventory'
 $a.observations.selection.case=$r.assistedColdInventory.case
 $o=$a.observations
 $p=[pscustomobject][ordered]@{contract='installed-native-open-selector-optionality-v1';actorId='8265f8cf-0969-4ec7-a266-2448de91c8c4';
  action='native-ShowPartySelection';openCount=1;switchCount=0;goCount=0;observedFrame=$o.loadedWorld.frame;
  readyAlpha=1.0;activeInHierarchy=$true;selectedActorIds=@($o.loadedWorld.partyCharacterIds);
  mainCharacter=$false;lockedCompanion=$false;mustBeInParty=$required;requiredByOpenedSelector=$required;
  eligibleForNativeRemoval=(-not$required);closeCount=1;closed=$true}
 $o|Add-Member NoteProperty selectorProbe $p
 [pscustomobject]@{request=$r;artifact=$a}
}
function Verify($c){Assert-KmcChunk7ThirdHorseColdEvidence $c.request $c.artifact $source}
function Refuse([string]$label,[scriptblock]$change){
 $c=Case $false;&$change $c
 try{Verify $c}catch{Write-Host ('PASS '+$label);return}
 throw ('Selector reader accepted '+$label)
}
Assert-KmcChunk7ThirdHorseColdEvidence $request $artifact $source
Write-Host 'PASS unchanged batch179 fourth-Horse cold reader shape remains valid'
foreach($required in @($false,$true)){ $c=Case $required;Verify $c }
Write-Host 'PASS both optional and required native selector outcomes preserve their own meaning'
Refuse 'missing opened selector observation' {param($c)$c.artifact.observations.PSObject.Properties.Remove('selectorProbe')}
Refuse 'Go or roster mutation claim' {param($c)$c.artifact.observations.selectorProbe.goCount=1}
Refuse 'another selected actor' {param($c)$c.artifact.observations.selectorProbe.selectedActorIds[2]='00000000-0000-0000-0000-000000000001'}
Refuse 'contradictory native required result' {param($c)$c.artifact.observations.selectorProbe.mustBeInParty=$true}
Refuse 'selector left open' {param($c)$c.artifact.observations.selectorProbe.closed=$false}
Write-Host 'FOURTH-HORSE SELECTOR READER PASS=8 FAIL=0 (one retained original plus synthetic reader cases; no new native verdict)'
