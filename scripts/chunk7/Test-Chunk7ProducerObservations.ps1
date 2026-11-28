[CmdletBinding()]
param()
# PR-2 round 19 (R13-F2, R18-F5): COMPLETE producer-shaped persistence observations through the affected readers' real dispatch.
# - Hook identity (R13-F2): NativeArchiveRestorationProbe records method.DeclaringType.FullName, method.Name, MetadataToken (X8) and
#   Module.ModuleVersionId of the methods it patches. This child takes the probe's own unconditional Patch/PatchProduct calls from src/,
#   resolves each native token in the installed Assembly-CSharp exactly as the probe does (the declared type's method with that token),
#   and emits the hook rows in the probe's order and shape. The product hooks (NativeCombatActorPersistence.RestoreActor/RestoreDebt, and the
#   Manual save probe's MountedPersistenceService.RecordCompletedSave) are reflected from the BUILT candidate bin\Release\KingmakerMountedCombat.dll
#   (the focused gate builds it first; absent, this child fails rather than substitute values). Round 20: the same rows replace EVERY copy a
#   record carries (outer record, source record, nested cold-inventory and Manual copies) and every request/artifact/source dllMvid becomes the
#   built module MVID; a scan then proves no impossible identity, fixture MVID or synthetic product token survives in any positive.
# - Archive rows: the producer writes data.SchemaVersion/CampaignId/AreaId/Pairs/Participations/Slots/Combat of the DECODED (normalized)
#   graph. Rows here are built from the candidate's own MountedArchiveCodec (compiled from src/) decoding a schema-3 input, so the input's
#   version (3) and the normalized graph version (4) are both real codec results. The Manual-derived write's schema is the SourceSchema of
#   the current writer's own output, re-read by the same codec.
# - Readers: bootstrap (Assert-KmcChunk7BootstrapInventoryEvidence), Manual seed (Assert-KmcChunk7ManualSeedEvidence), Manual-derived
#   non-missing archive (Assert-KmcChunk7ManualDerivedEvidence), pre-owned owner-seed world (Assert-KmcChunk7WorldEvidence), and assisted
#   cold (Assert-KmcChunk7AssistedColdInventoryEvidence) over the capture emitted by the actual assisted scenario source compiled with
#   its controlled native facades (Test-Chunk7AssistedColdScenario.ps1).
# - Same-process version (R18-F5): Manual-derived (normalized graph vs its own Manual write) and pre-owned owner seed (normalized graph vs
#   advertised writer capability) accept 3/3 and 4/4 and refuse both crossings made by changing only the normalized graph field.
# Round 20 controls: an impossible round hook in BOTH agreeing source copies is refused (the readers validate each copy's identity).
# Round 22 (R21-F1, R21-F2): current records carry the built DLL's SHA-256 too, so the readers bind current-build product rows (restoration
# and Manual save) to TRUSTED metadata reflected from that artifact (Get-KmcChunk7CandidateProductHooks, identity verified by SHA-256 and
# MVID); a uniform swap of RestoreActor/RestoreDebt across agreeing copies and a differing RecordCompletedSave token are refused. The synthetic
# earlier-build fixtures keep module 33333333-... with one completion token (06001234) throughout. The Manual seed reader requires its seed
# and cold-inventory normalized graphs to agree (3/3 and 4/4 accepted; 3/4 and 4/3 refused).
# Round 21: every archive row in a current Manual-derived same-build chain (outer, Manual seed source, its cold inventory, and their copies)
# is the candidate codec's normalized 4 of a historical schema-3 input; agreeing source copies downgraded to 3 beside the current writer,
# and agreeing copies that rebind a product method's MethodDef token, are refused. The earlier-writer 3/3 control keeps the retained
# synthetic fixture's own identity (module 33333333-..., its own product tokens): an earlier-build fixture, not the current DLL.
# The 3/3 records are earlier-build (schema-3 writer) controls taken from the retained synthetic fixtures; no earlier codec runs here.
# All observations are generated offline (nativeInvoked=false): reader behavior only, never a native save, load or gameplay session.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$kmcRepo=$repo;$ReaderDirectory=Join-Path $repo 'scripts/runtime'
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedFixtureEvidence.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedFixtureFixtures.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedColdInventoryEvidence.ps1')
. (Join-Path $ReaderDirectory 'Chunk7AssistedColdInventoryFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedCheckpointPreparationFixtures.ps1')
. (Join-Path $repo 'tests/Chunk7Diagnostics/OwnerSeedMigrationFixtures.ps1')
$fixtures=(Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json')).fixtures
# Existing suites' fixture builders, imported unchanged (all functions of the owned-pair suite, selected functions of the others).
function Import-SuiteFunctions([string]$Leaf,[string[]]$Names=@()){
 $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $Leaf),[ref]$null,[ref]$null)
 $found=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object{$Names.Count-eq0-or$_.Name-cin$Names})
 if($Names.Count-and$found.Count-ne$Names.Count){throw ('Missing suite functions in '+$Leaf)};$found|ForEach-Object{$_.Extent.Text}}
foreach($text in @(Import-SuiteFunctions 'Test-Chunk7OwnedPairsEvidence.ps1')){. ([scriptblock]::Create($text))}
foreach($text in @(Import-SuiteFunctions 'Test-Chunk7ManualSeedEvidence.ps1' @('Copy-SeedJson','New-ManualSeedFixture'))){. ([scriptblock]::Create($text))}
foreach($text in @(Import-SuiteFunctions 'Test-Chunk7ManualDerivedEvidence.ps1' @('New-ManualDerivedFixture'))){. ([scriptblock]::Create($text))}
foreach($text in @(Import-SuiteFunctions 'Test-Chunk7CurrentWriterReaders.ps1' @('Read-Archive','Copy-Graph','ConvertTo-KmcNumericCanonical','ConvertTo-CurrentWriterArchive'))){. ([scriptblock]::Create($text))}
foreach($text in @(Import-SuiteFunctions 'Test-Chunk7AssistedColdInventoryEvidence.ps1' @('Clone','Put','Json','Need','New-Fixture','Verify'))){. ([scriptblock]::Create($text))}
$clock=[Diagnostics.Stopwatch]::StartNew();$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Action,[bool]$Reject=$false){$why=$null;$ok=$false;try{&$Action;$ok=-not$Reject}catch{$why=$_.Exception.Message;$ok=$Reject};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Reject;error=$why});Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$(if($Reject){'refuses '}else{''})+$Name+$(if(-not$ok){': '+$why}elseif($Reject){' ('+$why+')'}else{''}))}
# A refusal counts only when its setup succeeded; only the mutated reader call may refuse.
function Reject([string]$Name,[scriptblock]$Setup,[scriptblock]$Refused){$x=$null;try{$x=& $Setup}catch{$results.Add([pscustomobject]@{name=$Name;pass=$false;expectedRefusal=$true;error=('setup failed: '+$_.Exception.Message)});Write-Output ('FAIL refuses '+$Name+': setup failed: '+$_.Exception.Message);return};$script:refusedCall=$Refused;$script:refusedValue=$x;Check $Name {& $script:refusedCall $script:refusedValue} $true}
function Copy-Json($Value){$Value|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json}

# --- The candidate's production archive codec (the source set Test-Chunk7CurrentWriterReaders.ps1 compiles). ---
$lab=Get-KmcLabRoot;$layout=(Read-KmcJson (Join-Path $lab 'environment-intake.json')).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$probeDirectory=Join-Path $repo 'obj/producer-observations';[void][IO.Directory]::CreateDirectory($probeDirectory)
foreach($stale in @(Get-ChildItem -LiteralPath $probeDirectory -Filter 'ProducerObservationCodec-*.dll' -ErrorAction SilentlyContinue)){try{Remove-Item -LiteralPath $stale.FullName -Force}catch{}}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$dll=Join-Path $probeDirectory ('ProducerObservationCodec-'+$PID+'.dll')
$sources=@('Integration/MountedSaveData.cs','Integration/SavedCombatData.cs','Domain/PairedActivationSnapshot.cs','Integration/MountedLoadAdmissionPolicy.cs','Integration/MountedArchiveData.cs','Domain/MountedControlSlotPolicy.cs','Domain/NativeMountedControl.cs','Integration/MountedArchiveCodec.cs','Integration/NativeMountedSaveFormat.cs','Integration/RetainedNativeReentryPolicy.cs','Domain/DeclaredSmallRiderBody.cs','Domain/MountedRiderProfileCatalog.cs','Domain/MountedRiderPoseProfile.cs','Domain/PoseVector3.cs')|ForEach-Object{Join-Path $repo ('src/KingmakerMountedCombat/'+$_)}
& $compiler /nologo /target:library /langversion:7.3 "/out:$dll" "/reference:$managed/Newtonsoft.Json.dll" @sources
if($LASTEXITCODE-ne0){throw 'Production archive codec compilation failed.'}
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'));$assembly=[Reflection.Assembly]::LoadFrom($dll)
$flags=[Reflection.BindingFlags]'NonPublic,Public,Static';$instance=[Reflection.BindingFlags]'NonPublic,Public,Instance'
$codec=$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveCodec',$true)
$decode=$codec.GetMethod('Decode',$flags);$encode=$codec.GetMethod('Encode',$flags)
$readType=$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveReadResult',$true)
$archiveSchema=[int]$assembly.GetType('KingmakerMountedCombat.Integration.MountedArchiveData',$true).GetField('Schema',$flags).GetRawConstantValue()
if($archiveSchema-ne4){throw ('This child pins the candidate writer 4; MountedArchiveData.Schema is '+$archiveSchema+'.')}
# A genuinely schema-3 empty graph (the previous writer's shape) decoded by the candidate codec: SourceSchema 3, normalized Data at 4.
function Read-HistoricalInput([string]$Campaign,[string]$Area){
 $json=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$Campaign;AreaId=$Area;GameTimeTicks=12345;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1';Pairs=@();Participations=@();Slots=@();Combat=$null}|ConvertTo-Json -Depth 8 -Compress
 $read=Read-Archive $json
 if($read.kind-cne'Current'-or$read.source-ne3-or$read.data.SchemaVersion-ne$archiveSchema){throw ('The candidate codec did not normalize the schema-3 input: '+$read.kind+' '+$read.source+' '+$read.feedback)};$read}
# The producers' archive row (Chunk7BootstrapInventoryScenario/ManualSeed/ManualDerived/AssistedCold "archive"; OwnerMadeArchiveRow), from decoded data.
function New-ProducerArchiveRow($Data){Copy-Json ([pscustomobject][ordered]@{schemaVersion=[int]$Data.SchemaVersion;campaignId=$Data.CampaignId;areaId=$Data.AreaId;pairs=@($Data.Pairs).Count;participations=@($Data.Participations).Count;slots=@($Data.Slots).Count;combatPresent=($null-ne$Data.Combat);nativeRestorationGrantedByDiagnostic=$false})}
# The current writer's own output for that data, re-read by the same codec: the Manual-derived producer's nativeWrite.schema (archive.SourceSchema).
function Get-CurrentWrittenSchema($Data){$back=Read-Archive ([string]$encode.Invoke($null,@($Data)));if($back.kind-cne'Current'){throw 'The current writer output did not decode.'};[int]$back.source}

# --- Hook identity as the producer reflects it (R13-F2). ---
$probeText=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativeArchiveRestorationProbe.cs'))
$begin=$probeText.IndexOf('active = this;',[StringComparison]::Ordinal);$end=$probeText.IndexOf('if (world != null)',$begin+1,[StringComparison]::Ordinal)
if($begin-lt0-or$end-lt0){throw 'The restoration probe installation body was not found.'}
$calls=@([regex]::Matches($probeText.Substring($begin,$end-$begin),'(?<kind>PatchProduct|Patch)\((?:typeof\((?<type>\w+)\), 0x(?<token>[0-9A-Fa-f]{8})|nameof\(NativeCombatActorPersistence\.(?<name>\w+)\)), "(?<prefix>\w+)", "(?<postfix>\w+)"\);'))
if($calls.Count-ne4-or$calls[0].Groups['kind'].Value-cne'Patch'-or$calls[1].Groups['kind'].Value-cne'Patch'-or$calls[2].Groups['kind'].Value-cne'PatchProduct'-or$calls[3].Groups['kind'].Value-cne'PatchProduct'){throw 'The restoration probe no longer installs exactly two native and two product hooks in this order.'}
if($probeText.IndexOf('"07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"',[StringComparison]::Ordinal)-lt0){throw 'The restoration probe no longer pins the installed module MVID.'}
$productNamespace=[regex]::Match([IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Integration/NativeCombatActorPersistence.cs')),'namespace\s+([A-Za-z0-9_.]+)').Groups[1].Value
[AppDomain]::CurrentDomain.add_AssemblyResolve({param($s,$e)$n=($e.Name-split',')[0];$p=Join-Path $managed ($n+'.dll');if(Test-Path -LiteralPath $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$game=[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$nativeHooks=@(foreach($call in $calls[0..1]){$token=[Convert]::ToInt32($call.Groups['token'].Value,16)
 $method=$game.ManifestModule.ResolveMethod($token);$declared=@($method.DeclaringType.GetMethods([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')|Where-Object{$_.MetadataToken-eq$token})
 if($method.DeclaringType.Name-cne$call.Groups['type'].Value-or$declared.Count-ne1){throw ('Installed token '+$call.Groups['token'].Value+' is not a method of '+$call.Groups['type'].Value+'.')}
 [pscustomobject][ordered]@{declaringType=$method.DeclaringType.FullName;method=$method.Name;token=$method.MetadataToken.ToString('X8');moduleMvid=$method.Module.ModuleVersionId.ToString();prefix=$call.Groups['prefix'].Value;postfix=$call.Groups['postfix'].Value}})
# The built candidate: its module MVID and the product methods the probes patch, reflected exactly as the probes reflect them.
$productDll=Join-Path $repo 'bin/Release/KingmakerMountedCombat.dll'
if(-not(Test-Path -LiteralPath $productDll -PathType Leaf)){throw ('The built candidate '+$productDll+' is absent: build the candidate first (the focused gate runs after the Fast build). No synthetic product identity is substituted.')}
$ummDirectory=Join-Path $managed 'UnityModManager'
[AppDomain]::CurrentDomain.add_AssemblyResolve({param($s,$e)$n=($e.Name-split',')[0];$p=Join-Path $ummDirectory ($n+'.dll');if(Test-Path -LiteralPath $p){[Reflection.Assembly]::LoadFrom($p)}else{$null}})
$product=[Reflection.Assembly]::LoadFrom($productDll);$productMvid=$product.ManifestModule.ModuleVersionId.ToString();$productSha=Get-KmcSha256 $productDll
$allMembers=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$persistenceHooks=$product.GetType($productNamespace+'.NativeCombatActorPersistence',$true)
$productHooks=@(for($i=2;$i-lt4;$i++){$method=$persistenceHooks.GetMethod($calls[$i].Groups['name'].Value,$allMembers)
 if($null-eq$method){throw ('The built candidate lacks '+$calls[$i].Groups['name'].Value+'.')}
 [pscustomobject][ordered]@{declaringType=$method.DeclaringType.FullName;method=$method.Name;token=$method.MetadataToken.ToString('X8');moduleMvid=$method.Module.ModuleVersionId.ToString();prefix=$calls[$i].Groups['prefix'].Value;postfix=$calls[$i].Groups['postfix'].Value}})
# The Manual save probe (Chunk7ManualSeedSaveProbe): SaveManager.SaveRoutine 06008029 and PrepareSave 06008025, then the product's RecordCompletedSave.
$saveHooks=@(foreach($pair in @(@(0x06008029,'SaveRoutine'),@(0x06008025,'PrepareSave'))){$method=$game.ManifestModule.ResolveMethod($pair[0]);if($method.Name-cne$pair[1]-or$method.DeclaringType.FullName-cne'Kingmaker.EntitySystem.Persistence.SaveManager'){throw ('Installed save token '+$pair[0].ToString('X8')+' moved.')}
  [pscustomobject][ordered]@{type=$method.DeclaringType.FullName;method=$method.Name;token=$method.MetadataToken.ToString('X8');moduleMvid=$method.Module.ModuleVersionId.ToString()}}
 $completed=@($product.GetType('KingmakerMountedCombat.Integration.MountedPersistenceService',$true).GetMethods($allMembers)|Where-Object{$_.Name-ceq'RecordCompletedSave'-and$_.GetParameters().Count-eq1})
 if($completed.Count-ne1){throw 'The built candidate does not have exactly one RecordCompletedSave(scope).'}
 [pscustomobject][ordered]@{type=$completed[0].DeclaringType.FullName;method=$completed[0].Name;token=$completed[0].MetadataToken.ToString('X8');moduleMvid=$completed[0].Module.ModuleVersionId.ToString()})
# Emits the rows individually; callers collect them with @() (a ConvertFrom-Json array stored whole would serialize as {value,Count}).
function New-ProducerHooks{foreach($h in @($nativeHooks)+@($productHooks)){Copy-Json $h}}
function New-ProducerSaveHooks{foreach($h in $saveHooks){Copy-Json $h}}
# Round 20: every copy in a record takes the same producer rows; every dllMvid equal to the fixture's becomes the built module MVID.
function Set-ProducerIdentity($Node,[string]$FixtureMvid,[string]$FixtureSha=''){
 if($Node-is[Array]){foreach($item in $Node){Set-ProducerIdentity $item $FixtureMvid $FixtureSha};return}
 if($Node-isnot[Management.Automation.PSCustomObject]){return}
 foreach($property in @($Node.PSObject.Properties)){
  if($property.Name-ceq'noPreparation'-and$null-ne$property.Value-and$null-ne$property.Value.PSObject.Properties['hooks']){$property.Value.hooks=@(New-ProducerHooks)}
  elseif($property.Name-ceq'nativeSave'-and$null-ne$property.Value-and$null-ne$property.Value.PSObject.Properties['hooks']){$property.Value.hooks=@(New-ProducerSaveHooks)}
  elseif($property.Name-ceq'dllMvid'-and$property.Value-ceq$FixtureMvid){$property.Value=$productMvid}
  elseif($property.Name-ceq'dllSha256'-and$FixtureSha-and$property.Value-ceq$FixtureSha){$property.Value=$productSha}
  Set-ProducerIdentity $property.Value $FixtureMvid $FixtureSha}}
# No positive may retain an impossible identity, the fixture product MVID or a synthetic product token anywhere in its tree.
function Assert-NoFixtureIdentity($Value,[string]$FixtureMvid){$text=ConvertTo-Json -InputObject $Value -Depth 100 -Compress
 foreach($bad in @('"NewRound"','Kingmaker.UnitLogic.UnitCombatState',('"'+$FixtureMvid+'"'),'"06001230"','"06001231"','"06001234"')){if($text.Contains($bad)){throw ('A retained fixture identity survives: '+$bad)}}
 $rows=[regex]::Matches($text,'"moduleMvid":"([0-9a-f-]+)"');if($rows.Count-lt4){throw 'No hook rows found.'};foreach($row in $rows){if($row.Groups[1].Value-cnotin@('07fa1e4d-8618-41b3-9b8d-faa17d3b26f7',$productMvid)){throw ('Foreign hook module '+$row.Groups[1].Value)}};$rows.Count}
$fixtureMvid='33333333-3333-4333-8333-333333333333'
# Round 21: replace every producer archive row (the exact-case observation row, never a capitalized archive graph) with the candidate
# codec's normalized row for a schema-3 input of that row's own campaign and area.
function Test-ArchiveRow($Value){$null-ne$Value-and$Value-is[Management.Automation.PSCustomObject]-and@($Value.PSObject.Properties.Name)-ccontains'schemaVersion'-and@($Value.PSObject.Properties.Name)-ccontains'nativeRestorationGrantedByDiagnostic'}
function Set-CurrentArchiveRows($Node){
 if($Node-is[Array]){foreach($item in $Node){Set-CurrentArchiveRows $item};return}
 if($Node-isnot[Management.Automation.PSCustomObject]){return}
 foreach($property in @($Node.PSObject.Properties)){
  if($property.Name-ceq'archive'-and(Test-ArchiveRow $property.Value)){$in=Read-HistoricalInput $property.Value.campaignId $property.Value.areaId;$property.Value=New-ProducerArchiveRow $in.data}
  else{Set-CurrentArchiveRows $property.Value}}}
function Get-ArchiveRowVersions($Node){
 if($Node-is[Array]){foreach($item in $Node){Get-ArchiveRowVersions $item};return}
 if($Node-isnot[Management.Automation.PSCustomObject]){return}
 foreach($property in @($Node.PSObject.Properties)){if($property.Name-ceq'archive'-and(Test-ArchiveRow $property.Value)){$property.Value.schemaVersion}else{Get-ArchiveRowVersions $property.Value}}}
function Assert-ArchiveRowVersions($Record,[int]$Expected){$versions=@(Get-ArchiveRowVersions $Record);if($versions.Count-lt5-or@($versions|Where-Object{$_-ne$Expected}).Count){throw ('Archive rows in the chain are not all '+$Expected+': '+($versions-join','))};$versions.Count}

$expectedNative=@(@('TurnBased.Controllers.TurnController','Prepare','06000C3C'),@('Kingmaker.Controllers.Combat.UnitCombatState','OnNewRound','0600939D'))
Check 'installed hook identity: the probe''s two native tokens reflect TurnController.Prepare 06000C3C and Kingmaker.Controllers.Combat.UnitCombatState.OnNewRound 0600939D in module 07fa1e4d' {
 for($i=0;$i-lt2;$i++){$h=$nativeHooks[$i];if($h.declaringType-cne$expectedNative[$i][0]-or$h.method-cne$expectedNative[$i][1]-or$h.token-cne$expectedNative[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw ('Reflected '+($h|ConvertTo-Json -Compress))}}
 if($null-ne$game.GetType('Kingmaker.UnitLogic.UnitCombatState',$false)){throw 'The legacy type exists in the installed module.'}
 if(@($game.GetType('Kingmaker.Controllers.Combat.UnitCombatState',$true).GetMember('NewRound',[Reflection.BindingFlags]'Public,NonPublic,Instance,Static')).Count-ne0){throw 'The installed type has a NewRound member.'}}
Check ('built candidate hook identity: product module '+$productMvid+' reflects NativeCombatActorPersistence.RestoreActor/RestoreDebt and MountedPersistenceService.RecordCompletedSave') {
 for($i=0;$i-lt2;$i++){$h=$productHooks[$i];if($h.declaringType-cne'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'-or$h.method-cne@('RestoreActor','RestoreDebt')[$i]-or$h.token-cnotmatch'^06[0-9A-F]{6}$'-or$h.moduleMvid-cne$productMvid){throw ('Reflected '+($h|ConvertTo-Json -Compress))}}
 if($saveHooks[2].type-cne'KingmakerMountedCombat.Integration.MountedPersistenceService'-or$saveHooks[2].moduleMvid-cne$productMvid){throw 'Completion hook differs.'}}
Check 'reader trusted metadata: the candidate artifact matched by SHA-256 and MVID yields exactly the child''s reflected product bindings' {
 $t=Get-KmcChunk7CandidateProductHooks ([pscustomobject]@{dllSha256=$productSha;dllMvid=$productMvid})
 if($null-eq$t-or$t.mvid-cne$productMvid-or$t.sha256-cne$productSha-or$t.tokens.Count-ne3){throw 'No trusted candidate metadata.'}
 foreach($h in $productHooks){if($t.tokens[$h.declaringType+'.'+$h.method]-cne$h.token){throw ('Trusted binding differs for '+$h.method)}}
 if($t.tokens[$saveHooks[2].type+'.'+$saveHooks[2].method]-cne$saveHooks[2].token){throw 'Trusted completion binding differs.'}
 if($null-ne(Get-KmcChunk7CandidateProductHooks ([pscustomobject]@{dllSha256=('e'*64);dllMvid=$productMvid}))){throw 'Another build identity received the candidate metadata.'}}
Reject 'reader trusted metadata: the candidate artifact''s SHA-256 under another module MVID' {[pscustomobject]@{dllSha256=$productSha;dllMvid='11111111-1111-4111-8111-111111111111'}} {param($r)[void](Get-KmcChunk7CandidateProductHooks $r)}
foreach($h in @($nativeHooks)+@($productHooks)){Write-Output ('HOOK restoration '+$h.declaringType+'.'+$h.method+' token '+$h.token+' module '+$h.moduleMvid+' ('+$h.prefix+'/'+$h.postfix+')')}
foreach($h in $saveHooks){Write-Output ('HOOK manual-save '+$h.type+'.'+$h.method+' token '+$h.token+' module '+$h.moduleMvid)}
# Incorrect hook identities each reader must refuse (round hook unless stated).
$hookMutations=[ordered]@{
 'legacy method name NewRound'={param($h)$h[1].method='NewRound'}
 'legacy declaring type Kingmaker.UnitLogic.UnitCombatState'={param($h)$h[1].declaringType='Kingmaker.UnitLogic.UnitCombatState'}
 'neighbouring token 0600939E'={param($h)$h[1].token='0600939E'}
 'preparation token on the round hook'={param($h)$h[1].token='06000C3C'}
 'round hook in a foreign module'={param($h)$h[1].moduleMvid='11111111-1111-4111-8111-111111111111'}
 'preparation declaring type substituted'={param($h)$h[0].declaringType='Kingmaker.Controllers.Combat.UnitCombatState'}}
$versionMutations=[ordered]@{'the input version 2 as the normalized graph'=2;'unsupported normalized graph 5'=5;'normalized graph as the string 4'='4'}

# --- (1) Bootstrap: a historical schema-3 input normalized to 4, complete current observation. ---
$bootstrapTemplate=Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json')
function New-BootstrapObservation{$f=Copy-Json $bootstrapTemplate;Set-ProducerIdentity $f $fixtureMvid $f.request.dllSha256;$o=$f.artifact.observations;$in=Read-HistoricalInput $f.request.persistenceLoad.gameId $f.request.persistenceLoad.area
 $o.archive=New-ProducerArchiveRow $in.data;Copy-Json $f}
function Invoke-Bootstrap($f){Assert-KmcChunk7BootstrapInventoryEvidence $f.request $f.artifact $f.source}
Check 'bootstrap reader: schema-3 input decoded by the candidate codec, normalized graph 4 and producer-reflected hooks accepted' {$f=New-BootstrapObservation;if($f.artifact.observations.archive.schemaVersion-ne4){throw 'not normalized'};[void](Assert-NoFixtureIdentity $f $fixtureMvid);Invoke-Bootstrap $f}
foreach($m in $hookMutations.GetEnumerator()){$script:mutation=$m.Value;Reject ('bootstrap reader: '+$m.Key) {New-BootstrapObservation} {param($f)& $script:mutation $f.artifact.observations.noPreparation.hooks;Invoke-Bootstrap $f}}
foreach($m in $versionMutations.GetEnumerator()){$script:value=$m.Value;Reject ('bootstrap reader: '+$m.Key) {New-BootstrapObservation} {param($f)$f.artifact.observations.archive.schemaVersion=$script:value;Invoke-Bootstrap $f}}

# --- (2) Manual seed: the same producer-shaped cold inventory as its source, through the Manual seed reader. ---
$seedDirectory=Join-Path ([IO.Path]::GetTempPath()) ('kmc-producer-observations-'+[Guid]::NewGuid().ToString('N'))
try{
 [void][IO.Directory]::CreateDirectory($seedDirectory);$seedFixture=Join-Path $seedDirectory 'bootstrap-producer-shaped.json'
 [IO.File]::WriteAllText($seedFixture,((New-BootstrapObservation)|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
 function Invoke-ManualSeed($f){Assert-KmcChunk7ManualSeedEvidence $f.request $f.artifact $f.source $f.inventory}
 # The suite's builder adds its Manual save observer with a synthetic completion token; the producer rows replace it (and every other copy).
 function New-CurrentManualSeed{$f=New-ManualSeedFixture;Set-ProducerIdentity $f $productMvid;$f} # in place: the builder's arrays must keep their shape
 Check 'Manual seed reader: producer-reflected hooks and normalized graph 4 (schema-3 input) accepted' {$f=New-CurrentManualSeed;[void](Assert-NoFixtureIdentity $f $fixtureMvid);if($f.artifact.observations.archive.schemaVersion-ne4){throw 'not normalized'};Invoke-ManualSeed $f}
 foreach($m in $hookMutations.GetEnumerator()){$script:mutation=$m.Value;Reject ('Manual seed reader: '+$m.Key) {New-CurrentManualSeed} {param($f)& $script:mutation $f.artifact.observations.noPreparation.hooks;Invoke-ManualSeed $f}}
 Reject 'Manual seed reader: impossible round hook in both agreeing cold-inventory copies (record copy and inventory artifact)' {New-CurrentManualSeed} {param($f)foreach($h in @($f.artifact.observations.sourceColdInventory.noPreparation.hooks[1],$f.inventory.observations.noPreparation.hooks[1])){$h.declaringType='Kingmaker.UnitLogic.UnitCombatState';$h.method='NewRound'};Invoke-ManualSeed $f}
 Reject 'Manual seed reader: agreeing cold-inventory copies give RestoreActor the RestoreDebt token' {New-CurrentManualSeed} {param($f)foreach($rows in @($f.artifact.observations.sourceColdInventory.noPreparation.hooks,$f.inventory.observations.noPreparation.hooks)){$rows[2].token=$rows[3].token};Invoke-ManualSeed $f}
 # Round 22 (R21-F2): the seed's normalized graph against its same-build cold inventory (both cold copies keep agreeing).
 $currentSeedFixture=$seedFixture;$earlierSeedFixture=Join-Path $repo 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'
 # Earlier-build synthetic fixture: module 33333333-..., its own product tokens and completion token 06001234, normalized 3 throughout.
 function New-EarlierManualSeed{$script:seedFixture=$earlierSeedFixture;try{New-ManualSeedFixture}finally{$script:seedFixture=$currentSeedFixture}}
 Check 'Manual seed reader: earlier-build synthetic fixture (module 33333333-..., not the current DLL) seed 3 with cold inventory 3 accepted' {$f=New-EarlierManualSeed;if($f.request.dllMvid-cne$fixtureMvid-or$f.artifact.observations.archive.schemaVersion-ne3-or$f.artifact.observations.sourceColdInventory.archive.schemaVersion-ne3-or$f.inventory.observations.archive.schemaVersion-ne3){throw 'not the earlier 3/3 fixture'};Invoke-ManualSeed $f}
 Reject 'Manual seed reader: seed normalized 3 beside its same-build cold inventory 4 (both cold copies agree)' {New-CurrentManualSeed} {param($f)$f.artifact.observations.archive.schemaVersion=3;Invoke-ManualSeed $f}
 Reject 'Manual seed reader: seed normalized 4 beside its same-build cold inventory 3 (both cold copies agree)' {New-EarlierManualSeed} {param($f)$f.artifact.observations.archive.schemaVersion=4;Invoke-ManualSeed $f}
 Reject 'Manual seed reader: a uniform RestoreActor/RestoreDebt swap across all agreeing copies' {New-CurrentManualSeed} {param($f)foreach($rows in @($f.artifact.observations.noPreparation.hooks,$f.artifact.observations.sourceColdInventory.noPreparation.hooks,$f.inventory.observations.noPreparation.hooks)){$t=$rows[2].token;$rows[2].token=$rows[3].token;$rows[3].token=$t};Invoke-ManualSeed $f}
 foreach($m in $versionMutations.GetEnumerator()){$script:value=$m.Value;Reject ('Manual seed reader: '+$m.Key) {New-CurrentManualSeed} {param($f)$f.artifact.observations.archive.schemaVersion=$script:value;Invoke-ManualSeed $f}}
}finally{if(Test-Path -LiteralPath $seedDirectory){Remove-Item -LiteralPath $seedDirectory -Recurse -Force}}

# --- (3) Manual-derived, non-missing archive (R13-F2, R18-F5). ---
$derivedFixtures=Join-Path $repo 'tests/Chunk7Diagnostics'
function Invoke-ManualDerived($f){Assert-KmcChunk7ManualDerivedEvidence $f.request $f.artifact $f.source}
# Current build: the schema-3 input decoded by the candidate (normalized 4) and this build's own Manual write (SourceSchema of its output).
function New-CurrentManualDerived([bool]$Paid=$true){$f=New-ManualDerivedFixture $Paid;Set-ProducerIdentity $f $fixtureMvid $f.request.dllSha256;Set-CurrentArchiveRows $f;$o=$f.artifact.observations;$in=Read-HistoricalInput $f.request.persistenceLoad.gameId $f.request.persistenceLoad.area
 $o.archive=New-ProducerArchiveRow $in.data;$o.nativeWrite.schema=Get-CurrentWrittenSchema $in.data;Copy-Json $f}
# Earlier build (schema-3 writer) control: the retained synthetic fixture with its own identity (module 33333333-..., product tokens
# 06001230/06001231/06001234 of that module, one completion token across source, copy and outer record) and 3/3 throughout its chain. The
# installed native rows are the real ones. It is not reflection of the current build and not native evidence.
function New-EarlierManualDerived{$f=New-ManualDerivedFixture $true;Copy-Json $f}
function Get-CompletionTokens($f){@(@($f.artifact.observations.nativeSave.hooks[2],$f.artifact.observations.sourceManualObservations.nativeSave.hooks[2],$f.source.observations.nativeSave.hooks[2])|ForEach-Object{$_.token}|Select-Object -Unique)}
foreach($paid in @($true,$false)){$script:paid=$paid;$label=if($paid){'paid'}else{'existing-Horse'}
 Check ('Manual-derived reader ('+$label+'): schema-3 input decoded by the current writer, graph 4 with its own Manual write 4, producer hooks accepted') {$f=New-CurrentManualDerived $script:paid;if($f.artifact.observations.archive.schemaVersion-ne4-or$f.artifact.observations.nativeWrite.schema-ne4){throw 'not 4/4'};[void](Assert-NoFixtureIdentity $f $fixtureMvid);[void](Assert-ArchiveRowVersions $f 4);Invoke-ManualDerived $f}}
Check 'Manual-derived reader: earlier-build synthetic fixture (module 33333333-..., its own product tokens, not the current DLL) 3/3 throughout its chain accepted' {$f=New-EarlierManualDerived;if($f.artifact.observations.nativeWrite.schema-ne3-or$f.request.dllMvid-cne$fixtureMvid-or$f.request.dllMvid-ceq$productMvid){throw 'not the earlier-build fixture'};if(@(Get-CompletionTokens $f).Count-ne1){throw 'The synthetic fixture carries more than one completion token.'};[void](Assert-ArchiveRowVersions $f 3);Invoke-ManualDerived $f}
Reject 'Manual-derived reader: same-process crossing, normalized graph 3 beside its own Manual write 4' {New-CurrentManualDerived} {param($f)$f.artifact.observations.archive.schemaVersion=3;Invoke-ManualDerived $f}
Reject 'Manual-derived reader: same-process crossing, normalized graph 4 beside its own Manual write 3' {New-EarlierManualDerived} {param($f)$f.artifact.observations.archive.schemaVersion=4;Invoke-ManualDerived $f}
foreach($m in $hookMutations.GetEnumerator()){$script:mutation=$m.Value;Reject ('Manual-derived reader: '+$m.Key) {New-CurrentManualDerived} {param($f)& $script:mutation $f.artifact.observations.noPreparation.hooks;Invoke-ManualDerived $f}}
# Round 22 (R21-F1): completion-token disagreement and a uniform swap of the reflected current restoration bindings.
Reject 'Manual-derived reader: synthetic fixture whose outer completion token 0600A777 differs from its source and copy 06001234 (same module)' {New-EarlierManualDerived} {param($f)$f.artifact.observations.nativeSave.hooks[2].token='0600A777';Invoke-ManualDerived $f}
foreach($paid in @($true,$false)){$script:paid=$paid;$label=if($paid){'paid'}else{'existing-Horse'}
 Reject ('Manual-derived reader ('+$label+'): outer RecordCompletedSave token differs from its same-candidate source and copy') {New-CurrentManualDerived $script:paid} {param($f)$f.artifact.observations.nativeSave.hooks[2].token=$(if($f.artifact.observations.nativeSave.hooks[2].token-ceq'06FFFFFD'){'06FFFFFC'}else{'06FFFFFD'});Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): a uniform RestoreActor/RestoreDebt swap across all otherwise agreeing copies') {New-CurrentManualDerived $script:paid} {param($f)foreach($rows in @($f.artifact.observations.noPreparation.hooks,$f.artifact.observations.sourceManualObservations.noPreparation.hooks,$f.artifact.observations.sourceManualObservations.sourceColdInventory.noPreparation.hooks,$f.source.observations.noPreparation.hooks,$f.source.observations.sourceColdInventory.noPreparation.hooks)){$t=$rows[2].token;$rows[2].token=$rows[3].token;$rows[3].token=$t};Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): a uniform RecordCompletedSave token change across all otherwise agreeing copies') {New-CurrentManualDerived $script:paid} {param($f)foreach($rows in @($f.artifact.observations.nativeSave.hooks,$f.artifact.observations.sourceManualObservations.nativeSave.hooks,$f.source.observations.nativeSave.hooks)){$rows[2].token='06FFFFFD'};Invoke-ManualDerived $f}}
# Round 20: both source copies agree (the source record and the outer record's copy), yet each carries the impossible round hook.
foreach($paid in @($true,$false)){$script:paid=$paid;$label=if($paid){'paid'}else{'existing-Horse'}
 Reject ('Manual-derived reader ('+$label+'): impossible round hook in both agreeing Manual seed source copies') {New-CurrentManualDerived $script:paid} {param($f)foreach($h in @($f.source.observations.noPreparation.hooks[1],$f.artifact.observations.sourceManualObservations.noPreparation.hooks[1])){$h.declaringType='Kingmaker.UnitLogic.UnitCombatState';$h.method='NewRound'};Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): impossible round hook in both agreeing cold-inventory source copies') {New-CurrentManualDerived $script:paid} {param($f)foreach($h in @($f.source.observations.sourceColdInventory.noPreparation.hooks[1],$f.artifact.observations.sourceManualObservations.sourceColdInventory.noPreparation.hooks[1])){$h.declaringType='Kingmaker.UnitLogic.UnitCombatState';$h.method='NewRound'};Invoke-ManualDerived $f}}
# Round 21 (R20-F2, R20-F3): agreeing same-candidate source copies that downgrade a normalized graph, or rebind a product method's token.
foreach($paid in @($true,$false)){$script:paid=$paid;$label=if($paid){'paid'}else{'existing-Horse'}
 Reject ('Manual-derived reader ('+$label+'): agreeing Manual seed source copies downgraded to normalized 3 beside the current writer') {New-CurrentManualDerived $script:paid} {param($f)foreach($row in @($f.source.observations.archive,$f.artifact.observations.sourceManualObservations.archive)){$row.schemaVersion=3};Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): agreeing cold-inventory source copies downgraded to normalized 3 beside the current writer') {New-CurrentManualDerived $script:paid} {param($f)foreach($row in @($f.source.observations.sourceColdInventory.archive,$f.artifact.observations.sourceManualObservations.sourceColdInventory.archive)){$row.schemaVersion=3};Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): agreeing Manual seed source copies give RestoreActor the RestoreDebt token') {New-CurrentManualDerived $script:paid} {param($f)foreach($rows in @($f.source.observations.noPreparation.hooks,$f.artifact.observations.sourceManualObservations.noPreparation.hooks)){$rows[2].token=$rows[3].token};Invoke-ManualDerived $f}
 Reject ('Manual-derived reader ('+$label+'): agreeing Manual seed source copies bind RestoreActor to another token of the same module than this record') {New-CurrentManualDerived $script:paid} {param($f)foreach($rows in @($f.source.observations.noPreparation.hooks,$f.artifact.observations.sourceManualObservations.noPreparation.hooks)){$rows[2].token='06FFFFFE'};Invoke-ManualDerived $f}}
foreach($m in $versionMutations.GetEnumerator()){$script:value=$m.Value;Reject ('Manual-derived reader: '+$m.Key) {New-CurrentManualDerived} {param($f)$f.artifact.observations.archive.schemaVersion=$script:value;Invoke-ManualDerived $f}}

# --- (4) Pre-owned owner seed (R18-F5): the owner-made seed's normalized graph against the same process's writer capability. ---
function Invoke-World($v){Assert-KmcChunk7WorldEvidence $v.request $v.artifact 'PASS'}
# Current build: the owner's schema-3 seed decoded by this build (normalized 4), capability 4 and its own Manual output from the current writer.
function New-CurrentPreOwned{$v=New-KmcMounted6ManualFixture;$o=$v.artifact.observations;$in=Read-HistoricalInput $v.request.persistenceLoad.gameId $v.request.persistenceLoad.area
 $o.ownerSeedSetup.ownerMadeEmptyArchive=New-ProducerArchiveRow $in.data;$o.nativeWrite.archive=ConvertTo-CurrentWriterArchive $o.nativeWrite.archive;$o.nativeWrite.sourceSchema=4;$o.capability.nativeWriterSchema=4;$v}
Check 'pre-owned owner-seed reader: schema-3 seed decoded by the current writer, graph 4 with capability 4 and Manual output 4 accepted' {$v=New-CurrentPreOwned;if($v.artifact.observations.ownerSeedSetup.ownerMadeEmptyArchive.schemaVersion-ne4){throw 'not normalized'};Invoke-World $v}
Check 'pre-owned owner-seed reader: earlier-build 3/3 control accepted' {$v=New-KmcMounted6ManualFixture;$o=$v.artifact.observations;if($o.ownerSeedSetup.ownerMadeEmptyArchive.schemaVersion-ne3-or$o.capability.nativeWriterSchema-ne3){throw 'not 3/3'};Invoke-World $v}
Reject 'pre-owned owner-seed reader: same-process crossing, normalized graph 3 beside writer capability 4' {New-CurrentPreOwned} {param($v)$v.artifact.observations.ownerSeedSetup.ownerMadeEmptyArchive.schemaVersion=3;Invoke-World $v}
Reject 'pre-owned owner-seed reader: same-process crossing, normalized graph 4 beside writer capability 3' {New-KmcMounted6ManualFixture} {param($v)$v.artifact.observations.ownerSeedSetup.ownerMadeEmptyArchive.schemaVersion=4;Invoke-World $v}

# --- (5) Assisted cold: the capture emitted by the actual assisted scenario source (compiled from src/ with controlled native facades). ---
$assistedRoot=Join-Path $lab ('analysis-cache/producer-observations-assisted-'+[Guid]::NewGuid().ToString('N'))
$scenarioScript=Join-Path $PSScriptRoot 'Test-Chunk7AssistedColdScenario.ps1'
$ErrorActionPreference='Continue';try{$global:LASTEXITCODE=0;$scenarioText=@(& (Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe') -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $scenarioScript -RepositoryRoot $repo -OutputDirectory (Join-Path $assistedRoot 'scenario') 2>&1);$scenarioExit=$global:LASTEXITCODE}finally{$ErrorActionPreference='Stop'}
$scenarioReceipt=Join-Path $assistedRoot 'scenario/receipt.json'
Check 'assisted scenario: the actual producer source compiled and emitted its cold capture' {if($scenarioExit-ne0-or-not(Test-Path -LiteralPath $scenarioReceipt)-or(Read-KmcJson $scenarioReceipt).status-cne'PASS'){throw ('Assisted scenario failed (exit '+$scenarioExit+'): '+(@($scenarioText|Select-Object -Last 3)-join' | '))}}
if($scenarioExit-eq0){
 $emitted=Join-Path $assistedRoot 'scenario/emitted';$raw=@{};foreach($leaf in @('cold-actual-capture.json','cold-actual-request.json','cold-generated-source.json')){$raw[$leaf]=Read-KmcJson (Join-Path $emitted $leaf)}
 # The emitted generated request/capture/source carry a placeholder DLL MVID; the built module MVID replaces it before composition.
 foreach($leaf in @($raw.Keys)){if($raw[$leaf].dllMvid-cne'11111111-1111-1111-1111-111111111111'){throw 'The emitted DLL MVID placeholder changed.'};$raw[$leaf].dllMvid=$productMvid}
 $template=Copy-Json $bootstrapTemplate;$output=Join-Path $assistedRoot 'reader-cases';[void][IO.Directory]::CreateDirectory($output);$script:caseNumber=0
 # As in the strict assisted reader suite, only the filesystem lab root is private to each generated case (defined last: later cases only).
 function Get-KmcLabRoot {$script:caseLab}
 function New-CurrentAssisted{$f=New-Fixture;$f.artifact.observations.noPreparation.hooks=@(New-ProducerHooks);$f}
 Check 'assisted cold reader: the emitted current capture (producer archive row at graph 4) with producer-reflected hooks accepted' {$f=New-CurrentAssisted;if($raw['cold-actual-capture.json'].observations.archive.schemaVersion-ne4-or$f.artifact.observations.archive.schemaVersion-ne4){throw 'The emitted capture is not at graph 4.'};[void](Assert-NoFixtureIdentity ([pscustomobject]@{request=$f.request;artifact=$f.artifact;source=$f.source}) $fixtureMvid);if(@($f.request.dllMvid,$f.artifact.dllMvid,$f.source.dllMvid)-cne$productMvid){throw 'A placeholder DLL MVID survives.'};Verify $f}
 Check 'assisted cold reader: earlier-build normalized graph 3 control accepted (this record carries no writer field)' {$f=New-CurrentAssisted;$f.artifact.observations.archive.schemaVersion=3;Verify $f}
 foreach($m in $hookMutations.GetEnumerator()){$script:mutation=$m.Value;Reject ('assisted cold reader: '+$m.Key) {New-CurrentAssisted} {param($f)& $script:mutation $f.artifact.observations.noPreparation.hooks;Verify $f}}
 foreach($m in $versionMutations.GetEnumerator()){$script:value=$m.Value;Reject ('assisted cold reader: '+$m.Key) {New-CurrentAssisted} {param($f)$f.artifact.observations.archive.schemaVersion=$script:value;Verify $f}}
}

$clock.Stop();$passed=@($results|Where-Object pass -EQ $true).Count;$failed=@($results|Where-Object pass -EQ $false).Count;$refusals=@($results|Where-Object{$_.pass-and$_.expectedRefusal}).Count
Write-Output ('PRODUCER OBSERVATIONS RESULT '+$passed+' PASS / '+$failed+' FAIL ('+$refusals+' expected refusals; '+[Math]::Round($clock.Elapsed.TotalSeconds,1)+' s; offline generated observations, nativeInvoked=false, no native save/load claim)')
if($failed){exit 1}
