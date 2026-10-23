[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory)
# The retired schema-3 pending control is refused by the actual runner before any selection
# intake, approval, lock, transaction, Mods swap or launch, while every other persistence case
# still passes this gate. Runner invocations are real child processes that fail fast.
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
$retired='pending-schema3-control-manual'
$reason=Get-KmcChunk7RetiredPersistenceCaseReason $retired
if([string]::IsNullOrEmpty($reason)-or-not$reason.Contains('is retired and not selected')-or-not$reason.Contains('docs/CHUNK7-RETIRED-SCHEMA3-CONTROL-2026-10-23.md')){throw 'Retired control reason differs.'}
Pass 'retired control has its exact refusal'

# Every other closed persistence case passes this gate; inexact names are not matched.
$ast=[Management.Automation.Language.Parser]::ParseFile($runner,[ref]$null,[ref]$null)
$parameter=@($ast.ParamBlock.Parameters|Where-Object{$_.Name.VariablePath.UserPath-ceq'PersistenceCase'})[0]
$set=@($parameter.Attributes|Where-Object{$_.TypeName.Name-ceq'ValidateSet'})[0]
$cases=@($set.PositionalArguments|ForEach-Object{$_.Value})
if($cases.Count-lt10-or$retired-cnotin$cases){throw 'Runner persistence case set does not contain the retired control.'}
foreach($case in $cases){if($case-cne$retired-and$null-ne(Get-KmcChunk7RetiredPersistenceCaseReason $case)){throw ('Selected persistence case refused: '+$case)}}
foreach($case in @($null,'','PENDING-SCHEMA3-CONTROL-MANUAL','pending-schema3-control-manual ')){if($null-ne(Get-KmcChunk7RetiredPersistenceCaseReason $case)){throw ('Inexact case matched: '+$case)}}
Pass ('every other runner persistence case ('+($cases.Count-1)+') and inexact names pass the gate')

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
Pass 'runner refuses the retired case before selection intake, approval, lock, transaction and launch'

# Actual runner: refused with its exact reason, with and without plausible arguments and -WhatIf.
$missing=Join-Path ([IO.Path]::GetTempPath()) ('never-read-'+[Guid]::NewGuid().ToString('N')+'.json')
foreach($extra in @(@('-WhatIf'),@('-BaseGameOnly','-SaveAccessAllowed','-OwnedPairsSelectionPath',$missing),@('-BaseGameOnly','-SaveAccessAllowed','-OwnedPairsSelectionPath',$missing,'-WhatIf'))){
    foreach($scenario in @('chunk7-owned-two-pair-campaign','chunk7-persistence-load')){
        $r=Invoke-Runner (@('-Scenario',$scenario,'-PersistenceCase',$retired)+$extra)
        if($r.code-eq0-or-not$r.text.Contains($reason)){throw ('Runner did not refuse the retired control under '+$scenario+': '+$r.text)}
    }
}
Pass 'actual runner refuses the retired control before reading any selection'
foreach($case in @('legacy-mounted-pending-tb-manual','owned-schema2-pending-current-manual')){
    $r=Invoke-Runner @('-Scenario','chunk7-owned-two-pair-campaign','-PersistenceCase',$case,'-WhatIf')
    if($r.code-eq0-or$r.text.Contains('is retired')-or-not$r.text.Contains('Inherited two-pair campaign requires explicit base-only staging')){throw ('Selected pending case did not pass the gate: '+$case+': '+$r.text)}
}
Pass 'actual runner admits the selected pending-only chain to its own intake checks'
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual runner refusal and shared decision only; no lock, transaction, Mods or launch')
exit 0
