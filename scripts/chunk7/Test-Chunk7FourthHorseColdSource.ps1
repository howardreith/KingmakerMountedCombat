[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$producer='20261022-c7b-world178-b178s1-fourthhorse-manual'
$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$producer+'/runtime-request.json'))
$selection=[pscustomobject][ordered]@{case='fourth-horse-manual-readonly-inventory';sourceRunId=$producer;
 sourceArtifactSha256='453c3c590a444807f9fba42e9e0eea6e44d8db42b6e3194437c71c8c9547ea02';
 sourceArchiveSha256='e82f7bb0775535ac2d9b782c6e53fa92a0893ab71bdf375a1e7cc06d50188759';
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$source=Get-KmcChunk7FourthHorseColdSource $selection $request.fixture $request
if($source.receiptPins.Count-ne10-or$source.sourceProcessId-le0-or$source.descriptor.fileName-cne'Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks'){
 throw 'Fourth-Horse immutable source admission omitted a receipt, process or Manual pin.'
}
Assert-KmcAssistedColdSourcePins $source $true $true
$routed=Get-KmcChunk7AssistedColdInventorySource $selection $request.fixture $request
Assert-KmcAssistedSame $routed.descriptor $source.descriptor 'fourth-Horse cold source routing'
foreach($mutation in @('archive','case','sourceRun')){
 $wrong=$selection|ConvertTo-Json -Depth 8|ConvertFrom-Json
 switch($mutation){
  'archive' {$wrong.sourceArchiveSha256='f'*64}
  'case' {$wrong.case='third-horse-manual-readonly-inventory'}
  'sourceRun' {$wrong.sourceRunId='20261022-foreign-fourth-horse'}
 }
 $refused=$false
 try{[void](Get-KmcChunk7FourthHorseColdSource $wrong $request.fixture $request)}catch{$refused=$true}
 if(-not$refused){throw ('Fourth-Horse source admitted a changed '+$mutation+'.')}
}
Write-Host 'FOURTH-HORSE COLD SOURCE PASS=5 FAIL=0 (nativeInvoked=false; original outer FAIL retained)'
