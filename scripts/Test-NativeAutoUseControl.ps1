param([ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$BaselineOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeAutoUseControlEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
. (Join-Path $PSScriptRoot 'runtime/NativeAutoUseControlEvidence.ps1')
# Exercise native JObject values before serialization erases String/null representation.
Add-Type -ReferencedAssemblies @((Join-Path $managed 'Newtonsoft.Json.dll'),'System.Core.dll') -TypeDefinition @"
using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
public static class KmcLiveAutoUseNullTokens {
 static void Expect(MethodInfo method,JObject evidence,bool expected,string label) {
  bool actual=true;string failure=null;
  try { method.Invoke(null,new object[]{evidence}); }
  catch(TargetInvocationException ex) {
   if(!(ex.InnerException is InvalidOperationException))throw;
   actual=false;failure=ex.InnerException.Message;
  }
  if(actual!=expected)throw new Exception(label+": expected="+expected+", actual="+actual+", failure="+failure);
 }
 static JObject StaleAvailable(JObject evidence) {
  return evidence["events"].OfType<JObject>().First(x=>(string)x["phase"]=="stale-reference"&&(string)x["kind"]=="available");
 }
 static void NativeStrings(JToken token) {
  var obj=token as JObject;
  if(obj!=null)foreach(var property in obj.Properties().ToArray()) {
   string name=property.Name;
   if(property.Value.Type==JTokenType.Null&&(name.EndsWith("Id",StringComparison.Ordinal)||name.EndsWith("Guid",StringComparison.Ordinal)||name=="guid"||name=="prefix"||name=="postfix"||name=="failure"))property.Value=(string)null;
   else NativeStrings(property.Value);
  }
  var array=token as JArray;if(array!=null)foreach(var child in array)NativeStrings(child);
 }
 public static int Run(string json,MethodInfo method,bool negatives) {
  int checks=0;var exact=JObject.Parse(json);StaleAvailable(exact)["result"]["guid"]=(string)null;
  var live=(JValue)StaleAvailable(exact)["result"]["guid"];
  if(live.Type!=JTokenType.String||live.Value!=null||live.ToString(Newtonsoft.Json.Formatting.None)!="null")throw new Exception("Native string-null setup differs");
  Expect(method,exact,true,"one native String/null availability");checks++;
  var all=JObject.Parse(json);NativeStrings(all);Expect(method,all,true,"native nullable strings without round trip");checks++;
  if(!negatives)return checks;
  JToken[] invalid={new JValue(""),new JValue(" "),new JValue("null"),new JValue("non-null-guid"),new JValue(0),new JValue(false),new JObject(),new JArray(),JValue.CreateUndefined()};
  foreach(var value in invalid) {
   var bad=(JObject)all.DeepClone();StaleAvailable(bad)["result"]["guid"]=value.DeepClone();
   Expect(method,bad,false,"non-null/malformed native availability "+value.Type);checks++;
  }
  var objectAvailable=(JObject)all.DeepClone();StaleAvailable(objectAvailable)["result"]["object"]=999;
  Expect(method,objectAvailable,false,"object availability despite null GUID");checks++;
  var suitable=(JObject)all.DeepClone();suitable["events"].OfType<JObject>().First(x=>(string)x["phase"]=="stale-reference"&&(string)x["kind"]=="suitability")["result"]=true;
  Expect(method,suitable,false,"native relationship eligibility");checks++;
  var admitted=(JObject)all.DeepClone();admitted["aiWithStaleReference"]["acted"]=true;
  Expect(method,admitted,false,"native AI admission");checks++;
  return checks;
 }
}
"@
$script:checks=0
function Copy-AutoUse($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Check($e,[bool]$expected,[string]$label){
 $producer=$true;$external=$true;$producerWhy='';$externalWhy=''
 try{$args=[object[]]::new(1);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$null=$method.Invoke($null,$args)}catch{$producer=$false;$producerWhy=$_.Exception.ToString()}
 try{Assert-KmcAutoUseControl $e}catch{$external=$false;$externalWhy=$_.Exception.ToString()}
 if($producer-ne$expected-or$external-ne$expected){throw ($label+' expected='+$expected+' producer='+$producer+' external='+$external+"`nProducer: "+$producerWhy+"`nExternal: "+$externalWhy)}
 $script:checks+=2
}
foreach($kind in @('mount','dismount')){foreach($original in @('false','true')){
 $p=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot ('fixtures/chunk6a-auto-use/fixture-'+$kind+'-'+$original+'.json'))|ConvertFrom-Json
 Check $p $true ('baseline '+$kind+' '+$original)
 $script:checks += [KmcLiveAutoUseNullTokens]::Run(($p|ConvertTo-Json -Depth 100 -Compress),$method,(-not $BaselineOnly))
 if($BaselineOnly){continue}
 function Reject([scriptblock]$Change){$x=Copy-AutoUse $p;& $Change $x;Check $x $false ($kind+' '+$original+' '+$Change.ToString())}
 foreach($field in @('riderObject','mountObject','targetObject','mechanicObject','aiObject','abilityObject','ordinaryAbilityObject','contextObject')){Reject {param($x)$x.$field++}}
 foreach($field in @('riderId','mountId','targetId','abilityGuid','ordinaryAbilityGuid','contract')){Reject {param($x)$x.$field='invalid'}}
 foreach($field in @('ordinarySuitable','constructorObserverExecuted','staleReferenceFixtureDeclared')){Reject {param($x)$x.$field=$false}}
 Reject {param($x)$x.suitable=$true};Reject {param($x)$x.availableObject=301};Reject {param($x)$x.availableGuid=$x.abilityGuid};Reject {param($x)$x.errors=@('missing observer')}
 Reject {param($x)$x.slotRegistration.occurrences=2};Reject {param($x)$x.slotRegistration.active=$false};Reject {param($x)$x.slotRegistration.casterId='mount'}
 foreach($endpoint in @('before','afterUi','afterLease','afterAi','afterRestoration')){
  foreach($field in @('frame','gameTicks','allocationSequence','selectedAbilityObject','autoUseObject','shellCount','processBindings')){Reject {param($x)$x.$endpoint.$field++}}
  foreach($field in @('paused','riderCommandsEmpty','mountCommandsEmpty')){Reject {param($x)$x.$endpoint.$field=$false}}
  Reject {param($x)$x.$endpoint.turnBased=$true};Reject {param($x)$x.$endpoint.autoUseGuid='other'}
  Reject {param($x)$x.$endpoint.state.generation++};Reject {param($x)$x.$endpoint.state.selectedIds=@('mount')};Reject {param($x)$x.$endpoint.state.relationshipState='Faulted'}
  Reject {param($x)$x.$endpoint.state.geometry.riderPosition.x++};Reject {param($x)$x.$endpoint.state.geometry.frame++}
  Reject {param($x)$x.$endpoint.state.ledger.forcedDetach++};Reject {param($x)$x.$endpoint.controls.NativeCastRequestCount++}
  foreach($role in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){Reject {param($x)$x.$endpoint.state.$role.$field++}}}
 }
 foreach($endpoint in @('before','after')){
  foreach($role in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','grantSequence','actorObject')){Reject {param($x)$x.resources.$endpoint.$role.$field++}}}
  foreach($field in @('frame','gameTicks','allocationSequence')){Reject {param($x)$x.resources.$endpoint.$field++}}
 }
 foreach($i in 0..5){foreach($field in @('token','moduleMvid','prefix','postfix','method')){Reject {param($x)$x.observerHooks[$i].$field='wrong'}}}
 foreach($i in 0..($p.events.Count-1)){
  foreach($field in @('enterOrdinal','exitOrdinal','frame','exitFrame','gameTicks','exitGameTicks','receiverObject','abilityObject')){Reject {param($x)$x.events[$i].$field++}}
  Reject {param($x)$x.events[$i].phase='restore'};Reject {param($x)$x.events[$i].complete=$false};Reject {param($x)$x.events[$i].kind='unknown'}
 }
 foreach($event in @($p.events|Where-Object kind -CEQ 'ai-create')){
  $index=[Array]::IndexOf($p.events,$event)
  foreach($field in @('contextObject','targetObject')){Reject {param($x)$x.events[$index].$field++}}
  foreach($field in @('casterId','targetId')){Reject {param($x)$x.events[$index].$field='other'}}
 }
 foreach($event in @($p.events|Where-Object kind -CEQ 'suitability')){$index=[Array]::IndexOf($p.events,$event);Reject {param($x)$x.events[$index].result=-not$x.events[$index].result};Reject {param($x)$x.events[$index].abilityGuid='wrong'}}
 foreach($key in @('constructorControl','baselineAi','aiWithStaleReference')){
  foreach($field in @('started','acted','finished')){Reject {param($x)$x.$key.$field=$true}}
  Reject {param($x)$x.$key.executorId='rider'};Reject {param($x)$x.$key.targetId='mount'};Reject {param($x)$x.$key.object++};Reject {param($x)$x.$key.type='unexpected'}
 }
 Reject {param($x)$x.constructorCallbacks=@()};Reject {param($x)$x.constructedAbilityCommands=@()};Reject {param($x)$x.events=@()}
 foreach($i in 0..($p.constructorCallbacks.Count-1)){
  foreach($field in @('ordinal','frame','gameTicks')){Reject {param($x)$x.constructorCallbacks[$i].$field++}}
  Reject {param($x)$x.constructorCallbacks[$i].phase='stale-reference'};Reject {param($x)$x.constructorCallbacks[$i].token='BAD-TOKEN'}
 }
 # Isolated reaction corruption keeps the ordinary action proof and restored endpoint intact.
 foreach($role in @('rider','mount')){foreach($field in @('reactions','reactionCooldown')){
  $x=Copy-AutoUse $p;$x.afterLease.state.$role.$field++
  Assert-KmcNativePassiveResources $x.resources
  Check $x $false ('Transient '+$role+' '+$field+' despite intact passive action proof')
 }}
}}
Write-Output ('AUTO USE PASS='+$script:checks+' FAIL=0; synthetic only, no native qualification')
