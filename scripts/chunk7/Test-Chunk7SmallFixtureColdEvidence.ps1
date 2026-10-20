[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$EmittedRoot,[string]$OutputRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7SmallFixtureEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7AssistedColdInventoryEvidence.ps1')
if(-not$OutputRoot){$OutputRoot=Join-Path (Get-KmcLabRoot) ('analysis-cache/f09-cold-reader-'+[Guid]::NewGuid().ToString('N'))}
$OutputRoot=Assert-KmcChildPath $OutputRoot (Join-Path (Get-KmcLabRoot) 'analysis-cache') 'F09 cold reader output'
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve the earlier F09 reader output.'};[void][IO.Directory]::CreateDirectory($OutputRoot)
function Clone($x){ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $x -Depth 80 -Compress)}
function Put($path,$value){[void][IO.Directory]::CreateDirectory((Split-Path $path));[IO.File]::WriteAllText($path,(ConvertTo-Json -InputObject $value -Depth 80 -Compress),[Text.UTF8Encoding]::new($false))}
$rawRequest=Read-KmcJson (Join-Path $EmittedRoot 'cold-f09-actual-request.json')
$rawArtifact=Read-KmcJson (Join-Path $EmittedRoot 'cold-f09-actual-capture.json')
$rawSource=Read-KmcJson (Join-Path $EmittedRoot 'cold-f09-generated-source.json')
$template=Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json')
$script:caseLab=$null;$script:qualified=$null;$script:pass=0;$script:fail=0
function Get-KmcLabRoot {$script:caseLab}
function Get-KmcChunk7SmallFixtureColdSource($SourceRunId,$ArtifactSha256,$ArchiveSha256,$Fixture,$Candidate){
 if($SourceRunId-cne$script:qualified.sourceRunId-or$ArtifactSha256-cne$script:qualified.artifactSha256-or
    $ArchiveSha256-cne$script:qualified.descriptor.sha256){throw 'Generated F09 qualification pin differs.'}
 $script:qualified
}
function New-Fixture {
 $script:caseLab=Join-Path $OutputRoot ([Guid]::NewGuid().ToString('N'))
 [void][IO.Directory]::CreateDirectory($script:caseLab)
 $r=Clone $rawRequest;$a=Clone $rawArtifact;$s=Clone $rawSource;$o=$a.observations
 $r.evidenceRoot=Join-Path $script:caseLab ('runtime-evidence/'+$r.runId)
 $r|Add-Member NoteProperty fixture ([pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64);gameId='bbbbbbbb-1111-1111-1111-111111111111'};working=[pscustomobject]@{sha256=('c'*64);gameId='cccccccc-1111-1111-1111-111111111111'}})
 $root=Join-Path $script:caseLab ('runtime-staging/persistence-'+$r.runId+'/Saved Games')
 foreach($row in @($o.beforeLoad,$o.loadedWorld,$o.settledWorld,$o.loader,$o.settledLoader,$o.cleanup)){$row.authority.root=$root}
 foreach($row in @($o.loader,$o.settledLoader)){$row.path=Join-Path $root $r.persistenceLoad.fileName}
 $o.source.copiedArtifactPath=Join-Path (Split-Path $root -Parent) 'assisted-cold-source.json'
 $o.fixtureReadiness.isolatedSaveDescriptors.root=$root
 $o.fixtureReadiness.gameplayAssemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
 $sourcePath=Join-Path $script:caseLab ('runtime-evidence/'+$r.assistedColdInventory.sourceRunId+'/chunk7-small-fixture-evidence.json')
 $s|Add-Member NoteProperty schemaVersion 1
 Put $sourcePath $s;$sourceHash=Get-KmcSha256 $sourcePath
 $r.assistedColdInventory.sourceArtifactSha256=$sourceHash;$o.selection.sourceArtifactSha256=$sourceHash;$o.source.artifactSha256=$sourceHash
 $script:qualified=[pscustomobject]@{path='synthetic-archive';artifactPath=$sourcePath;artifactSha256=$sourceHash;
  sourceRunId=$r.assistedColdInventory.sourceRunId;sourceProcessId=$s.observations.processId;
  descriptor=[pscustomobject]@{sha256=$r.persistenceLoad.sha256}}
 $profile=Clone $template.artifact.observations.nativeProfileInventory;$rows=@()
 foreach($actor in $o.loadedWorld.party){$v=Clone $profile.party[0];$v.id=$actor.id;$v.blueprintGuid=$actor.blueprint;$v.nativeLevelUpReadiness.characterLevel=$actor.characterLevel;$v.nativeLevelUpReadiness.experience=$actor.experience;$rows+=,$v};$profile.party=$rows
 $main=@($o.loadedWorld.party|Where-Object id -CEQ $o.loadedWorld.mainActorId)[0];$q=$profile.ordinaryAcquisitionPrerequisites
 $q.nativeMoney=$o.sourceNativeMoney;$q.nativeHireCostNow=2000;$q.nativeAffordableNow=$true;$q.nativePartyLevel=$main.characterLevel;$q.mainCharacterId=$main.id;$q.mainCharacterLevel=$main.characterLevel;$q.mainCharacterExperience=$main.experience;$q.nativePartyCharacterIds=@($o.loadedWorld.partyCharacterIds|ForEach-Object{[string]$_})
 $o.nativeProfileInventory=$profile
 $o.noPreparation=Clone $template.artifact.observations.noPreparation
 $o.noPreparation.hooks[2].moduleMvid=$r.dllMvid;$o.noPreparation.hooks[3].moduleMvid=$r.dllMvid
 $o.noPreparation.hooks[1].declaringType='Kingmaker.Controllers.Combat.UnitCombatState';$o.noPreparation.hooks[1].method='OnNewRound'
 [pscustomobject]@{request=$r;artifact=$a;source=$s}
}
function Check([string]$name,[bool]$expected,[scriptblock]$action){
 $accepted=$true;$errorText=$null
 try{$f=New-Fixture;&$action $f|Out-Null}catch{$accepted=$false;$errorText=$_.Exception.Message}
 if($accepted-eq$expected){$script:pass++;Write-Host ('PASS '+$name)}else{$script:fail++;Write-Host ('FAIL '+$name+': '+$errorText)}
}
Check 'F09 separate cold actual emitted native observations' $true {param($f)Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source}
Check 'F09 cold rejects changed paid money' $false {param($f)$f.artifact.observations.loadedWorld.nativeMoney++;Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source}
Check 'F09 cold rejects changed second Horse ownership' $false {param($f)$f.artifact.observations.loadedWorld.party[6].masterId=$null;Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source}
Check 'F09 cold rejects foreign source kind' $false {param($f)$f.artifact.observations.source.kind='qualified-assisted-fixture';Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source}
Check 'F09 cold rejects same producer process' $false {param($f)$f.artifact.observations.processId=$f.source.observations.processId;Assert-KmcChunk7AssistedColdInventoryEvidence $f.request $f.artifact $f.source}
Write-Host ('F09 cold evidence PASS '+$pass+' FAIL '+$fail)
if($fail-ne0){exit 1}
