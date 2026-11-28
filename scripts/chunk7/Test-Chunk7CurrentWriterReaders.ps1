[CmdletBinding()]
param()
# PR #70 R13-F2 (round 16): the persistence evidence readers that consume this candidate's CURRENT output, exercised through the real
# dispatcher (Assert-KmcChunk7PersistenceObservation) and the runner's parent verifier (Assert-KmcChunk7OwnerSeedCheckpointColdSource).
# Every schema-4 graph below is produced by the candidate's own MountedArchiveCodec (compiled from src/ at this commit): the existing
# synthetic reader fixtures' schema-3 graphs are decoded by the explicit schema-3 reader and re-encoded by the current writer, exactly
# what a native resave of that graph writes. Cases: the current-format migration child and its parent (schema-1 and schema-2 lineage),
# the historical-input migrations that write current output (schema 1 and schema 2), the ordinary (Working) and inherited (owned) save
# and cold paths, previous-writer (schema 3) controls, and refusals of malformed/missing/extra StepTravel and mismatched versions.
# Round 17: the rider-fall save and cold observations through the dispatcher, the small-fixture/stock-Mammoth Manual archive leaf,
# and the owner-seed Manual world evidence whose writer capability must equal its Manual output version (R16-F5).
# All observations are generated offline (nativeInvoked=false): they establish reader behavior only, never that a native save or load ran.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$kmcRepo=$repo;$ReaderDirectory=Join-Path $repo 'scripts/runtime'
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7PersistenceSaveFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1')
$fixtures=(Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json')).fixtures
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7OwnedPairsEvidence.ps1'),[ref]$null,[ref]$null)
foreach($node in $ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]},$true)){. ([scriptblock]::Create($node.Extent.Text))}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action,[bool]$Reject=$false){$why=$null;$ok=$false;try{&$Action;$ok=-not$Reject}catch{$why=$_.Exception.Message;$ok=$Reject};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$why});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$(if($Reject){'refuses '}else{''})+$Name+$(if(-not$ok){': '+$why}elseif($Reject){' ('+$why+')'}else{''}))}
# A refusal counts only when its setup (fixture conversion through the codec) succeeded; only the mutated reader call may refuse.
function Reject([string]$Name,[scriptblock]$Setup,[scriptblock]$Refused){$x=$null;try{$x=& $Setup}catch{$results.Add([pscustomobject]@{name=$Name;pass=$false;expectedRefusal=$true;error=('setup failed: '+$_.Exception.Message)});Write-Output ('FAIL refuses '+$Name+': setup failed: '+$_.Exception.Message);return};$script:refusedCall=$Refused;$script:refusedValue=$x;Check $Name {& $script:refusedCall $script:refusedValue} $true}

# --- The candidate's production codec (the same source set Test-PersistenceData.ps1 compiles, without its tests). ---
$lab=Get-KmcLabRoot;$layout=(Read-KmcJson (Join-Path $lab 'environment-intake.json')).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$probe=Join-Path $repo 'obj/current-writer-readers';[void][IO.Directory]::CreateDirectory($probe)
foreach($stale in @(Get-ChildItem -LiteralPath $probe -Filter 'CurrentWriterCodec-*.dll' -ErrorAction SilentlyContinue)){try{Remove-Item -LiteralPath $stale.FullName -Force}catch{}}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$dll=Join-Path $probe ('CurrentWriterCodec-'+$PID+'.dll')
$sources=@('Integration/MountedSaveData.cs','Integration/SavedCombatData.cs','Domain/PairedActivationSnapshot.cs','Integration/MountedLoadAdmissionPolicy.cs','Integration/MountedArchiveData.cs','Domain/MountedControlSlotPolicy.cs','Domain/NativeMountedControl.cs','Integration/MountedArchiveCodec.cs','Integration/NativeMountedSaveFormat.cs','Integration/RetainedNativeReentryPolicy.cs','Domain/DeclaredSmallRiderBody.cs','Domain/MountedRiderProfileCatalog.cs','Domain/MountedRiderPoseProfile.cs','Domain/PoseVector3.cs')|ForEach-Object{Join-Path $repo ('src/KingmakerMountedCombat/'+$_)}
& $compiler /nologo /target:library /langversion:7.3 "/out:$dll" "/reference:$managed/Newtonsoft.Json.dll" @sources
if($LASTEXITCODE-ne0){throw 'Production archive codec compilation failed.'}
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'));$assembly=[Reflection.Assembly]::LoadFrom($dll)
$flags=[Reflection.BindingFlags]'NonPublic,Public,Static';$instance=[Reflection.BindingFlags]'NonPublic,Public,Instance'
$codec=$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveCodec',$true)
$decode=$codec.GetMethod('Decode',$flags);$encode=$codec.GetMethod('Encode',$flags)
$readType=$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveReadResult',$true)
$archiveSchema=[int]$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveData',$true).GetField('Schema',$flags).GetRawConstantValue()
$selectedWriter=[int]$assembly.GetType('KingmakerMountedCombat.Integration.NativeMountedSaveFormat',$true).GetProperty('SelectedWriterSchema',$flags).GetValue($null)
function Read-Archive([string]$Json){$r=$decode.Invoke($null,@($Json));[pscustomobject]@{kind=$readType.GetField('Kind',$instance).GetValue($r).ToString();source=[int]$readType.GetField('SourceSchema',$instance).GetValue($r);data=$readType.GetField('Data',$instance).GetValue($r);feedback=[string]$readType.GetField('Feedback',$instance).GetValue($r)}}
function Copy-Graph($Value){$Value|ConvertTo-Json -Depth 64 -Compress|ConvertFrom-Json}
# A previous-writer graph re-encoded by the candidate's current writer. The codec output must equal that graph plus StepTravel at version 4.
# Equality is numeric: the writer serializes floats (0.0) where the synthetic fixtures carry JSON integers (0), and Windows PowerShell keeps
# that lexical form. The returned graph therefore keeps the fixture's own number tokens (so the fixture's other recorded fields still match
# it) and takes the version and the StepTravel rows from the writer; a row equal to its allocation's MetresStepped keeps that token too.
function ConvertTo-KmcNumericCanonical($Value){(ConvertTo-KmcChunk7Canonical $Value)-replace'(\.\d*?[1-9])0+(?=[,\]}])','$1'-replace'(\d)\.0+(?=[,\]}])','$1'}
function ConvertTo-CurrentWriterArchive($Archive,[scriptblock]$Edit={}){
 $source=Copy-Graph $Archive;& $Edit $source
 $read=Read-Archive ($source|ConvertTo-Json -Depth 64 -Compress)
 if($read.kind-cne'Current'-or$read.source-notin@(3,4)){throw ('The production codec did not read the fixture graph: '+$read.kind+' '+$read.feedback)}
 $graph=($encode.Invoke($null,@($read.data)))|ConvertFrom-Json
 $expected=Copy-Graph $source;$expected.SchemaVersion=$archiveSchema;if($null-ne$expected.PSObject.Properties['StepTravel']){$expected.PSObject.Properties.Remove('StepTravel')};$without=Copy-Graph $graph;$without.PSObject.Properties.Remove('StepTravel')
 $e=ConvertTo-KmcNumericCanonical $expected;$w=ConvertTo-KmcNumericCanonical $without
 if($graph.SchemaVersion-ne4-or$null-eq$graph.PSObject.Properties['StepTravel']-or$e-cne$w){$i=0;while($i-lt[Math]::Min($e.Length,$w.Length)-and$e[$i]-ceq$w[$i]){$i++};$from=[Math]::Max(0,$i-60);throw ('The current writer changed more than the version and StepTravel at '+$i+': fixture ...'+$e.Substring($from,[Math]::Min(140,$e.Length-$from))+' / writer ...'+$w.Substring($from,[Math]::Min(140,$w.Length-$from)))}
 if($null-ne$source.PSObject.Properties['StepTravel']-and(ConvertTo-KmcNumericCanonical @($source.StepTravel))-cne(ConvertTo-KmcNumericCanonical @($graph.StepTravel))){throw 'The current writer changed the StepTravel rows.'}
 $result=Copy-Graph $source;$result.SchemaVersion=$graph.SchemaVersion
 $rows=@(foreach($row in @($graph.StepTravel)){$value=$row.MetresStepTravelled
  $allocation=@(if($null-ne$result.Combat){@($result.Combat.Allocations)|Where-Object{$null-ne$_-and$_.ActorId-ceq$row.ActorId-and$null-ne$_.Movement}})
  if($allocation.Count-eq1-and[double]$allocation[0].Movement.MetresStepped-eq[double]$value){$value=$allocation[0].Movement.MetresStepped}
  [pscustomobject][ordered]@{ActorId=$row.ActorId;MetresStepTravelled=$value}})
 $result|Add-Member NoteProperty StepTravel $rows -Force
 $result
}
function Set-Path($Root,[string]$Path,$Value){$parts=$Path.Split('.');$t=$Root;for($i=0;$i-lt$parts.Count-1;$i++){$t=$t.($parts[$i])};$t.($parts[-1])=$Value}
function Get-Path($Root,[string]$Path){$t=$Root;foreach($p in $Path.Split('.')){$t=$t.$p};$t}
# Convert the named writer graphs of one observation (writes and normalized graphs) and label the observation with the current writer.
function Set-CurrentWriter($Observations,[string[]]$Archives,[string[]]$WriterFields=@()){
 foreach($path in $Archives){Set-Path $Observations $path (ConvertTo-CurrentWriterArchive (Get-Path $Observations $path))}
 $Observations.nativeSchema=$archiveSchema
 foreach($path in $WriterFields){Set-Path $Observations $path $archiveSchema}
}

Check 'reader current writer is the candidate codec writer (Get-KmcChunk7CurrentWriterSchema = MountedArchiveData.Schema = SelectedWriterSchema = 4)' {if((Get-KmcChunk7CurrentWriterSchema)-ne$archiveSchema-or$archiveSchema-ne$selectedWriter-or$archiveSchema-ne4-or(Get-KmcChunk7PreviousWriterSchema)-ne3){throw ('Reader/codec writer mismatch: '+$archiveSchema+'/'+$selectedWriter)}}

# --- (1) Current-format migration: the cold child of a native resave, through the dispatcher and the parent verifier. ---
function New-CurrentPair([string]$Case,[bool]$Current){
 $v=New-KmcOwnerSeedCurrentFixture $Case;$p=New-KmcOwnerSeedCurrentParent $v
 if($Current){
  Set-CurrentWriter $p.observations @('nativeWrite.archive') @('nativeWrite.sourceSchema')
  if($Case-ceq'owned-schema1-current-manual'){foreach($path in @('loadedArchive','schema1Migration.migratedArchive')){Set-Path $p.observations $path (ConvertTo-CurrentWriterArchive (Get-Path $p.observations $path))}}
  else{$p.observations.loadedArchive=ConvertTo-CurrentWriterArchive $p.observations.loadedArchive;$p.observations.schema2Migration.migratedArchive=Copy-Graph $p.observations.loadedArchive;$p.observations.nativeWrite.nativeMetadata=Copy-Graph $p.observations.nativeWrite.archive}
  $o=$v.artifact.observations;$o.nativeSchema=$archiveSchema;$o.loadedSourceSchema=$archiveSchema;$o.currentFormatMigration.inputSchema=$archiveSchema;$o.loadedArchive=Copy-Graph $p.observations.nativeWrite.archive
 }
 [pscustomobject]@{child=$v;parent=$p}
}
function Invoke-CurrentPair($Pair){Assert-KmcChunk7PersistenceObservation $Pair.child.request $Pair.child.artifact 'PASS';Assert-KmcChunk7OwnerSeedCheckpointColdSource $Pair.child.request $Pair.parent $Pair.child.artifact}
foreach($case in @('owned-schema1-current-manual','owned-schema2-pending-current-manual')){
 Check ($case+': schema-4 native resave and its cold child pass the dispatcher and the parent verifier') {$x=New-CurrentPair $case $true;if(@($x.child.artifact.observations.loadedArchive.PSObject.Properties.Name)-cnotcontains'StepTravel'){throw 'No StepTravel.'};Invoke-CurrentPair $x}
 Check ($case+': previous-writer (schema 3) recorded pair still passes with its own exact shape') {$x=New-CurrentPair $case $false;Invoke-CurrentPair $x}
 $faults=[ordered]@{
  'child input labelled schema 3 under a schema-4 resave'={param($x)$x.child.artifact.observations.currentFormatMigration.inputSchema=3}
  'child observed source schema 3 under a schema-4 resave'={param($x)$x.child.artifact.observations.loadedSourceSchema=3}
  'child input labelled historical schema 2'={param($x)$x.child.artifact.observations.currentFormatMigration.inputSchema=2;$x.child.artifact.observations.loadedSourceSchema=2}
  'child writer schema 3 with a schema-4 graph'={param($x)$x.child.artifact.observations.nativeSchema=3}
  'parent write labelled schema 3 under a schema-4 child'={param($x)$x.parent.observations.nativeWrite.sourceSchema=3}
  'parent writer schema 3 under a schema-4 child'={param($x)$x.parent.observations.nativeSchema=3}
  'parent and child both claim an unsupported writer 5'={param($x)foreach($o in @($x.parent.observations,$x.child.artifact.observations)){$o.nativeSchema=5};$x.parent.observations.nativeWrite.sourceSchema=5;$x.child.artifact.observations.loadedSourceSchema=5;$x.child.artifact.observations.currentFormatMigration.inputSchema=5}
  'schema-4 child graph missing StepTravel'={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.PSObject.Properties.Remove('StepTravel')}}
  'schema-4 child graph with an extra top-level field'={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a|Add-Member NoteProperty StepTravelVersion 1}}
  'schema-4 child graph with a null StepTravel'={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel=$null}}
 }
 if($case-ceq'owned-schema1-current-manual'){$faults['schema-4 exploration graph with a StepTravel row and no combat snapshot']={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel=@([pscustomobject]@{ActorId=$x.child.request.ownedPairs.mountAId;MetresStepTravelled=0.5})}}}
 else{
  $faults['schema-4 StepTravel row with an extra field']={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel[0]|Add-Member NoteProperty MetresStepped 0.5}}
  $faults['schema-4 StepTravel row with a string distance']={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel[0].MetresStepTravelled='0.5'}}
  $faults['schema-4 StepTravel row with a negative distance']={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel[0].MetresStepTravelled=-0.1}}
  $faults['schema-4 StepTravel missing one allocation row']={param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a.StepTravel=@($a.StepTravel|Select-Object -Skip 1)}}
 }
 foreach($name in $faults.Keys){$mutate=$faults[$name];Reject ($case+': '+$name) {New-CurrentPair $case $true} {param($x)& $mutate $x;Invoke-CurrentPair $x}}
 Reject ($case+': previous-writer (schema 3) graph carrying StepTravel') {New-CurrentPair $case $false} {param($x)foreach($a in @($x.child.artifact.observations.loadedArchive,$x.parent.observations.nativeWrite.archive)){$a|Add-Member NoteProperty StepTravel @()};Invoke-CurrentPair $x}
 Reject ($case+': schema-3 recorded child of a schema-4 parent resave') {$x=New-CurrentPair $case $true;$x|Add-Member NoteProperty recorded (New-CurrentPair $case $false);$x} {param($x)$x.child=$x.recorded.child;Invoke-CurrentPair $x}
}

# --- (2) Historical inputs migrated to current output: the parents above through their own dispatcher and producer verifier. ---
function New-Schema1Parent([bool]$Current){$c=New-KmcOwnerSeedSchema1ColdFixture;$p=New-KmcOwnerSeedSchema1Producer $c
 if($Current){Set-CurrentWriter $c.artifact.observations @('loadedArchive','schema1Migration.migratedArchive','nativeWrite.archive') @('nativeWrite.sourceSchema');Set-CurrentWriter $p.observations @('nativeWrite.archive') @('ownedSchema1.sourceArchiveSchema')}
 [pscustomobject]@{cold=$c;producer=$p}}
function Invoke-Schema1Parent($x){Assert-KmcChunk7PersistenceObservation $x.cold.request $x.cold.artifact 'PASS';Assert-KmcChunk7OwnerSeedCheckpointColdSource $x.cold.request $x.producer $x.cold.artifact}
function New-Schema2Parent([bool]$Current){$c=New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual';$p=New-KmcOwnerSeedCombatCheckpointProducer $c
 if($Current){$o=$c.artifact.observations;Set-CurrentWriter $o @('loadedArchive','nativeWrite.archive') @('nativeWrite.sourceSchema');$o.schema2Migration.migratedArchive=Copy-Graph $o.loadedArchive;$o.nativeWrite.nativeMetadata=Copy-Graph $o.nativeWrite.archive
  Set-CurrentWriter $p.observations @('nativeWrite.archive') @('nativeWrite.sourceSchema')}
 [pscustomobject]@{cold=$c;producer=$p}}
function Invoke-Schema2Parent($x){Assert-KmcChunk7PersistenceObservation $x.cold.request $x.cold.artifact 'PASS';Assert-KmcChunk7OwnerSeedCheckpointColdSource $x.cold.request $x.producer $x.cold.artifact}
Check 'owned-schema1-resave-manual: historical schema-1 input, schema-4 normalized graph and schema-4 resave (dispatcher + producer verifier)' {$x=New-Schema1Parent $true;if($x.cold.artifact.observations.schema1Migration.inputSchema-ne1){throw 'Input is not historical schema 1.'};Invoke-Schema1Parent $x}
Check 'owned-schema1-resave-manual: previous-writer (schema 3) control' {Invoke-Schema1Parent (New-Schema1Parent $false)}
Reject 'owned-schema1-resave-manual: schema-4 writer with a schema-3 resave label' {New-Schema1Parent $true} {param($x)$x.cold.artifact.observations.nativeWrite.sourceSchema=3;Invoke-Schema1Parent $x}
Reject 'owned-schema1-resave-manual: schema-4 writer with a schema-3 resave graph' {New-Schema1Parent $true} {param($x)$x.cold.artifact.observations.nativeWrite.archive=(New-Schema1Parent $false).cold.artifact.observations.nativeWrite.archive;Invoke-Schema1Parent $x}
Reject 'owned-schema1-resave-manual: historical input relabelled as schema 3' {New-Schema1Parent $true} {param($x)$x.cold.artifact.observations.schema1Migration.inputSchema=3;Invoke-Schema1Parent $x}
Reject 'owned-schema1-resave-manual: schema-4 normalized graph missing StepTravel' {New-Schema1Parent $true} {param($x)foreach($a in @($x.cold.artifact.observations.loadedArchive,$x.cold.artifact.observations.schema1Migration.migratedArchive)){$a.PSObject.Properties.Remove('StepTravel')};Invoke-Schema1Parent $x}
Check 'legacy-mounted-pending-tb-manual: historical schema-2 input, schema-4 migrated graph with single-value StepTravel and schema-4 resave (dispatcher + producer verifier)' {$x=New-Schema2Parent $true;$o=$x.cold.artifact.observations;if($o.loadedSourceSchema-ne2-or@($o.loadedArchive.StepTravel).Count-ne@($o.loadedArchive.Combat.Allocations).Count){throw 'Not a schema-2 input with travel rows.'};Invoke-Schema2Parent $x}
Check 'legacy-mounted-pending-tb-manual: previous-writer (schema 3) control' {Invoke-Schema2Parent (New-Schema2Parent $false)}
Reject 'legacy-mounted-pending-tb-manual: schema-4 migrated graph missing StepTravel' {New-Schema2Parent $true} {param($x)$o=$x.cold.artifact.observations;foreach($a in @($o.loadedArchive,$o.schema2Migration.migratedArchive)){$a.PSObject.Properties.Remove('StepTravel')};Invoke-Schema2Parent $x}
Reject 'legacy-mounted-pending-tb-manual: schema-2 migration with an invented independent step distance' {New-Schema2Parent $true} {param($x)$o=$x.cold.artifact.observations;foreach($a in @($o.loadedArchive,$o.schema2Migration.migratedArchive)){$a.StepTravel[0].MetresStepTravelled=[double]$a.StepTravel[0].MetresStepTravelled+0.25};Invoke-Schema2Parent $x}
Reject 'legacy-mounted-pending-tb-manual: producer write labelled schema 3 under a schema-4 producer' {New-Schema2Parent $true} {param($x)$x.producer.observations.nativeWrite.sourceSchema=3;Invoke-Schema2Parent $x}
Reject 'legacy-mounted-pending-tb-manual: schema-4 resave labelled schema 3' {New-Schema2Parent $true} {param($x)$x.cold.artifact.observations.nativeWrite.sourceSchema=3;Invoke-Schema2Parent $x}

# --- (3) Protected ordinary (Working) and inherited (owned) current output through the dispatcher. ---
function New-OrdinaryFixtures{
 # The ordinary reader test's own synthetic fixture statements (Test-Chunk7PersistenceEvidence.ps1), without its checks.
 $tree=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1'),[ref]$null,[ref]$null)
 foreach($s in $tree.EndBlock.Statements){$t=$s.Extent.Text
  if($t-match'^\s*\.\s'-or$t-match'^\s*(Check|Reject|Write-Host|Set-StrictMode)\b'-or$t-match'^\s*\$(ErrorActionPreference|passed)\b'-or$t-match'^\s*function\s+(Check|Reject)\b'-or($s-is[Management.Automation.Language.ForEachStatementAst]-and$t-match'\b(Check|Reject)\s')){continue}
  . ([scriptblock]::Create($t))}
 [pscustomobject]@{save=[pscustomobject]@{request=$request;artifact=$good};cold=[pscustomobject]@{request=$coldRequest;artifact=$cold}}
}
$ordinary=New-OrdinaryFixtures
function Get-Fixture([string]$Name){Copy-Value @($fixtures|Where-Object name -CEQ $Name)[0]}
$protected=@(
 [pscustomobject]@{name='ordinary Working save multi-exploration-manual';value=$ordinary.save;archive='nativeWrite.archive'},
 [pscustomobject]@{name='ordinary Working cold multi-exploration-manual';value=$ordinary.cold;archive='loadedArchive'},
 [pscustomobject]@{name='inherited owned save multi-exploration-manual';value=(Get-Fixture 'persistence-save-multi-exploration-manual');archive='nativeWrite.archive'},
 [pscustomobject]@{name='inherited owned cold multi-exploration-manual';value=(Get-Fixture 'persistence-load-multi-exploration-manual');archive='loadedArchive'},
 [pscustomobject]@{name='inherited owned combat save multi-tb-debt-manual';value=(Get-Fixture 'persistence-save-multi-tb-debt-manual');archive='nativeWrite.archive'},
 [pscustomobject]@{name='inherited owned combat cold multi-tb-debt-manual';value=(Get-Fixture 'persistence-load-multi-tb-debt-manual');archive='loadedArchive'})
function New-Protected($Spec,[bool]$Current){$v=Copy-Value $Spec.value;$o=$v.artifact.observations
 if($Current){$fields=@();if($Spec.archive-ceq'nativeWrite.archive'-and$null-ne$o.nativeWrite.PSObject.Properties['sourceSchema']){$fields+='nativeWrite.sourceSchema'};if($Spec.archive-ceq'loadedArchive'-and$null-ne$o.PSObject.Properties['loadedSourceSchema']){$fields+='loadedSourceSchema'};Set-CurrentWriter $o @($Spec.archive) $fields
  # A combat save records its actual metadata readback, which is the written graph itself.
  if($Spec.archive-ceq'nativeWrite.archive'-and$null-ne$o.nativeWrite.PSObject.Properties['nativeMetadata']){$o.nativeWrite.nativeMetadata=Copy-Graph $o.nativeWrite.archive}}
 $v}
function Invoke-Protected($v){Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
foreach($spec in $protected){
 Check ($spec.name+': schema-4 current output accepted') {$v=New-Protected $spec $true;Invoke-Protected $v}
 Check ($spec.name+': previous-writer (schema 3) control accepted') {Invoke-Protected (New-Protected $spec $false)}
 Reject ($spec.name+': schema-4 graph under a schema-3 writer label') {New-Protected $spec $true} {param($v)$v.artifact.observations.nativeSchema=3;Invoke-Protected $v}
 Reject ($spec.name+': schema-3 graph under the schema-4 writer label') {New-Protected $spec $false} {param($v)$v.artifact.observations.nativeSchema=4;Invoke-Protected $v}
 Reject ($spec.name+': historical schema 2 claimed as the writer') {New-Protected $spec $false} {param($v)$v.artifact.observations.nativeSchema=2;(Get-Path $v.artifact.observations $spec.archive).SchemaVersion=2;Invoke-Protected $v}
 Reject ($spec.name+': schema-4 graph missing StepTravel') {New-Protected $spec $true} {param($v)(Get-Path $v.artifact.observations $spec.archive).PSObject.Properties.Remove('StepTravel');Invoke-Protected $v}
 Reject ($spec.name+': schema-4 StepTravel row with an unknown actor') {New-Protected $spec $true} {param($v)$a=Get-Path $v.artifact.observations $spec.archive;$a.StepTravel=@(@($a.StepTravel)+[pscustomobject]@{ActorId='99999999-9999-4999-8999-999999999999';MetresStepTravelled=0.25});Invoke-Protected $v}
}
Check 'inherited owned combat save multi-tb-debt-manual: schema-4 independent step travel (codec round trip, distinct from MetresStepped) accepted' {
 $v=New-Protected $protected[4] $true;$o=$v.artifact.observations
 $o.nativeWrite.archive=ConvertTo-CurrentWriterArchive $o.nativeWrite.archive {param($a)$a.StepTravel[0].MetresStepTravelled=[double]$a.StepTravel[0].MetresStepTravelled+0.25}
 $o.nativeWrite.nativeMetadata=Copy-Graph $o.nativeWrite.archive
 $row=$o.nativeWrite.archive.StepTravel[0];$allocation=@($o.nativeWrite.archive.Combat.Allocations|Where-Object ActorId -CEQ $row.ActorId)[0]
 if([double]$row.MetresStepTravelled-eq[double]$allocation.Movement.MetresStepped){throw 'The step travel is not independent of the native counter.'}
 Invoke-Protected $v}

# --- (4) Round 17 (R13-F2, R16-F5): the remaining current-write leaves. ---
# Rider fall: the F6 reader suite's own synthetic producer and cold observations (Test-Finalize6Readers.ps1, statements only), wrapped
# in the ordinary fixture's request/artifact envelope so they enter through the real dispatcher (rider-fall-integration-manual route).
function New-RiderFallFixtures{
 $tree=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'scripts/Test-Finalize6Readers.ps1'),[ref]$null,[ref]$null)
 foreach($s in $tree.EndBlock.Statements){$t=$s.Extent.Text
  if($t-match'^\s*\.\s'-or$t-match'^\s*(Check|Refuses|\w+Refuses|Assert-\w+|Write-Host|exit)\b'-or$t-match'^\s*\$(ErrorActionPreference|passes)\b'-or$t-match'^\s*function\s+(Check|Refuses|\w+Refuses)\b'){continue}
  if($s.Extent.StartLineNumber-gt189){break}
  . ([scriptblock]::Create($t))}
 [pscustomobject]@{fixture=$fixture;save=$prodObs;cold=$coldObs}
}
$riderFall=New-RiderFallFixtures
function New-RiderFall([bool]$Cold,[bool]$Current){
 $base=if($Cold){$ordinary.cold}else{$ordinary.save};$v=Copy-Value $base;$v.request.persistenceCase='rider-fall-integration-manual';$v.request.fixture=Copy-Value $riderFall.fixture
 $o=Copy-Value $(if($Cold){$riderFall.cold}else{$riderFall.save});$o.checkpoint='rider-fall-integration-manual';$v.artifact.observations=$o
 # The F6 suite's placeholder identities ('camp', 'area', pair 'aaaa') are not valid production identities: the codec refuses them.
 # Substitute well-formed values consistently across the request and observations (values only, never member names).
 $json=$v|ConvertTo-Json -Depth 90 -Compress
 foreach($r in @(@('camp','11111111-1111-4111-8111-111111111111'),@('area',('a'*32)),@('aaaa','a1a1a1a1a1a14a1a8a1aa1a1a1a1a1a1'))){$json=[regex]::Replace($json,'"'+$r[0]+'"(?!\s*:)','"'+$r[1]+'"')}
 $v=$json|ConvertFrom-Json;$o=$v.artifact.observations
 # Its archive actors carry only an Id; a written archive carries each actor's complete native debt row (here: no debt).
 $a=if($Cold){$o.loadedArchive}else{$o.nativeWrite.archive};foreach($pair in @($a.Pairs)){foreach($role in @('Rider','Mount')){$id=$pair.$role.Id;$pair.$role=[pscustomobject][ordered]@{Id=$id;Standard=0.0;Move=0.0;Swift=0.0;Initiative=0.0;Reaction=0.0;ReactionsRemaining=1;LastSurpriseTicks=0}}}
 if($Current){Set-CurrentWriter $o @($(if($Cold){'loadedArchive'}else{'nativeWrite.archive'}))}
 $v}
foreach($cold in @($false,$true)){$label=if($cold){'rider-fall cold'}else{'rider-fall save'};$path=if($cold){'loadedArchive'}else{'nativeWrite.archive'}
 Check ($label+': schema-4 observation through the dispatcher') {$v=New-RiderFall $cold $true;Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
 Check ($label+': previous-writer (schema 3) control through the dispatcher') {$v=New-RiderFall $cold $false;Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
 Reject ($label+': schema-4 graph missing StepTravel') {New-RiderFall $cold $true} {param($v)(Get-Path $v.artifact.observations $path).PSObject.Properties.Remove('StepTravel');Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
 Reject ($label+': schema-4 graph with an extra field') {New-RiderFall $cold $true} {param($v)(Get-Path $v.artifact.observations $path)|Add-Member NoteProperty Extra 1;Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
 Reject ($label+': schema-4 graph under a schema-3 writer label') {New-RiderFall $cold $true} {param($v)$v.artifact.observations.nativeSchema=3;Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
 Reject ($label+': schema-3 graph under the schema-4 writer label') {New-RiderFall $cold $false} {param($v)$v.artifact.observations.nativeSchema=4;Assert-KmcChunk7PersistenceObservation $v.request $v.artifact 'PASS'}
}
# Small-fixture and stock-Mammoth Manual leaf (Assert-KmcSmallFixtureEmptyArchive, shared by both evidence readers and their fixture
# qualifiers): a codec-written unmounted Manual graph.
$descriptor=[pscustomobject]@{gameId='11111111-1111-4111-8111-111111111111';area=('a'*32)}
$empty3=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$descriptor.gameId;AreaId=$descriptor.area;GameTimeTicks=12345;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=@();Participations=@();Slots=@();Combat=$null}
Check 'fixture Manual leaf: schema-4 codec-written unmounted Manual accepted' {Assert-KmcSmallFixtureEmptyArchive (ConvertTo-CurrentWriterArchive $empty3) $descriptor}
Check 'fixture Manual leaf: previous-writer (schema 3) control accepted' {Assert-KmcSmallFixtureEmptyArchive (Copy-Graph $empty3) $descriptor}
Reject 'fixture Manual leaf: schema-4 graph missing StepTravel' {ConvertTo-CurrentWriterArchive $empty3} {param($a)$a.PSObject.Properties.Remove('StepTravel');Assert-KmcSmallFixtureEmptyArchive $a $descriptor}
Reject 'fixture Manual leaf: schema-3 graph carrying StepTravel' {Copy-Graph $empty3} {param($a)$a|Add-Member NoteProperty StepTravel @();Assert-KmcSmallFixtureEmptyArchive $a $descriptor}
Reject 'fixture Manual leaf: unsupported writer 5' {ConvertTo-CurrentWriterArchive $empty3} {param($a)$a.SchemaVersion=5;Assert-KmcSmallFixtureEmptyArchive $a $descriptor}
# World/owner-seed capability (R16-F5): the advertised writer must be the writer of the record's own Manual output.
function New-SeedManual([int]$Capability,[int]$Output){$v=New-KmcOwnerSeedManualFixture;$o=$v.artifact.observations
 if($Output-eq4){$o.nativeWrite.archive=ConvertTo-CurrentWriterArchive $o.nativeWrite.archive;$o.nativeWrite.sourceSchema=4}
 $o.capability.nativeWriterSchema=$Capability;$v}
foreach($pair in @(@(4,4),@(3,3))){$c=$pair[0];$w=$pair[1];Check ('owner-seed Manual world evidence: capability '+$c+' with Manual output '+$w+' accepted') {$v=New-SeedManual $c $w;Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}}
foreach($pair in @(@(3,4),@(4,3))){$c=$pair[0];$w=$pair[1];Reject ('owner-seed Manual world evidence: capability '+$c+' with Manual output '+$w) {New-SeedManual $c $w} {param($v)Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}}
Reject 'owner-seed Manual world evidence: schema-4 Manual output missing StepTravel' {New-SeedManual 4 4} {param($v)$v.artifact.observations.nativeWrite.archive.PSObject.Properties.Remove('StepTravel');Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}

# --- (5) Leaf-level offline cases (round 17): the leaf validators called directly with codec-written graphs, where a full request
# envelope for the native family is impractical offline. They prove the leaf's version binding only, not the whole family reader.
# Progression write leaf (Assert-KmcProgressionWrite), non-owned horse-native-level Auto: the ordinary fixture graph is its source.
$progressionRequest=[pscustomobject]@{runId='synthetic-progression';evidenceRoot='C:\synthetic\runtime-evidence\synthetic-progression';fixture=(Copy-Value $ordinary.save.request.fixture)
 progression=[pscustomobject]@{case='horse-native-level';sourceArchiveSha256=('e'*64);actorId='d17c8fd0-6627-40c4-bf9a-024cf001c1f7';controlActorId='b6628a77-4962-47a4-a17c-88d9836fc9d5'}}
$progressionSource=Copy-Graph $ordinary.save.artifact.observations.nativeWrite.archive
function New-ProgressionWrite([bool]$Current){$w=$progressionRequest.fixture.working;$root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($progressionRequest.evidenceRoot))) ('runtime-staging/persistence-'+$progressionRequest.runId+'/Saved Games')
 $archive=if($Current){ConvertTo-CurrentWriterArchive $progressionSource}else{Copy-Graph $progressionSource}
 [pscustomobject]@{path=(Join-Path $root 'Auto_1.zks');leaf='Auto_1.zks';type='Auto';operation='None';sha256=('f'*64);name='Auto';gameId=$w.gameId;gameName=$w.gameName;area=$w.area;length=4096;schema=[int]$archive.SchemaVersion;snapshotCount=1;completedWrites=1;nativeAutoLevelup=$true;nativeCallbackSupplied=$false;pairs=2;archive=$archive}}
Check 'leaf-level offline: progression write leaf accepts a schema-4 codec-written Auto' {Assert-KmcProgressionWrite (New-ProgressionWrite $true) $progressionRequest $progressionSource}
Check 'leaf-level offline: progression write leaf previous-writer (schema 3) control' {Assert-KmcProgressionWrite (New-ProgressionWrite $false) $progressionRequest $progressionSource}
Reject 'leaf-level offline: progression write leaf, schema-4 graph missing StepTravel' {New-ProgressionWrite $true} {param($w)$w.archive.PSObject.Properties.Remove('StepTravel');Assert-KmcProgressionWrite $w $progressionRequest $progressionSource}
Reject 'leaf-level offline: progression write leaf, schema-4 graph recorded as a schema-3 write' {New-ProgressionWrite $true} {param($w)$w.schema=3;Assert-KmcProgressionWrite $w $progressionRequest $progressionSource}
Reject 'leaf-level offline: progression write leaf, schema-3 graph recorded as a schema-4 write' {New-ProgressionWrite $false} {param($w)$w.schema=4;Assert-KmcProgressionWrite $w $progressionRequest $progressionSource}
# Stock-Mammoth (and small/medium-fixture) Manual write leaf: the recorded sourceSchema binds the written graph.
function New-FixtureWrite([bool]$Current){$a=if($Current){ConvertTo-CurrentWriterArchive $empty3}else{Copy-Graph $empty3};[pscustomobject]@{sourceSchema=[int]$a.SchemaVersion;archive=$a}}
Check 'leaf-level offline: stock-Mammoth Manual write leaf accepts sourceSchema 4 with a codec-written schema-4 graph' {Assert-KmcChunk7FixtureManualWrite (New-FixtureWrite $true) $descriptor 'Stock Mammoth Manual'}
Check 'leaf-level offline: stock-Mammoth Manual write leaf previous-writer (3/3) control' {Assert-KmcChunk7FixtureManualWrite (New-FixtureWrite $false) $descriptor 'Stock Mammoth Manual'}
Reject 'leaf-level offline: stock-Mammoth Manual write leaf, schema-4 graph missing StepTravel' {New-FixtureWrite $true} {param($w)$w.archive.PSObject.Properties.Remove('StepTravel');Assert-KmcChunk7FixtureManualWrite $w $descriptor 'Stock Mammoth Manual'}
Reject 'leaf-level offline: stock-Mammoth Manual write leaf, schema-4 graph recorded as sourceSchema 3' {New-FixtureWrite $true} {param($w)$w.sourceSchema=3;Assert-KmcChunk7FixtureManualWrite $w $descriptor 'Stock Mammoth Manual'}
Reject 'leaf-level offline: stock-Mammoth Manual write leaf, schema-3 graph recorded as sourceSchema 4' {New-FixtureWrite $false} {param($w)$w.sourceSchema=4;Assert-KmcChunk7FixtureManualWrite $w $descriptor 'Stock Mammoth Manual'}
# Third-Horse Manual capability cross-check leaf (Assert-KmcChunk7ManualWriterCapability, also used by the owner-seed reader).
foreach($pair in @(@(4,4),@(3,3))){$c=$pair[0];$out=$pair[1];Check ('leaf-level offline: third-Horse capability '+$c+' with Manual output '+$out+' accepted') {Assert-KmcChunk7ManualWriterCapability ([pscustomobject]@{nativeWriterSchema=$c}) (New-FixtureWrite ($out-eq4)) 'Third-Horse world'}}
foreach($pair in @(@(3,4),@(4,3))){$c=$pair[0];$out=$pair[1];Reject ('leaf-level offline: third-Horse capability '+$c+' with Manual output '+$out) {New-FixtureWrite ($out-eq4)} {param($w)Assert-KmcChunk7ManualWriterCapability ([pscustomobject]@{nativeWriterSchema=$c}) $w 'Third-Horse world'}}

$clock.Stop();$passed=@($results|Where-Object pass -EQ $true).Count;$failed=@($results|Where-Object pass -EQ $false).Count;$refusals=@($results|Where-Object{$_.pass-and$_.expectedRefusal}).Count
Write-Output ('CURRENT-WRITER READERS RESULT '+$passed+' PASS / '+$failed+' FAIL ('+$refusals+' expected refusals; '+[Math]::Round($clock.Elapsed.TotalSeconds,1)+' s; offline generated observations, nativeInvoked=false, no native save/load claim)')
if($failed){exit 1}
