[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$request=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world182-b182s1-fifthhorse-cold/runtime-request.json')
$selection=$request.assistedColdInventory|ConvertTo-Json -Depth 8|ConvertFrom-Json
$selection.case='fifth-horse-native-field-departure'
$source=Get-KmcChunk7AssistedColdInventorySource $selection $request.fixture $request
Assert-KmcAssistedSame $source.descriptor $request.persistenceLoad 'departure preserves the genuine cold-qualified Manual'
if($source.receiptPins.Count-ne16-or$source.nativeAuthorityGranted-ne$false){throw 'Native departure source omitted its producer/cold receipts or acquired native authority.'}
Assert-KmcAssistedColdSourcePins $source $true $false $true
foreach($mutation in @('missing receipt','duplicate receipt','changed receipt','old mode','foreign combination')){
 $wrong=$source|ConvertTo-Json -Depth 30|ConvertFrom-Json;$small=$true;$fourth=$false;$departure=$true
 switch($mutation){
  'missing receipt'{$wrong.receiptPins=@($wrong.receiptPins|Select-Object -Skip 1)}
  'duplicate receipt'{$wrong.receiptPins[1]=$wrong.receiptPins[0]}
  'changed receipt'{$wrong.receiptPins[0].sha256='f'*64}
  'old mode'{$departure=$false}
  'foreign combination'{$fourth=$true}
 }
 $refused=$false
 try{Assert-KmcAssistedColdSourcePins $wrong $small $fourth $departure}catch{$refused=$true}
 if(-not$refused){throw ('Native departure admitted '+$mutation)}
}
foreach($mutation in @('producer','archive','artifact','case','working')){
 $wrong=$selection|ConvertTo-Json -Depth 8|ConvertFrom-Json;$fixture=$request.fixture|ConvertTo-Json -Depth 8|ConvertFrom-Json
 switch($mutation){
  'producer'{$wrong.sourceRunId='foreign-producer'}
  'archive'{$wrong.sourceArchiveSha256='f'*64}
  'artifact'{$wrong.sourceArtifactSha256='f'*64}
  'case'{$wrong.case='fifth-horse-manual-readonly-inventory'}
  'working'{$fixture.working.sha256='f'*64}
 }
 $refused=$false
 try{[void](Get-KmcChunk7FifthHorseDepartureSource $wrong $fixture $request)}catch{$refused=$true}
 if(-not$refused){throw ('Native departure admitted a changed '+$mutation+'.')}
}
Write-Host 'FIFTH-HORSE DEPARTURE SOURCE PASS=11 FAIL=0 (read-only receipt qualification; nativeInvoked=false)'
