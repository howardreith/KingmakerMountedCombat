$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7AreaPersistenceEvidence.ps1')
$passed=0
$script:sourceLab=Join-Path $chunk7AnalysisRoot ('area-source-fixtures-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($script:sourceLab)
function Get-KmcLabRoot {return $script:sourceLab}
$tokens=$null;$errors=$null;$sourceAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7PersistenceSource.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Write-TestJson','Write-TestZip','Pin-TestReceipts','New-TestSource')){$f=$sourceAst.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true);. ([scriptblock]::Create($f.Extent.Text))}
$areaO=Copy-Test $o;$areaO.checkpoint='multi-area-autosave';$areaO.PSObject.Properties.Remove('saveRequested')
$areaO.nativeWrite.nativeType='Auto';$areaO.nativeWrite.nativeCallback=$false;$areaO.nativeWrite.archive=Copy-Test $areaArchive
$areaO|Add-Member NoteProperty areaTransfer (Copy-Test $areaObservation.areaTransfer)
$areaGood=New-Artifact $areaRequest $areaO
Check 'complete area source observation without invented SaveGame or callback' {Assert-KmcChunk7PersistenceObservation $areaRequest $areaGood 'PASS'}
foreach($mutation in @('explicit-save','callback','wrong-header-owner')){Reject ('complete area source '+$mutation){$bad=Copy-Test $areaGood;switch($mutation){
 'explicit-save'{$bad.observations|Add-Member NoteProperty saveRequested (Copy-Test $o.saveRequested)}
 'callback'{$bad.observations.nativeWrite.nativeCallback=$true}
 'wrong-header-owner'{$bad.observations.areaTransfer.headerStarting.owners[1].mountId=$mammoth}
 };Assert-KmcChunk7PersistenceObservation $areaRequest $bad 'PASS'}}
function New-AreaSource([string]$Label,[string]$HeaderArea=''){
 $c=New-TestSource $Label;$run=$c.request.runId;$q=Copy-Test $areaRequest;$q.runId=$run;$q|Add-Member NoteProperty evidenceRoot $c.evidence
 $a=Copy-Test $areaGood;$a.runId=$run;$newPath=Join-Path $c.profile 'Saved Games/Auto_1.zks';Move-Item -LiteralPath $c.path -Destination $newPath;$c.path=$newPath
 $c.request=$q;$c.artifact=$a;$c.owner.persistenceCase=$q.persistenceCase
 $header=[pscustomobject]@{Name='Native localized Auto';Type='Auto';CompatibilityVersion=1;GameId=$q.fixture.working.gameId;GameName=$q.fixture.working.gameName;Area=$(if($HeaderArea){$HeaderArea}else{$q.persistenceAreaTarget.area})}
 Write-TestZip $c.path $header $areaArchive;$c.sha=Get-KmcSha256 $c.path;$a.observations.nativeWrite.sha256=$c.sha;$a.observations.nativeWrite.length=[long](Get-Item -LiteralPath $c.path).Length
 $a.observations.areaTransfer.nativeArchiveSha256=$c.sha;$a.observations.nativeWrite|Add-Member NoteProperty path $c.path -Force;Pin-TestReceipts $c;return $c
}
function Read-AreaSource($Context,$Target=$areaRequest.persistenceAreaTarget){Get-KmcChunk7PersistenceSource -SourceRunId $Context.request.runId -ExpectedSha256 $Context.sha -Fixture $Context.request.fixture -Case $Context.request.persistenceCase -ExpectedDllSha256 $request.dllSha256 -ExpectedCommit $request.commit -AreaTarget $Target}
$c=New-AreaSource 'positive'
Check 'actual Auto ZIP destination and explicit source pin' {$found=Read-AreaSource $c;if($found.descriptor.fileName-cne'Auto_1.zks'-or$found.descriptor.area-cne$areaRequest.persistenceAreaTarget.area){throw 'Area source selected wrong exact native category or destination.'}}
Reject 'area source requires explicit caller target' {Read-AreaSource $c $null}
Reject 'same-length wrong enter point source pin' {$target=Copy-Test $areaRequest.persistenceAreaTarget;$target.enterPoint='d'*32;Read-AreaSource $c $target}
Reject 'source uses original area despite exact repinned transport' {$bad=New-AreaSource 'wrong-header-area' ('a'*32);Read-AreaSource $bad}
$found=Read-AreaSource $c
$q=Copy-Test $coldRequest;$q.persistenceCase='multi-area-autosave';$q.persistenceLoad=[pscustomobject]$found.descriptor;$q|Add-Member NoteProperty persistenceAreaTarget (Copy-Test $areaRequest.persistenceAreaTarget)
$v=Copy-Test $co;$v.checkpoint='multi-area-autosave';$v.loadedArchive=Copy-Test $areaArchive;$v.loadedDescriptor=Copy-Test $q.persistenceLoad
$a=New-Artifact $q $v
Check 'complete cold Auto restoration at exact destination' {Assert-KmcChunk7PersistenceObservation $q $a 'PASS';Assert-KmcChunk7PersistenceColdSource $c.request.runId $q $a}
Reject 'cold source substitutes enter point without changing area' {$bad=Copy-Test $q;$bad.persistenceAreaTarget.enterPoint='d'*32;Assert-KmcChunk7PersistenceColdSource $c.request.runId $bad $a}
Reject 'cold source substitutes stable owner graph' {$bad=Copy-Test $a;$bad.observations.loadedArchive.Pairs[1].Generation++;Assert-KmcChunk7PersistenceColdSource $c.request.runId $q $bad}
Write-Host ('CHUNK7 AREA SOURCE PASS='+$passed+' FAIL=0; actual private ZIP and immutable source/cold guards only; fixtures='+$script:sourceLab)
