[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$OutputRoot,[Parameter(Mandatory=$true)][string]$CorrectedProofPath,[Parameter(Mandatory=$true)][string]$CorrectedProofSha256,[Parameter(Mandatory=$true)][string]$LabRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(Test-Path $OutputRoot){throw 'Preserve proof selection tests.'};[void][IO.Directory]::CreateDirectory($OutputRoot)
$homeRoot=Split-Path $PSScriptRoot -Parent;$env:TEMP=Join-Path $OutputRoot 'temp';$env:TMP=$env:TEMP;[void][IO.Directory]::CreateDirectory($env:TEMP)
Add-Type -Path (Join-Path $homeRoot 'src/ScopedPreflightObservation.cs')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightProof.ps1')
. (Join-Path $homeRoot 'overlays/scripts/runtime/ScopedPreflightFullAuditReevaluation.ps1')
$script:pass=0;$script:fail=0;$script:index=0;$rows=[Collections.Generic.List[object]]::new()
function Need([bool]$Value,[string]$Message){if(-not$Value){throw $Message}}
function Check([string]$TestName,[scriptblock]$TestBody){try{&$TestBody;$script:pass++;[void]$rows.Add([pscustomobject]@{name=$TestName;status='PASS'})}catch{$script:fail++;[void]$rows.Add([pscustomobject]@{name=$TestName;status='FAIL';error=$_.Exception.ToString()})}}
function Reject([scriptblock]$Body,[string]$Pattern){$message=$null;try{$null=&$Body}catch{$message=$_.Exception.Message};Need ($null-ne$message-and$message-like('*'+$Pattern+'*')) ('Expected '+$Pattern+'; got '+$message)}
function Ast([string]$Path){$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$null,[ref]$errors);Need ($errors.Count-eq0) 'Source parse failed';$ast}
$entry=Ast (Join-Path $homeRoot 'overlays/scripts/runtime/Invoke-Chunk7ScopedPreflight.ps1')
$function=@($entry.FindAll({param($a)$a-is[Management.Automation.Language.FunctionDefinitionAst]-and$a.Name-ceq'Get-KmcScopedSelectedHistoricalProof'},$true));Need ($function.Count-eq1) 'Selected entry has no exact proof function';. ([scriptblock]::Create($function[0].Extent.Text))
# Reuse the existing generated successful-proof fixture exactly; no old assertions
# or implementation are replaced. New branch consumes the independently tested
# separate proof supplied explicitly to this selected test.
$old=Ast (Join-Path $PSScriptRoot 'Test-ScopedPreflightProof.ps1')
foreach($name in @('Hash','Put','Pin','Fixture')){$f=@($old.FindAll({param($a)$a-is[Management.Automation.Language.FunctionDefinitionAst]-and$a.Name-ceq$name},$true));Need ($f.Count-eq1) 'Existing fixture helper absent';. ([scriptblock]::Create($f[0].Extent.Text))}
function Selected([string]$Path,[string]$Sha,[string]$Root){$owners=[Collections.Generic.List[object]]::new();try{Get-KmcScopedSelectedHistoricalProof $Path $Sha $Root $owners}finally{foreach($lease in $owners){$lease.Dispose()}}}
Check 'old successful envelope executes unchanged old reader' {$f=Fixture {};$v=Selected $f.path $f.sha256 $f.lab;Need ($v.documents.observation.status-ceq'PASS'-and$v.documents.callerExit.exitCode-eq0-and-not$v.evidenceProvenance.readerOnlyReevaluation-and$v.evidenceProvenance.kind-ceq'original-successful-full-audit') 'Old provenance changed'}
Check 'old caller failure never falls back to corrected reader' {$f=Fixture {param($v)$v.callerExit.exitCode=1};Reject {Selected $f.path $f.sha256 $f.lab} 'actual caller exit'}
Check 'old failed supervisor remains refusal' {$f=Fixture {param($v)$v.observation.status='FAIL'};Reject {Selected $f.path $f.sha256 $f.lab} 'observation status'}
Check 'old log wrapping is not silently normalized' {$f=Fixture {param($v)$v.log=$v.log.Replace('lock, transaction','lock, \ntransaction')};Reject {Selected $f.path $f.sha256 $f.lab} 'exact single full assertion'}
Check 'separate corrected envelope uses real shared adapter and retains FAIL1' {$v=Selected $CorrectedProofPath $CorrectedProofSha256 $LabRoot;Need ($v.documents.observation.status-ceq'FAIL'-and$v.documents.callerExit.exitCode-eq1-and$v.rereadCallerExit.exitCode-eq0-and$v.evidenceProvenance.readerOnlyReevaluation-and$v.evidenceProvenance.kind-ceq'separate-immutable-formatting-reevaluation'-and-not$v.nativeAdmission-and-not$v.freshAuditExecuted) 'Corrected provenance flattened'}
foreach($kind in @('full-whatif-reader-only-reevaluation','closed-preflight-inherited-full-proof ','FULL-WHATIF-FORMATTING-REEVALUATION-PROOF','unknown')){Check ('unselected envelope '+$kind){$f=Fixture {param($v)$v.bindingMutation={param($b)$b.kind=$kind}};Reject {Selected $f.path $f.sha256 $f.lab} 'outside the two selected contracts'}}
Check 'array-valued kind fails closed' {$f=Fixture {param($v)$v.bindingMutation={param($b)$b.kind=@('closed-preflight-inherited-full-proof')}};Reject {Selected $f.path $f.sha256 $f.lab} 'exact envelope kind'}
Check 'new kind alone cannot manufacture corrected evidence' {$f=Fixture {param($v)$v.bindingMutation={param($b)$b.kind='full-whatif-formatting-reevaluation-proof'}};Reject {Selected $f.path $f.sha256 $f.lab} 'corrected proof envelope property set'}
Check 'selected proof hash remains mandatory' {Reject {Selected $CorrectedProofPath ('0'*64) $LabRoot} 'immutable pin'}
Check 'both delegates remain exact and precede fresh guard call' {$commands=@($entry.FindAll({param($a)$a-is[Management.Automation.Language.CommandAst]},$true));Need (@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcScopedInheritedProof'}).Count-eq1) 'Old reader delegate changed';Need (@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcReevaluatedFullAuditProof'}).Count-eq1) 'New reader delegate changed';$select=@($commands|Where-Object{$_.GetCommandName()-ceq'Get-KmcScopedSelectedHistoricalProof'})[0];$guard=@($commands|Where-Object{$_.GetCommandName()-ceq'Invoke-KmcScopedFreshGuards'})[0];Need ($select.Extent.StartOffset-lt$guard.Extent.StartOffset) 'Fresh guards precede proof admission'}
[pscustomobject]@{schemaVersion=1;status=$(if($script:fail){'FAIL'}else{'PASS'});pass=$script:pass;fail=$script:fail;rows=$rows;entrySha256=(Hash (Join-Path $homeRoot 'overlays/scripts/runtime/Invoke-Chunk7ScopedPreflight.ps1'));correctedProofSha256=$CorrectedProofSha256;qualification=$false;nativeExecution=$false;actualArchiveOperations=0;scope='Actual selector AST and both real proof readers; explicit supplied corrected proof may be a generated test graph, never a native qualification.'}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputRoot 'receipt.json') -Encoding UTF8
Write-Host ('Historical proof selection '+$script:pass+' PASS / '+$script:fail+' FAIL');if($script:fail){throw 'Historical proof selection checks failed.'}
