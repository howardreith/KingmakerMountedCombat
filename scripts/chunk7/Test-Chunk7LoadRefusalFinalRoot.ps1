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
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve final-root reproduction.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1');. (Join-Path $ReaderDirectory 'Chunk7OwnedSchema1Fixtures.ps1');Add-Type -AssemblyName System.IO.Compression
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7LoadRefusalCopies.ps1'),[ref]$null,[ref]$null)
foreach($name in @('Clone','Put','Get-KmcLabRoot','Assert-KmcRuntimeLockOwner','Assert-KmcNoGameProcesses','Get-KmcChunk7OwnedPairsSource','Write-Zip','New-Rig','Derive','Stage-Rig','Copy-Rig')){$node=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($node.Count-ne1){throw 'Fixture helper missing.'};. ([scriptblock]::Create($node[0].Extent.Text))}
$results=[Collections.Generic.List[object]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
foreach($checkpoint in @('multi-malformed-refusal-manual','multi-future-refusal-manual')){Check ('actual runner final-root accepts exactly two immutable refusal inputs '+$checkpoint){$rig=New-Rig $checkpoint;[void](Stage-Rig $rig);Copy-Rig $rig;Assert-KmcChunk7OwnedFinalSaveRoot $rig.request @(Get-ChildItem -LiteralPath (Join-Path $rig.profile 'Saved Games') -Force|ForEach-Object Name) $null}}
foreach($fault in @('primary-only','alternate-only','third-save','directory','duplicate')){Check ('exact refusal final-root refuses '+$fault){$rig=New-Rig;[void](Stage-Rig $rig);$leaves=@($rig.request.persistenceLoad.fileName,$rig.request.persistenceAlternate.fileName);switch($fault){'primary-only'{$leaves=@($leaves[0])};'alternate-only'{$leaves=@($leaves[1])};'third-save'{$leaves+=@('Manual_999_foreign.zks')};'directory'{$leaves+=@('foreign-directory')};'duplicate'{$leaves=@($leaves[0],$leaves[0])}};Assert-KmcChunk7OwnedFinalSaveRoot $rig.request $leaves $null} $true}
$fixtures=([IO.File]::ReadAllText($FixturePath)|ConvertFrom-Json).fixtures
Check 'old zero-write cold root still requires its sole original input' {$r=(Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]).request;Assert-KmcChunk7OwnedFinalSaveRoot $r @($r.persistenceLoad.fileName) $null}
Check 'old zero-write root still refuses a borrowed alternate' {$r=(Clone @($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]).request;Assert-KmcChunk7OwnedFinalSaveRoot $r @($r.persistenceLoad.fileName,'Manual_812_KMC_C7_SEED_REFUSAL.zks') $null} $true
$clock.Stop();$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;seconds=$clock.Elapsed.TotalSeconds;readerSha256=(Get-FileHash -LiteralPath (Join-Path $ReaderDirectory 'Chunk7OwnedPairsEvidence.ps1')).Hash.ToLowerInvariant();nativeInvoked=$false;scope='Actual runner-called final-root function with actual guarded synthetic input staging; original one-input neighbor preserved.';results=@($results)}|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false));Write-Output ('Independent refusal final root '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
