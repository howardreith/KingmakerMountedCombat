[CmdletBinding()]
param(
    [string]$LedgerPath,
    [string]$LabRoot,
    # Completion mode: beyond record consistency, every mandatory Chunk 6A
    # behavior in the fixed list below must be a PASS entry on the frozen
    # payload. A mandatory entry that is missing, NOT RUN, BLOCKED, FAIL, or
    # MAPPED / EXCLUDED without the owner's own recorded decision fails this
    # gate. Record consistency is NOT completion and must never be reported as
    # "N requirements satisfied".
    [switch]$Completion
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if([string]::IsNullOrEmpty($LedgerPath)){$LedgerPath=Join-Path $repoRoot 'docs\chunk6a-ledger.json'}
if([string]::IsNullOrEmpty($LabRoot)){$LabRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))}
function Get-Sha256([string]$path){ (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant() }
function Get-Field($object,[string]$name){ if($null-ne$object-and@($object.PSObject.Properties.Name)-ccontains$name){$object.$name}else{$null} }

# ---------------------------------------------------------------------------
# The agreed finite Chunk 6A list, one id per mandatory behavior of the owner's
# CM01-CM08 families. Nothing is removed or relabelled to obtain completion.
# ---------------------------------------------------------------------------
$mandatory=@(
    # CM01 legal combat Mount through normal controls.
    'CM01-horse-rt','CM01-horse-tb','CM01-mammoth-rt','CM01-mammoth-tb','CM01-exploration-free',
    # R3: a turn-based transition requires an acting rider turn; Preparing is refused.
    'CM01-combat-mount-preparing-refused',
    # CM02 approach, arrival and stale-state revalidation.
    'CM02-approach-arrival','CM02-geometry-change','CM02-obstruction','CM02-wrong-creature-target',
    'CM02-foreign-companion','CM02-ownership-change','CM02-size-form-change','CM02-lost-direct-control',
    'CM02-left-area','CM02-view-agent-lost','CM02-loading-cutscene','CM02-rider-incapacitated',
    'CM02-mount-incapacitated','CM02-generation-change','CM02-target-selection-cancelled',
    # R2: relationship attachment and encounter adoption are one transaction.
    'CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases',
    # CM03 resource debt, actor order and turn ownership.
    'CM03-rider-before-mount-slot','CM03-mount-slot-before-rider','CM03-mount-spent-move',
    'CM03-mount-spent-standard','CM03-mount-spent-all','CM03-rider-without-move','CM03-rider-other-action',
    'CM03-early-end-turn','CM03-next-round-activation','CM03-unrelated-candidate-between',
    # CM04 interruption and lifecycle cleanup.
    'CM04-stop-during-approach','CM04-command-replacement','CM04-turn-end','CM04-combat-end',
    'CM04-mode-exit','CM04-area-session-transition','CM04-disable-unload','CM04-rider-death',
    'CM04-mount-death','CM04-injected-exception',
    # CM05 voluntary combat Dismount and the forced-detach distinction.
    'CM05-rt','CM05-tb','CM05-after-rider-expenditure','CM05-after-mount-expenditure',
    'CM05-immediately-after-mount','CM05-repeated-input','CM05-forced-detach',
    # CM06 native input, pause/queue and negative controls.
    'CM06-hotbar-path','CM06-pointer-target','CM06-paused-queue','CM06-repeated-request',
    'CM06-mount-selected','CM06-multiple-selection','CM06-unrelated-actor','CM06-ai-auto-use',
    'CM06-unmounted-ordinary',
    # CM07 persistence and restoration.
    'CM07-mount-save-rt','CM07-mount-load-rt','CM07-mount-save-tb','CM07-mount-load-tb',
    'CM07-dismount-save','CM07-dismount-load','CM07-cold-load','CM07-save-slot-routes',
    'CM07-unsettled-save-deferred','CM07-area-reload','CM07-schema-unchanged',
    # CM08 regression and exact-candidate consolidation.
    'CM08-mounted-charge-rejected-rt','CM08-mounted-charge-rejected-tb','CM08-unmounted-charge',
    'CM08-ordinary-attack-controls-tb','CM08-chunk4-sustained-tb','CM08-mounted-mammoth-primary-hit-tb',
    'CM08-sustained-rt','CM08-incoming-targeting','CM08-rider-death-cleanup','CM08-mount-death-cleanup',
    'CM08-horse-smoke','CM08-mammoth-smoke','CM08-persistence-suite','CM08-area-restoration',
    'CM08-disable-removal-readiness'
)

$ledger=Get-Content -Raw -LiteralPath $LedgerPath -Encoding UTF8|ConvertFrom-Json
if([int]$ledger.schemaVersion-ne1){throw 'Unknown Chunk 6A ledger schema.'}
$payload=$ledger.payload
foreach($name in @('version','commit','branch','dllSha256','dllMvid','packageSha256','manifestSha256','suiteId','suiteSha256')){
    if([string]::IsNullOrEmpty([string](Get-Field $payload $name))){throw "Chunk 6A ledger payload lacks $name."}
}
$statuses=@('PASS','FAIL','MAPPED','NOT RUN','BLOCKED','EXCLUDED')
$ids=@{}
$counts=@{}
foreach($s in $statuses){$counts[$s]=0}
$checks=0
foreach($entry in $ledger.entries){
    $entryId=[string](Get-Field $entry 'id')
    if([string]::IsNullOrEmpty($entryId)-or$ids.ContainsKey($entryId)){throw 'Chunk 6A ledger entry id missing or duplicated.'}
    $ids[$entryId]=$entry
    $status=[string](Get-Field $entry 'status')
    if($status-cnotin$statuses){throw "Chunk 6A ledger entry $entryId has an unknown status."}
    $counts[$status]++
    if([string]::IsNullOrEmpty([string](Get-Field $entry 'family'))){throw "Chunk 6A ledger entry $entryId names no CM family."}
    if([string]::IsNullOrEmpty([string](Get-Field $entry 'claim'))){throw "Chunk 6A ledger entry $entryId states no claim."}
}
# Every id the ledger declares must belong to the fixed mandatory list, and every
# mandatory id must be present: the list cannot be quietly widened or trimmed.
foreach($id in $ids.Keys){
    if($id-cnotin$mandatory){throw "Chunk 6A ledger declares an id outside the fixed mandatory list: $id"}
}
foreach($id in $mandatory){
    if(-not$ids.ContainsKey($id)){throw "Chunk 6A ledger omits the mandatory id: $id"}
}

foreach($entry in $ledger.entries){
    $id=[string]$entry.id
    switch -CaseSensitive ([string]$entry.status){
        'PASS' {
            $run=[string](Get-Field $entry 'runId')
            if([string]::IsNullOrEmpty($run)){throw "Chunk 6A PASS entry $id names no run."}
            $root=Join-Path $LabRoot ('runtime-evidence/'+$run)
            $result=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-result.json')|ConvertFrom-Json
            $game=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-game-result.json')|ConvertFrom-Json
            $request=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-request.json')|ConvertFrom-Json
            if([string]$result.runId-cne$run-or[string]$result.status-cne'PASS'-or[string]$game.status-cne'PASS'){
                throw "Chunk 6A entry ${id}: run $run is not a PASS."
            }
            if([string]$result.scenario-cne[string](Get-Field $entry 'scenario')){throw "Chunk 6A entry ${id}: scenario differs from its run."}
            if([string]$game.commit-cne$payload.commit-or[string]$game.dllSha256-cne$payload.dllSha256-or
                [string]$game.dllMvid-cne$payload.dllMvid-or[string]$game.productVersion-cne$payload.version){
                throw "Chunk 6A entry ${id}: run $run did not execute the frozen payload."
            }
            if([int]$result.assertionPassCount-ne[int](Get-Field $entry 'passCount')-or
                [int]$result.assertionFailCount-ne[int](Get-Field $entry 'failCount')-or
                [int](Get-Field $entry 'failCount')-ne0){
                throw "Chunk 6A entry ${id}: assertion counts differ from the run."
            }
            if($result.modsRestored-ne$true-or$result.workingRestored-ne$true){throw "Chunk 6A entry ${id}: run $run did not restore the intake."}
            if([string]$request.qualificationSuite.suiteId-cne$payload.suiteId-or
                [string]$request.qualificationSuite.snapshotSha256-cne$payload.suiteSha256){
                throw "Chunk 6A entry ${id}: run $run used another qualification suite."
            }
            # Each PASS entry must name the rows it claims and each must be a PASS
            # row of that run's own evidence artifact.
            $rows=@(Get-Field $entry 'rows')
            if($rows.Count-lt1){throw "Chunk 6A entry ${id}: names no evidence rows."}
            $evidenceLeaf=[string](Get-Field $entry 'evidenceLeaf')
            if([string]::IsNullOrEmpty($evidenceLeaf)){throw "Chunk 6A entry ${id}: names no evidence artifact."}
            $evidencePath=Join-Path $root $evidenceLeaf
            $artifact=Get-Content -Raw -LiteralPath $evidencePath|ConvertFrom-Json
            foreach($rowName in $rows){
                $matched=@(@($artifact.rows)|Where-Object{[string]$_.name-ceq[string]$rowName})
                if($matched.Count-ne1-or[string]$matched[0].status-cne'PASS'){
                    throw "Chunk 6A entry ${id}: evidence has no single PASS row named $rowName."
                }
            }
            $evidenceSha=[string](Get-Field $entry 'evidenceSha256')
            if([string]::IsNullOrEmpty($evidenceSha)){throw "Chunk 6A entry ${id}: the evidence artifact is not bound by hash."}
            if((Get-Sha256 $evidencePath)-cne$evidenceSha){throw "Chunk 6A entry ${id}: evidence bytes differ."}
            $checks++
        }
        'FAIL' {
            $run=[string](Get-Field $entry 'runId')
            if([string]::IsNullOrEmpty($run)-or[string]::IsNullOrEmpty([string](Get-Field $entry 'reason'))){
                throw "Chunk 6A FAIL entry $id needs its run and reason."
            }
            $result=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/runtime-result.json'))|ConvertFrom-Json
            if([string]$result.status-ceq'PASS'){throw "Chunk 6A entry $id claims FAIL for a passing run."}
            $checks++
        }
        'MAPPED' {
            $targets=@(Get-Field $entry 'mappedTo')
            if($targets.Count-lt1-or[string]::IsNullOrEmpty([string](Get-Field $entry 'justification'))){
                throw "Chunk 6A MAPPED entry $id needs targets and a justification."
            }
            foreach($target in $targets){
                if(-not$ids.ContainsKey([string]$target)-or[string]$ids[[string]$target].status-cne'PASS'){
                    throw "Chunk 6A MAPPED entry $id points at a non-PASS entry $target."
                }
            }
            $checks++
        }
        default {
            if([string]::IsNullOrEmpty([string](Get-Field $entry 'reason'))){throw "Chunk 6A $($entry.status) entry $id needs a reason."}
            $checks++
        }
    }
}

Write-Host ("CHUNK6A LEDGER payload="+$payload.version+" commit="+$payload.commit+" entries="+@($ledger.entries).Count+
    " PASS="+$counts['PASS']+" FAIL="+$counts['FAIL']+" MAPPED="+$counts['MAPPED']+" NOTRUN="+$counts['NOT RUN']+
    " BLOCKED="+$counts['BLOCKED']+" EXCLUDED="+$counts['EXCLUDED'])
Write-Host ("LEDGER RECORD CHECKS PASS=$checks FAIL=0 (record consistency only; this is not the completion gate)")
if($Completion){
    $open=@()
    foreach($id in $mandatory){
        $entry=$ids[$id]
        if([string]$entry.status-ceq'PASS'){continue}
        $decision=[string](Get-Field $entry 'ownerDecision')
        if([string]$entry.status-cin @('EXCLUDED','MAPPED')-and-not[string]::IsNullOrEmpty($decision)){continue}
        $open+=("${id}: "+$entry.status)
    }
    if($open.Count-ne0){
        Write-Host ("CHUNK6A COMPLETION GATE FAIL: "+$open.Count+" of "+$mandatory.Count+
            " mandatory behaviors are not PASS on the frozen payload")
        foreach($line in $open){Write-Host ("  OPEN "+$line)}
        exit 1
    }
    Write-Host ("CHUNK6A COMPLETION GATE PASS: all "+$mandatory.Count+" mandatory behaviors PASS on "+$payload.version)
}
