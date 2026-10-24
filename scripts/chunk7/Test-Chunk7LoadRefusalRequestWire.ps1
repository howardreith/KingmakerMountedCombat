[CmdletBinding()]
param(
 [string]$RepositoryRoot,
 [string]$ReaderDirectory,
 [string]$FixturePath,
 [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Run-scoped synthetic archives only. Existing output directories are preserved.
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve wire evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema2Evidence.ps1');Add-Type -AssemblyName System.IO.Compression
function Import-Functions([string]$Path,[string[]]$Names){$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$null,[ref]$null);foreach($name in $Names){$node=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($node.Count-ne1){throw ('Expected test helper '+$name)};. ([scriptblock]::Create($node[0].Extent.Text))}}
. Import-Functions (Join-Path $PSScriptRoot 'Test-Chunk7LoadRefusalCopies.ps1') @('Clone','Put','Get-KmcLabRoot','Assert-KmcRuntimeLockOwner','Assert-KmcNoGameProcesses','Get-KmcChunk7OwnedPairsSource','Write-Zip','New-Rig','Derive','Stage-Rig')
$path=Join-Path $ReaderDirectory 'Test-RuntimeRequest.ps1';$ast=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$null,[ref]$null)
$keys=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$commonRequired'},$true))
$extras=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$extra'-and$n.Operator.ToString()-ceq'Equals'},$true))
$refusal=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$loadRefusal'},$true))
$shape=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcExactProperties'-and$n.Extent.Text.Contains("'runtime request v2'")},$true))
$descriptor=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcChunk7LoadRefusalDescriptor'},$true))
$owned=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq'Assert-KmcChunk7OwnedPairsDescriptor'},$true))
foreach($set in @($keys,$extras,$refusal,$shape,$descriptor,$owned)){if($set.Count-ne1){throw 'Actual wire optional-field/shape/descriptor seam is absent or ambiguous.'}}
function Assert-ActualWire($request){
 $alternating=$false;$validation=$false;$crossArea=$false;$chunk7Area=$false;$hasWorkingObservation=$false;$hasPersistenceCase=$true
 . ([scriptblock]::Create($owned[0].Extent.Text))
 . ([scriptblock]::Create($refusal[0].Extent.Text));. ([scriptblock]::Create($keys[0].Extent.Text));. ([scriptblock]::Create($extras[0].Extent.Text));. ([scriptblock]::Create($shape[0].Extent.Text))
 if($loadRefusal){. ([scriptblock]::Create($descriptor[0].Extent.Text))}
}
$fixtures=([IO.File]::ReadAllText($FixturePath)|ConvertFrom-Json).fixtures
function Wire-Envelope($r){if($null-eq$r.PSObject.Properties['transactionToken']){Put $r transactionToken ('1'*64)};Put $r qualificationSuite ([pscustomobject]@{suiteId='synthetic-independent-wire';snapshotSha256=('4'*64)});$r}
function Wire([string]$Checkpoint='multi-malformed-refusal-manual'){$rig=New-Rig $Checkpoint;$r=Stage-Rig $rig;$template=@($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0].request;foreach($key in @('schemaVersion','branch','commit','productVersion','dllSha256','dllMvid','evidenceRoot')){Put $r $key (Clone $template.$key)};Wire-Envelope $r}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
foreach($checkpoint in @('multi-malformed-refusal-manual','multi-future-refusal-manual')){Check ('actual wire admits exact refusal alternate and receipt '+$checkpoint){Assert-ActualWire (Wire $checkpoint)}}
Check 'actual wire preserves old owned Manual shape' {$r=Wire-Envelope (Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0].request);Assert-ActualWire $r}
foreach($fault in @('missing-alternate','missing-receipt','receipt-array','receipt-hash','unknown-extra','wrong-leaf','wrong-name','wrong-area','primary-hash-alias','seed-hash-alias','working-hash-alias','wrong-case','wrong-checkpoint','native-quota','write')){Check ('actual request wire refuses '+$fault){$r=Wire;switch($fault){'missing-alternate'{$r.PSObject.Properties.Remove('persistenceAlternate')};'missing-receipt'{$r.PSObject.Properties.Remove('loadRefusalDerivationSha256')};'receipt-array'{$r.loadRefusalDerivationSha256=@($r.loadRefusalDerivationSha256)};'receipt-hash'{$r.loadRefusalDerivationSha256='unbound'};'unknown-extra'{Put $r arbitraryLoadPermission $true};'wrong-leaf'{$r.persistenceAlternate.fileName='Manual_813_KMC_C7_SEED_REFUSAL.zks'};'wrong-name'{$r.persistenceAlternate.internalName='KMC_C7_SEED_REFUSAL'};'wrong-area'{$r.persistenceAlternate.area='e'*32};'primary-hash-alias'{$r.persistenceAlternate.sha256=$r.persistenceLoad.sha256};'seed-hash-alias'{$r.persistenceAlternate.sha256=$r.ownedPairs.seedSha256};'working-hash-alias'{$r.persistenceAlternate.sha256=$r.fixture.working.sha256};'wrong-case'{$r.ownedPairs.case='owner-seed-persistence-load'};'wrong-checkpoint'{$r.persistenceCase='multi-exploration-manual'};'native-quota'{$r.ownedPairs.expectedNativeLoadCount=2};'write'{$r.ownedPairs.expectedNativeWriteCount=1}};Assert-ActualWire $r} $true}
foreach($field in @('persistenceAlternate','loadRefusalDerivationSha256')){Check ('old owned wire rejects borrowed '+$field){$r=Wire-Envelope (Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0].request);Put $r $field $(if($field-ceq'persistenceAlternate'){Clone $r.persistenceLoad}else{'e'*64});Assert-ActualWire $r} $true}
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;seconds=$clock.Elapsed.TotalSeconds;wireSha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant();nativeInvoked=$false;scope='Unchanged actual request-wire AST property admission and descriptor commands, with actual descriptor/selection validators. Does not run package/suite/identity preflight or native wrapper.';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));Write-Output ('Independent actual refusal wire '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
