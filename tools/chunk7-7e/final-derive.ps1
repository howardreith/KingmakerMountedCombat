[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$Tag,
 [ValidateSet('Final','Reproduce','CrossLineage')][string]$Mode='Final',
 [string]$ExpectPath,
 [string]$RunMapPath,
 [ValidatePattern('^[0-9a-f]{40}$')][string]$Commit,
 [ValidatePattern('^[0-9a-f]{64}$')][string]$DllSha256,
 [ValidatePattern('^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')][string]$DllMvid,
 [string]$LabRoot='C:\Dev\KingmakerMountedCombatLab'
)
# Chunk 7 7E final selection deriver (planning/CHUNK7-7E-FINAL-RUN-PLAN.md, section 5, gate G6).
# A selection deriver only: it prints one validated selection, never a run plan, request or launch.
#
# Final: start from the named template request's selection and replace only lineage pins
# (predecessor run ids, evidence artifact SHA-256, written archive SHA-256, minted actor ids,
# capital sourceArea, assisted readiness facts) with values read from the final campaign's own
# settled predecessor runs, named in -RunMapPath ({"tag":{"runId","resultSha256","readerVerdict":"PASS",
# "restorationVerified":true}}, written only after the external reader and restoration checks). Every predecessor must be a
# PASS on the final candidate (-Commit/-DllSha256/-DllMvid). Prints the compact selection.
# Reproduce: the same code path, with each role bound to the run the template itself names; the
# derived bytes must equal the template request's selection (and the retained selection file
# where one exists) byte for byte. -Tag all reproduces every tag. Writes nothing.
# CrossLineage (offline proof only): Final's substitution with any settled run ids of one earlier
# candidate in the run map; with -ExpectPath the output must equal that file byte for byte.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$ev=Join-Path $LabRoot 'runtime-evidence';$cache=Join-Path $LabRoot 'analysis-cache\chunk7-continuation'
$O='ownedPairs';$OP='OwnedPairsSelectionPath'
$sp=@{source=@('sourceRunId')};$scp=@{source=@('sourceRunId');checkpoint=@('checkpointSourceRunId')}
$ownSrc=@(@('sourceArtifactSha256','source','artifact'),@('sourceArchiveSha256','source','ev:observations.nativeWrite.sha256'))
$ownCp=$ownSrc+@(,@('checkpointSourceArtifactSha256','checkpoint','artifact'))
$pairIds=@(@('mountAId','source','pair:riderAId'),@('mountBId','source','pair:riderBId'))
$cpPairIds=@(@('mountAId','checkpoint','pair:riderAId'),@('mountBId','checkpoint','pair:riderBId'))
$acqIds=@(@('mountAId','source','rq:acquiredManual.priorPetId'),@('riderBId','source','rq:acquiredManual.actorId'),@('mountBId','source','rq:acquiredManual.petId'))
$prog=@(@('sourceArtifactSha256','source','artifact'),@('sourceArchiveSha256','source','ev:observations.nativeWrite.sha256'),@('inventoryArtifactSha256','inventory','artifact'),@('ownedSource','inventory','rq:ownedPairs'))
$progCold=@(@('sourceArtifactSha256','source','artifact'),@('sourceArchiveSha256','source','ev:observations.session.nativeWrite.sha256'),@('inventoryArtifactSha256','inventory','artifact'))
$progRoles=@{source=@('sourceRunId');inventory=@('inventoryRunId')}
$assistedWrite='ev:observations.nativeWrites[?fileName~^Manual_\d+_KMC_C7_ASSISTED_FIXTURE\.zks$].sha256'
$coldPre='ev:observations.nativeProfileInventory.ordinaryAcquisitionPrerequisites.'
function S($tpl,$key,$param,$map,$roles=@{},$pins=@(),$ids=@(),$file=$null){[pscustomobject]@{tpl=$tpl;key=$key;param=$param;map=$map;roles=$roles;pins=$pins;ids=$ids;file=$file}}
# Tag -> template run, selection key, runner selection parameter, final role -> predecessor tag,
# role -> template selection field naming its run, pins (selection path, role, resolver), minted ids.
$Spec=[ordered]@{
 c1 =S '20261024-c7e-b199-c1-ownerseed-cold' 'assistedColdInventory' 'AssistedColdInventorySelectionPath' @{} -file 'b199-c1-selection.json'
 c2 =S '20261024-c7e-b199-c2-ownerfield' 'manualDerived' 'ManualDerivedSelectionPath' @{source='c1'} $sp @(,@('sourceArtifactSha256','source','artifact')) -file 'b199-c2-selection.json'
 c3 =S '20261024-c7e-b199-c3-acquired-cold' 'acquiredManual' 'AcquiredManualSelectionPath' @{source='c2'} $sp $ownSrc @(,@('petId','source','ev:observations.field.petId')) 'b199-c3-selection.json'
 c4 =S '20261024-c7e-b199-c4-cumulative' 'cumulativeAcquisition' 'CumulativeAcquisitionSelectionPath' @{source='c3'} $sp $ownSrc @(,@('priorPetId','source','rq:acquiredManual.petId')) 'b199-c4-selection.json'
 c5 =S '20261024-c7e-b199-c5-acquired-postfield' 'acquiredManual' 'AcquiredManualSelectionPath' @{source='c4'} $sp $ownSrc @(@('petId','source','ev:observations.field.petId'),@('actorId','source','ev:observations.field.actorId'),@('priorPetId','source','rq:cumulativeAcquisition.priorPetId')) 'b199-c5-selection.json'
 r01=S '20261018-c7b-world26-ownerseed-b3s1d-save' $O $OP @{}
 r02=S '20261018-c7b-world26-ownerseed-b4s1-load' $O $OP @{source='r01'} $sp $ownSrc $pairIds
 r03=S '20261018-c7b-world36-ownerseed-b9s1-quicksave' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r04=S '20261018-c7b-world36-ownerseed-b9s2-quickload' $O $OP @{source='r01';checkpoint='r03'} $scp $ownCp $pairIds
 r05=S '20261018-c7b-world36-ownerseed-b9s3-autosave' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r06=S '20261018-c7b-world36-ownerseed-b9s4-autoload' $O $OP @{source='r01';checkpoint='r05'} $scp $ownCp $pairIds
 r11=S '20261018-c7b-world37-ownerseed-b10s1-rtsave' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r12=S '20261018-c7b-world38-ownerseed-b10s3-rtload' $O $OP @{source='r01';checkpoint='r11'} $scp $ownCp $pairIds
 r13=S '20261018-c7b-world40-ownerseed-b11s1-tbsave' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r14=S '20261018-c7b-world41-ownerseed-b12s1-tbload' $O $OP @{source='r01';checkpoint='r13'} $scp $ownCp $pairIds
 r15=S '20261019-c7b-world52-ownerseed-b28s1-zerosave' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r16=S '20261019-c7b-world52-ownerseed-b30s1-zerocold' $O $OP @{source='r01';checkpoint='r15'} $scp $ownCp $pairIds
 r17=S '20261019-c7b-directive4-b41s1-schema3-producer' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r18=S '20261019-c7b-directive4-b41s2-schema2-resave' $O $OP @{source='r01';checkpoint='r17'} $scp $ownCp $pairIds
 r19=S '20261019-c7b-directive4-b42s1-schema2-current-cold' $O $OP @{source='r01';checkpoint='r18'} $scp $ownCp $pairIds
 # X3 residual (row 75): the reinstated pending-schema3-control-manual cold consumer of r17's own genuine Manual.
 # Its template b37s1 is an outer-FAIL development run used only for the selection shape, never as a predecessor.
 x3 =S '20261019-c7b-directive4-b37s1-schema3-corrected' $O $OP @{source='r01';checkpoint='r17'} $scp $ownCp $pairIds
 r20=S '20261019-c7b-world45-ownerseed-b19s4-schema1save' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r21=S '20261019-c7b-world45-ownerseed-b19s5-schema1cold' $O $OP @{source='r01';checkpoint='r20'} $scp $ownCp $pairIds
 r22=S '20261019-c7b-world45-ownerseed-b20s1-schema1current' $O $OP @{source='r01';checkpoint='r21'} $scp $ownCp $pairIds
 r23=S '20261019-c7b-world52-ownerseed-b31s1-malformed' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r24=S '20261019-c7b-world52-ownerseed-b31s2-future' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r25=S '20261019-c7b-world43-ownerseed-b14s1-worker' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r26=S '20261019-c7b-world43-ownerseed-b14s2-barrier' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r27=S '20261019-c7b-world48-ownerseed-b23s2-threeloads' $O $OP @{source='r01'} $sp $ownSrc $pairIds
 r28=S '20261020-c7b-world77-b60s1-manticore-spider-auto' $O $OP @{}
 r29=S '20261020-c7b-world77-b61s1-manticore-spider-auto-cold' $O $OP @{checkpoint='r28'} @{checkpoint=@('checkpointSourceRunId')} @(,@('checkpointSourceArtifactSha256','checkpoint','artifact'))
 r30=S '20261023-c7b-world192d-b192d1-doorstatic-explore' $O $OP @{} -file 'b192-door-seed-exploration-selection.json'
 x4s=S '20261024-c7e-b199-x4s-mounted-save' $O $OP @{source='c5'} $sp $ownSrc $acqIds 'b199-x4s-selection.json'
 x4c=S '20261024-c7e-b196-x4c-three-area-cold' $O $OP @{source='c5';checkpoint='x4s'} $scp $ownCp $acqIds 'b196-x4c-selection.json'
 r58=S '20261024-c7e-b199-r58-capital-arrival' $O $OP @{source='c5';checkpoint='x4s'} $scp $ownCp $acqIds 'b199-r58-selection.json'
 r59=S '20261024-c7e-b199-r59-capital-cold' $O $OP @{source='c5';checkpoint='r58'} $scp ($ownCp+@(,@('sourceArea','checkpoint','ev:observations.capitalArrival.sourceArea'))) $acqIds 'b199-r59-selection.json'
 r07=S '20261018-c7b-world35-ownerseed-b8s2-tbrider' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r08=S '20261018-c7b-world35-ownerseed-b8s3-tbmount' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r09=S '20261018-c7b-world35-ownerseed-b8s1-rt' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r10=S '20261019-c7b-world52-ownerseed-b27s1-disable' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds
 r57=S '20261024-c7e-b197-r57-ownerseed-life' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds 'b197-r57-selection.json'
 p2 =S '20261024-c7e-b197-p2-twopair-life' $O $OP @{source='c5'} $sp $ownSrc $acqIds 'b197-p2-selection.json'
 d1 =S '20261024-c7e-b205-d1-owner-seed-native-death-cleanup' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds 'b205-d1-selection.json'
 am =S '20261024-c7e-b205-am-owner-seed-attack-modes' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds 'b205-am-selection.json'
 cr =S '20261024-c7e-b205-cr-owner-seed-combat-reaction' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds 'b205-cr-selection.json'
 st =S '20261024-c7e-b205-st-owner-seed-staged' $O $OP @{source='r01';checkpoint='r02'} $scp $ownCp $pairIds 'b205-st-selection.json'
 # B17 mounted6 lineage (rows 79-81): m1 and m2 are literals in the plan; m3-m5 re-pin the b207 pilot's m2 (and m3) lineage.
 # m5's template (b207 m5, a false uncovered refusal before any input) is used only for the selection shape, never as a predecessor.
 m3 =S '20261025-c7e-b207-m3-mounted6-load' $O $OP @{source='m2'} $sp $ownSrc $pairIds 'b207-m3-selection.json'
 m4 =S '20261025-c7e-b207-m4-mounted6-prepared-casting' $O $OP @{source='m2';checkpoint='m3'} $scp $ownCp $pairIds 'b207-m4-selection.json'
 m5 =S '20261025-c7e-b207-m5-mounted6-attack-modes' $O $OP @{source='m2';checkpoint='m3'} $scp $ownCp $pairIds 'b207-m5-selection.json'
 k1 =S '20261024-c7e-b201-k1-chargeseed-cold' 'assistedColdInventory' 'AssistedColdInventorySelectionPath' @{} -file 'b201-k1-selection.json'
 k2 =S '20261024-c7e-b201-k2-chargeseed-save' $O $OP @{} -file 'b201-k2-selection.json'
 k3 =S '20261024-c7e-b201-k3-chargeseed-load' $O $OP @{source='k2'} $sp $ownSrc $pairIds 'b201-k3-selection.json'
 k4 =S '20261024-c7e-b205-k4-chargeseed-rtcharge' $O $OP @{source='k2';checkpoint='k3'} $scp $ownCp $pairIds 'b205-k4-selection.json'
 s1 =S '20261024-c7e-b197-s1-small-presentation' 'smallCalibration' 'SmallCalibrationSelectionPath' @{} -file 'b197-s1-selection.json'
 r33=S '20261021-c7b-world116-b110s1-small-admission' $O $OP @{}
 r34=S '20261021-c7b-world115-b108s2-small-mounted-cold' $O $OP @{checkpoint='r33'} @{checkpoint=@('checkpointSourceRunId')} @(,@('checkpointSourceArtifactSha256','checkpoint','artifact')) $cpPairIds
 r35=S '20261021-c7b-world124-b120s1-medium-mounted' $O $OP @{}
 r36=S '20261021-c7b-world124-b120s2-medium-mounted-cold' $O $OP @{checkpoint='r35'} @{checkpoint=@('checkpointSourceRunId')} @(,@('checkpointSourceArtifactSha256','checkpoint','artifact')) $cpPairIds
 r37=S '20261022-c7b-world189-b189s1-sixsave' $O $OP @{} -file 'b189-sixsave-selection.json'
 r43=S '20261021-c7b-world158-b158s1-assisted-producer' 'assistedFixture' 'AssistedFixtureSelectionPath' @{}
 r44=S '20261021-c7b-world158-b158s2-assisted-cold' 'assistedColdInventory' 'AssistedColdInventorySelectionPath' @{source='r43'} $sp @(@('sourceArtifactSha256','source','artifact'),@('sourceArchiveSha256','source',$assistedWrite))
 r45=S '20261021-c7b-world158-b158s3-paid-small' 'cumulativeAcquisition' 'CumulativeAcquisitionSelectionPath' @{source='r43';cold='r44'} @{source=@('sourceRunId');cold=@('assistedColdRunId')} @(
   @('sourceArtifactSha256','source','artifact'),@('sourceArchiveSha256','source',$assistedWrite),@('assistedColdArtifactSha256','cold','artifact'),
   @('readiness.mainActorId','cold',($coldPre+'mainCharacterId')),@('readiness.mainLevel','cold',($coldPre+'mainCharacterLevel')),
   @('readiness.mainExperience','cold',($coldPre+'mainCharacterExperience')),@('readiness.money','cold',($coldPre+'nativeMoney')),
   @('readiness.nativeHireCost','cold',($coldPre+'nativeHireCostNow')),@('readiness.defaultHireExperience','cold',($coldPre+'nativeDefaultHireExperienceNow')),
   @('readiness.partyCharacterIds','cold',($coldPre+'nativePartyCharacterIds|sort')),@('priorOwnerId','cold',($coldPre+'mainCharacterId')),
   @('priorPetId','cold','ev:observations.nativeProfileInventory.party[?masterId~.].id'))
 r48=S '20261021-c7b-world115-b108s3-small-native-level' 'progression' 'ProgressionSelectionPath' @{source='r33';inventory='r34'} $progRoles $prog
 r49=S '20261021-c7b-world115-b108s4-small-level-cold' 'progression' 'ProgressionSelectionPath' @{source='r48';inventory='r34'} $progRoles $progCold
 r50=S '20261020-c7b-world102-b89s3-small-native-dismiss' 'progression' 'ProgressionSelectionPath' @{source='r33';inventory='r34'} $progRoles $prog
 r51=S '20261021-c7b-world112-b100s3-small-native-retrain' 'progression' 'ProgressionSelectionPath' @{source='r33';inventory='r34'} $progRoles $prog
 r52=S '20261021-c7b-world112-b100s4-small-retrain-cold' 'progression' 'ProgressionSelectionPath' @{source='r51';inventory='r34'} $progRoles $progCold @(,@('petId','source','ev:observations.session.nativeUi.nativeRespec.reboundPetId'))
 r53=S '20261021-c7b-world124-b120s3-medium-mammoth-retrain' 'progression' 'ProgressionSelectionPath' @{source='r35';inventory='r36'} $progRoles $prog
 r54=S '20261021-c7b-world122-b118s4-medium-mammoth-cold' 'progression' 'ProgressionSelectionPath' @{source='r53';inventory='r36'} $progRoles $progCold @(,@('petId','source','ev:observations.session.nativeUi.nativeRespec.reboundPetId'))
 r38=S '20261022-c7b-world189-b189s2-sixcold' $O $OP @{checkpoint='r37'} @{checkpoint=@('checkpointSourceRunId')} @(,@('checkpointSourceArtifactSha256','checkpoint','artifact')) $cpPairIds 'b189-sixcold-selection.json'
 r39=S '20261022-c7b-world190-b190s1-sixmeasure' $O $OP @{} -file 'b188-sixslot-selection.json'
 r40=S '20261021-c7d-world156-b157s1-observer-off' $O $OP @{}
 r41=S '20261021-c7d-world156-b156s1-f11-native' $O $OP @{}
}
# Rows 31, 32, 42, 55 and 56 (r31, r32, r42, u1-u3) take no selection file: their runner parameters
# are fixed in the plan (section 5, B14/B15), so this deriver has nothing to derive for them. Row 76 (l6, B16) has no
# lineage pin: its selection is the literal owner-seed cold-inventory selection on KMC_C7_LEVEL6_SEED given in the plan.

function Read-Json([string]$Path){if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw "Missing receipt: $Path"};[IO.File]::ReadAllText($Path)|ConvertFrom-Json}
function Compact($Value){ConvertTo-Json -InputObject $Value -Depth 8 -Compress}
$cacheRun=@{}
function Get-Run([string]$RunId){
 if($cacheRun.ContainsKey($RunId)){return $cacheRun[$RunId]}
 if($RunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$'){throw "Malformed run id: $RunId"}
 $root=Join-Path $ev $RunId
 $request=Read-Json (Join-Path $root 'runtime-request.json');$result=Read-Json (Join-Path $root 'runtime-result.json')
 if($request.runId-cne$RunId-or$result.runId-cne$RunId){throw "Run receipts name another run: $RunId"}
 $rows=@((Read-Json (Join-Path $root 'runtime-artifacts.json')).artifacts|Where-Object{$_.kind-like'chunk7-*'})
 $art=$null;$evidence=$null
 if($rows.Count-eq1){
  $path=Join-Path $root $rows[0].relativePath;$sha=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
  if($sha-cne$rows[0].sha256){throw "Evidence artifact differs from its manifest: $RunId"}
  $art=$sha;$evidence=Read-Json $path
 }
 $o=[pscustomobject]@{runId=$RunId;request=$request;result=$result;artifactSha256=$art;evidence=$evidence;artifactCount=$rows.Count}
 $cacheRun[$RunId]=$o;$o
}
function Get-PathValue($Object,[string]$Path){
 $cur=$Object
 foreach($m in [regex]::Matches($Path,'\[\?([A-Za-z0-9_.]+)~([^\]]+)\]|\[(\d+)\]|([A-Za-z0-9_]+)')){
  if($null-eq$cur){throw "Path $Path reaches null."}
  if($m.Groups[1].Success){$prop=$m.Groups[1].Value;$rx=$m.Groups[2].Value
   $hit=@(@($cur)|Where-Object{$v=Get-PathValue $_ $prop;$v-is[string]-and$v-cmatch$rx});if($hit.Count-ne1){throw "Path $Path filter is not unique ($($hit.Count))."};$cur=$hit[0]}
  elseif($m.Groups[3].Success){$cur=@($cur)[[int]$m.Groups[3].Value]}
  else{$p=$cur.PSObject.Properties[$m.Groups[4].Value];if($null-eq$p){throw "Path $Path lacks $($m.Groups[4].Value)."};$cur=$p.Value}
 }
 ,$cur
}
function Set-PathValue($Object,[string]$Path,$Value){
 $parts=$Path.Split('.');$cur=$Object
 for($i=0;$i-lt$parts.Count-1;$i++){$cur=$cur.PSObject.Properties[$parts[$i]].Value}
 $p=$cur.PSObject.Properties[$parts[-1]];if($null-eq$p){throw "Selection lacks pinned field $Path."};$p.Value=$Value
}
function Resolve-Pin($Run,[string]$Resolver,$Selection){
 if($Resolver-ceq'artifact'){if($Run.artifactCount-ne1){throw "Run $($Run.runId) lacks one exact chunk7 evidence artifact."};return $Run.artifactSha256}
 if($Resolver.StartsWith('pair:')){
  $rider=Get-PathValue $Selection $Resolver.Substring(5)
  $all=Get-PathValue $Run.evidence 'observations.nativeWrite.archive.Pairs';$pairs=@($all|Where-Object{$_.Rider.Id-ceq$rider})
  if($pairs.Count-ne1){throw "Run $($Run.runId) archive lacks one pair for rider $rider."};return $pairs[0].Mount.Id
 }
 $sort=$Resolver.EndsWith('|sort');$r=if($sort){$Resolver.Substring(0,$Resolver.Length-5)}else{$Resolver}
 $value=if($r.StartsWith('ev:')){Get-PathValue $Run.evidence $r.Substring(3)}elseif($r.StartsWith('rq:')){Get-PathValue $Run.request $r.Substring(3)}else{throw "Unknown resolver $Resolver"}
 if($sort){$list=[Collections.Generic.List[string]]::new();foreach($x in @($value)){$list.Add([string]$x)};$list.Sort([StringComparer]::Ordinal);return ,([string[]]$list.ToArray())}
 ,$value
}
function Replace-Value($Node,[string]$Old,[string]$New){
 if($Node-is[pscustomobject]){foreach($p in @($Node.PSObject.Properties)){if($p.Value-is[string]){if($p.Value-ceq$Old){$p.Value=$New}}else{Replace-Value $p.Value $Old $New}};return}
 if($Node-is[Array]){for($i=0;$i-lt$Node.Count;$i++){if($Node[$i]-is[string]){if($Node[$i]-ceq$Old){$Node[$i]=$New}}else{Replace-Value $Node[$i] $Old $New}}}
}
function Get-LeafDiff($A,$B,[string]$Path){
 if($A-is[pscustomobject]){
  if($B-isnot[pscustomobject]-or(@($A.PSObject.Properties.Name)-join"`n")-cne(@($B.PSObject.Properties.Name)-join"`n")){throw "Derived selection changed shape at '$Path'."}
  foreach($p in $A.PSObject.Properties){Get-LeafDiff $p.Value $B.PSObject.Properties[$p.Name].Value ($(if($Path){$Path+'.'}else{''})+$p.Name)};return
 }
 if($A-is[Array]){
  if($B-isnot[Array]){throw "Derived selection changed shape at '$Path'."}
  if($A.Count-ne$B.Count){[pscustomobject]@{path=$Path;old=$null};return}
  for($i=0;$i-lt$A.Count;$i++){Get-LeafDiff $A[$i] $B[$i] ($Path+'['+$i+']')};return
 }
 if((Compact $A)-cne(Compact $B)){[pscustomobject]@{path=$Path;old=$A}}
}
# A runner FAIL settled by its reader-only re-evaluation (plan section 8): see final-reevaluation.ps1.
. (Join-Path $PSScriptRoot 'final-reevaluation.ps1')
$deriverRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
function Derive([string]$T,[string]$DeriveMode,$RunMap){
 $s=$Spec[$T];if($null-eq$s){throw "No selection is derived for tag $T."}
 $template=Get-Run $s.tpl;$tplSel=$template.request.($s.key);if($null-eq$tplSel){throw "Template $($s.tpl) has no $($s.key) selection."}
 $tplText=Compact $tplSel;$tplObj=$tplText|ConvertFrom-Json;$out=$tplText|ConvertFrom-Json
 $tplRuns=@{};$useRuns=@{}
 foreach($role in $s.map.Keys){
  $field=$s.roles[$role][0];$tplRuns[$role]=Get-Run ([string](Get-PathValue $tplObj $field))
  if($DeriveMode-ceq'Reproduce'){$useRuns[$role]=$tplRuns[$role]}else{
   $pred=$s.map[$role];if(-not$RunMap.ContainsKey($pred)){throw "Predecessor $pred ($role of $T) has not settled in the run map."}
   $entry=$RunMap[$pred];$id=if($entry-is[string]){$entry}else{[string]$entry.runId}
   if($DeriveMode-ceq'Final'){
    # Settled = the external Test-RuntimeResult verdict PASS plus the section 5 restoration checks,
    # recorded by the executor with the result hash it read; the runner status alone is not enough.
    if($entry-is[string]-or$entry.readerVerdict-cne'PASS'-or$entry.restorationVerified-ne$true-or[string]$entry.resultSha256-cnotmatch'^[0-9a-f]{64}$'){throw "Predecessor $pred lacks a settled verdict receipt (runId, resultSha256, readerVerdict PASS, restorationVerified true)."}
    $rs=(Get-FileHash -LiteralPath (Join-Path (Join-Path $ev $id) 'runtime-result.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    if($rs-cne$entry.resultSha256){throw "Predecessor $pred result differs from its settled verdict receipt."}
   }if($DeriveMode-ceq'Final'-and$id-cnotmatch('-c7e-final-B[0-9]+-'+[regex]::Escape($pred)+'r?$')){throw "Run map entry for $pred is not a final campaign run: $id"}
   $run=Get-Run $id
   if($run.result.status-cne'PASS'){
    # Plan section 8, reader-only defect: a runner FAIL is a predecessor only in Final mode and only with its reviewed
    # re-evaluation receipt (Test-KmcFinalReaderReevaluation); the retained runner verdict is never rewritten.
    if($DeriveMode-cne'Final'-or-not(Test-KmcFinalReaderReevaluation -Pred $pred -Entry $entry -Run $run -EvidenceRoot $ev -CacheRoot $cache -RepoRoot $deriverRepo -LabRoot $LabRoot)){throw "Predecessor $pred ($id) is not PASS."}
   }
   foreach($k in @(@('commit',$Commit),@('dllSha256',$DllSha256),@('dllMvid',$DllMvid))){if($run.request.($k[0])-cne$k[1]){throw "Predecessor $pred ($id) is not on the final candidate ($($k[0]))."}}
   $useRuns[$role]=$run}
 }
 $changes=[Collections.Generic.List[object]]::new()
 foreach($role in $s.map.Keys){$field=$s.roles[$role][0];Set-PathValue $out $field $useRuns[$role].runId;$changes.Add([pscustomobject]@{field=$field;template=$tplRuns[$role].runId;final=$useRuns[$role].runId})}
 foreach($pin in $s.pins){
  $tv=Resolve-Pin $tplRuns[$pin[1]] $pin[2] $tplObj
  if((Compact (Get-PathValue $tplObj $pin[0]))-cne(Compact $tv)){throw "Template $T field $($pin[0]) is not reproduced by $($pin[2]) on $($tplRuns[$pin[1]].runId)."}
  $fv=Resolve-Pin $useRuns[$pin[1]] $pin[2] $tplObj;Set-PathValue $out $pin[0] $fv
  $changes.Add([pscustomobject]@{field=$pin[0];template=(Compact $tv);final=(Compact $fv)})
 }
 foreach($idPin in $s.ids){
  $tv=[string](Resolve-Pin $tplRuns[$idPin[1]] $idPin[2] $tplObj);$fv=[string](Resolve-Pin $useRuns[$idPin[1]] $idPin[2] $tplObj)
  if([string](Get-PathValue $tplObj $idPin[0])-cne$tv){throw "Template $T id $($idPin[0]) is not reproduced by $($idPin[2])."}
  if($tv-cnotmatch'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'-or$fv-cnotmatch'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'){throw "Minted id for $T $($idPin[0]) is not a GUID."}
  Replace-Value $out $tv $fv;$changes.Add([pscustomobject]@{field=('id:'+$idPin[0]);template=$tv;final=$fv})
 }
 $text=Compact $out
 # Pin substitution only: the output has the template's shape, and every leaf that differs lies
 # under a declared role or pin field or held a declared minted id in the template.
 $fields=@($changes|Where-Object{-not$_.field.StartsWith('id:')}|ForEach-Object{$_.field})
 $oldIds=@($changes|Where-Object{$_.field.StartsWith('id:')}|ForEach-Object{$_.template})
 foreach($d in (Get-LeafDiff $tplObj ($text|ConvertFrom-Json) '')){
  $under=@($fields|Where-Object{$d.path-ceq$_-or$d.path.StartsWith($_+'.')-or$d.path.StartsWith($_+'[')}).Count-gt0
  if(-not$under-and-not($d.old-is[string]-and$d.old-cin$oldIds)){throw "Derived $T changed undeclared field $($d.path)."}
 }
 [pscustomobject]@{tag=$T;template=$s.tpl;text=$text;templateText=$tplText;changes=$changes;file=$s.file;param=$s.param}
}
if($Mode-ceq'Reproduce'){
 $tags=@(if($Tag-ceq'all'){$Spec.Keys}else{$Tag});$fail=0
 foreach($t in $tags){
  try{
   $d=Derive $t 'Reproduce' @{}
   $ok=$d.text-ceq$d.templateText;$fileNote='no retained file'
   if($d.file){$bytes=[IO.File]::ReadAllText((Join-Path $cache $d.file))
    # Older retained files (b188-b192) are indented JSON: they are compared after one parse.
    if($bytes.StartsWith('{"')){$fileOk=$d.text-ceq$bytes;$how='bytes'}else{$fileOk=$d.text-ceq(Compact ($bytes|ConvertFrom-Json));$how='parsed'}
    $ok=$ok-and$fileOk;$fileNote="file $($d.file) identical($how)=$fileOk"}
   "$t template=$($d.template) identical=$ok pins=$($d.changes.Count) $fileNote"
   if(-not$ok){$fail++}
  }catch{"$t FAIL: $($_.Exception.Message)";$fail++}
 }
 "REPRODUCE tags=$($tags.Count) fail=$fail"
 if($fail-ne0){exit 1};exit 0
}
if([string]::IsNullOrEmpty($RunMapPath)-or[string]::IsNullOrEmpty($Commit)-or[string]::IsNullOrEmpty($DllSha256)-or[string]::IsNullOrEmpty($DllMvid)){throw 'Final derivation requires -RunMapPath and the final candidate -Commit, -DllSha256 and -DllMvid.'}
$map=@{};foreach($p in (Read-Json $RunMapPath).PSObject.Properties){$map[$p.Name]=$p.Value}
$text=(Derive $Tag $Mode $map).text
if($Mode-ceq'CrossLineage'-and-not[string]::IsNullOrEmpty($ExpectPath)){
 $want=[IO.File]::ReadAllText($ExpectPath);$same=$text-ceq$want
 "$Tag cross-lineage identical=$same expected=$ExpectPath";if(-not$same){$text;exit 1};exit 0
}
$text
