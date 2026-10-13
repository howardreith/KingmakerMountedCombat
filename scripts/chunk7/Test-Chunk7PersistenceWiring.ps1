$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7CombatEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7RealtimeEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7NativeLifeEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/ScenarioDispatchEvidence.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Body){$refused=$false;try{& $Body}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
function Copy-Test($value){return ($value|ConvertTo-Json -Depth 90 -Compress|ConvertFrom-Json)}
$private=Join-Path $chunk7AnalysisRoot ('wiring-probe-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($private)
$overlay=Join-Path $repo 'scripts/runtime'
foreach($name in @('Invoke-KingmakerRuntimeScenario.ps1','RuntimeArtifactManifestEvidence.ps1','RuntimeHarness.Common.ps1','ScenarioDispatchEvidence.ps1','Test-RuntimeGameResult.ps1','Test-RuntimeRequest.ps1','Test-RuntimeResult.ps1')){
    $file=Get-Item -LiteralPath (Join-Path $overlay $name)
    $tokens=$null;$errors=$null;[void][Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
    Check ('PowerShell parser '+$file.Name) {if($errors.Count-ne0){throw ($errors|Out-String)}}
}
# Get-Command reads parameter metadata only; it never invokes the live runner.
$runnerMetadata=Get-Command (Join-Path $overlay 'Invoke-KingmakerRuntimeScenario.ps1')
$caseAttribute=@($runnerMetadata.Parameters['PersistenceCase'].Attributes | Where-Object { $_ -is [Management.Automation.ValidateSetAttribute] })
Check 'runner declares one closed persistence case set' {if($caseAttribute.Count-ne1){throw 'Expected one native case ValidateSet.'}}
foreach($case in Get-KmcChunk7PersistenceCases){
    Check ('runner parameter admits selected case '+$case) {if($case-cnotin$caseAttribute[0].ValidValues){throw 'Selected native case is unreachable from guarded runner.'}}
}
# The script body is unchanged except its already supplied common module load.
# Reads target version/repository identity; all fixture files stay in analysis-cache.
$body=[IO.File]::ReadAllText((Join-Path $overlay 'Test-RuntimeRequest.ps1'))
$body=$body.Replace(". (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')",'# Common functions supplied by isolated caller')
# The source literal contains its own PSScriptRoot, not this test directory.
$line=". (Join-Path "+'$PSScriptRoot'+" 'RuntimeHarness.Common.ps1')"
$body=$body.Replace($line,'# Common functions supplied by isolated caller')
$validator=[scriptblock]::Create($body)
function Descriptor([string]$Name,[string]$Hash){[pscustomobject]@{internalName=$Name;fileName='Manual_1_'+$Name+'.zks';sha256=$Hash;length=1024L;lastWriteTimeUtcTicks=1L;gameId='11111111-1111-4111-8111-111111111111';gameName='owned';area='a'*32}}
$baseline=Descriptor 'KMC_AUTOMATION_BASELINE' ('4'*64);$working=Descriptor 'KMC_AUTOMATION_WORKING' ('5'*64)
$base=[pscustomobject]@{schemaVersion=2;runId='synthetic-wiring';scenario='chunk7-persistence-save';persistenceCase='multi-exploration-manual';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;productVersion=(Read-KmcJson (Join-Path $repo 'version.json')).productVersion;dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';transactionToken='6'*64;evidenceRoot=(Join-Path (Get-KmcLabRoot) 'runtime-evidence/synthetic-wiring');qualificationSuite=[pscustomobject]@{suiteId='synthetic-suite';snapshotSha256='7'*64};fixture=[pscustomobject]@{baseline=$baseline;working=$working;writeAuthorization=[pscustomobject]@{mode='working-only';allowedInternalName='KMC_AUTOMATION_WORKING';allowedFileName=$working.fileName;baselineImmutable=$true}}}
function Invoke-Request($Value){$path=Join-Path $private 'request.json';$Value|ConvertTo-Json -Depth 90|Set-Content -LiteralPath $path;& $validator -RequestPath $path|Out-Null}
foreach($case in Get-KmcChunk7PersistenceCases){foreach($scenario in @('chunk7-persistence-save','chunk7-persistence-load')){
    $q=Copy-Test $base;$q.scenario=$scenario;$q.persistenceCase=$case
    if(($case-ceq'legacy-schema1-resave-manual'-or(Test-KmcChunk7RepeatedWorldCase $case))-and$scenario-ceq'chunk7-persistence-save'){Reject ($case+' cannot produce a fabricated source') {Invoke-Request $q};continue}
    if($case-ceq'multi-exploration-three-area-transfers-manual'){$q|Add-Member NoteProperty persistenceReplayTargets @(
        [pscustomobject]@{enterPoint='c'*32;area='b'*32;autoSaveMode='None'},
        [pscustomobject]@{enterPoint='e'*32;area='d'*32;autoSaveMode='None'},
        [pscustomobject]@{enterPoint='1'*32;area='f'*32;autoSaveMode='None'})}
    if($case-ceq'multi-area-autosave'){$q|Add-Member NoteProperty persistenceAreaTarget ([pscustomobject]@{enterPoint='c'*32;area='b'*32;autoSaveMode='AfterEntry'})}
    if($scenario-ceq'chunk7-persistence-load'){$d=Descriptor 'KMC_C7_WORLD' ('8'*64);$d.fileName=Get-KmcChunk7PersistenceLeaf $case;if($case-ceq'multi-area-autosave'){$d.area='b'*32};$q|Add-Member NoteProperty persistenceLoad $d}
    if($case-ceq'legacy-schema1-resave-manual'){$q.persistenceLoad.internalName='KMC_P01';$q.persistenceLoad.fileName='Manual_812_KMC_P06.zks';$q|Add-Member NoteProperty schema1DerivationSha256 ('9'*64)}
    Check ('exact full PowerShell request '+$case+' '+$scenario) {Invoke-Request $q}
}}
foreach($wrongCase in @('multi-exploration-manual','multi-exploration-three-loads-manual')) {
    Reject ('full request rejects replay route on '+$wrongCase) {
        $q=Copy-Test $base;$q.scenario='chunk7-persistence-load';$q.persistenceCase=$wrongCase
        $d=Descriptor 'KMC_C7_WORLD' ('8'*64);$d.fileName='Manual_300_KMC_C7_WORLD.zks';$q|Add-Member NoteProperty persistenceLoad $d
        $q|Add-Member NoteProperty persistenceReplayTargets @([pscustomobject]@{enterPoint='c'*32;area='b'*32;autoSaveMode='None'})
        Invoke-Request $q
    }
}
foreach($mutation in @('missing-receipt','receipt-number','wrong-input-leaf','wrong-input-name','foreign-receipt-field')){
 Reject ('schema1 exact full request '+$mutation){$q=Copy-Test $base;$q.scenario='chunk7-persistence-load';$q.persistenceCase='legacy-schema1-resave-manual';$d=Descriptor 'KMC_P01' ('8'*64);$d.fileName='Manual_812_KMC_P06.zks';$q|Add-Member NoteProperty persistenceLoad $d;$q|Add-Member NoteProperty schema1DerivationSha256 ('9'*64)
  switch($mutation){'missing-receipt'{$q.PSObject.Properties.Remove('schema1DerivationSha256')};'receipt-number'{$q.schema1DerivationSha256=1};'wrong-input-leaf'{$q.persistenceLoad.fileName='Manual_813_KMC_C7_SCHEMA1.zks'};'wrong-input-name'{$q.persistenceLoad.internalName='KMC_C7_SCHEMA1'};'foreign-receipt-field'{$q.persistenceCase='multi-exploration-manual';$q.persistenceLoad.internalName='KMC_C7_WORLD';$q.persistenceLoad.fileName='Manual_300_KMC_C7_WORLD.zks'}}
  Invoke-Request $q
 }
}
foreach($mutation in @('missing-target','wrong-mode','same-area','zero-enter','wrong-load-area','wrong-category')){
    Reject ('area request '+$mutation){$q=Copy-Test $base;$q.persistenceCase='multi-area-autosave';$q.scenario='chunk7-persistence-load'
      $q|Add-Member NoteProperty persistenceAreaTarget ([pscustomobject]@{enterPoint='c'*32;area='b'*32;autoSaveMode='AfterEntry'})
      $d=Descriptor 'Auto' ('8'*64);$d.fileName='Auto_1.zks';$d.area='b'*32;$q|Add-Member NoteProperty persistenceLoad $d
      switch($mutation){'missing-target'{$q.PSObject.Properties.Remove('persistenceAreaTarget')};'wrong-mode'{$q.persistenceAreaTarget.autoSaveMode='BeforeExit'};'same-area'{$q.persistenceAreaTarget.area='a'*32};'zero-enter'{$q.persistenceAreaTarget.enterPoint='0'*32};'wrong-load-area'{$q.persistenceLoad.area='a'*32};'wrong-category'{$q.persistenceLoad.fileName='Manual_300_KMC_C7_WORLD.zks'}}
      Invoke-Request $q
    }
}
foreach($scenario in @('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first','chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge','chunk7-two-pair-native-life-cleanup')){$q=Copy-Test $base;$q.scenario=$scenario;$q.PSObject.Properties.Remove('persistenceCase');Check ('exact full PowerShell combat request '+$scenario) {Invoke-Request $q}}
Reject 'unselected standalone RT combat remains closed' {$q=Copy-Test $base;$q.scenario='chunk7-two-pair-combat-rt';$q.PSObject.Properties.Remove('persistenceCase');Invoke-Request $q}
foreach($mutation in @('wrong-case','missing-case','historical-family','no-load-descriptor','wrong-leaf','protected-baseline','foreign-campaign','foreign-area','second-archive','area-write','schema-one')){
    Reject ('request '+$mutation){$q=Copy-Test $base;$q.scenario='chunk7-persistence-load';$d=Descriptor 'KMC_C7_WORLD' ('8'*64);$d.fileName='Manual_300_KMC_C7_WORLD.zks';$q|Add-Member NoteProperty persistenceLoad $d;switch($mutation){
      'wrong-case' {$q.persistenceCase='MULTI-exploration-manual'}
      'missing-case' {$q.PSObject.Properties.Remove('persistenceCase')}
      'historical-family' {$q.scenario='persistence-p05-load'}
      'no-load-descriptor' {$q.PSObject.Properties.Remove('persistenceLoad')}
      'wrong-leaf' {$q.persistenceLoad.fileName='Manual_300_KMC_P01.zks'}
      'protected-baseline' {$q.persistenceLoad.sha256=$baseline.sha256}
      'foreign-campaign' {$q.persistenceLoad.gameId='99999999-9999-4999-8999-999999999999'}
      'foreign-area' {$q.persistenceLoad.area='b'*32}
      'second-archive' {$q|Add-Member NoteProperty persistenceAlternate (Copy-Test $d)}
      'area-write' {$q|Add-Member NoteProperty persistenceAreaTarget ([pscustomobject]@{area='b'*32;enterPoint='c'*32;autoSaveMode='AfterEntry'})}
      'schema-one' {$q.schemaVersion=1}
    };Invoke-Request $q}
}
foreach($file in @('Test-RuntimeGameResult.ps1','Test-RuntimeResult.ps1')){
    $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $overlay $file),[ref]$tokens,[ref]$errors)
    $function=$ast.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'Assert-SubscenarioResults'},$true)
    . ([scriptblock]::Create($function.Extent.Text))
    foreach($scenario in @('chunk7-persistence-save','chunk7-persistence-load','chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first','chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge','chunk7-two-pair-native-life-cleanup')){
        $names=if($scenario-ceq'chunk7-persistence-save'){@('C7-persistence-two-native-mounts','C7-persistence-native-category-all-pair-archive','C7-persistence-native-combat-continuation','C7-persistence-cleanup')}else{@('C7-persistence-cold-all-pair-identity-and-no-prepare','C7-persistence-schema2-native-resave','C7-persistence-native-combat-continuation','C7-persistence-cleanup')}
        if(Test-KmcChunk7CombatScenario $scenario){$names=@(Get-KmcChunk7CombatRows $scenario|Where-Object{$_-cne$scenario-and$_-cnotin@('C7-combat-runtime','C7-combat-interrupted')})}
        if(Test-KmcChunk7RealtimeScenario $scenario){$names=@(Get-KmcChunk7RealtimeRows $scenario|Where-Object{$_-cne$scenario-and$_-cnotin@('C7-combat-runtime','C7-combat-interrupted')})}
        if(Test-KmcChunk7NativeLifeScenario $scenario){$names=@(Get-KmcChunk7NativeLifeRows $scenario|Where-Object{$_-cne$scenario-and$_-cnotin@('C7-combat-runtime','C7-combat-interrupted')})}
        $v=[pscustomobject]@{scenario=$scenario;subscenarioResults=@($names|ForEach-Object{[pscustomobject]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}});subscenarioTotal=$names.Count;subscenarioPassCount=$names.Count;subscenarioFailCount=0;assertionPassCount=$names.Count;assertionFailCount=0}
        Check ($file+' complete '+$scenario) {Assert-SubscenarioResults $v}
        Reject ($file+' unknown row '+$scenario) {$bad=Copy-Test $v;$bad.subscenarioResults[0].name=if($scenario-ceq'chunk7-persistence-load'){'C7-persistence-schema2-native-resave-extra'}else{'C7-unregistered'};Assert-SubscenarioResults $bad}
    }
}
$quota=[IO.File]::ReadAllText((Join-Path $overlay 'Test-RuntimeGameResult.ps1'))
$start=$quota.IndexOf('    $expectedNativeWrites = ');$end=$quota.IndexOf('    $expectedWorkingLoads = ',$start)
$quotaBlock=[scriptblock]::Create($quota.Substring($start,$end-$start)+[Environment]::NewLine+'return $expectedNativeWrites')
foreach($scenario in @('chunk7-persistence-save','chunk7-persistence-load')){$game=[pscustomobject]@{scenario=$scenario};$request=Copy-Test $base;$alternating=$false;$campaignB=$false;$nativeBootstrap=Test-KmcChunk7NativeBootstrapScenario $game.scenario;Check ('exact native save quota '+$scenario) {$expected=if($scenario-ceq'chunk7-persistence-save'){1}else{0};if((& $quotaBlock)-ne$expected){throw 'Native write quota differs.'}}}
$game=[pscustomobject]@{scenario='chunk7-persistence-load'};$request=Copy-Test $base;$request.persistenceCase='legacy-schema1-resave-manual'
Check 'schema-1 cold native resave has exactly one write quota' {if((& $quotaBlock)-ne1){throw 'Schema-1 native write quota differs.'}}
Write-Host ('CHUNK7 PERSISTENCE WIRING PASS='+$passed+' FAIL=0; isolated parser/request/completion checks only; fixtures='+$private)
