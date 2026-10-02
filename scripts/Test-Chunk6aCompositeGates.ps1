param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# The four CM08 set rows are strict composite gates over other mandatory PASS entries and, for
# the persistence and disable/removal sets, the current Chunk 5 persistence ledger on the same
# product payload. Synthetic Chunk 6A ledgers bound to the committed Chunk 5 ledger; no native
# qualification and no current-candidate claim.
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '..\..'))
$checks=0
function Reject([scriptblock]$Action,[string]$Expected){
 $rejected=$false
 try{& $Action}catch{if($_.Exception.Message.IndexOf($Expected,[StringComparison]::Ordinal)-lt0){throw};$rejected=$true}
 if(-not$rejected){throw ('Composite gate accepted invalid evidence: '+$Expected)};$script:checks++
}
function Copy-Json($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
$sets=@(Get-KmcChunk6aCompositeSets)
if($sets.Count-ne4-or(($sets|ForEach-Object id) -join ',')-cne'CM08-horse-smoke,CM08-mammoth-smoke,CM08-persistence-suite,CM08-disable-removal-readiness'){throw 'Composite set registry differs'};$checks++
$expectedMembers=@{
 'CM08-horse-smoke'=@('CM01-horse-rt','CM01-horse-tb','CM05-rt','CM05-tb','CM06-hotbar-path','CM06-pointer-target')
 'CM08-mammoth-smoke'=@('CM01-mammoth-rt','CM01-mammoth-tb','CM08-mounted-mammoth-primary-hit-tb')
 'CM08-persistence-suite'=@('CM07-mount-save-rt','CM07-mount-load-rt','CM07-mount-save-tb','CM07-mount-load-tb','CM07-dismount-save','CM07-dismount-load','CM07-cold-load','CM07-save-slot-routes','CM07-unsettled-save-deferred','CM07-area-reload','CM07-schema-unchanged')
 'CM08-disable-removal-readiness'=@('CM05-dismount-survives-feature-policy-disable','CM04-disable-unload')
}
foreach($set in $sets){
 if((@($set.members) -join ',')-cne($expectedMembers[$set.id] -join ',')){throw ('Composite members differ for '+$set.id)};$checks++
 if(-not(Test-KmcChunk6aCompositeId $set.id)){throw 'Composite id not recognised'};$checks++
}
if(Test-KmcChunk6aCompositeId 'CM01-horse-rt'){throw 'A member id must not be a composite'};$checks++
if($sets[2].chunk5Completion-ne$true-or$sets[3].chunk5Completion-ne$false-or(@($sets[3].chunk5Entries) -join ',')-cne'P07-disable-reenable,P07-prepare-removal,P07-absent-kmc,P07-removal-no-dll'-or@($sets[0].chunk5Entries).Count-ne0){throw 'Chunk 5 gate requirements differ'};$checks++
# A synthetic Chunk 6A ledger whose payload is the committed Chunk 5 completion payload, so the
# bound Chunk 5 ledger executes "the same product payload" for the positive persistence gates.
$chunk5Path=Join-Path $repo 'docs\chunk5-ledger.json'
$chunk5=Get-Content -Raw -LiteralPath $chunk5Path|ConvertFrom-Json
$chunk5Sha=(Get-FileHash -LiteralPath $chunk5Path -Algorithm SHA256).Hash.ToLowerInvariant()
function New-Ledger([string[]]$PassIds){
 $entries=@(foreach($id in $PassIds){[pscustomobject]@{id=$id;status='PASS'}})
 [pscustomobject]@{payload=(Copy-Json $chunk5.payload);entries=$entries}
}
function New-CompositeEntry([string]$Id,[string[]]$Members,[bool]$BindChunk5){
 $entry=[pscustomobject]@{id=$Id;status='PASS';compositeOf=@($Members)}
 if($BindChunk5){$entry|Add-Member -NotePropertyName chunk5Ledger -NotePropertyValue ([pscustomobject]@{path=$chunk5Path;sha256=$chunk5Sha})}
 $entry
}
foreach($set in $sets){
 $needs=$set.chunk5Completion-or@($set.chunk5Entries).Count-gt0
 $ledger=New-Ledger $set.members
 $entry=New-CompositeEntry $set.id $set.members $needs
 Assert-KmcChunk6aCompositeEntry $set.id $entry $ledger $lab $repo;$checks++
 # Missing, non-PASS and foreign members.
 $short=New-CompositeEntry $set.id (@($set.members)|Select-Object -Skip 1) $needs
 Reject {Assert-KmcChunk6aCompositeEntry $set.id $short $ledger $lab $repo} 'exact member set'
 $failed=New-Ledger $set.members;$failed.entries[0].status='BLOCKED'
 Reject {Assert-KmcChunk6aCompositeEntry $set.id $entry $failed $lab $repo} 'to be PASS on the same frozen payload'
 $foreign=New-CompositeEntry $set.id (@($set.members|Select-Object -Skip 1)+@('CM01-exploration-free')) $needs
 $foreignLedger=New-Ledger (@($set.members)+@('CM01-exploration-free'))
 Reject {Assert-KmcChunk6aCompositeEntry $set.id $foreign $foreignLedger $lab $repo} 'omits member'
 $duplicate=New-CompositeEntry $set.id (@($set.members)+@($set.members[0])) $needs
 Reject {Assert-KmcChunk6aCompositeEntry $set.id $duplicate $ledger $lab $repo} 'exact member set'
 if($needs){
  $unbound=New-CompositeEntry $set.id $set.members $false
  Reject {Assert-KmcChunk6aCompositeEntry $set.id $unbound $ledger $lab $repo} 'requires its current Chunk 5 persistence ledger binding'
  $wrongBytes=Copy-Json $entry;$wrongBytes.chunk5Ledger.sha256='0'*64
  Reject {Assert-KmcChunk6aCompositeEntry $set.id $wrongBytes $ledger $lab $repo} 'bytes differ'
  $otherPayload=New-Ledger $set.members;$otherPayload.payload.commit='0'*40
  Reject {Assert-KmcChunk6aCompositeEntry $set.id $entry $otherPayload $lab $repo} 'another product payload'
  $escape=Copy-Json $entry;$escape.chunk5Ledger.path='..\\outside\\ledger.json'
  Reject {Assert-KmcChunk6aCompositeEntry $set.id $escape $ledger $lab $repo} 'invalid Chunk 5 ledger path'
 } else {
  $overbound=New-CompositeEntry $set.id $set.members $true
  Reject {Assert-KmcChunk6aCompositeEntry $set.id $overbound $ledger $lab $repo} 'does not require'
 }
}
Reject {Assert-KmcChunk6aCompositeEntry 'CM01-horse-rt' (New-CompositeEntry 'CM01-horse-rt' @('CM05-rt') $false) (New-Ledger @('CM05-rt')) $lab $repo} 'non-composite id'
# The ledger validator refuses a composite PASS that binds a native run of its own, and keeps
# every composite id and member on the mandatory list.
$ledgerSource=Get-Content -Raw (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1')
foreach($pin in @('if(Test-KmcChunk6aCompositeId $id){','must not bind a native run of its own','Assert-KmcChunk6aCompositeEntry $id $entry $ledger $LabRoot $repoRoot','declares composite members without a composite PASS','names a non-mandatory member')){
 if($ledgerSource.IndexOf($pin,[StringComparison]::Ordinal)-lt0){throw ('Ledger validator composite pin missing: '+$pin)};$checks++
}
Write-Output ('COMPOSITE GATES PASS='+$checks+' FAIL=0; synthetic ledgers bound to the committed Chunk 5 ledger, no native qualification')
