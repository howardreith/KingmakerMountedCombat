[CmdletBinding()]
param(
    [string]$LedgerPath,
    [string]$LabRoot,
    # Completion mode: beyond record consistency, every mandatory Chunk 5
    # behavior below must be a PASS entry on the frozen payload. A mandatory
    # entry that is missing, NOT RUN, BLOCKED, FAIL, MAPPED or EXCLUDED fails
    # this gate unless the entry names the owner's own scope decision.
    [switch]$Completion
)
# The Chunk 5 acceptance ledger is one finite, machine-checkable list. Every
# entry is one of: a native run on the frozen payload (PASS/FAIL), MAPPED (its
# claim is carried by named PASS entries, with the justification stated), NOT RUN,
# BLOCKED or EXCLUDED (with the reason). This checker binds each run entry to the
# actual restored runtime result in the lab, to the frozen payload identity and
# to the exact evidence bytes, and refuses any entry that claims more. A death
# cold entry is additionally bound to its source run: the life state the cold
# world carries for the subject must be the one the source run recorded at its
# save admission. Record consistency alone is NOT completion: the -Completion
# gate holds the agreed finite list of mandatory behaviors.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if([string]::IsNullOrEmpty($LedgerPath)){$LedgerPath=Join-Path $repoRoot 'docs\chunk5-ledger.json'}
if([string]::IsNullOrEmpty($LabRoot)){$LabRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))}
function Get-Sha256([string]$path){ (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant() }
function Get-Field($object,[string]$name){ if($null-ne$object-and@($object.PSObject.Properties.Name)-ccontains$name){$object.$name}else{$null} }
function Read-Rows([string]$run){ @(Get-Content -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/persistence-observations.jsonl')) -Encoding UTF8 | ForEach-Object { $_|ConvertFrom-Json }) }

# The agreed finite list. Every id must be a PASS entry on the frozen payload
# for completion. P08-tb (the obsolete Phase 3H expectation) is deliberately
# not mandatory: its failure is history; the accepted turn-based controls are.
$mandatory=@('P01-save','P01-load')
foreach($c in @('partial-movement','rider-spent','between-partner-orders','exhausted','explicit-end')){$mandatory+=@("P02-save-$c","P02-load-$c")}
foreach($c in @('step','conversion','round-effect','reaction','condition','condition-preparing','suspended')){$mandatory+=@("P03-save-$c","P03-load-$c")}
foreach($c in @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting')){$mandatory+=@("P04-save-$c","P04-load-$c")}
foreach($c in @('manual','quick','auto','queued','alternating')){$mandatory+=@("P05-save-$c","P05-load-$c")}
$mandatory+=@('P05-load-manual-renamed')
foreach($c in @('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai','failed-area-load','foreign-header-campaign')){$mandatory+=@("P06-$c")}
$mandatory+=@('P07-timeout','P07-timeout-cold','P07-cancel-wait','P07-cancel-wait-cold','P07-locked-replace','P07-locked-replace-cold',
    'P07-serialization-cancel','P07-serialization-cancel-output','P07-serialization-cancel-output-cold','P07-disable-reenable',
    'P07-campaign-b','P07-campaign-b-cold','P07-prepare-removal','P07-absent-kmc','P07-removal-no-dll','P07-disable-during-load',
    'P07-rider-death','P07-rider-death-cold','P07-mount-death','P07-mount-death-cold','P07-rider-size-change','P07-rider-size-change-cold',
    'P07-area-reload','P07-area-reload-cold','P07-area-cross-entry','P07-area-cross-entry-cold','P07-area-cross-entry-auto',
    'P07-area-cross-exit','P07-area-cross-exit-cold','P07-area-cross-exit-auto',
    'P08-rt','P08-ordinary-attack-controls-tb','P08-chunk4-sustained-tb','P08-mounted-mammoth-primary-hit-tb')

$ledger=Get-Content -Raw -LiteralPath $LedgerPath -Encoding UTF8|ConvertFrom-Json
if([int]$ledger.schemaVersion-ne1){throw 'Unknown ledger schema.'}
$payload=$ledger.payload
foreach($name in @('version','commit','dllSha256','dllMvid','packageSha256','manifestSha256','suiteId','suiteSha256')){
    if([string]::IsNullOrEmpty([string](Get-Field $payload $name))){throw "Ledger payload lacks $name."}
}
$statuses=@('PASS','FAIL','MAPPED','NOT RUN','BLOCKED','EXCLUDED')
$ids=@{}
$counts=@{}
foreach($s in $statuses){$counts[$s]=0}
$pass=0
foreach($entry in $ledger.entries){
    $entryId=[string](Get-Field $entry 'id')
    if([string]::IsNullOrEmpty($entryId)-or$ids.ContainsKey($entryId)){throw 'Ledger entry id missing or duplicated.'}
    $ids[$entryId]=$entry
    $status=[string](Get-Field $entry 'status')
    if($status-cnotin$statuses){throw "Ledger entry $entryId has an unknown status."}
    $counts[$status]++
}
foreach($entry in $ledger.entries){
    $id=[string]$entry.id
    switch -CaseSensitive ([string]$entry.status){
        'PASS' {
            $run=[string](Get-Field $entry 'runId')
            if([string]::IsNullOrEmpty($run)){throw "Ledger PASS entry $id names no run."}
            $resultPath=Join-Path $LabRoot ('runtime-evidence/'+$run+'/runtime-result.json')
            $result=Get-Content -Raw -LiteralPath $resultPath|ConvertFrom-Json
            $game=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/runtime-game-result.json'))|ConvertFrom-Json
            $request=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/runtime-request.json'))|ConvertFrom-Json
            if([string]$result.runId-cne$run-or[string]$result.status-cne'PASS'-or[string]$game.status-cne'PASS'){throw "Ledger entry ${id}: run $run is not a PASS."}
            if([string]$result.scenario-cne[string](Get-Field $entry 'scenario')){throw "Ledger entry ${id}: scenario differs from its run."}
            if([string](Get-Field $entry 'case')-cne[string](Get-Field $request 'persistenceCase')){throw "Ledger entry ${id}: case differs from its run."}
            if([string]$game.commit-cne$payload.commit-or[string]$game.dllSha256-cne$payload.dllSha256-or[string]$game.dllMvid-cne$payload.dllMvid-or
                [string]$game.productVersion-cne$payload.version){throw "Ledger entry ${id}: run $run did not execute the frozen payload."}
            if([int]$result.assertionPassCount-ne[int](Get-Field $entry 'passCount')-or[int]$result.assertionFailCount-ne[int](Get-Field $entry 'failCount')-or[int](Get-Field $entry 'failCount')-ne0){
                throw "Ledger entry ${id}: assertion counts differ from the run."
            }
            if($result.modsRestored-ne$true-or$result.workingRestored-ne$true){throw "Ledger entry ${id}: run $run did not restore the intake."}
            if([string]$request.qualificationSuite.suiteId-cne$payload.suiteId-or[string]$request.qualificationSuite.snapshotSha256-cne$payload.suiteSha256){
                throw "Ledger entry ${id}: run $run used another qualification suite."
            }
            # The no-DLL observation is written by the removal observer, never by KMC.
            $noDll=[string](Get-Field $entry 'scenario')-ceq'persistence-p07-load'-and[string](Get-Field $entry 'case')-ceq'removal-no-dll'
            $evidence=Join-Path $LabRoot ('runtime-evidence/'+$run+'/'+$(if($noDll){'observer-result.json'}else{'persistence-observations.jsonl'}))
            $evidenceSha=[string](Get-Field $entry 'evidenceSha256')
            if(-not[string]::IsNullOrEmpty($evidenceSha)){
                if((Get-Sha256 $evidence)-cne$evidenceSha){throw "Ledger entry ${id}: evidence bytes differ."}
            }elseif(Test-Path -LiteralPath $evidence){throw "Ledger entry ${id}: evidence rows exist but are not bound."}
            # A death cold entry carries the life state its source run recorded.
            if([string](Get-Field $entry 'scenario')-ceq'persistence-p07-load'-and[string](Get-Field $entry 'case')-cin @('rider-death','mount-death')){
                $sourceId=[string](Get-Field $entry 'sourceEntry')
                if([string]::IsNullOrEmpty($sourceId)-or-not$ids.ContainsKey($sourceId)-or[string]$ids[$sourceId].status-cne'PASS'){throw "Ledger entry ${id}: names no PASS source entry."}
                $sourceRows=Read-Rows ([string]$ids[$sourceId].runId)
                $admission=@($sourceRows|Where-Object kind -CEQ 'death-save-admission')
                $saved=@($sourceRows|Where-Object kind -CEQ 'death-saved')
                if($admission.Count-ne1-or$saved.Count-ne1){throw "Ledger entry ${id}: source run lacks its admission and save rows."}
                $subjectIsMount=[string]$entry.case-ceq'mount-death'
                $subjectId=if($subjectIsMount){[string]$saved[0].detail.mountId}else{[string]$saved[0].detail.riderId}
                $expected=[string]$admission[0].detail.admission.subjectLifeState
                $cold=@((Read-Rows $run)|Where-Object kind -CEQ 'death-cold-complete')
                if($cold.Count-ne1){throw "Ledger entry ${id}: cold run lacks its completion row."}
                $states=Get-Field $cold[0].detail 'lifeStates'
                if($null-eq$states-or@($states.PSObject.Properties.Name)-cnotcontains$subjectId){throw "Ledger entry ${id}: cold world does not carry the subject $subjectId."}
                if([string]$states.$subjectId.lifeState-cne$expected){throw "Ledger entry ${id}: cold life state $($states.$subjectId.lifeState) differs from the recorded $expected."}
                if([string]$cold[0].detail.archiveSha256-cne[string]$saved[0].detail.archive.sha256){throw "Ledger entry ${id}: cold archive is not the source run's death archive."}
            }
            # The no-DLL entry must be a genuine observer run of the prepared cleanup save.
            if($noDll){
                if([string](Get-Field $game 'evidenceKind')-cne'kmc-removal-observer'-or(Get-Field $game 'kmcDllInstalled')-ne$false-or
                    $game.observations.absence.kmcAssemblyLoaded-ne$false-or$game.observations.absence.kmcModsDirectoryPresent-ne$false-or
                    @($game.observations.absence.kmcHarmonyOwners).Count-ne0-or@($game.observations.absence.kmcModEntries).Count-ne0){throw "Ledger entry ${id}: run $run was not a genuine no-DLL observation."}
                if([string]::IsNullOrEmpty($evidenceSha)){throw "Ledger entry ${id}: the observer result is not bound."}
                $sourceId=[string](Get-Field $entry 'sourceEntry')
                if([string]::IsNullOrEmpty($sourceId)-or-not$ids.ContainsKey($sourceId)-or[string]$ids[$sourceId].status-cne'PASS'-or[string](Get-Field $ids[$sourceId] 'case')-cne'prepare-removal'){throw "Ledger entry ${id}: names no PASS prepare-removal source entry."}
                $prepared=@((Read-Rows ([string]$ids[$sourceId].runId))|Where-Object kind -CEQ 'removal-prepared')
                if($prepared.Count-ne1-or[string]$prepared[0].detail.cleanupSha256-cne[string]$game.observations.archive.sha256Before){throw "Ledger entry ${id}: the observed archive is not the source run's prepared cleanup save."}
            }
            $pass++
        }
        'FAIL' {
            $run=[string](Get-Field $entry 'runId')
            if([string]::IsNullOrEmpty($run)-or[string]::IsNullOrEmpty([string](Get-Field $entry 'reason'))){throw "Ledger FAIL entry $id needs its run and reason."}
            $result=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/runtime-result.json'))|ConvertFrom-Json
            if([string]$result.status-ceq'PASS'){throw "Ledger entry $id claims FAIL for a passing run."}
            $pass++
        }
        'MAPPED' {
            $targets=@(Get-Field $entry 'mappedTo')
            if($targets.Count-lt1-or[string]::IsNullOrEmpty([string](Get-Field $entry 'justification'))){throw "Ledger MAPPED entry $id needs targets and a justification."}
            foreach($target in $targets){
                if(-not$ids.ContainsKey([string]$target)-or[string]$ids[[string]$target].status-cne'PASS'){throw "Ledger MAPPED entry $id points at a non-PASS entry $target."}
            }
            $pass++
        }
        default {
            if([string]::IsNullOrEmpty([string](Get-Field $entry 'reason'))){throw "Ledger $($entry.status) entry $id needs a reason."}
            $pass++
        }
    }
}
Write-Host ("CHUNK5 LEDGER payload="+$payload.version+" commit="+$payload.commit+" entries="+@($ledger.entries).Count+
    " PASS="+$counts['PASS']+" FAIL="+$counts['FAIL']+" MAPPED="+$counts['MAPPED']+" NOTRUN="+$counts['NOT RUN']+" BLOCKED="+$counts['BLOCKED']+" EXCLUDED="+$counts['EXCLUDED'])
Write-Host ("LEDGER CHECKS PASS=$pass FAIL=0")
if($Completion){
    $open=@()
    foreach($id in $mandatory){
        if(-not$ids.ContainsKey($id)){$open+=("${id}: missing from the ledger");continue}
        $entry=$ids[$id]
        if([string]$entry.status-ceq'PASS'){continue}
        $decision=[string](Get-Field $entry 'ownerDecision')
        if([string]$entry.status-cin @('EXCLUDED','MAPPED')-and-not[string]::IsNullOrEmpty($decision)){continue}
        $open+=("${id}: "+$entry.status)
    }
    if($open.Count-ne0){
        Write-Host ("CHUNK5 COMPLETION GATE FAIL: "+$open.Count+" of "+$mandatory.Count+" mandatory behaviors are not PASS on the frozen payload")
        foreach($line in $open){Write-Host ("  OPEN "+$line)}
        exit 1
    }
    Write-Host ("CHUNK5 COMPLETION GATE PASS: all "+$mandatory.Count+" mandatory behaviors PASS on "+$payload.version)
}
