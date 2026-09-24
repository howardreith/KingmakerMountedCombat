[CmdletBinding()]
param(
    [string]$LedgerPath,
    [string]$LabRoot
)
# The Chunk 5 acceptance ledger is one finite, machine-checkable list. Every
# entry is one of: a native run on the frozen payload (PASS/FAIL), MAPPED (its
# claim is carried by named PASS entries, with the justification stated), NOT RUN,
# BLOCKED or EXCLUDED (with the reason). This checker binds each run entry to the
# actual restored runtime result in the lab, to the frozen payload identity and
# to the exact evidence bytes, and refuses any entry that claims more. A death
# cold entry is additionally bound to its source run: the life state the cold
# world carries for the subject must be the one the source run recorded at its
# save admission.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if([string]::IsNullOrEmpty($LedgerPath)){$LedgerPath=Join-Path $repoRoot 'docs\chunk5-ledger.json'}
if([string]::IsNullOrEmpty($LabRoot)){$LabRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))}
function Get-Sha256([string]$path){ (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant() }
function Get-Field($object,[string]$name){ if($null-ne$object-and@($object.PSObject.Properties.Name)-ccontains$name){$object.$name}else{$null} }
function Read-Rows([string]$run){ @(Get-Content -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$run+'/persistence-observations.jsonl')) -Encoding UTF8 | ForEach-Object { $_|ConvertFrom-Json }) }
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
            $evidence=Join-Path $LabRoot ('runtime-evidence/'+$run+'/persistence-observations.jsonl')
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
