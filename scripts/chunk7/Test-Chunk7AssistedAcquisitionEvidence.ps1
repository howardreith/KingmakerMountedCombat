[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$TwoOwnerFixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AssistedAcquisitionConnectionFixture.json'}
if(-not$TwoOwnerFixturePath){$TwoOwnerFixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/TwoOwnerManualPaidFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/assisted-connection-evidence-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh generated output required.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0;$watch=[Diagnostics.Stopwatch]::StartNew()
function Copy-Connection($Value){return ($Value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
function New-ConnectionFixture{return ([IO.File]::ReadAllText($FixturePath)|ConvertFrom-Json)}
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$caught=$false;try{&$Body}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
function Project-Connection($F){Assert-KmcAssistedCumulativeSource $F.request $F.producer $F.cold}
Good 'actual formatter-shaped source projects main XP from readiness' {$f=New-ConnectionFixture;$p=Project-Connection $f;Assert-KmcAssistedSame $p $f.expectedProjection 'actual CSharp projection';if($null-ne$f.producer.observations.settledWorld.party[0].PSObject.Properties['experience']){throw 'Native-shaped main XP omission lost.'}}
$mutations=[ordered]@{
 'cold candidate'={param($f)$f.cold.dllSha256='f'*64}
 'producer status'={param($f)$f.producer.status='FAIL'}
 'cold callback'={param($f)$f.cold.observations.settledLoader.callbackObserved=$false}
 'cold source byte pin'={param($f)$f.cold.observations.selection.sourceArchiveSha256='e'*64}
 'cold source graph copy'={param($f)$f.cold.observations.sourceNativeWorld.party[0].blueprint='e'*32}
 'main XP loaded'={param($f)$f.cold.observations.loadedWorld.party[0].experience++}
 'main XP settled'={param($f)$f.cold.observations.settledWorld.party[0].experience++}
 'main progression missing'={param($f)$f.producer.observations.fixtureReadiness.PSObject.Properties.Remove('mainProgression')}
 'main progression mismatched'={param($f)$f.cold.observations.fixtureReadiness.mainProgression.experience++}
 'nonmain XP'={param($f)$f.cold.observations.loadedWorld.party[1].experience++}
 'money'={param($f)$f.cold.observations.settledWorld.nativeMoney++}
 'readiness hire cost'={param($f)$f.request.cumulativeAcquisition.readiness.nativeHireCost++}
 'cold writes'={param($f)$f.cold.observations.moneyWrites=1}
 'ordinary proof invented'={param($f)$f.cold.observations.ordinaryAcquisitionProven=$true}
 'rank lost'={param($f)$f.producer.observations.settledWorld.party[0].features[1].rank=0;$f.cold.observations.sourceNativeWorld=Copy-Connection $f.producer.observations.settledWorld}
 'prior pair changed'={param($f)$f.producer.observations.settledWorld.party[0].petId=$null;$f.cold.observations.sourceNativeWorld=Copy-Connection $f.producer.observations.settledWorld}
 'same source run'={param($f)$f.request.cumulativeAcquisition.assistedColdRunId=$f.request.cumulativeAcquisition.sourceRunId}
 'new mode singleton'={param($f)$f.request.cumulativeAcquisition.sourceMode=@($f.request.cumulativeAcquisition.sourceMode)}
}
foreach($m in $mutations.GetEnumerator()){Bad $m.Key {$f=New-ConnectionFixture;&$m.Value $f;[void](Project-Connection $f)}}
function New-CopiedSource($F){$s=$F.request.cumulativeAcquisition;$p=Project-Connection $F;[pscustomobject]@{processId=999999;assistedSource=[pscustomobject]@{contract='assisted-producer-cold-cumulative-v1';mode=$s.sourceMode;producerRunId=$s.sourceRunId;producerArtifactSha256=$s.sourceArtifactSha256;coldRunId=$s.assistedColdRunId;coldArtifactSha256=$s.assistedColdArtifactSha256;archiveSha256=$s.sourceArchiveSha256;setupWasAssisted=$true;setupIsOrdinaryAcquisitionProof=$false;coldObservations=$F.cold.observations;derivedInputs=$p}}}
Good 'copied raw source and explicit projection remain distinct' {$f=New-ConnectionFixture;$o=New-CopiedSource $f;Assert-KmcAssistedCumulativeCopiedSource $f.request $o $f.cold $f.expectedProjection}
foreach($kind in @('ordinary claim','new process','projection','cold bytes')){Bad ('copied '+$kind){$f=New-ConnectionFixture;$o=New-CopiedSource $f;switch($kind){'ordinary claim'{$o.assistedSource.setupIsOrdinaryAcquisitionProof=$true};'new process'{$o.processId=$f.cold.observations.processId};'projection'{$o.assistedSource.derivedInputs.afterSaveArrangement.money++};'cold bytes'{$o.assistedSource.coldArtifactSha256='a'*64}};Assert-KmcAssistedCumulativeCopiedSource $f.request $o $f.cold $f.expectedProjection}}
function New-AssistedSmallSource {
 $f=[IO.File]::ReadAllText($TwoOwnerFixturePath)|ConvertFrom-Json;$o=$f.artifact.observations;$choice=$o.sourceAcquisition.selection
 $choice|Add-Member sourceMode 'assisted-producer-and-cold';$choice|Add-Member assistedColdRunId 'synthetic-assisted-cold';$choice|Add-Member assistedColdArtifactSha256 ('a'*64)
 $choice.nativeWriteLeaf='Manual_301_KMC_C7_MANUAL_DERIVED_FIELD.zks'
 $o.sourceAcquisition|Add-Member assistedSource ([pscustomobject]@{contract='assisted-producer-cold-cumulative-v1';setupWasAssisted=$true;setupIsOrdinaryAcquisitionProof=$false})
 $s=[pscustomobject]@{sourceRunId=$f.request.runId;sourceArtifactSha256='a'*64;sourceArchiveSha256=$o.nativeWrite.sha256;actorId=$o.selection.actorId;petId=$o.selection.petId}
 [pscustomobject]@{source=$s;observations=$o}
}
Good 'distinct assisted two-owner Small source validates retained ordinary choices' {$f=New-AssistedSmallSource;Assert-KmcChunk7SmallManualSourceObservation $f.source $f.observations -Assisted}
Bad 'old Small source still refuses two owners' {$f=New-AssistedSmallSource;Assert-KmcChunk7SmallManualSourceObservation $f.source $f.observations}
foreach($kind in @('mode','paid','prior fact','ordinary claim')){Bad ('Small source '+$kind){$f=New-AssistedSmallSource;switch($kind){'mode'{$f.observations.sourceAcquisition.selection.sourceMode='foreign'};'paid'{$f.observations.sourceAcquisition.selection.case='paid-medium-ranger-horse'};'prior fact'{$f.observations.twoOwners[0].rank++};'ordinary claim'{$f.observations.sourceAcquisition.assistedSource.setupIsOrdinaryAcquisitionProof=$true}};Assert-KmcChunk7SmallManualSourceObservation $f.source $f.observations -Assisted}}
function New-InheritedPair {
 $f=New-AssistedSmallSource;$saved=$f.observations;$p=$saved.twoOwners[0];$actors=@();$label=1000
 foreach($a in $saved.savedWorld.party){$label+=3;$actors+=,[pscustomobject]@{id=$a.id;object=$label;descriptor=$label+1;view=$label+2}}
 $owner=@($actors|Where-Object id -CEQ $p.actorId)[0];$pet=@($actors|Where-Object id -CEQ $p.petId)[0]
 $row=[pscustomobject]@{actorId=$p.actorId;petId=$p.petId;actorObject=$owner.object;petObject=$pet.object;actorDescriptor=$owner.descriptor;petDescriptor=$pet.descriptor;actorView=$owner.view;petView=$pet.view;featureObject=2001;sourceObject=2002;rankObject=2003;actorBlueprint=$p.actorBlueprint;petBlueprint=$p.petBlueprint;featureGuid=$p.featureGuid;sourceGuid=$p.sourceGuid;rank=$p.rank;reciprocal=$true;resources=@($saved.resourcesAfterSave.actors|Where-Object{$_.actorId-cin@($p.actorId,$p.petId)})}
 [pscustomobject]@{saved=$saved;observations=[pscustomobject]@{inheritedAssistedPairBefore=$row;inheritedAssistedPairAfter=Copy-Connection $row;manualNativeRoster=[pscustomobject]@{actors=$actors}}}
}
Good 'Small inherited pair exact identities/facts/resources' {$f=New-InheritedPair;Assert-KmcAssistedSmallInheritedPair $f.observations $f.saved}
foreach($kind in @('rank','fact','actor','resource','alias','pair removed')){Bad ('Small inherited '+$kind){$f=New-InheritedPair;$o=$f.observations;switch($kind){'rank'{$o.inheritedAssistedPairAfter.rank++};'fact'{$o.inheritedAssistedPairAfter.featureObject++};'actor'{@($o.manualNativeRoster.actors|Where-Object id -CEQ $o.inheritedAssistedPairBefore.actorId)[0].object++};'resource'{$o.inheritedAssistedPairAfter.resources[0].standard++};'alias'{$o.inheritedAssistedPairBefore.featureObject=$o.inheritedAssistedPairBefore.actorObject;$o.inheritedAssistedPairAfter.featureObject=$o.inheritedAssistedPairBefore.actorObject};'pair removed'{$o.manualNativeRoster.actors=@($o.manualNativeRoster.actors|Where-Object id -CNE $o.inheritedAssistedPairBefore.actorId)}};Assert-KmcAssistedSmallInheritedPair $o $f.saved}}
$watch.Stop();[ordered]@{schemaVersion=1;pass=$script:pass;fail=$script:fail;seconds=$watch.Elapsed.TotalSeconds;nativeInvoked=$false;archiveReads=0;boundary='Actual new projection and inherited-source readers. Producer/cold formatting derived from actual facade emitters; inventory and historical two-owner native observation fixtures are generated. Complete source/restoration qualification is separate.';fixtures=@($FixturePath,$TwoOwnerFixturePath)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){throw 'Assisted connection reader checks failed.'}
