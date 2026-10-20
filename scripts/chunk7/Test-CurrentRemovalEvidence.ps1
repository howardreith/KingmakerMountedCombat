param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$draft=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$reader=Join-Path $draft 'scripts/runtime'
. (Join-Path $reader 'RuntimeHarness.Common.ps1')
. (Join-Path $reader 'PersistenceSaveFixtures.ps1')
$script:ownedRemovalLab=Join-Path $OutputDirectory 'synthetic-lab';[IO.Directory]::CreateDirectory($script:ownedRemovalLab)|Out-Null
function Get-KmcLabRoot{return $script:ownedRemovalLab}
function Copy-RemovalValue($Value){$Value|ConvertTo-Json -Depth 70|ConvertFrom-Json}
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $draft 'scripts/Test-PersistenceSaveFixtures.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count-ne0){throw 'Pinned existing P07 fixture generator has parse errors.'}
$node=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-KmcRemovalRow'},$true));if($node.Count-ne1){throw 'Expected one existing P07 generated-row helper.'}
. ([scriptblock]::Create($node[0].Extent.Text.Replace('rider-a','10101010-1010-4010-8010-101010101010').Replace('mount-a','20202020-2020-4020-8020-202020202020')))
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Reject=$false){$why=$null;$ok=$false;try{&$Body;$ok=-not$Reject}catch{$why=$_.Exception.Message;$ok=$Reject};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$why});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$why}else{''}))}
function New-RemovalNative([string]$Id){[pscustomobject][ordered]@{Id=$Id;Standard=0.0;Move=0.25;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}}
function New-RemovalProjection($a,[bool]$Mounted){[pscustomobject][ordered]@{SchemaVersion=2;CampaignId=$a.CampaignId;AreaId=$a.AreaId;GameTimeTicks=$a.GameTimeTicks;Policy=$a.Policy;RulesId=$a.RulesId;Mounted=$Mounted;ProfileId=$(if($Mounted){$a.Pairs[0].ProfileId}else{$null});Rider=$(if($Mounted){Copy-RemovalValue $a.Pairs[0].Rider}else{$null});Mount=$(if($Mounted){Copy-RemovalValue $a.Pairs[0].Mount}else{$null});Slots=@($a.Slots|ForEach-Object{Copy-RemovalValue $_});Combat=$null}}
function Write-RemovalSyntheticZip([string]$Path,$Header,$Kmc,$Extras){
 Add-Type -AssemblyName System.IO.Compression
 $stream=[IO.File]::Open($Path,[IO.FileMode]::Create);$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
 try{$parts=[ordered]@{'header.json'=($Header|ConvertTo-Json -Depth 20 -Compress);'kmc-mounted-state'=($Kmc|ConvertTo-Json -Depth 30 -Compress)};if($null-ne$Extras){foreach($key in $Extras.Keys){$parts[$key]=$Extras[$key]}}
  foreach($key in $parts.Keys){$writer=[IO.StreamWriter]::new($zip.CreateEntry($key).Open(),[Text.UTF8Encoding]::new($false));try{$writer.Write($parts[$key])}finally{$writer.Dispose()}}
 }finally{$zip.Dispose();$stream.Dispose()}
}
function Sync-RemovalArchive($v,[bool]$Cleanup,$Extras){
 $rec=if($Cleanup){$v.rows[5].detail.cleanup}else{$v.rows[1].detail};$f=if($v.request.PSObject.Properties['ownerStockRemoval'] -and $v.request.ownerStockRemoval){$v.request.persistenceLoad}else{$v.request.fixture.working};$name=if($Cleanup){'KMC_CLEANUP'}else{'KMC_P01'}
 Write-RemovalSyntheticZip $rec.path ([ordered]@{Name=$name;Type='Manual';CompatibilityVersion=1;GameId=$f.gameId;GameName=$f.gameName;Area=$f.area}) $rec.nativeArchive $Extras
 $rec.sha256=Get-KmcSha256 $rec.path;$rec.length=(Get-Item -LiteralPath $rec.path).Length
 if($Cleanup){$scan=Get-KmcCurrentRemovalReferenceScan $rec.path;$v.rows[5].detail.cleanupSha256=$rec.sha256;$v.rows[5].detail.scannedMembers=$scan.scannedMembers;$v.rows[5].detail.scannedBytes=$scan.scannedBytes}
 else{$v.rows[5].detail.firstSha256=$rec.sha256}
}
function Bind-RemovalResult($v){
 $r=$v.request;$e=Join-Path $script:ownedRemovalLab ('runtime-evidence/'+$r.runId);$s=Join-Path $script:ownedRemovalLab ('runtime-staging/persistence-'+$r.runId)
 [IO.Directory]::CreateDirectory($e)|Out-Null
 $lines=@($v.rows|ForEach-Object{$_|ConvertTo-Json -Depth 40 -Compress});[IO.File]::WriteAllLines((Join-Path $e 'persistence-observations.jsonl'),[string[]]$lines,[Text.UTF8Encoding]::new($false))
 Write-KmcJsonAtomic (Join-Path $e 'runtime-request.json') $r
 Write-KmcJsonAtomic (Join-Path $s 'owner.json') ([ordered]@{runId=$r.runId;scenario=$r.scenario;persistenceCase=$r.persistenceCase;transactionToken=$r.transactionToken})
 $manifest=[pscustomobject]@{runId=$r.runId;artifacts=@([pscustomobject]@{relativePath='persistence-observations.jsonl';kind='persistence-evidence';sha256=(Get-KmcSha256 (Join-Path $e 'persistence-observations.jsonl'));length=(Get-Item -LiteralPath (Join-Path $e 'persistence-observations.jsonl')).Length})}
 Write-KmcJsonAtomic (Join-Path $e 'runtime-artifacts.json') $manifest
 $game=[pscustomobject]@{runId=$r.runId;scenario=$r.scenario;processId=4242;status='PASS';evidenceManifestSha256=(Get-KmcSha256 (Join-Path $e 'runtime-artifacts.json'))}
 Write-KmcJsonAtomic (Join-Path $e 'runtime-game-result.json') $game
 $result=[ordered]@{runId=$r.runId;scenario=$r.scenario;status='PASS';transactionToken=$r.transactionToken;commit=$r.commit;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;branch=$r.branch;productVersion=$r.productVersion;errors=@();modsRestored=$true;workingRestored=$true;saveProtectionPassed=$true;baselineImmutable=$true;saveWriteAllowlistPassed=$true;gameResultSha256=(Get-KmcSha256 (Join-Path $e 'runtime-game-result.json'));evidenceManifestSha256=$game.evidenceManifestSha256}
 Write-KmcJsonAtomic (Join-Path $e 'runtime-result.json') $result
 $v|Add-Member manifest $manifest -Force;$v|Add-Member game $game -Force
}
function New-CurrentRemovalFixture{
 $id='synthetic-current-removal-'+[Guid]::NewGuid().ToString('N');$script:removalRoot=Join-Path $script:ownedRemovalLab ('runtime-staging/persistence-'+$id+'/Saved Games');[IO.Directory]::CreateDirectory($script:removalRoot)|Out-Null
 $input=Join-Path $script:removalRoot 'Manual_299_KMC_AUTOMATION_WORKING.zks';[IO.File]::WriteAllBytes($input,[byte[]]@(1,2,3,4))
 $fixture=[pscustomobject]@{baseline=[pscustomobject]@{sha256=('b'*64)};working=[pscustomobject]@{internalName='KMC_AUTOMATION_WORKING';fileName=[IO.Path]::GetFileName($input);sha256=(Get-KmcSha256 $input);length=4;lastWriteTimeUtcTicks=(Get-Item -LiteralPath $input).LastWriteTimeUtc.Ticks;gameId='90909090-9090-4090-8090-909090909090';gameName='Generated fixture';area=('a'*32)}}
 $r=[pscustomobject]@{scenario='persistence-p07-save';persistenceCase='prepare-removal';runId=$id;fixture=$fixture;commit=('1'*40);dllSha256=('2'*64);dllMvid='33333333-3333-4333-8333-333333333333';branch='codex/private-reader-test';productVersion='generated';transactionToken=('f'*64);evidenceRoot=(Join-Path $script:ownedRemovalLab ('runtime-evidence/'+$id))}
 $rider='10101010-1010-4010-8010-101010101010';$mount='20202020-2020-4020-8020-202020202020'
 $a=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$fixture.working.gameId;AreaId=$fixture.working.area;GameTimeTicks=100000;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=@([pscustomobject]@{PairId=('c'*32);Generation=1;ProfileId='medium-humanoid-mammoth-v1';Rider=(New-RemovalNative $rider);Mount=(New-RemovalNative $mount)});Participations=@();Slots=@([pscustomobject]@{ActorId=$rider;Index=10;Kind=2},[pscustomobject]@{ActorId=$rider;Index=11;Kind=3});Combat=$null}
 $c=Copy-RemovalValue $a;$c.Pairs=@();$c.Slots=@();$c.GameTimeTicks=200000
 $open=[pscustomobject]@{ordinal=1;path=(Join-Path $script:removalRoot 'Manual_300_KMC_P01.zks');sha256=('0'*64);length=1;nativeType='Manual';nativeCallback=$true;operation='None';snapshot=(New-RemovalProjection $a $true);nativeSourceSchema=3;snapshotProjection='schema3-single-pair-observation-v1';nativeArchive=$a}
 $clean=[pscustomobject]@{path=(Join-Path $script:removalRoot 'Manual_301_KMC_CLEANUP.zks');leaf='Manual_301_KMC_CLEANUP.zks';sha256=('0'*64);length=1;nativeType='Manual';internalName='KMC_CLEANUP';gameId=$fixture.working.gameId;area=$fixture.working.area;operation='None';kmcMember='Current';snapshot=(New-RemovalProjection $c $false);nativeSourceSchema=3;snapshotProjection='schema3-single-pair-observation-v1';nativeArchive=$c}
 $script:firstSha=$open.sha256
 $rows=@([pscustomobject]@{kind='initial';checkpoint='prepare-removal';stage=0;relationship='Mounted';rider=[pscustomobject]@{Id=$rider};mount=[pscustomobject]@{Id=$mount};detail=$null},[pscustomobject]@{kind='native-write-complete';checkpoint='prepare-removal';stage=1000;relationship='Mounted';rider=[pscustomobject]@{Id=$rider};mount=[pscustomobject]@{Id=$mount};detail=$open},
  (New-KmcRemovalRow 'removal-refused' 1000 'Mounted' @{state='Refused';assessments=1;refusals=1;beganRefused=$true;spawnedId='horse-x';spawnedBlueprint='4016c7db400ab721ff125aef9e65e202';references=@('unit Horse [horse-x] is the KMC Horse');reasons=@('Permanent KMC Horse remains.')}),
  (New-KmcRemovalRow 'removal-refused-combat' 1002 'Mounted' @{state='Refused';refusals=2;combatRefused=$true;partyCombat=$true;targetId='enemy-e';reasons=@('The party is in combat.')}),
  (New-KmcRemovalRow 'removal-requested' 1003 'Unmounted' @{state='Saving';refusals=2;began=$true}),
  (New-KmcRemovalRow 'removal-prepared' 1004 'Unmounted' @{state='Ready';refusals=2;cleanupSaves=1;cleanupLeaf=$clean.leaf;cleanupSha256=$clean.sha256;cleanupCampaign=$fixture.working.gameId;binding='bound';scannedMembers=1;scannedBytes=1;snapshots=2;completedSaves=2;cleanup=$clean;firstSha256=$open.sha256}),
  (New-KmcRemovalRow 'removal-disabled-reenabled' 1004 'Mounted' @{state='Ready';refusals=2;cleanupSaves=1;disabled=$true;reEnabled=$true;remounted=$true}))
 foreach($kind in @('movement-dispatched','movement-completed','attack-dispatched','attack-delivered','usable-continuation-complete')){$rows+=,[pscustomobject]@{kind=$kind;checkpoint='prepare-removal';stage=1010;relationship='Mounted';rider=[pscustomobject]@{Id=$rider};mount=[pscustomobject]@{Id=$mount};detail=[pscustomobject]@{rules=1;rolls=1}}}
 foreach($row in $rows){foreach($kv in @(@('runId',$id),@('scenario',$r.scenario),@('source',$r.commit),@('dll',$r.dllSha256),@('processId',4242),@('controls',[pscustomobject]@{DuplicateFactCount=0}))){$row|Add-Member $kv[0] $kv[1] -Force}}
 $v=[pscustomobject]@{request=$r;rows=$rows};Sync-RemovalArchive $v $false $null;Sync-RemovalArchive $v $true ([ordered]@{'areas/stashed.json'='{"native":"stock references only"}'});Bind-RemovalResult $v;$v
}
function New-OwnerStockRemovalReaderFixture{
 $v=New-CurrentRemovalFixture
 $root=$script:removalRoot;$source=Join-Path $root 'Manual_304_KMC_C7_STOCK_MAMMOTH.zks'
 Move-Item -LiteralPath (Join-Path $root 'Manual_299_KMC_AUTOMATION_WORKING.zks') -Destination $source
 $file=Get-Item -LiteralPath $source
 $v.request|Add-Member ownerStockRemoval $true
 $v.request|Add-Member persistenceLoad ([pscustomobject]@{internalName='KMC_C7_STOCK_MAMMOTH';fileName=$file.Name;sha256=(Get-KmcSha256 $source);length=$file.Length;lastWriteTimeUtcTicks=$file.LastWriteTimeUtc.Ticks;gameId='3f7381f7-aba6-43bf-bae6-39c6a66a7180';gameName='Testy the Modless';area='b10e86516b6437d45a629931c30936fe'})
 $text=$v.rows|ConvertTo-Json -Depth 60 -Compress
 $text=$text.Replace('10101010-1010-4010-8010-101010101010','73681ef2-b281-4bc6-bc05-2de68c1616cb').Replace('20202020-2020-4020-8020-202020202020','628c6d9a-5ab7-4e87-8202-9fcbe38a7827').Replace('90909090-9090-4090-8090-909090909090','3f7381f7-aba6-43bf-bae6-39c6a66a7180').Replace(('a'*32),'b10e86516b6437d45a629931c30936fe')
 $v.rows=$text|ConvertFrom-Json
 Move-Item -LiteralPath (Join-Path $root 'Manual_300_KMC_P01.zks') -Destination (Join-Path $root 'Manual_305_KMC_P01.zks')
 Move-Item -LiteralPath (Join-Path $root 'Manual_301_KMC_CLEANUP.zks') -Destination (Join-Path $root 'Manual_306_KMC_CLEANUP.zks')
 $v.rows[1].detail.path=Join-Path $root 'Manual_305_KMC_P01.zks'
 $v.rows[5].detail.cleanup.path=Join-Path $root 'Manual_306_KMC_CLEANUP.zks'
 $v.rows[5].detail.cleanup.leaf='Manual_306_KMC_CLEANUP.zks';$v.rows[5].detail.cleanupLeaf='Manual_306_KMC_CLEANUP.zks'
 Sync-RemovalArchive $v $false $null
 Sync-RemovalArchive $v $true ([ordered]@{'areas/stashed.json'='{"native":"stock references only"}'})
 Bind-RemovalResult $v
 $v
}
Check 'current P07 full native schema3 opening and cleanup plus continuation' {$v=New-CurrentRemovalFixture;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game}
Check 'owner stock P07 synthetic public reader uses distinct input and mounted305/cleanup306 graph' {$v=New-OwnerStockRemovalReaderFixture;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game}
Check 'owner stock P07 public reader rejects a missing mounted305 despite valid cleanup306' {$v=New-OwnerStockRemovalReaderFixture;$v.rows[1].detail.path=Join-Path $script:removalRoot 'Manual_300_KMC_P01.zks';Bind-RemovalResult $v;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game} $true
Check 'owner stock P07 public reader rejects Working ancestry relabelled as its stock input' {$v=New-OwnerStockRemovalReaderFixture;$v.request.ownerStockRemoval=$false;Bind-RemovalResult $v;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game} $true
Check 'owner stock P07 refuses historical schema2 fallback even with exact two native writes' {$v=New-OwnerStockRemovalReaderFixture;$f=$v.request.persistenceLoad
 foreach($rec in @($v.rows[1].detail,$v.rows[5].detail.cleanup)){
  $name=if($rec.snapshot.Mounted){'KMC_P01'}else{'KMC_CLEANUP'}
  Write-RemovalSyntheticZip $rec.path ([ordered]@{Name=$name;Type='Manual';CompatibilityVersion=1;GameId=$f.gameId;GameName=$f.gameName;Area=$f.area}) $rec.snapshot $null
  $rec.sha256=Get-KmcSha256 $rec.path;$rec.length=(Get-Item -LiteralPath $rec.path).Length
  foreach($key in @('nativeSourceSchema','snapshotProjection','nativeArchive')){$rec.PSObject.Properties.Remove($key)}
 }
 $v.rows[5].detail.firstSha256=$v.rows[1].detail.sha256;$v.rows[5].detail.cleanupSha256=$v.rows[5].detail.cleanup.sha256
 Bind-RemovalResult $v;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game
} $true
Check 'current P07 actual cleanup source qualifies through public intake' {$v=New-CurrentRemovalFixture;$s=Get-KmcPersistenceSource $v.request.runId $v.rows[5].detail.cleanup.sha256 $v.request.fixture -NativeCase prepare-removal -ArtifactRole cleanup-manual;if($s.descriptor.sha256-cne$v.rows[5].detail.cleanup.sha256){throw 'Wrong source bytes.'}}
Check 'historical schema2 P07 reader and public intake remain separate' {$v=New-CurrentRemovalFixture;$f=$v.request.fixture.working
 foreach($rec in @($v.rows[1].detail,$v.rows[5].detail.cleanup)){
  $name=if($rec.snapshot.Mounted){'KMC_P01'}else{'KMC_CLEANUP'}
  Write-RemovalSyntheticZip $rec.path ([ordered]@{Name=$name;Type='Manual';CompatibilityVersion=1;GameId=$f.gameId;GameName=$f.gameName;Area=$f.area}) $rec.snapshot $null
  $rec.sha256=Get-KmcSha256 $rec.path;$rec.length=(Get-Item -LiteralPath $rec.path).Length
  foreach($key in @('nativeSourceSchema','snapshotProjection','nativeArchive')){$rec.PSObject.Properties.Remove($key)}
 }
 $v.rows[5].detail.firstSha256=$v.rows[1].detail.sha256;$v.rows[5].detail.cleanupSha256=$v.rows[5].detail.cleanup.sha256;Bind-RemovalResult $v
 Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game
 Get-KmcPersistenceSource $v.request.runId $v.rows[5].detail.cleanup.sha256 $v.request.fixture -NativeCase prepare-removal -ArtifactRole cleanup-manual|Out-Null
}
Check 'integration-disabled evidence cannot claim the genuine DLL-absent reader' {$v=New-CurrentRemovalFixture;$r=$v.request;$r.scenario='persistence-p07-load';$r.persistenceCase='removal-no-dll';Assert-KmcPersistenceScenarioEvidence $r $v.manifest 'PASS' $v.game} $true
$faults=[ordered]@{
 'opening second owner'={param($v)$v.rows[1].detail.nativeArchive.Pairs+=,(Copy-RemovalValue $v.rows[1].detail.nativeArchive.Pairs[0])}
 'cleanup extra pair'={param($v)$v.rows[5].detail.cleanup.nativeArchive.Pairs=@($v.rows[1].detail.nativeArchive.Pairs[0])}
 'cleanup retained owner'={param($v)$v.rows[5].detail.cleanup.nativeArchive.Participations=@([pscustomobject]@{OwnerId=('d'*32)})}
 'cleanup live slot'={param($v)$v.rows[5].detail.cleanup.nativeArchive.Slots=@($v.rows[1].detail.nativeArchive.Slots[0])}
 'cleanup combat'={param($v)$v.rows[5].detail.cleanup.nativeArchive.Combat=[pscustomobject]@{TurnBased=$true}}
 'opening Horse profile'={param($v)$v.rows[1].detail.nativeArchive.Pairs[0].ProfileId='medium-humanoid-horse-v1'}
 'opening actor substitution'={param($v)$v.rows[1].detail.nativeArchive.Pairs[0].Mount.Id='40404040-4040-4040-8040-404040404040'}
 'opening native debt projection mismatch'={param($v)$v.rows[1].detail.snapshot.Mount.Move++}
 'cleanup projection mismatch'={param($v)$v.rows[5].detail.cleanup.snapshot.Slots=@($v.rows[1].detail.nativeArchive.Slots[0])}
 'projection unlabelled'={param($v)$v.rows[1].detail.PSObject.Properties.Remove('snapshotProjection')}
 'schema2 called native current'={param($v)$v.rows[1].detail.nativeSourceSchema=2}
 'current schema string'={param($v)$v.rows[5].detail.cleanup.nativeSourceSchema='3'}
 'native graph omitted'={param($v)$v.rows[5].detail.cleanup.PSObject.Properties.Remove('nativeArchive')}
 'future native schema'={param($v)$v.rows[5].detail.cleanup.nativeArchive.SchemaVersion=4}
 'projection masquerades as native3'={param($v)$v.rows[1].detail.snapshot.SchemaVersion=3}
 'Kind5 projection not representable'={param($v)$v.rows[1].detail.nativeArchive.Slots[0].Kind=5}
 'native ZIP differs from recorded graph'={param($v)$v.rows[1].detail.nativeArchive.GameTimeTicks++}
 'stashed permanent Horse reference'={param($v)Sync-RemovalArchive $v $true ([ordered]@{'areas/stashed.json'='{"fact":"4016c7db400ab721ff125aef9e65e202"}'})}
 'stashed mounted Charge reference case insensitive'={param($v)Sync-RemovalArchive $v $true ([ordered]@{'areas/stashed.JSON'='{"fact":"D79EAEC224A7A832E738EB81BAEF9D49"}'})}
 'reference scan counter mismatch'={param($v)$v.rows[5].detail.scannedBytes++}
 'unmeasured extra archive'={param($v)[IO.File]::WriteAllBytes((Join-Path (Split-Path $v.rows[1].detail.path) 'Manual_302_UNMEASURED.zks'),[byte[]]@(9))}
 'qualified input changed'={param($v)[IO.File]::WriteAllBytes((Join-Path (Split-Path $v.rows[1].detail.path) $v.request.fixture.working.fileName),[byte[]]@(9))}
 'first archive modified'={param($v)[IO.File]::WriteAllBytes($v.rows[1].detail.path,[byte[]]@(9))}
 'registered reenable failed'={param($v)$v.rows[6].detail.reEnabled=$false}
 'native continuation omitted'={param($v)$v.rows=@($v.rows|Where-Object kind -CNE 'attack-delivered')}
}
foreach($key in $faults.Keys){$faultAction=$faults[$key];Check ('refuses '+$key) {$v=New-CurrentRemovalFixture;&$faultAction $v;Bind-RemovalResult $v;Assert-KmcPersistenceScenarioEvidence $v.request $v.manifest 'PASS' $v.game} $true}
foreach($key in @('source failed','source un-restored','source manifest changed','source observations changed','source foreign candidate')){Check ('public current source refuses '+$key){$v=New-CurrentRemovalFixture;$e=$v.request.evidenceRoot
 switch($key){'source failed'{$p=Join-Path $e 'runtime-result.json';$x=Read-KmcJson $p;$x.status='FAIL';Write-KmcJsonAtomic $p $x}'source un-restored'{$p=Join-Path $e 'runtime-result.json';$x=Read-KmcJson $p;$x.baselineImmutable=$false;Write-KmcJsonAtomic $p $x}'source manifest changed'{[IO.File]::AppendAllText((Join-Path $e 'runtime-artifacts.json'),' ')}'source observations changed'{[IO.File]::AppendAllText((Join-Path $e 'persistence-observations.jsonl'),' ')}'source foreign candidate'{$p=Join-Path $e 'runtime-result.json';$x=Read-KmcJson $p;$x.commit=('9'*40);Write-KmcJsonAtomic $p $x}}
 Get-KmcPersistenceSource $v.request.runId $v.rows[5].detail.cleanup.sha256 $v.request.fixture -NativeCase prepare-removal -ArtifactRole cleanup-manual|Out-Null} $true}
foreach($key in @('schema1','schema string','future schema')){Check ('public current source refuses '+$key){$v=New-CurrentRemovalFixture;$rec=$v.rows[5].detail.cleanup;$rec.nativeArchive.SchemaVersion=switch($key){'schema1'{1}'schema string'{'3'}'future schema'{4}};Sync-RemovalArchive $v $true $null;Bind-RemovalResult $v
 Get-KmcPersistenceSource $v.request.runId $rec.sha256 $v.request.fixture -NativeCase prepare-removal -ArtifactRole cleanup-manual|Out-Null} $true}
$clock.Stop();$summary=[ordered]@{scope='generated current P07 legacy and owner-stock reader/source checks only; no native or DLL-absent credit';passed=@($results|Where-Object pass -EQ $true).Count;failed=@($results|Where-Object pass -EQ $false).Count;durationSeconds=$clock.Elapsed.TotalSeconds;inputs=@(@($PSCommandPath,(Join-Path $reader 'CurrentRemovalEvidence.ps1'),(Join-Path $reader 'PersistenceSaveFixtures.ps1'),(Join-Path $draft 'scripts/Test-PersistenceSaveFixtures.ps1'))|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-KmcSha256 $_)}});results=$results.ToArray()}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'summary.json'),($summary|ConvertTo-Json -Depth 60),[Text.UTF8Encoding]::new($false));Write-Output ('RESULT '+$summary.passed+' passed; '+$summary.failed+' failed; '+$clock.Elapsed.TotalSeconds+' seconds');if($summary.failed-gt0){exit 1}
