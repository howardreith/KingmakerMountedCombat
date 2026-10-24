[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$cold='20261022-c7b-world179-b179s1-fourthhorse-cold'
$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$cold+'/runtime-request.json'))
$source=Get-KmcChunk7F09NineActorSource $request.fixture
if($source.producerRunId-cne'20261022-c7b-world178-b178s1-fourthhorse-manual'-or
   $source.coldRunId-cne$cold-or$source.descriptor.fileName-cne'Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'-or
   @($source.rosterIds).Count-ne9-or@($source.characterIds).Count-ne5-or@($source.horseFacts).Count-ne4-or
   $source.nativeFreeFieldCharacterSlots-ne1-or$source.nativeHireCostNow-ne18000-or
   $source.fixtureSetupOnly-ne$true-or$source.nativeAuthorityGranted-ne$false){
 throw 'Nine-actor F09 source granted unproven native authority or lost its exact roster.'
}
$wrong=$request.fixture|ConvertTo-Json -Depth 32|ConvertFrom-Json
$wrong.working.sha256='f'*64
$refused=$false
try{[void](Get-KmcChunk7F09NineActorSource $wrong)}catch{$refused=$true}
if(-not$refused){throw 'Nine-actor F09 source admitted a changed Working anchor.'}
$selector=Get-KmcChunk7F09SelectorOptionalitySource $request.fixture
if($selector.selectorRunId-cne'20261022-c7b-world180-b180s1-fourthhorse-selector'-or
   $selector.optionalActorId-cne'8265f8cf-0969-4ec7-a266-2448de91c8c4'-or
   $selector.eligibleForNativeRemoval-ne$true-or$selector.nativeRosterTransitionProven-ne$false-or
   $selector.fixtureSetupOnly-ne$true-or$selector.nativeAuthorityGranted-ne$false-or
   $selector.nineActorSource.descriptor.sha256-cne$source.descriptor.sha256){
 throw 'Selector optionality source granted unproved native roster/build authority.'
}
$refused=$false
try{[void](Get-KmcChunk7F09SelectorOptionalitySource $wrong)}catch{$refused=$true}
if(-not$refused){throw 'Selector optionality source admitted a changed Working anchor.'}
Write-Host 'F09 NINE-ACTOR/SELECTOR SOURCE PASS=4 FAIL=0 (nativeInvoked=false; setup only)'
