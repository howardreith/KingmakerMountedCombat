[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory)
# The two retired Small calibration cases are refused by the actual runner as soon as their
# selection file names them: before that selection is validated or admitted, and before any
# source intake, approval, lock, transaction, Mods swap or launch. Every other Small case still
# reaches its own intake checks. Runner invocations are real child processes that fail fast.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
$runtime=[IO.Path]::GetFullPath($RuntimeDirectory)
. (Join-Path $runtime 'RuntimeHarness.Common.ps1')
$script:passes=0
function Pass([string]$Name){$script:passes++;Write-Host ('PASS '+$Name)}
$runner=Join-Path $runtime 'Invoke-KingmakerRuntimeScenario.ps1'
$fixtures=Join-Path $runtime 'Chunk7SmallCalibrationFixtures.ps1'
$labRoot=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))
$lockSentinel=Join-Path $labRoot 'runtime-state/active-transaction.lock'
$evidenceRoot=Join-Path $labRoot 'runtime-evidence'
$selectionRoot=Join-Path $labRoot ('analysis-cache/chunk7-continuation/retired-small-calibration-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($selectionRoot)
function Get-EvidenceNames{if(Test-Path -LiteralPath $evidenceRoot){@(Get-ChildItem -LiteralPath $evidenceRoot -Directory|ForEach-Object Name|Sort-Object)}else{@()}}
function Invoke-Runner([string[]]$Arguments){
    $before=Get-EvidenceNames;$lockBefore=Test-Path -LiteralPath $lockSentinel
    $ErrorActionPreference='Continue'
    $text=(& powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $runner @Arguments 2>&1|ForEach-Object{"$_"})-join"`n"
    $code=$LASTEXITCODE
    if(((Get-EvidenceNames)-join'|')-cne($before-join'|')-or(Test-Path -LiteralPath $lockSentinel)-ne$lockBefore){throw 'A runner invocation changed runtime evidence or the transaction sentinel.'}
    [pscustomobject]@{code=$code;text=($text-replace'\s+',' ')}
}
function New-Selection([string]$Name,$Value){
    $path=Join-Path $selectionRoot ($Name+'.json')
    [IO.File]::WriteAllText($path,($Value|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))
    return $path
}
$retired=@('compiled-manual-derived-small-horse-rt','compiled-assisted-two-owner-small-horse-rt')
$admitted=@('compiled-small-horse-rt','compiled-owner-seed-fixture-small-horse-rt','compiled-owner-seed-fixture-small-horse-presentation-rt')
$doc='docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md'

foreach($case in $retired){
    $reason=Get-KmcChunk7RetiredSmallCalibrationCaseReason $case
    if([string]::IsNullOrEmpty($reason)-or-not$reason.StartsWith('Small calibration case '+$case+' is retired and not selected')-or-not$reason.Contains($doc)-or
        -not$reason.Contains('compiled-owner-seed-fixture-small-horse-rt')-or-not$reason.Contains('rows 33-34')-or-not$reason.Contains('x4s/x4c')){throw ('Retired Small case reason differs: '+$case)}
}
Pass 'both retired Small cases have their exact refusal and named replacements'

# Every selected Small case and every inexact name passes this gate.
$source=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationSelection.cs'))
$declared=@([regex]::Matches($source,'internal const string \w*CaseName = "([^"]+)"')|ForEach-Object{$_.Groups[1].Value})
if($declared.Count-ne5-or@($retired+$admitted|Where-Object{$_-cnotin$declared}).Count-ne0){throw 'Declared Small calibration case set differs.'}
foreach($case in $admitted){if($null-ne(Get-KmcChunk7RetiredSmallCalibrationCaseReason $case)){throw ('Selected Small case refused: '+$case)}}
foreach($case in @($null,'','COMPILED-MANUAL-DERIVED-SMALL-HORSE-RT','compiled-manual-derived-small-horse-rt ','compiled-assisted-two-owner-small-horse')){if($null-ne(Get-KmcChunk7RetiredSmallCalibrationCaseReason $case)){throw ('Inexact Small case matched: '+$case)}}
Pass ('the '+$admitted.Count+' selected Small cases and inexact names pass the gate')

# The gate runs inside the guarded selection read, before the selection is validated; the runner
# reads that selection before any source intake, approval, lock, transaction or launch.
$commands={param($Ast,$Name)@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true)|Sort-Object{$_.Extent.StartOffset})}
$fixtureAst=[Management.Automation.Language.Parser]::ParseFile($fixtures,[ref]$null,[ref]$null)
$read=@($fixtureAst.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Read-KmcChunk7SmallSelectionFile'},$true))
if($read.Count-ne1){throw 'Small selection reader is absent or ambiguous.'}
$gate=@(& $commands $read[0] 'Assert-KmcChunk7SmallCalibrationCaseSelected');$json=@(& $commands $read[0] 'Read-KmcJson');$validate=@(& $commands $read[0] 'Assert-KmcChunk7SmallSelection')
if($gate.Count-ne1-or$json.Count-ne1-or$validate.Count-ne1-or$gate[0].Extent.StartOffset-le$json[0].Extent.StartOffset-or$gate[0].Extent.StartOffset-ge$validate[0].Extent.StartOffset){
    throw 'Retired Small case gate is not between the guarded read and selection validation.'
}
$ast=[Management.Automation.Language.Parser]::ParseFile($runner,[ref]$null,[ref]$null)
$firstRead=@(& $commands $ast 'Read-KmcChunk7SmallSelectionFile')[0].Extent.StartOffset
$first=@('Get-KmcChunk7SmallF09CalibrationSource','Get-KmcChunk7SmallManualSource','Get-KmcChunk7FieldSource','Open-KmcRuntimeLock',
    'New-KmcPersistenceProfileSnapshot','New-KmcRunTransactionState','Enter-KmcWorkingSaveTransaction','Enter-KmcModsTransaction','Start-Process')|ForEach-Object{& $commands $ast $_}|ForEach-Object{$_.Extent.StartOffset}|Sort-Object|Select-Object -First 1
$approval=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true)|Sort-Object{$_.Extent.StartOffset})
if($null-eq$first-or$approval.Count-eq0-or$firstRead-ge$first-or$firstRead-ge$approval[0].Extent.StartOffset){throw 'Runner reads the Small selection after intake, approval, lock or launch.'}
Pass 'gate sits before selection validation, source intake, approval, lock, transaction and launch'

# Actual runner: a minimal and a full-shaped selection of each retired case are refused with the
# exact reason, with and without -WhatIf, without creating evidence or a transaction sentinel.
$fixture=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'))|ConvertFrom-Json
foreach($case in $retired){
    $reason=(Get-KmcChunk7RetiredSmallCalibrationCaseReason $case)-replace'\s+',' '
    $full=$fixture.request.smallCalibration|ConvertTo-Json -Depth 20|ConvertFrom-Json;$full.case=$case
    foreach($selection in @((New-Selection ($case+'-minimal') ([pscustomobject]@{case=$case})),(New-Selection ($case+'-full') $full))){
        foreach($extra in @(@(),@('-WhatIf'))){
            $r=Invoke-Runner (@('-Scenario','chunk7-small-profile-calibration','-BaseGameOnly','-SmallCalibrationSelectionPath',$selection)+$extra)
            if($r.code-eq0-or-not$r.text.Contains($reason)){throw ('Runner did not refuse '+$case+': '+$r.text)}
        }
    }
}
Pass 'actual runner refuses both retired cases before admitting their selection'
foreach($case in $admitted){
    $selection=New-Selection ($case+'-minimal') ([pscustomobject]@{case=$case})
    $r=Invoke-Runner @('-Scenario','chunk7-small-profile-calibration','-BaseGameOnly','-SmallCalibrationSelectionPath',$selection,'-WhatIf')
    if($r.code-eq0-or$r.text.Contains('is retired')-or-not$r.text.Contains('property set is not exact')){throw ('Selected Small case did not reach its own intake checks: '+$case+': '+$r.text)}
}
Pass 'actual runner admits the selected Small cases to their own intake checks'
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual runner refusal and shared decision only; no lock, transaction, Mods or launch')
exit 0
