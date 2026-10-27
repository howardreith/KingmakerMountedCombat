[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$run='20261022-c7b-world184-b184s1-sixthhorse-manual'
$r=Read-KmcJson (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$run+'/runtime-request.json'))
$s=[pscustomobject][ordered]@{case='sixth-horse-manual-readonly-inventory';sourceRunId=$run;
 sourceArtifactSha256='0f6aabe42d0aeaf63f60c6912c1a029c0175ebe87806768f1191df23a913fa13';
 sourceArchiveSha256='b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291';
 baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$source=Get-KmcChunk7AssistedColdInventorySource $s $r.fixture $r
if($source.sourceProcessId-ne50784-or$source.descriptor.fileName-cne'Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'-or$source.nativeAuthorityGranted-ne$false){throw 'Sixth-Horse source lost its actual producer or claims native authority.'}
Assert-KmcAssistedColdSourcePins $source $true $false $false $true
$count=1
foreach($mutation in @('archive','case','sourceRun','artifact','write','second load')){
 $wrong=$s|ConvertTo-Json -Depth 8|ConvertFrom-Json
 switch($mutation){
  'archive'{$wrong.sourceArchiveSha256='f'*64};'case'{$wrong.case='fifth-horse-manual-readonly-inventory'}
  'sourceRun'{$wrong.sourceRunId='20261022-foreign-sixth-horse'};'artifact'{$wrong.sourceArtifactSha256='f'*64}
  'write'{$wrong.expectedNativeWriteCount=1};'second load'{$wrong.expectedNativeLoadCount=2}
 }
 $refused=$false;try{[void](Get-KmcChunk7SixthHorseColdSource $wrong $r.fixture $r)}catch{$refused=$true}
 if(-not$refused){throw ('Sixth-Horse source admitted '+$mutation)};$count++
}
foreach($mutation in @('missing pin','duplicate pin','changed hash','foreign mode','borrowed fifth shape')){
 $wrong=$source|ConvertTo-Json -Depth 15|ConvertFrom-Json
 switch($mutation){
  'missing pin'{$wrong.receiptPins=@($wrong.receiptPins|Select-Object -Skip 1)}
  'duplicate pin'{$wrong.receiptPins[1]=$wrong.receiptPins[0]}
  'changed hash'{$wrong.receiptPins[0].sha256='f'*64}
  'foreign mode'{$wrong.sourceRunId='foreign'}
 }
 $refused=$false
 try{if($mutation-ceq'borrowed fifth shape'){Assert-KmcAssistedColdSourcePins $wrong $true $false $false}else{Assert-KmcAssistedColdSourcePins $wrong $true $false $false $true}}catch{$refused=$true}
 if(-not$refused){throw ('Sixth-Horse closure admitted '+$mutation)};$count++
}
Write-Host ('SIXTH-HORSE COLD SOURCE PASS='+$count+' FAIL=0 (nativeInvoked=false; original outer FAIL preserved)')
