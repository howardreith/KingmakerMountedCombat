[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$OutputDirectory)
# Stale level-up autosave families are NOT READY: the installed game never registers
# LevelUpAutoSaveController, so their completion waits can never finish. The actual runner must
# refuse each of them (and each progression case that writes the terminal autosave) before any
# selection intake, lock, transaction, Mods swap or launch, and must still admit the selected
# families. Runner invocations are real child processes that fail fast on refusal; no admitted
# invocation is allowed to reach environment intake, WhatIf purity, a lock or a launch.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('RuntimeDirectory')){$RuntimeDirectory=Join-Path $PSScriptRoot '../runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/stale-levelup-families-'+[Guid]::NewGuid().ToString('N'))}
$runtime=[IO.Path]::GetFullPath($RuntimeDirectory);$output=[IO.Path]::GetFullPath($OutputDirectory);[void][IO.Directory]::CreateDirectory($output)
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $runtime -FixturesOnly
$script:passes=0
function Pass([string]$Name){$script:passes++;Write-Host ('PASS '+$Name)}
function Ast([string]$Path){$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($Path,[ref]$tokens,[ref]$errors);if($errors){throw ($errors|Out-String)};return $ast}
function Commands($Ast,[string]$Name){@($Ast.FindAll({param($n)$n-is[Management.Automation.Language.CommandAst]-and$n.GetCommandName()-ceq$Name},$true))}
$runner=Join-Path $runtime 'Invoke-KingmakerRuntimeScenario.ps1'
$labRoot=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))
$lockSentinel=Join-Path $labRoot 'runtime-state/active-transaction.lock'
$evidenceRoot=Join-Path $labRoot 'runtime-evidence'
function Get-EvidenceNames{if(Test-Path -LiteralPath $evidenceRoot){@(Get-ChildItem -LiteralPath $evidenceRoot -Directory|ForEach-Object Name|Sort-Object)}else{@()}}
function Invoke-Runner([string[]]$Arguments){
    $before=Get-EvidenceNames;$lockBefore=Test-Path -LiteralPath $lockSentinel
    # The child reports its refusal on stderr; read it as text rather than as a terminating error.
    $ErrorActionPreference='Continue'
    $text=(& powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $runner @Arguments 2>&1|ForEach-Object{"$_"})-join"`n"
    $code=$LASTEXITCODE
    if(((Get-EvidenceNames)-join'|')-cne($before-join'|')-or(Test-Path -LiteralPath $lockSentinel)-ne$lockBefore){throw 'A runner invocation changed runtime evidence or the transaction sentinel.'}
    [pscustomobject]@{code=$code;text=($text-replace'\s+',' ')}
}
function Expected([string]$Name,[string]$Site){'Scenario family '+$Name+' is not ready: it waits for a native level-up autosave that the installed game never requests (LevelUpAutoSaveController is not registered); repair its completion contract before selection. Wait site: '+$Site+'; see docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md.'}

$blocked=[ordered]@{
    'chunk7-ordinary-acquisition'='src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionScenario.cs:112 (ObserveCompletedAutosave, :266)'
}
$cumulativeSite='src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedScenario.Build.cs (legacy TickBuild/ObserveTerminalAutosave; reached through Chunk7ManualDerivedScenario.CreateCumulative)'
$progressionSite='src/KingmakerMountedCombat/Diagnostics/Chunk7NativeProgressionSession.cs (legacy ObserveTerminalAutosave)'
$blockedCases=@('horse-native-level','horse-native-retrain-horse','horse-native-retrain-mammoth')
$admittedCases=@('horse-native-dismiss','owned-horse-native-dismiss','owned-horse-native-level','owned-horse-native-retrain-horse','owned-horse-native-retrain-mammoth','small-horse-native-dismiss','small-horse-native-retrain-horse','small-horse-native-level','medium-horse-native-retrain-mammoth')

# The registry is exact and every recorded wait site is the live wait in the source.
$families=@(Get-KmcChunk7StaleLevelUpAutosaveFamilies)
if((($families|ForEach-Object scenario)-join'|')-cne((@($blocked.Keys)+'chunk7-cumulative-acquisition'+'chunk7-native-progression')-join'|')){throw 'Stale family registry differs.'}
if(((@($families|Where-Object scenario -CEQ 'chunk7-cumulative-acquisition')[0].cases)-join'|')-cne'legacy-auto'){throw 'Cumulative legacy Auto refusal differs.'}
if(((@($families|Where-Object scenario -CEQ 'chunk7-native-progression')[0].cases)-join'|')-cne($blockedCases-join'|')){throw 'Stale progression cases differ.'}
if(@(Compare-Object @(Get-KmcChunk7ProgressionCases) @($blockedCases+$admittedCases)).Count-ne0){throw 'Progression cases are not partitioned into exactly blocked and admitted.'}
Pass 'stale family registry is exact and partitions every progression case'
$siteChecks=@(
    @('Chunk7OrdinaryAcquisitionScenario.cs',112,'if (!ObserveCompletedAutosave()) return;'),@('Chunk7OrdinaryAcquisitionScenario.cs',266,'private bool ObserveCompletedAutosave()'))
foreach($check in $siteChecks){
    $lines=[IO.File]::ReadAllLines((Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$check[0])))
    if(-not$lines[$check[1]-1].Contains($check[2])){throw ('Recorded wait site moved: '+$check[0]+':'+$check[1])}
    if(-not$lines[$check[1]-7].Contains('NOT READY - stale level-up autosave wait')){throw ('Wait site lacks its NOT READY note: '+$check[0]+':'+$check[1])}
}
Pass 'every recorded wait site is the live wait and carries its NOT READY note'
$manualSource=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedScenario.Build.cs'))
foreach($value in @('if (cumulative == null || cumulative.UsesAssistedCold) { TickManualBuild(); return; }','if (!ObserveTerminalAutosave()) return;','private bool ObserveTerminalAutosave()','NOT READY - stale level-up autosave wait')){if(-not$manualSource.Contains($value)){throw ('Cumulative completion dispatch changed: '+$value)}}
Pass 'assisted cumulative uses Manual completion while legacy Auto wait remains guarded'
$progressionSource=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7NativeProgressionSession.cs'))
if(-not$progressionSource.Contains('if (WritesAutosave && !ObserveTerminalAutosave()) return;')-or
   -not$progressionSource.Contains('private bool ObserveTerminalAutosave()')-or
   -not$progressionSource.Contains('if (WritesManual)')-or
   -not$progressionSource.Contains('private bool ObserveManualSave()')){throw 'Legacy Auto refusal or owned Manual completion route differs.'}
Pass 'legacy progression retains Auto wait while owned completion has distinct Manual route'
$doc=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md'))
foreach($value in @($blocked.Keys)+@('chunk7-cumulative-acquisition')+$blockedCases+@($siteChecks|ForEach-Object{$_[0];(':'+$_[1])})+@('LevelUpAutoSaveController','follow completion with an explicitly authorized native Manual save','Never
manufacture a level-up callback or relabel a Manual as Auto')){if(-not$doc.Contains($value)){throw ('Tracked note omits: '+$value)}}
Pass 'tracked note records every blocked family, case, wait site and the required repair'

# Exact reasons from the shared decision.
foreach($entry in $blocked.GetEnumerator()){
    foreach($case in @($null,'horse-native-dismiss')){if((Get-KmcChunk7FamilyNotReadyReason $entry.Key $case)-cne(Expected $entry.Key $entry.Value)){throw ('Reason differs: '+$entry.Key)}}
}
if((Get-KmcChunk7FamilyNotReadyReason 'chunk7-cumulative-acquisition' 'legacy-auto')-cne(Expected 'chunk7-cumulative-acquisition/legacy-auto' $cumulativeSite)){throw 'Legacy cumulative refusal differs.'}
foreach($case in @($null,'assisted-manual')){if($null-ne(Get-KmcChunk7FamilyNotReadyReason 'chunk7-cumulative-acquisition' $case)){throw 'Assisted cumulative Manual was refused.'}}
foreach($case in $blockedCases){if((Get-KmcChunk7FamilyNotReadyReason 'chunk7-native-progression' $case)-cne(Expected ('chunk7-native-progression/'+$case) $progressionSite)){throw ('Progression reason differs: '+$case)}}
Pass 'every blocked family and progression case has its exact refusal'
$admitted=@('chunk7-owned-two-pair-campaign','chunk7-assisted-cold-inventory','chunk7-two-pair-exploration','chunk7-two-pair-combat','chunk7-persistence-save','chunk7-persistence-load',
    'chunk7-acquired-manual-derivative','chunk7-manual-derived-acquisition','chunk7-cumulative-acquisition','chunk7-bootstrap-manual-seed','chunk7-bootstrap-cold-inventory','chunk7-native-main-bootstrap','chunk7-small-profile-calibration','chunk7-ordinary-field-admission','mod-load-smoke')
foreach($name in $admitted){if($null-ne(Get-KmcChunk7FamilyNotReadyReason $name $null)){throw ('Selected family refused: '+$name)}}
foreach($case in @($admittedCases)+@($null,'')){if($null-ne(Get-KmcChunk7FamilyNotReadyReason 'chunk7-native-progression' $case)){throw ('Admitted progression case refused: '+$case)}}
foreach($name in @('CHUNK7-ORDINARY-ACQUISITION','chunk7-ordinary-acquisition ','chunk7-manual-derived')){if($null-ne(Get-KmcChunk7FamilyNotReadyReason $name $null)){throw ('Inexact name matched: '+$name)}}
Pass 'selected families, dismissal cases and inexact names are not refused'

# The runner asks before any intake, lock, transaction, Mods swap or launch.
$ast=Ast $runner
$asserts=@(Commands $ast 'Assert-KmcChunk7FamilyReady')
$first=@('Read-KmcChunk7OrdinarySelectionFile','Read-KmcChunk7ProgressionSelectionFile','Read-KmcChunk7FieldSelectionFile','Get-KmcRepositoryRoot','Get-KmcLabRoot','Open-KmcRuntimeLock',
    'New-KmcPersistenceProfileSnapshot','New-KmcRunTransactionState','Enter-KmcWorkingSaveTransaction','Enter-KmcModsTransaction','Start-Process')|ForEach-Object{Commands $ast $_}|ForEach-Object{$_.Extent.StartOffset}|Sort-Object|Select-Object -First 1
$progressionRead=@(Commands $ast 'Read-KmcChunk7ProgressionSelectionFile'|Sort-Object{$_.Extent.StartOffset}) # the first is intake; the later one re-reads under the lock
$cumulativeRead=@(Commands $ast 'Read-KmcChunk7CumulativeSelectionFile'|Sort-Object{$_.Extent.StartOffset})
$later=@('Read-KmcChunk7FieldSelectionFile','Read-KmcChunk7OrdinarySelectionFile','Get-KmcRepositoryRoot','Open-KmcRuntimeLock','Enter-KmcModsTransaction','Start-Process')|ForEach-Object{Commands $ast $_}|ForEach-Object{$_.Extent.StartOffset}|Sort-Object|Select-Object -First 1
$approval=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.InvokeMemberExpressionAst]-and$n.Member.Extent.Text-ceq'ShouldProcess'},$true))
if($asserts.Count-ne3-or$progressionRead.Count-lt1-or$cumulativeRead.Count-lt1-or$approval.Count-ne1-or
    $asserts[0].Extent.Text-cne'Assert-KmcChunk7FamilyReady $Scenario $null'-or$asserts[0].Extent.StartOffset-ge$first-or
    $asserts[1].Extent.Text-cne'Assert-KmcChunk7FamilyReady $Scenario $(if(Test-KmcAssistedCumulativeMode $cumulativeAcquisitionInput.selection){''assisted-manual''}else{''legacy-auto''})'-or
    $asserts[1].Extent.StartOffset-le$cumulativeRead[0].Extent.StartOffset-or$asserts[1].Extent.StartOffset-ge$approval[0].Extent.StartOffset-or
    $asserts[2].Extent.Text-cne'Assert-KmcChunk7FamilyReady $Scenario ([string]$progressionSelectionInput.selection.case)'-or
    $asserts[2].Extent.StartOffset-le$progressionRead[0].Extent.StartOffset-or$asserts[2].Extent.StartOffset-ge$later-or
    $asserts[2].Extent.StartOffset-ge$approval[0].Extent.StartOffset){throw 'Runner readiness refusal is not ahead of intake, approval, lock and launch.'}
Pass 'runner refuses before selection intake, approval, lock, transaction, Mods swap and launch'

# Actual runner: each blocked family is refused with its exact reason even with -WhatIf and with
# otherwise-plausible arguments, before the family's own selection is read.
$missing=Join-Path $output 'never-read-selection.json'
$selectionParameter=@{'chunk7-ordinary-acquisition'='OrdinarySelectionPath';'chunk7-cumulative-acquisition'='CumulativeAcquisitionSelectionPath'}
foreach($entry in $blocked.GetEnumerator()){
    foreach($extra in @(@(),@('-BaseGameOnly','-SaveAccessAllowed',('-'+$selectionParameter[$entry.Key]),$missing))){
        $r=Invoke-Runner (@('-Scenario',$entry.Key,'-WhatIf')+$extra)
        if($r.code-eq0-or-not$r.text.Contains((Expected $entry.Key $entry.Value))){throw ('Runner did not refuse '+$entry.Key+': '+$r.text)}
    }
    Pass ('actual runner refuses '+$entry.Key+' before any intake or lock')
}
$legacyFixture=Read-KmcJson (Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/CumulativePaidFixture.json')
$legacySelection=Join-Path $output 'legacy-cumulative-selection.json'
[IO.File]::WriteAllText($legacySelection,($legacyFixture.request.cumulativeAcquisition|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
$legacyResult=Invoke-Runner @('-Scenario','chunk7-cumulative-acquisition','-RunId','stale-cumulative-gate','-BaseGameOnly','-SaveAccessAllowed','-CumulativeAcquisitionSelectionPath',$legacySelection)
if($legacyResult.code-eq0-or-not$legacyResult.text.Contains((Expected 'chunk7-cumulative-acquisition/legacy-auto' $cumulativeSite))){throw ('Actual legacy cumulative refusal differs: '+$legacyResult.text)}
Pass 'actual legacy cumulative selection is refused before approval, lock and launch'
# Progression: a real closed selection under the lab cache is read, then refused by its case.
# Selection intake hashes its file, which -WhatIf would turn into a no-op, so these runs omit
# -WhatIf and instead carry a forbidden field selection: the runner's own next intake check
# throws right after the readiness gate, long before environment intake, approval or a lock.
function Write-Selection([string]$Case){
    $p=Join-Path $output ('progression-'+$Case+'.json')
    if(Test-KmcChunk7MediumProgressionCase $Case){
        $producerRun='20261021-c7b-world119-b115s1-medium-mounted'
        $coldRun='20261021-c7b-world119-b115s2-medium-mounted-cold'
        $producerPath=Join-Path $labRoot ('runtime-evidence/'+$producerRun+'/chunk7-persistence-evidence.json')
        $coldPath=Join-Path $labRoot ('runtime-evidence/'+$coldRun+'/chunk7-persistence-evidence.json')
        $producer=Read-KmcJson $producerPath;$cold=Read-KmcJson $coldPath
        $request=Read-KmcJson (Join-Path $labRoot ('runtime-evidence/'+$coldRun+'/runtime-request.json'))
        $s=[pscustomobject][ordered]@{case=$Case;sourceRunId=$producerRun;sourceArtifactSha256=(Get-KmcSha256 $producerPath)
            sourceArchiveSha256=$producer.observations.nativeWrite.sha256;inventoryRunId=$coldRun;inventoryArtifactSha256=(Get-KmcSha256 $coldPath)
            actorId=$request.ownedPairs.riderBId;petId=$request.ownedPairs.mountBId
            controlActorId=$request.ownedPairs.riderAId;controlPetId=$request.ownedPairs.mountAId
            raceGuid='0a5d473ead98b0646b94495af250fdc4';presetGuid='58181bf151eb0c0408f82546541dcc03'
            skeletonName='MaleHuman';gender='Male';leftHanded=$false;bodySize='Medium';minimumRangerLevel=10
            expectedNativeWriteCount=1;nativeWriteLeaf=$null;ownedSource=$request.ownedPairs;fixtureExperienceGrant=96000}
    }elseif(Test-KmcChunk7SmallProgressionCase $Case){
        $source=Join-Path $labRoot 'runtime-evidence/20261020-c7b-world102-b89s3-small-native-dismiss/runtime-request.json'
        if((Get-KmcSha256 $source)-cne'90f0974ec2f033e7fcb4e0ba07cf0cf33a35651cdfcabe4975ff4bb276bf972b'){
            throw 'Immutable batch89 Small progression intake source changed.'
        }
        $s=(Get-Content -Raw -LiteralPath $source|ConvertFrom-Json).progression
        if($Case-ceq'small-horse-native-retrain-horse'){
            $s.case=$Case;$s.raceGuid='b0c3ef2729c498f47970bb50fa1acd30';$s.presetGuid='639cebe8cbc098345af429d62fd79578'
            $s.skeletonName='MaleHalfling';$s.gender='Male';$s.leftHanded=$false;$s.bodySize='Small'
            $s.minimumRangerLevel=4;$s.expectedNativeWriteCount=1
        }
        if($Case-ceq'small-horse-native-level'){
            $s.case=$Case;$s.minimumRangerLevel=4;$s.expectedNativeWriteCount=1
            $s|Add-Member -NotePropertyName fixtureExperienceGrant -NotePropertyValue 6000
        }
    }else{$s=(New-ProgressionFixture $Case).request.progression;if(Test-KmcChunk7OwnedProgressionCase $Case){$s.nativeWriteLeaf=$null}}
    [IO.File]::WriteAllText($p,(ConvertTo-KmcChunk7Canonical $s),[Text.UTF8Encoding]::new($false));$p
}
foreach($case in $blockedCases){
    $r=Invoke-Runner @('-Scenario','chunk7-native-progression','-BaseGameOnly','-SaveAccessAllowed','-ProgressionSelectionPath',(Write-Selection $case),'-OrdinaryFieldSelectionPath',$missing)
    if($r.code-eq0-or-not$r.text.Contains((Expected ('chunk7-native-progression/'+$case) $progressionSite))){throw ('Runner did not refuse progression '+$case+': '+$r.text)}
    Pass ('actual runner refuses chunk7-native-progression/'+$case+' after its selection read, before any lock')
}
# Admitted: the runner passes the readiness gate and stops at the family's own next intake refusal.
foreach($case in $admittedCases){
    $r=Invoke-Runner @('-Scenario','chunk7-native-progression','-BaseGameOnly','-SaveAccessAllowed','-ProgressionSelectionPath',(Write-Selection $case),'-OrdinaryFieldSelectionPath',$missing)
    if($r.code-eq0-or$r.text.Contains('is not ready')-or-not$r.text.Contains('Field selection is forbidden under another scenario.')){throw ('Admitted progression case did not pass readiness: '+$case+': '+$r.text)}
    Pass ('actual runner admits chunk7-native-progression/'+$case+' to its own intake checks')
}
$next=[ordered]@{
    'chunk7-assisted-fixture-preparation'='Assisted setup requires explicit base-only staging and the exact owner-authorized closed selection.'
    'chunk7-owned-two-pair-campaign'='Inherited two-pair campaign requires explicit base-only staging and a qualified two-owner native Manual selection.'
    'chunk7-assisted-cold-inventory'='Assisted cold inventory requires base-only staging and exact completed assisted final Manual source pins.'
    'chunk7-native-progression'='Native progression requires exact source/inventory pins and explicit base-only staging.'
    'chunk7-acquired-manual-derivative'='Acquired Manual acquisition requires explicit base-only staging'
    'chunk7-manual-derived-acquisition'='Manual-derived acquisition requires explicit base-only staging'
}
foreach($entry in $next.GetEnumerator()){
    $r=Invoke-Runner @('-Scenario',$entry.Key,'-WhatIf')
    if($r.code-eq0-or$r.text.Contains('is not ready')-or-not$r.text.Contains($entry.Value)){throw ('Selected family did not pass readiness: '+$entry.Key+': '+$r.text)}
    Pass ('actual runner admits '+$entry.Key+' to its own intake checks')
}
# WhatIf must reach the guarded ShouldProcess boundary only after read-only
# selection intake. A second scenario's forbidden argument stops this probe
# before environment/suite admission, lock, transaction or launch.
$assistedSelectionPath=Join-Path $output 'assisted-whatif-selection.json'
$assistedSelection=[ordered]@{
    case='authored-main-ranger4-manual';ownerAuthorizationSha256='052eb32338bba397e852cba8605ebc6878deff1dd907675041d5b1d2f802e28e'
    baseGameOnly=$true;assistedSetupOnly=$true;storyPolicy='first-unused-eligible-row-else-root-system-button'
    assistancePolicy='native-main-ranger4-and-one-hire-shortfalls';expectedNativeLoadCount=0;maximumNativeWriteCount=3
    targetRangerLevel=4;maximumTargetLevel=8;maximumStoryChoices=48;maximumStorySeconds=300
    maximumExperienceShortfall=200000;maximumMoneyShortfall=100000;nativeInitialAutoLeaf='Auto_1.zks'
    nativeManualName='KMC_C7_ASSISTED_FIXTURE'
}
[IO.File]::WriteAllText($assistedSelectionPath,($assistedSelection|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
$assistedWhatIf=Invoke-Runner @('-Scenario','chunk7-assisted-fixture-preparation','-BaseGameOnly','-SaveAccessAllowed',
    '-AssistedFixtureSelectionPath',$assistedSelectionPath,'-OrdinaryFieldSelectionPath',$missing,'-WhatIf')
if($assistedWhatIf.code-eq0-or-not$assistedWhatIf.text.Contains('Field selection is forbidden under another scenario.')){
    throw ('Assisted WhatIf selection intake did not reach the next safe refusal: '+$assistedWhatIf.text)
}
Pass 'assisted WhatIf hashes its closed selection before the next intake refusal'
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; actual runner refusals and shared readiness decision only; no lock, transaction, Mods or launch')
