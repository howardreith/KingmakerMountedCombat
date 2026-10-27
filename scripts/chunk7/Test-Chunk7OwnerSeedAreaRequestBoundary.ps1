[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeScriptPath,[string]$OutputDirectory)
# Execute only the selected request-target AST and unchanged strict DTO guard.
# No wrapper invocation, source qualification, save/Mods staging or game.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$RuntimeScriptPath){$RuntimeScriptPath=Join-Path $RepositoryRoot 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/area-request-boundary-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$pass=0;$fail=0;$clock=[Diagnostics.Stopwatch]::StartNew();$checks=[Collections.Generic.List[object]]::new()
function Require([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Check([string]$Name,[scriptblock]$Body){try{& $Body;$script:pass++;$checks.Add([pscustomobject]@{name=$Name;status='PASS'});'PASS '+$Name}catch{$script:fail++;$checks.Add([pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.Message});'FAIL '+$Name+': '+$_.Exception.Message}}
function Refuses([scriptblock]$Body){$refused=$false;try{& $Body|Out-Null}catch{$refused=$true};Require $refused 'Expected exact-target refusal absent.'}
function Clone($Value){$Value|ConvertTo-Json -Depth 15|ConvertFrom-Json}
$parse=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($RuntimeScriptPath,[ref]$null,[ref]$parse)
Require ($parse.Count-eq0) 'Runtime script parse failed.'
$assignments=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.AssignmentStatementAst]-and$n.Left.Extent.Text-ceq'$request[''persistenceAreaTarget'']'},$true))
Require ($assignments.Count-eq1) 'Exactly one actual persistenceAreaTarget construction required.'
$assignment=$assignments[0];$parent=$assignment.Parent
while($null-ne$parent-and$parent-isnot[Management.Automation.Language.IfStatementAst]){$parent=$parent.Parent}
Require ($null-ne$parent) 'Actual area selection branch absent.'
$branch=$parent.Extent.Text
# Retain the precise prior in-memory representation as the failing boundary.
# On preview45 this differs only in the construction cast; JSON remains equal.
$oldBranch=$branch.Replace('[pscustomobject][ordered]@{','[ordered]@{')
function Build([string]$Code,[string]$Case,[string]$Entry='fd225a93828d26449803a4847947f8c1',[string]$Area='ead426a6c23d39548a670ee515d77df4'){
 $request=[ordered]@{};$PersistenceCase=$Case;$PersistenceAreaEnterPoint=$Entry;$PersistenceAreaTargetArea=$Area
 & ([scriptblock]::Create($Code))
 if($request.Contains('persistenceAreaTarget')){return ,$request['persistenceAreaTarget']}
 return $null
}
$source=[pscustomobject]@{case='owner-seed-checkpoint-save';sourceArea='b10e86516b6437d45a629931c30936fe'}
$cold=[pscustomobject]@{case='owner-seed-checkpoint-load';sourceArea='b10e86516b6437d45a629931c30936fe'}
Check 'prior construction reproduces strict dictionary-properties failure' {
 $target=Build $oldBranch 'multi-area-autosave'
 Require ($target-is[Collections.Specialized.OrderedDictionary]) 'Prior construction fixture is not its actual OrderedDictionary.'
 $message='';try{Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}catch{$message=$_.Exception.Message}
 Require ($message.Contains('property set is not exact')-and$message.Contains('Keys')-and$message.Contains('Values')) ('Prior exact properties guard did not reproduce: '+$message)
}
foreach($family in @('owner-seed-checkpoint-save','owner-seed-checkpoint-load')){
 $f=$family
 Check ('actual request DTO passes unchanged strict target for '+$f) {
  $s=[pscustomobject]@{case=$f;sourceArea=$source.sourceArea};$target=Build $branch 'multi-area-autosave'
  Require ($target-is[pscustomobject]) 'Actual request target is not normalized to PSCustomObject.'
  Assert-KmcChunk7OwnerSeedAreaTarget $s 'multi-area-autosave' $target
  Require (($target|ConvertTo-Json -Compress)-ceq((Build $oldBranch 'multi-area-autosave')|ConvertTo-Json -Compress)) 'DTO cast changed intended JSON values or order.'
 }.GetNewClosure()
}
Check 'normalized target retains exact three ordered DTO properties' {$target=Build $branch 'multi-area-autosave';Require ((@($target.PSObject.Properties.Name)-join',')-ceq'enterPoint,area,autoSaveMode') 'Target property set/order changed.'}
foreach($key in @('enterPoint','area','autoSaveMode')){
 $k=$key
 Check ('target guard rejects wrong '+$k) {$target=Build $branch 'multi-area-autosave';$target.$k='wrong';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}}.GetNewClosure()
 Check ('target guard rejects missing '+$k) {$target=Build $branch 'multi-area-autosave';$target.PSObject.Properties.Remove($k);Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}}.GetNewClosure()
 Check ('target guard rejects array '+$k) {$target=Build $branch 'multi-area-autosave';$target.$k=@($target.$k);Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}}.GetNewClosure()
}
Check 'target guard rejects wrong source area' {$s=Clone $source;$s.sourceArea='ead426a6c23d39548a670ee515d77df4';$target=Build $branch 'multi-area-autosave';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $s 'multi-area-autosave' $target}}
Check 'target guard rejects unknown extra property' {$target=Build $branch 'multi-area-autosave';$target|Add-Member -NotePropertyName createMapRow -NotePropertyValue $true;Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}}
Check 'target guard rejects case-swapped property' {$target=Build $branch 'multi-area-autosave';$target.PSObject.Properties.Remove('enterPoint');$target|Add-Member -NotePropertyName EnterPoint -NotePropertyValue 'fd225a93828d26449803a4847947f8c1';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $target}}
Check 'target guard rejects null DTO' {Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' $null}}
Check 'target guard rejects array DTO' {$target=Build $branch 'multi-area-autosave';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source 'multi-area-autosave' @($target)}}
Check 'noncheckpoint family cannot borrow valid target' {$s=Clone $source;$s.case='owner-seed-combat-disable';$target=Build $branch 'multi-area-autosave';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $s 'multi-area-autosave' $target}}
foreach($case in @('multi-exploration-manual','multi-combat-tb-manual','owned-schema1-manual','multi-malformed-refusal-manual')){
 $c=$case
 Check ('nonarea request constructs no borrowed target '+$c) {Require ($null-eq(Build $branch $c)) 'Nonarea branch borrowed target.';Assert-KmcChunk7OwnerSeedAreaTarget $source $c $null;$target=Build $branch 'multi-area-autosave';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $source $c $target}}.GetNewClosure()
}
foreach($case in @('area-cross-entry','area-cross-entry-auto','area-cross-exit','area-cross-exit-auto')){
 $c=$case
 Check ('legacy target mode and JSON preserved '+$c) {
  $target=Build $branch $c;$old=Build $oldBranch $c
  Require (($target|ConvertTo-Json -Compress)-ceq($old|ConvertTo-Json -Compress)) 'Legacy target changed JSON.'
  $mode=if($c-cin@('area-cross-entry','area-cross-entry-auto')){'AfterEntry'}else{'BeforeExit'}
  Require ($target.autoSaveMode-ceq$mode-and$target.enterPoint-ceq'fd225a93828d26449803a4847947f8c1'-and$target.area-ceq'ead426a6c23d39548a670ee515d77df4') 'Legacy mode/entry/area values changed.'
 }.GetNewClosure()
}
Check 'entry parameter conservation reaches actual target' {$target=Build $branch 'multi-area-autosave' 'wrong-entry';Require ($target.enterPoint-ceq'wrong-entry') 'Assignment masked entry input.';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $cold 'multi-area-autosave' $target}}
Check 'area parameter conservation reaches actual target' {$target=Build $branch 'multi-area-autosave' 'fd225a93828d26449803a4847947f8c1' 'wrong-area';Require ($target.area-ceq'wrong-area') 'Assignment masked area input.';Refuses {Assert-KmcChunk7OwnerSeedAreaTarget $cold 'multi-area-autosave' $target}}
$clock.Stop();$reader=Join-Path $RepositoryRoot 'scripts/runtime/Chunk7OwnedPairsEvidence.ps1'
$receipt=[ordered]@{status=$(if($fail-eq0){'PASS'}else{'FAIL'});passed=$pass;failed=$fail;seconds=$clock.Elapsed.TotalSeconds;runtimeScriptPath=$RuntimeScriptPath;runtimeScriptSha256=(Get-KmcSha256 $RuntimeScriptPath);testSha256=(Get-KmcSha256 $PSCommandPath);strictReaderSha256=(Get-KmcSha256 $reader);nativeInvoked=$false;whatIfInvoked=$false;sourceQualificationInvoked=$false;wrapperInvoked=$false;limits='Actual selected assignment/if AST plus unchanged exact target reader only. No locked admission, source archive, area entry, native autosave, stage or restoration delivery.';checks=$checks.ToArray()}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
'AREA REQUEST BOUNDARY PASS='+$pass+' FAIL='+$fail
if($fail-gt0){throw 'Private actual area request boundary regression failed; preserve outputs.'}
