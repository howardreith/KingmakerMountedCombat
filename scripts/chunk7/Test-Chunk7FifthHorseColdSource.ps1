[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$producer='20261022-c7b-world181-b181s1-fifthhorse-manual'
$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$producer+'/runtime-request.json'))
$selection=[pscustomobject][ordered]@{case='fifth-horse-manual-readonly-inventory';sourceRunId=$producer;
 sourceArtifactSha256='e05fdd3cfb16c344dcb821a57476cefdfb91978aa8a39aca9322e5b4fc24a47b';
 sourceArchiveSha256='afbeaa5bc749d91bd969581977f396ff4b0c2716691625a9648b2bb827c2318e';
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$source=Get-KmcChunk7FifthHorseColdSource $selection $request.fixture $request
if($source.receiptPins.Count-ne9-or$source.sourceProcessId-le0-or$source.descriptor.fileName-cne'Manual_307_KMC_C7_MANUAL_DERIVED_FIELD.zks'){
 throw 'Fifth-Horse immutable source admission omitted a receipt, process or Manual pin.'
}
Assert-KmcAssistedColdSourcePins $source $true $false
$routed=Get-KmcChunk7AssistedColdInventorySource $selection $request.fixture $request
Assert-KmcAssistedSame $routed.descriptor $source.descriptor 'fifth-Horse cold source routing'
foreach($mutation in @('archive','case','sourceRun','artifact')){
 $wrong=$selection|ConvertTo-Json -Depth 8|ConvertFrom-Json
 switch($mutation){
  'archive' {$wrong.sourceArchiveSha256='f'*64}
  'case' {$wrong.case='fourth-horse-manual-readonly-inventory'}
  'sourceRun' {$wrong.sourceRunId='20261022-foreign-fifth-horse'}
  'artifact' {$wrong.sourceArtifactSha256='f'*64}
 }
 $refused=$false
 try{[void](Get-KmcChunk7FifthHorseColdSource $wrong $request.fixture $request)}catch{$refused=$true}
 if(-not$refused){throw ('Fifth-Horse source admitted a changed '+$mutation+'.')}
}
Write-Host 'FIFTH-HORSE COLD SOURCE PASS=6 FAIL=0 (nativeInvoked=false; batch181 remains producer-only)'
