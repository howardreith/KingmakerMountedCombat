[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$run='20261022-c7b-world183-b183s1-linzi-departure'
$set=Get-KmcChunk7OwnedReceiptSet $run 'chunk7-assisted-cold-inventory-evidence.json'
$fixture=$set.request.fixture
Assert-KmcChunk7F09SixthHireOriginalReceipts $set $fixture
$passed=1
function CopyValue($value){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $value -Depth 90 -Compress)}
foreach($mutation in @('outer promoted','foreign error','native failure','second load','write','restoration failed','wrong digest','qualification flags promoted','foreign artifact','changed Working','changed Baseline')){
 $wrong=[pscustomobject]@{root=$set.root;profile=$set.profile;pins=$set.pins.Clone();
  request=(CopyValue $set.request);result=(CopyValue $set.result);game=(CopyValue $set.game);
  owner=(CopyValue $set.owner);isolation=(CopyValue $set.isolation)}
 $anchor=CopyValue $fixture
 switch($mutation){
  'outer promoted'{$wrong.result.status='PASS'}
  'foreign error'{$wrong.result.errors=@('other native failure')}
  'native failure'{$wrong.game.status='FAIL'}
  'second load'{$wrong.game.loadRequestCount=2}
  'write'{$wrong.game.saveRequestCount=1}
  'restoration failed'{$wrong.result.modsRestored=$false}
  'wrong digest'{$wrong.result.restoredSaveInventoryDigest='f'*64}
  'qualification flags promoted'{$wrong.isolation.nativeCompatibilityQualified=$true}
  'foreign artifact'{$wrong.pins[(Join-Path $set.root 'chunk7-assisted-cold-inventory-evidence.json')]='f'*64}
  'changed Working'{$anchor.working.sha256='f'*64}
  'changed Baseline'{$anchor.baseline.sha256='f'*64}
 }
 $refused=$false
 try{Assert-KmcChunk7F09SixthHireOriginalReceipts $wrong $anchor}catch{$refused=$true}
 if(-not$refused){throw ('Sixth-hire provenance admitted '+$mutation)}
 $passed++
}
$source=Get-KmcChunk7F09SixthHireSource $fixture
if($source.sourceRunId-cne$run-or$source.originalOuterStatus-cne'FAIL'-or$source.nativeGameStatus-cne'PASS'-or
 $source.readerOnlyStatus-cne'PASS'-or$source.nativeAuthorityGranted-ne$false-or$source.ordinaryAcquisitionProven-ne$false-or
 $source.fixtureSetupOnly-ne$true-or$source.nativeRosterTransitionProven-ne$true-or
 $source.sourceFieldActorIds.Count-ne11-or$source.retainedFieldActorIds.Count-ne10-or
 $source.retainedCharacterIds.Count-ne5-or$source.horseFacts.Count-ne5-or$source.receiptPins.Count-ne28-or
 $source.descriptor.fileName-cne'Manual_307_KMC_C7_MANUAL_DERIVED_FIELD.zks'-or
 $source.descriptor.sha256-cne'afbeaa5bc749d91bd969581977f396ff4b0c2716691625a9648b2bb827c2318e'){
 throw 'Sixth-hire source gate lost native transition, immutable Manual or distinct failure/re-evaluation identities.'
}
$passed++
Write-Host ('SIXTH-HIRE READ-ONLY SOURCE PASS='+$passed+' FAIL=0 (nativeInvoked=false; no acquisition/save authority)')
