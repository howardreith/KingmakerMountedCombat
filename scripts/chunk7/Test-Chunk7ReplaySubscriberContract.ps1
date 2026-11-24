[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
# The repeated-world subscriber contract is declared twice, on purpose: by the producer
# (Diagnostics/Chunk7ReplaySubscriberContract.cs) and by the evidence reader
# (scripts/runtime/Chunk7RepeatedWorldEvidence.ps1). This check compares both with what the
# compiled product actually is:
#   - each production subscriber type is subscribed to the installed game's own EventBus and read
#     back through the product's own Chunk4SubscriptionSnapshot, so the interface rows are the ones
#     the game registers, not a reading of source text;
#   - the producer's own rule and the reader's own boundary are run on that genuine inventory;
#   - Horse owners are the compiled product's profiles that mount the KMC Horse blueprint;
#   - the historical revision is compared with the qualified chunk7 source it described.
# Detached: subscriber instances are uninitialized objects, no game is launched and nothing is
# installed. Agreement here is compiled-metadata agreement, not native repeated-world evidence.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"
if(-not(Test-Path -LiteralPath $dll -PathType Leaf)){throw "Build the product first: $dll is missing."}
if((Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant()-cne'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native identity changed.'}
. (Join-Path $repo 'scripts/runtime/Chunk7RepeatedWorldEvidence.ps1')
Add-Type -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
public static class ReplaySubscriberProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static Assembly native,mod;static MethodInfo subscribe,unsubscribe,capture;static Type rowType,contractType,serialsType;
 static object Blank(Type type){return FormatterServices.GetUninitializedObject(type);}
 static object Item(object token,string key){return token.GetType().GetProperty("Item",new[]{typeof(object)}).GetValue(token,new object[]{key});}
 static string Flat(object json){return json.ToString().Replace("\r","").Replace("\n","");}
 // The product's own snapshot of the installed EventBus while the given objects are subscribed.
 static object Captured(IEnumerable<object> subscribers){
  var live=new List<object>();
  try{
   foreach(var subscriber in subscribers){subscribe.Invoke(null,new[]{subscriber});live.Add(subscriber);}
   return capture.Invoke(null,new[]{Activator.CreateInstance(serialsType,F,null,new object[]{256},null)});
  }finally{foreach(var subscriber in live)unsubscribe.Invoke(null,new[]{subscriber});}
 }
 // The producer's own rule on a captured inventory: null means the rows are exactly its revision.
 static string Violation(object captured,int horseFacts){
  var entries=((IEnumerable)Item(captured,"entries")).Cast<object>().ToArray();
  var rows=Array.CreateInstance(rowType,entries.Length);
  for(var i=0;i<entries.Length;i++)rows.SetValue(Activator.CreateInstance(rowType,F,null,new object[]{
   Item(entries[i],"interface").ToString(),Item(entries[i],"type").ToString(),long.Parse(Item(entries[i],"serial").ToString())},null),i);
  return (string)contractType.GetMethod("Violation",F).Invoke(null,new object[]{rows,horseFacts});
 }
 public static string[] Run(string managed,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   var leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));mod=Assembly.LoadFrom(dll);
  var lines=new List<string>();
  var global=native.GetType("Kingmaker.PubSubSystem.IGlobalSubscriber",true);
  var bus=native.GetType("Kingmaker.PubSubSystem.EventBus",true);
  subscribe=bus.GetMethod("Subscribe",new[]{global});unsubscribe=bus.GetMethod("Unsubscribe",new[]{global});
  capture=mod.GetType("KingmakerMountedCombat.Diagnostics.Chunk4SubscriptionSnapshot",true).GetMethod("Capture",F);
  serialsType=mod.GetType("KingmakerMountedCombat.Diagnostics.WeakObjectSerials",true);
  rowType=mod.GetType("KingmakerMountedCombat.Diagnostics.Chunk7ReplaySubscriberRow",true);
  contractType=mod.GetType("KingmakerMountedCombat.Diagnostics.Chunk7ReplaySubscriberContract",true);
  lines.Add("native.mvid|"+native.ManifestModule.ModuleVersionId);
  lines.Add("product.mvid|"+mod.ManifestModule.ModuleVersionId);
  lines.Add("product.revision|"+(string)contractType.GetField("Revision",F).GetRawConstantValue());
  var horseFact=(string)contractType.GetField("HorseFactType",F).GetRawConstantValue();
  lines.Add("product.horseFact|"+horseFact+"|"+(string)contractType.GetField("HorseFactInterface",F).GetRawConstantValue());
  var declared=((IEnumerable)contractType.GetField("ProcessOwned",F).GetValue(null)).Cast<object>().ToArray();
  var processTypes=new List<Type>();
  foreach(var owner in declared){
   var type=(string)owner.GetType().GetProperty("Key").GetValue(owner,null);
   var interfaces=(string[])owner.GetType().GetProperty("Value").GetValue(owner,null);
   lines.Add("product.owned|"+type+"|"+string.Join(",",interfaces.OrderBy(name=>name,StringComparer.Ordinal)));
   processTypes.Add(mod.GetType(type,true));
  }
  // Every concrete production type the EventBus can hold globally. Diagnostics subscribe only inside their own scenarios.
  var production=mod.GetTypes().Where(type=>type.IsClass&&!type.IsAbstract&&global.IsAssignableFrom(type)&&
   type.Namespace!="KingmakerMountedCombat.Diagnostics").OrderBy(type=>type.FullName,StringComparer.Ordinal).ToArray();
  foreach(var type in production){
   var alone=Captured(new[]{Blank(type)});
   var rows=((IEnumerable)Item(alone,"entries")).Cast<object>().Select(entry=>Item(entry,"interface").ToString()).OrderBy(name=>name,StringComparer.Ordinal);
   lines.Add("eventbus|"+type.FullName+"|"+string.Join(",",rows));
  }
  var horseType=mod.GetType(horseFact,true);
  Func<int,List<object>> settled=horses=>{var all=processTypes.Select(Blank).ToList();for(var i=0;i<horses;i++)all.Add(Blank(horseType));return all;};
  var exact=Captured(settled(1));
  lines.Add("capture.exact|"+Flat(exact));
  lines.Add("producer.exact|"+(Violation(exact,1)??"OK"));
  lines.Add("producer.twoHorses|"+(Violation(Captured(settled(2)),2)??"OK"));
  var withoutTack=settled(1);withoutTack.RemoveAll(value=>value.GetType().Name=="RidingDogTackService");
  var missing=Captured(withoutTack);
  lines.Add("capture.missing|"+Flat(missing));lines.Add("producer.missing|"+(Violation(missing,1)??"OK"));
  var twoLifecycles=settled(1);twoLifecycles.Add(Blank(processTypes.Single(type=>type.Name=="MountedLifecycleSubscriber")));
  var second=Captured(twoLifecycles);
  lines.Add("capture.second|"+Flat(second));lines.Add("producer.second|"+(Violation(second,1)??"OK"));
  var withDivineFact=settled(1);withDivineFact.Add(Blank(mod.GetType("KingmakerMountedCombat.Integration.DivineMountSpellResistance",true)));
  var foreign=Captured(withDivineFact);
  lines.Add("capture.foreign|"+Flat(foreign));lines.Add("producer.foreign|"+(Violation(foreign,1)??"OK"));
  lines.Add("eventbus.residue|"+((IEnumerable)Item(Captured(new object[0]),"entries")).Cast<object>().Count());
  // Horse owners: the compiled product's own profiles and the mount blueprint each resolves to.
  var profiles=mod.GetType("KingmakerMountedCombat.Integration.SupportedMountedProfiles",true);
  var catalog=mod.GetType("KingmakerMountedCombat.Domain.MountedRiderProfileCatalog",true);
  lines.Add("horse.blueprint|"+(string)profiles.GetField("HorseBlueprintGuid",F).GetRawConstantValue());
  var ids=new List<string>{(string)catalog.GetField("MediumMammothId",F).GetRawConstantValue(),(string)catalog.GetField("MediumHorseId",F).GetRawConstantValue()};
  foreach(var small in (IEnumerable)catalog.GetProperty("Small",F).GetValue(null,null)){
   var body=small.GetType().GetProperty("Body",F).GetValue(small,null);
   ids.Add((string)body.GetType().GetProperty("ProfileId",F).GetValue(body,null));
  }
  var resolve=profiles.GetMethod("ResolveProfileId",F);
  foreach(var id in ids){
   var profile=resolve.Invoke(null,new object[]{id});
   lines.Add("profile|"+id+"|"+(profile==null?"<unresolved>":(string)profile.GetType().GetProperty("BlueprintGuid",F).GetValue(profile,null)));
  }
  return lines.ToArray();
 }
}
'@
$passed=0;$unassessed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Refuses([string]$Name,[string]$Pattern,[scriptblock]$Body){$message=$null;try{& $Body}catch{$message=$_.Exception.Message};if($null-eq$message){throw ('Unexpected acceptance: '+$Name)};if($message-notmatch$Pattern){throw ('Refused for another reason: '+$Name+' :: '+$message)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
try{$facts=@([ReplaySubscriberProbe]::Run($managed,$dll))}catch{Write-Output $_.Exception.ToString();exit 1}
function Fact([string]$Key){@($facts|Where-Object{$_.StartsWith($Key+'|',[StringComparison]::Ordinal)}|ForEach-Object{$_.Substring($Key.Length+1)})}
function One([string]$Key){$values=@(Fact $Key);if($values.Count-ne1){throw ('Probe fact is not unique: '+$Key)};$values[0]}
function Sorted($Values){@($Values|Sort-Object -CaseSensitive)-join','}
$revision=(One 'product.revision');$contract=Get-KmcChunk7ReplaySubscriberContract $revision
$horseFact=((One 'product.horseFact')-split'\|')
$productOwned=@{};foreach($line in Fact 'product.owned'){$parts=$line-split'\|';$productOwned[$parts[0]]=$parts[1]}
$eventBus=@{};foreach($line in Fact 'eventbus'){$parts=$line-split'\|';$eventBus[$parts[0]]=$parts[1]}
Write-Host ('INFO product '+$dll+' MVID '+(One 'product.mvid')+' declares '+$revision+'; native Assembly-CSharp MVID '+(One 'native.mvid'))
# A release 0.2.N, or a private alpha of that release, 0.2.N-alpha.M (owner amendment 2): the release number N is what the
# subscriber revision's first release is compared with.
function Get-KmcReleaseNumber([string]$Version){ if($Version-cmatch'^0[.]2[.]([0-9]+)(?:-alpha[.][0-9]+)?$'){[int]$Matches[1]}else{$null} }
Check 'the release-number reading accepts 0.2.N and 0.2.N-alpha.M and refuses other shapes' {
 if((Get-KmcReleaseNumber '0.2.15')-ne15-or(Get-KmcReleaseNumber '0.2.16-alpha.1')-ne16){throw 'A release or private alpha was not read.'}
 foreach($bad in '0.2.16-beta.1','0.2.16-alpha','1.2.16-alpha.1','0.2.16-alpha.1.2','0.2.x'){ if($null-ne(Get-KmcReleaseNumber $bad)){throw ('Accepted '+$bad)} }
}
Check 'the compiled producer declares a revision the reader holds and accepts as declared evidence' {
 if($null-eq$contract.firstRelease){throw 'The producer declares the implicit historical revision.'}
 $version=(Get-Content -Raw (Join-Path $repo 'version.json')|ConvertFrom-Json).productVersion
 $release=Get-KmcReleaseNumber $version
 if($null-eq$release-or$release-lt$contract.firstRelease){throw ('Product version '+$version+' is outside the releases the reader accepts for '+$revision+'.')}
}
Check 'producer and reader declare the same process-owned subscribers and interfaces' {
 if((Sorted $productOwned.Keys)-cne(Sorted @($contract.processOwned|ForEach-Object{$_.type}))){throw 'Process-owned subscriber types differ between producer and reader.'}
 foreach($owner in $contract.processOwned){if($productOwned[$owner.type]-cne(Sorted $owner.interfaces)){throw ('Interfaces differ between producer and reader for '+$owner.type)}}
}
foreach($owner in $contract.processOwned){
 Check ('the installed EventBus registers exactly the reviewed interfaces for compiled '+($owner.type-replace'^.*\.','')+' ('+@($owner.interfaces).Count+')') {
  if(-not$eventBus.ContainsKey($owner.type)){throw 'The compiled type is not an EventBus global subscriber.'}
  if($eventBus[$owner.type]-cne(Sorted $owner.interfaces)){throw ('Compiled registration differs: '+$eventBus[$owner.type])}
 }
}
Check 'the compiled Horse fact registers exactly one ISceneHandler row, as producer and reader both expect' {
 if($horseFact[0]-cne'KingmakerMountedCombat.Integration.HorseCompanionAddPet'-or$horseFact[1]-cne'Kingmaker.PubSubSystem.ISceneHandler'-or$eventBus[$horseFact[0]]-cne$horseFact[1]){throw 'Horse fact registration differs.'}
}
# Fact-owned subscribers exist per world, not per process. DivineMountSpellResistance is the Paladin's Divine Mount
# fact; repeated-world fixtures hold no Paladin, so the contract treats it as foreign (checked below).
Check 'every compiled production EventBus subscriber type is reviewed: process-owned, the Horse fact or the Divine Mount fact' {
 $reviewed=@($contract.processOwned|ForEach-Object{$_.type})+@($horseFact[0],'KingmakerMountedCombat.Integration.DivineMountSpellResistance')
 if((Sorted $eventBus.Keys)-cne(Sorted $reviewed)){throw ('Production subscriber types changed without a contract revision: '+(Sorted $eventBus.Keys))}
}
$exact=(One 'capture.exact')|ConvertFrom-Json
$entryTotal=(@($contract.processOwned|ForEach-Object{@($_.interfaces).Count})|Measure-Object -Sum).Sum+1
Check 'the producer rule accepts the genuine settled inventory beside one and two Horse facts' {
 if((One 'producer.exact')-cne'OK'-or(One 'producer.twoHorses')-cne'OK'){throw ('Producer refused the genuine inventory: '+(One 'producer.exact')+' / '+(One 'producer.twoHorses'))}
 if($exact.uniqueOwnedSubscribers-ne(@($contract.processOwned).Count+1)-or$exact.ownedInterfaceEntries-ne$entryTotal-or@($exact.entries).Count-ne$entryTotal){throw 'Genuine totals differ from the reviewed set.'}
}
Check 'the reader boundary accepts the same genuine inventory under the declared revision' {Assert-KmcChunk7RepeatedSubscriptionBoundary $null $exact $false 1 $contract}
Refuses 'the genuine current inventory under the historical revision' 'reviewed contract kmc-global-subscribers-r1' {Assert-KmcChunk7RepeatedSubscriptionBoundary $null $exact $false 1 (Get-KmcChunk7ReplaySubscriberContract 'kmc-global-subscribers-r1')}
foreach($case in @(@('missing','a process-owned subscriber that is not subscribed','lists 0 of 2','RidingDogTackService does not list exactly'),
                   @('second','a second live lifecycle instance','lists 30 of 15','MountedLifecycleSubscriber does not list exactly'),
                   @('foreign','a live Divine Mount fact','outside the reviewed revision','a subscriber type outside the reviewed set'))){
 Check ('the producer rule refuses the genuine inventory with '+$case[1]) {$verdict=(One ('producer.'+$case[0]));if($verdict-ceq'OK'-or$verdict-notmatch[regex]::Escape($case[2])){throw ('Producer verdict: '+$verdict)}}
 Refuses ('the reader boundary on the genuine inventory with '+$case[1]) $case[3] {Assert-KmcChunk7RepeatedSubscriptionBoundary $null ((One ('capture.'+$case[0]))|ConvertFrom-Json) $false 1 $contract}
}
Check 'the detached EventBus holds no KMC subscriber after the probe' {if((One 'eventbus.residue')-cne'0'){throw 'Probe subscribers remain registered.'}}
Check 'reader Horse owner profiles are exactly the compiled profiles that mount the KMC Horse blueprint' {
 $blueprint=(One 'horse.blueprint');$resolved=@(Fact 'profile'|ForEach-Object{,($_-split'\|')})
 if(@($resolved|Where-Object{$_[1]-ceq'<unresolved>'}).Count-ne0){throw 'A declared product profile does not resolve.'}
 $horseOwners=@($resolved|Where-Object{$_[1]-ceq$blueprint}|ForEach-Object{$_[0]})
 if((Sorted $horseOwners)-cne(Sorted $contract.horseProfileIds)){throw ('Compiled Horse profiles differ from the reader: '+(Sorted $horseOwners))}
 if(@($resolved|Where-Object{$_[0]-clike'*ridingdog*'-and$_[1]-ceq$blueprint}).Count-ne0){throw 'A Riding Dog profile mounts the Horse blueprint.'}
}
# The historical revision against the qualified chunk7 source it described. A checkout without that tag cannot
# assess it; that is reported and never counted as a pass.
$tag='v0.1.0-chunk7-qualified.96';$historical=Get-KmcChunk7ReplaySubscriberContract 'kmc-global-subscribers-r1'
$git=Get-Command git -ErrorAction SilentlyContinue;$tagCommit=$null
if($null-ne$git){$tagCommit=@(& git -C $repo rev-parse --verify --quiet ($tag+'^{commit}') 2>$null)|Select-Object -First 1}
if([string]::IsNullOrWhiteSpace([string]$tagCommit)){
 $unassessed++;Write-Host ('UNASSESSED historical revision against '+$tag+': the tag is not available in this checkout.')
}else{
 function Get-TaggedSource([string]$Path){(@(& git -C $repo show ($tag+':'+$Path))-join"`n")}
 function Get-DeclaredInterfaces([string]$Source,[string]$Class){
  $match=[regex]::Match($Source,'class '+$Class+'\s*:\s*([^{]+)\{');if(-not$match.Success){throw ('Class declaration not found: '+$Class)}
  @($match.Groups[1].Value-split','|ForEach-Object{$_.Trim()}|Where-Object{$_-and$_-cne'IDisposable'}|ForEach-Object{'Kingmaker.PubSubSystem.'+$_})
 }
 Check ('historical revision lists the subscriber interfaces the qualified chunk7 source declares ('+$tag+' '+$tagCommit.Substring(0,8)+')') {
  foreach($class in @('MountedLifecycleSubscriber','NativeMountedControlService')){
   $declared=Get-DeclaredInterfaces (Get-TaggedSource ('src/KingmakerMountedCombat/Integration/'+$class+'.cs')) $class
   $owner=@($historical.processOwned|Where-Object type -CEQ ('KingmakerMountedCombat.Integration.'+$class))
   if($owner.Count-ne1-or(Sorted $declared)-cne(Sorted $owner[0].interfaces)){throw ('Historical '+$class+' differs from its qualified source: '+(Sorted $declared))}
  }
  if(@($historical.processOwned).Count-ne2){throw 'The historical revision names a subscriber the qualified source did not have.'}
 }
 Check 'historical Horse owner profiles are the ones the qualified chunk7 catalog declared' {
  $catalog=Get-TaggedSource 'src/KingmakerMountedCombat/Domain/MountedRiderProfileCatalog.cs'
  $small=@([regex]::Matches($catalog,'const string id = "([a-z0-9-]+)"')|ForEach-Object{$_.Groups[1].Value})
  $medium=[regex]::Match($catalog,'MediumHorseId = "([a-z0-9-]+)"').Groups[1].Value
  if((Sorted (@($medium)+$small))-cne(Sorted $historical.horseProfileIds)){throw ('Qualified catalog Horse profiles differ: '+(Sorted (@($medium)+$small)))}
 }
}
Write-Host ('REPLAY SUBSCRIBER CONTRACT PASS='+$passed+' FAIL=0 UNASSESSED='+$unassessed+'; compiled metadata on the detached EventBus, not native repeated-world evidence.')
