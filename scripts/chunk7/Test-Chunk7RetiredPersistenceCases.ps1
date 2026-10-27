[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory)
# The retired-case gate stays ahead of selection intake, approval, lock, transaction, Mods swap and launch.
# The schema-3 pending control (pending-schema3-control-manual) was reinstated on October 24 as 7E row 75,
# the X3 residual cold consumer of row 17's genuine live-producer Manual, so no persistence case is retired
# now; every runner case passes the gate. Runner invocations are real child processes that fail fast.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
$runtime=[IO.Path]::GetFullPath($RuntimeDirectory)
. (Join-Path $runtime 'RuntimeHarness.Common.ps1')
$script:passes=0
function Pass([string]$Name){$script:passes++;Write-Host ('PASS '+$Name)}
$runner=Join-Path $runtime 'Invoke-KingmakerRuntimeScenario.ps1'
$labRoot=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))
$lockSentinel=Join-Path $labRoot 'runtime-state/active-transaction.lock'
$evidenceRoot=Join-Path $labRoot 'runtime-evidence'
function Get-EvidenceNames{if(Test-Path -LiteralPath $evidenceRoot){@(Get-ChildItem -LiteralPath $evidenceRoot -Directory|ForEach-Object Name|Sort-Object)}else{@()}}
function Invoke-Runner([string[]]$Arguments){
    $before=Get-EvidenceNames;$lockBefore=Test-Path -LiteralPath $lockSentinel
    $ErrorActionPreference='Continue'
    $text=(& powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $runner @Arguments 2>&1|ForEach-Object{"$_"})-join"`n"
    $code=$LASTEXITCODE
    if(((Get-EvidenceNames)-join'|')-cne($before-join'|')-or(Test-Path -LiteralPath $lockSentinel)-ne$lockBefore){throw 'A runner invocation changed runtime evidence or the transaction sentinel.'}
    [pscustomobject]@{code=$code;text=($text-replace'\s+',' ')}
}
$reinstated='pending-schema3-control-manual'
if($null-ne(Get-KmcChunk7RetiredPersistenceCaseReason $reinstated)){throw 'Reinstated schema-3 control (7E row 75) is still refused.'}
Pass 'reinstated schema-3 control (row 75) has no retirement refusal'

# Every closed persistence case passes this gate; inexact names are not matched either.
$ast=[Management.Automation.Language.Parser]::ParseFile($runner,[ref]$null,[ref]$null)
$parameter=@($ast.ParamBlock.Parameters|Where-Object{$_.Name.VariablePath.UserPath-ceq'PersistenceCase'})[0]
$set=@($parameter.Attributes|Where-Object{$_.TypeName.Name-ceq'ValidateSet'})[0]
$cases=@($set.PositionalArguments|ForEach-Object{$_.Value})
if($cases.Count-lt10-or$reinstated-cnotin$cases){throw 'Runner persistence case set does not contain the schema-3 control.'}
foreach($case in $cases){if($null-ne(Get-KmcChunk7RetiredPersistenceCaseReason $case)){throw ('Selected persistence case refused: '+$case)}}
foreach($case in @($null,'','PENDING-SCHEMA3-CONTROL-MANUAL','pending-schema3-control-manual ')){if($null-ne(Get-KmcChunk7RetiredPersistenceCaseReason $case)){throw ('Inexact case matched: '+$case)}}
Pass ('every runner persistence case ('+$cases.Count+') and inexact names pass the gate')

# The gate sits right after the scenario readiness gate, before intake, approval, lock and launch.
$commands={param($Name)@($ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}
$gate=@(& $commands 'Assert-KmcChunk7PersistenceCaseSelected')
$ready=@(& $commands 'Assert-KmcChunk7FamilyReady'|Sort-Object{$_.Extent.StartOffset})
$first=@('Read-KmcChunk7OwnedPairsSelectionFile','Read-KmcChunk7OrdinarySelectionFile','Read-KmcChunk7ProgressionSelectionFile','Get-KmcRepositoryRoot','Get-KmcLabRoot','Open-KmcRuntimeLock',
    'New-KmcPersistenceProfileSnapshot','New-KmcRunTransactionState','Enter-KmcWorkingSaveTransaction','Enter-KmcModsTransaction','Start-Process')|ForEach-Object{& $commands $_}|ForEach-Object{$_.Extent.StartOffset}|Sort-Object|Select-Object -First 1
$approval=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
if($gate.Count-ne1-or$gate[0].Extent.Text-cne'Assert-KmcChunk7PersistenceCaseSelected $PersistenceCase'-or
    $gate[0].Extent.StartOffset-le$ready[0].Extent.StartOffset-or$gate[0].Extent.StartOffset-ge$first-or$gate[0].Extent.StartOffset-ge$approval[0].Extent.StartOffset){
    throw 'Retired persistence case gate is not ahead of intake, approval, lock and launch.'
}
Pass 'retired-case gate stays ahead of selection intake, approval, lock, transaction and launch'

# Actual runner: the pending-only chain, now including the reinstated control, reaches its own intake checks.
foreach($case in @('legacy-mounted-pending-tb-manual','owned-schema2-pending-current-manual','pending-schema3-control-manual')){
    $r=Invoke-Runner @('-Scenario','chunk7-owned-two-pair-campaign','-PersistenceCase',$case,'-WhatIf')
    if($r.code-eq0-or$r.text.Contains('is retired')-or-not$r.text.Contains('Inherited two-pair campaign requires explicit base-only staging')){throw ('Selected pending case did not pass the gate: '+$case+': '+$r.text)}
}
Pass 'actual runner admits the pending-only chain and the reinstated control to their own intake checks'
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual runner gate and shared decision only; no lock, transaction, Mods or launch')
exit 0
