$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7CombatPersistenceObservation.ps1')
$passed=0
$script:sourceLab=Join-Path $chunk7AnalysisRoot ('combat-source-fixtures-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($script:sourceLab)
function Get-KmcLabRoot {return $script:sourceLab}
$tokens=$null;$errors=$null;$sourceAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7PersistenceSource.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Write-TestJson','Write-TestZip','Pin-TestReceipts','New-TestSource','Read-TestSource')){$f=$sourceAst.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true);. ([scriptblock]::Create($f.Extent.Text))}
function New-CombatSource($Spec,[string]$Label){
    $c=New-TestSource $Label;$run=$c.request.runId
    $q=Copy-Test $Spec.request;$q.runId=$run;$q|Add-Member NoteProperty evidenceRoot $c.evidence -Force
    $a=Copy-Test $Spec.artifact;$a.runId=$run
    foreach($field in @('combatCheckpoint','combatContinuation','checkpointCleanup')){$a.observations.$field.targetGroupId='KMC.RuntimeHostile.'+$run+':two-pair-target'}
    $c.request=$q;$c.artifact=$a;$c.owner.persistenceCase=$q.persistenceCase
    $metadata=if($q.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){$a.observations.nativeWrite.nativeMetadata}else{$a.observations.nativeWrite.archive}
    $header=[pscustomobject]@{Name='KMC_C7_WORLD';Type='Manual';CompatibilityVersion=1;GameId=$q.fixture.working.gameId;GameName=$q.fixture.working.gameName;Area=$q.fixture.working.area}
    Write-TestZip $c.path $header $metadata
    $c.sha=Get-KmcSha256 $c.path;$a.observations.nativeWrite.sha256=$c.sha;$a.observations.nativeWrite.length=[long](Get-Item -LiteralPath $c.path).Length
    $a.observations.nativeWrite|Add-Member NoteProperty path $c.path -Force
    Pin-TestReceipts $c;return $c
}
$contexts=@();for($i=0;$i-lt$complete.Count;$i+=2){$c=New-CombatSource $complete[$i] ('complete-'+$i);$contexts+=@($c);Check ('actual ZIP source admission '+$c.request.persistenceCase) {$result=Read-TestSource $c;if($result.path-cne$c.path-or$result.descriptor.sha256-cne$c.sha){throw 'Native source selection differs.'}}}
$legacyContext=$contexts[3]
Reject 'legacy native ZIP replaced with migrated schema3 metadata despite repinned transport' {
    $c=New-CombatSource $complete[6] 'wrong-native-version';$header=[pscustomobject]@{Name='KMC_C7_WORLD';Type='Manual';CompatibilityVersion=1;GameId=$c.request.fixture.working.gameId;GameName=$c.request.fixture.working.gameName;Area=$c.request.fixture.working.area}
    Write-TestZip $c.path $header $c.artifact.observations.nativeWrite.archive;$c.sha=Get-KmcSha256 $c.path;$c.artifact.observations.nativeWrite.sha256=$c.sha;$c.artifact.observations.nativeWrite.length=[long](Get-Item -LiteralPath $c.path).Length;Pin-TestReceipts $c;Read-TestSource $c
}
for($i=0;$i-lt$contexts.Count;$i++){
    $c=$contexts[$i];$coldSpec=Copy-Test $complete[$i*2+1];$found=Read-TestSource $c;$coldSpec.request.persistenceLoad=[pscustomobject]$found.descriptor
    $coldSpec.artifact.observations.loadedDescriptor=Copy-Test $found.descriptor;$coldSpec.artifact.observations.processId=456
    $coldSpec.artifact.observations.combatContinuation.targetGroupId='KMC.RuntimeHostile.'+$c.request.runId+':two-pair-target'
    Check ('exact cold source target binding '+$c.request.persistenceCase){Assert-KmcChunk7PersistenceColdSource $c.request.runId $coldSpec.request $coldSpec.artifact}
    foreach($mutation in @('target-id','target-group','actor-order','same-process','archive-graph')){Reject ('cold source '+$c.request.persistenceCase+' '+$mutation){$bad=Copy-Test $coldSpec;switch($mutation){
      'target-id' {$bad.artifact.observations.combatContinuation.targetId=$horse}
      'target-group' {$bad.artifact.observations.combatContinuation.targetGroupId='KMC.RuntimeHostile.other:two-pair-target'}
      'actor-order' {$bad.artifact.observations.combatContinuation.actorIds[0]=$horse}
      'same-process' {$bad.artifact.observations.processId=$c.artifact.observations.processId}
      'archive-graph' {$bad.artifact.observations.loadedArchive.Combat.Actors[0].Native.Standard=0.25}
    };Assert-KmcChunk7PersistenceColdSource $c.request.runId $bad.request $bad.artifact}}
}
Write-Host ('CHUNK7 COMBAT PERSISTENCE SOURCE PASS='+$passed+' FAIL=0; actual private ZIP/receipt guards only; native gameplay not exercised; fixtures='+$script:sourceLab)
