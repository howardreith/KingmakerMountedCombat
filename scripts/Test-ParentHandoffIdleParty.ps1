param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Parent-to-child handoff contract: every tranche scenario whose child captures the
# whole disposable party as idle is handed off only after native combat has ended for
# every member, behind an exploration preamble Mount. Pins the exact scenario set, the
# bounded re-check immediately before child creation, the child's VoluntaryCombat
# preamble refusal and the read-only dispatch admission record. No native qualification.
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repoRoot 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$modPath=Join-Path $repoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')
$checks=0
function Check([bool]$Condition,[string]$Detail){if(-not$Condition){throw $Detail};$script:checks++;Write-Output ('PASS '+$Detail)}

# 1. The exact handoff set, executed against the launcher's complete scenario registry.
$launcher=Get-Content -Raw (Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1')
$setMatch=[regex]::Match($launcher,'(?s)\[ValidateSet\((.*?)\)\]\[string\]\$Scenario=')
if(-not$setMatch.Success){throw 'Launcher scenario registry not found'}
$registry=@([regex]::Matches($setMatch.Groups[1].Value,"'([^']+)'")|ForEach-Object{$_.Groups[1].Value})
if($registry.Count-lt100-or@($registry|Select-Object -Unique).Count-ne$registry.Count){throw 'Launcher scenario registry is incomplete or duplicated'}
Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Reflection;
public static class KmcParentHandoffProbe {
 public static string Evaluate(string modPath,string managed,string[] scenarios){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var mod=Assembly.LoadFrom(modPath);
  var tranche=mod.GetType("KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche",true);
  var requires=tranche.GetMethod("RequiresIdlePartyHandoff",BindingFlags.Static|BindingFlags.NonPublic);
  var supports=tranche.GetMethod("SupportsScenario",BindingFlags.Static|BindingFlags.NonPublic);
  if(requires==null||supports==null)throw new InvalidOperationException("Tranche predicates are missing");
  var lines=new System.Text.StringBuilder();
  foreach(var scenario in scenarios){
   var r=(bool)requires.Invoke(null,new object[]{scenario});
   var sup=(bool)supports.Invoke(null,new object[]{scenario});
   lines.Append(scenario).Append('|').Append(r?"1":"0").Append('|').Append(sup?"1":"0").Append('\n');
  }
  return lines.ToString();
 }
}
"@
$evaluated=[KmcParentHandoffProbe]::Evaluate($modPath,$managed,[string[]]$registry)
$rows=@($evaluated-split"`n"|Where-Object{$_-ne''}|ForEach-Object{$p=$_-split'\|';[pscustomobject]@{scenario=$p[0];requires=($p[1]-eq'1');supports=($p[2]-eq'1')}})
Check ($rows.Count-eq$registry.Count) 'every registered scenario was evaluated by the compiled predicates'
foreach($row in $rows){
 $expected=$row.supports-and($row.scenario-clike'chunk4-*'-or$row.scenario-clike'actor-allocation-*'-or$row.scenario-clike'chunk6a-*'-or$row.scenario-clike'chunk6b-*'-or$row.scenario-ceq'unmounted-attack-controls-rt')
 if($row.requires-ne$expected){throw ('Idle-party handoff set differs for '+$row.scenario+': compiled='+$row.requires+' expected='+$expected)}
}
$checks++;Write-Output ('PASS the compiled idle-party handoff set equals every tranche-supported chunk4, actor-allocation, chunk6a and unmounted-attack-controls scenario ('+@($rows|Where-Object requires).Count+' scenarios)')
Check (@($rows|Where-Object{$_.requires-and-not$_.supports}).Count-eq0) 'no scenario outside the tranche allowlist requires the handoff'
foreach($name in @('ordinary-attack-controls-tb','phase3d-unified-combat-tb-suite','phase3d-horse-presentation-suite','chunk6a-mount-preamble','mod-load-smoke','phase3h-combat-loop-rt')){
 Check (@($rows|Where-Object{$_.scenario-ceq$name-and-not$_.requires}).Count-eq1) ($name+' does not require the idle-party handoff')
}
foreach($name in @('unmounted-attack-controls-rt','chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-charge-safety-rt','chunk4-session-tb','actor-allocation-rider-first-tb','chunk6a-combat-mount-rt','chunk6a-repeated-mount-request')){
 Check (@($rows|Where-Object{$_.scenario-ceq$name-and$_.requires}).Count-eq1) ($name+' requires the idle-party handoff')
}

# 2. Every child that captures the idle party is a tranche scenario family named by the predicate.
$callers=@(Get-ChildItem (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics') -Filter '*.cs'|Where-Object{(Get-Content -Raw $_.FullName).Contains('CaptureIdleFixturePartyForCleanup();')}|ForEach-Object Name)
$expectedCallers=@('ActorAllocationScenarios.cs','Chunk4ChargeScenario.cs','Chunk6bChargePathScenario.cs','Chunk6bChargeScenario.cs','Chunk4GroundArrivalScenario.cs','Chunk4HorseStrikeScenario.cs','Chunk4IncomingScenario.cs','Chunk4InspectionScenario.cs','Chunk4InterruptScenario.cs','Chunk4NativeLifeScenario.cs','Chunk4NativeRangedControl.cs','Chunk4ObstructionScenario.cs','Chunk4PairedPlayScenario.cs','Chunk4SessionScenario.cs','Chunk4SustainedScenario.cs','Chunk6aCombatMountScenario.cs','Phase3dHorseScenarioTranche.cs')
Check ((@($callers|Sort-Object)-join'|')-ceq(@($expectedCallers|Sort-Object)-join'|')) 'the idle-party capture callers are exactly the known chunk4, actor-allocation, chunk6a and unmounted-controls child families'

# 3. Parent engine: generalized admission wait and the bounded re-check immediately before the child.
$parent=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/HorseCompanionUnmountedScenarioEngine.cs')
Check ($parent.Contains('if (Phase3dHorseScenarioTranche.RequiresIdlePartyHandoff(request.Scenario))')) 'the admission wait keys on the exact handoff predicate'
Check (-not$parent.Contains('Phase3dHorseScenarioTranche.IsChunk4ChargeScenario(request.Scenario))')) 'the two-family admission condition is gone'
$admission=[regex]::Match($parent,'(?s)private void AwaitMountedAlphaAdmission\(\)(.*?)private void BeginMountedAlpha\(\)')
Check ($admission.Success-and$admission.Value.Contains('RequiresIdlePartyHandoff(request.Scenario)')-and$admission.Value.Contains('owner.Group.Any(member => member.IsInCombat)')) 'the admission wait still checks Player.IsInCombat and every member before the preamble Mount'
$ready=[regex]::Match($parent,'(?s)private void AwaitMountedReady\(\)(.*?)private double trancheHandoffStartedAtSeconds;')
Check ($ready.Success-and$ready.Value.Contains('BeginTrancheHandoff();')-and-not$ready.Value.Contains('BeginPhase3dTranche(true);')) 'AwaitMountedReady hands off through the bounded re-check, never directly'
$handoff=[regex]::Match($parent,'(?s)private void AwaitTrancheHandoff\(\)(.*?)private void BeginPhase3dTranche\(bool pairAlreadyMounted\)')
Check ($handoff.Success) 'AwaitTrancheHandoff precedes BeginPhase3dTranche'
$body=$handoff.Value
$order=@('RequiresIdlePartyHandoff(request.Scenario)','["preambleMountAdmission"]','observations["chunk6aPartyHandoffAtChild"]','owner.Group.Any(member => member.IsInCombat)','MountedAlphaAdmissionTimeoutSeconds','"tranche-handoff-idle-party-deadline"','BeginCleanup();','BeginPhase3dTranche(true);')
$last=-1;foreach($needle in $order){$i=$body.IndexOf($needle,[StringComparison]::Ordinal);if($i-le$last){throw ('Handoff re-check order differs at '+$needle)};$last=$i}
$checks++;Write-Output 'PASS the handoff re-check publishes exact party state and admission mode, waits within the bounded leaf, fails with the state, and only then creates the child'
Check (([regex]::Matches($parent,[regex]::Escape('BeginPhase3dTranche(true);')).Count-eq1)) 'the mounted child is created in exactly one place'
Check ($parent.Contains('case EngineStep.AwaitTrancheHandoff:')-and$parent.Contains('AwaitTrancheHandoff,')) 'the handoff is its own re-entrant engine step'

# 4. Child: a VoluntaryCombat preamble is refused with its exact reason before the tranche starts.
$child=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
$start=[regex]::Match($child,'(?s)internal void Start\(bool pairAlreadyMounted\)(.*?)started = true;')
Check ($start.Success-and$start.Value.Contains('pairAlreadyMounted && RequiresIdlePartyHandoff(request.Scenario) &&')-and$start.Value.Contains('"VoluntaryCombat"')-and$start.Value.Contains('admitted as VoluntaryCombat')) 'the child refuses a VoluntaryCombat preamble Mount before starting'
Check ($child.Contains('observations["parentPreambleMountAdmission"] = playerAction.LastRelationshipDispatchAdmission ?? "<none>";')) 'the child publishes the preamble admission mode'

# 5. Controller: the admission mode is recorded exactly where the dispatch is made, read-only.
$controller=Get-Content -Raw (Join-Path $repoRoot 'src/KingmakerMountedCombat/Integration/MountedPlayerActionController.cs')
$record=$controller.IndexOf('LastRelationshipDispatchAdmission = admission.ToString();',[StringComparison]::Ordinal)
$dispatch=$controller.IndexOf('var transition = relationship.MountRiderOn(caster, target, admission);',[StringComparison]::Ordinal)
Check ($record-ge0-and$dispatch-gt$record) 'the dispatch admission is recorded immediately before the native Mount dispatch'
Check ($controller.Contains('internal string LastRelationshipDispatchAdmission { get; private set; }')-and([regex]::Matches($controller,[regex]::Escape('LastRelationshipDispatchAdmission =')).Count-eq1)) 'the admission record is read-only outside the dispatch'
Write-Output ('PARENT HANDOFF IDLE PARTY PASS='+$checks+' FAIL=0; compiled predicate and source contracts only, no native qualification')
