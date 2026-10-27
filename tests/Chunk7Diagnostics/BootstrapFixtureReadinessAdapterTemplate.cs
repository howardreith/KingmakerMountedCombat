using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Blueprints;
using Newtonsoft.Json.Linq;
namespace UnityEngine { public static class Time { public static int frameCount = 20; } }
namespace Kingmaker.Blueprints { public sealed class LockedCompanionComponent {} public sealed class Blueprint { public string AssetGuid = "a".PadLeft(32, 'a'); public string AssetGuidThreadSafe { get { return AssetGuid; } } public bool Locked; public T GetComponent<T>() where T : class { return Locked ? new LockedCompanionComponent() as T : null; } } }
namespace Kingmaker.UI.SettingsUI {
 public sealed class SettingsBool { public bool Value; public int Reads; public Action BeforeRead; public bool CurrentValue { get { Reads++; BeforeRead?.Invoke(); return Value; } } }
 public sealed class SettingsSlider { public float Value = 3f; public int Reads; public Action OverrideDefaultValue; public float CurrentValue { get { Reads++; OverrideDefaultValue?.Invoke(); return Value; } } }
 public sealed class SettingsList { public SettingsBool OnlyOneSave = new SettingsBool(); public SettingsBool AutosaveEnabled = new SettingsBool { Value=true }; public SettingsSlider AutosaveSlots = new SettingsSlider(); }
 public static class SettingsRoot { public static SettingsList Instance = new SettingsList(); }
}
namespace Kingmaker.EntitySystem.Entities {
 public sealed class UnitReference { public UnitEntityData Value; public UnitReference(UnitEntityData value) { Value=value; } }
 public sealed class ClassData { public Blueprint CharacterClass = new Blueprint(); public int Level=1; public List<Blueprint> Archetypes = new List<Blueprint>(); }
 public sealed class Progression { public int CharacterLevel=1; public int Experience=0; public List<ClassData> Classes = new List<ClassData> { new ClassData() }; }
 public sealed class Descriptor { public Progression Progression = new Progression(); }
 public sealed class UnitEntityData { public string UniqueId = "44444444-1111-4111-8111-111111111111"; public Blueprint Blueprint = new Blueprint(); public Descriptor Descriptor = new Descriptor(); }
}
namespace Kingmaker {
 public sealed class Player { public int ExperienceRatePercent=100; public UnitReference MainCharacter; public List<UnitReference> PartyCharacters = new List<UnitReference>(); }
 public sealed class Game { public Player Player = new Player(); }
}
namespace Kingmaker.EntitySystem.Persistence {
 public sealed class SaveInfo {
  public enum SaveType { Auto, Manual } public enum StateType { None, Saving }
  public string FileName="Auto_1.zks", Name="Autosave", GameId="22222222-1111-4111-8111-111111111111", GameName="Native";
  public Blueprint Area = new Blueprint { AssetGuid="33333333333333333333333333333333" }; public SaveType Type=SaveType.Auto; public bool IsAutoLevelupSave=false, IsActuallySaved=true, HasFileOnDisk=true;
  public StateType OperationState=StateType.None; public DateTime SystemSaveTime = new DateTime(2026,10,12);
 }
 public sealed class SaveManager : IEnumerable<SaveInfo> {
  public string SavePath="private-isolated-root"; public List<SaveInfo> Values = new List<SaveInfo> { new SaveInfo() }; public int Enumerated, Mutations;
  public IEnumerator<SaveInfo> GetEnumerator() { foreach(var value in Values) { Enumerated++; yield return value; } } IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  public SaveInfo GetNextAutoslot() { Mutations++; return null; } public SaveInfo CreateNewSave() { Mutations++; return null; }
 }
}
namespace KingmakerMountedCombat.Integration { public static class NativePersistenceIsolation { public static bool NativeSlotRotationEnabled; } }
namespace KingmakerMountedCombat.Diagnostics {
 using KingmakerMountedCombat.Integration;
 internal sealed class LoadDescriptor { public string FileName="Auto_1.zks", InternalName="Autosave", GameId="22222222-1111-4111-8111-111111111111", GameName="Native", Area="33333333333333333333333333333333"; }
 internal sealed class Request { public LoadDescriptor PersistenceLoad = new LoadDescriptor(); }
 internal sealed class ActualCapture {
  public readonly Game game = new Game(); public readonly SaveManager manager = new SaveManager(); public readonly Request request = new Request(); public readonly JObject evidence = new JObject(); public UnitEntityData[] actors; public int WorldChecks; public bool ForeignWorld;
  public ActualCapture() { var actor = new UnitEntityData(); actors = new[] {actor}; game.Player.MainCharacter=new UnitReference(actor); game.Player.PartyCharacters.Add(new UnitReference(actor)); }
  private void RequireWorld() { WorldChecks++; if(ForeignWorld) throw new InvalidOperationException("foreign world"); }
  private static void Require(bool value,string error) { if(!value) throw new InvalidOperationException(error); }
  public JObject Capture() { return CaptureFixtureReadiness(); }
/* ACTUAL_METHOD */
 }
 internal static class Program {
  static int pass,fail;
  static ActualCapture New() { Kingmaker.UI.SettingsUI.SettingsRoot.Instance=new Kingmaker.UI.SettingsUI.SettingsList(); NativePersistenceIsolation.NativeSlotRotationEnabled=false; return new ActualCapture(); }
  static void Check(string name,Action action) { try {action();pass++;Console.WriteLine("PASS "+name);}catch(Exception e){fail++;Console.WriteLine("FAIL "+name+": "+e.Message);} }
  static void Need(bool value,string message) { if(!value)throw new Exception(message); }
  static void Refuse(string name,Action<ActualCapture> arrange,Action<ActualCapture> after=null) { Check(name,()=> {var h=New();arrange(h);bool rejected=false;try{h.Capture();}catch(InvalidOperationException){rejected=true;}Need(rejected,"accepted invalid capture");after?.Invoke(h);Need(h.manager.Mutations==0,"native save allocation called");}); }
  public static int Main() {
   Check("ordinary capture preserves native types and bounds",()=> {var h=New();var v=h.Capture();Need((string)v["captureStatus"]=="PASS"&&h.WorldChecks==2,"not bracketed by exact world checks");Need((int)v["experienceRatePercent"]==100&&(float)v["saveSettings"]["autosaveSlots"]==3f,"native scalar differs");Need((bool)v["staticPartyFacts"]["liveSelectorRequiredAuthorityAvailable"]==false,"invented live membership");Need(((JArray)v["isolatedSaveDescriptors"]["descriptors"]).Count==1&&h.manager.Mutations==0,"descriptor changed");Need(h.game.Player.MainCharacter.Value.Descriptor.Progression.Experience==0,"XP changed");});
   Check("fractional and negative Single are not rounded into capacities",()=>{foreach(var value in new[]{2.75f,-.5f}){var h=New();Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveSlots.Value=value;var v=h.Capture();Need((float)v["saveSettings"]["autosaveSlots"]==value,"slot value rounded");}});
   Check("configured disabled autosaves and single-save mode stay observations",()=>{var h=New();var s=Kingmaker.UI.SettingsUI.SettingsRoot.Instance;s.OnlyOneSave.Value=true;s.AutosaveEnabled.Value=false;var v=h.Capture();Need((bool)v["saveSettings"]["onlyOneSave"]&&!(bool)v["saveSettings"]["autosaveEnabled"],"settings projected");});
   Check("signed native Int32 XP rates preserved",()=>{foreach(var value in new[]{int.MinValue,int.MaxValue}){var h=New();h.game.Player.ExperienceRatePercent=value;Need((int)h.Capture()["experienceRatePercent"]==value,"XP rate converted");}});
   Check("exact native classes and archetypes preserved",()=>{var h=New();var p=h.game.Player.MainCharacter.Value.Descriptor.Progression;p.CharacterLevel=2;p.Classes.Add(new ClassData{CharacterClass=new Blueprint{AssetGuid=new string('b',32)}});p.Classes[1].Archetypes.Add(new Blueprint{AssetGuid=new string('c',32)});Need(((JArray)h.Capture()["mainProgression"]["classes"]).Count==2,"class lost");});
   Check("locked component remains static fact",()=>{var h=New();h.actors[0].Blueprint.Locked=true;Need((bool)h.Capture()["staticPartyFacts"]["characters"][0]["lockedCompanionComponent"],"locked fact lost");});
   Refuse("unknown slider fallback never executes",h=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveSlots.OverrideDefaultValue=()=>{throw new Exception("callback executed");},h=>Need(Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveSlots.Reads==0,"getter admitted"));
   Refuse("unexpected diagnostic projection refuses before any getters",h=>NativePersistenceIsolation.NativeSlotRotationEnabled=true,h=>{var s=Kingmaker.UI.SettingsUI.SettingsRoot.Instance;Need(s.OnlyOneSave.Reads==0&&s.AutosaveSlots.Reads==0,"getter admitted");Need((bool)h.evidence["fixtureReadiness"]["saveSettings"]["projectionEnabled"],"projection receipt missing");});
   Refuse("foreign admitted world refuses all observation",h=>h.ForeignWorld=true,h=>Need(h.manager.Enumerated==0&&Kingmaker.UI.SettingsUI.SettingsRoot.Instance.OnlyOneSave.Reads==0,"read foreign world"));
   Refuse("missing settings refuses",h=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance=null);
   Refuse("NaN native slot refuses",h=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveSlots.Value=float.NaN);
   Refuse("infinite native slot refuses",h=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveSlots.Value=float.PositiveInfinity);
   Refuse("settings identity replacement refuses",h=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance.OnlyOneSave.BeforeRead=()=>Kingmaker.UI.SettingsUI.SettingsRoot.Instance=new Kingmaker.UI.SettingsUI.SettingsList());
   Refuse("empty class list refuses",h=>h.actors[0].Descriptor.Progression.Classes.Clear());
   Refuse("null native class refuses",h=>h.actors[0].Descriptor.Progression.Classes[0]=null);
   Refuse("class sum mismatch refuses",h=>h.actors[0].Descriptor.Progression.Classes[0].Level=2);
   Refuse("duplicate native class refuses",h=>{var p=h.actors[0].Descriptor.Progression;p.CharacterLevel=2;p.Classes.Add(p.Classes[0]);});
   Refuse("21 classes refuse within bound",h=>{var p=h.actors[0].Descriptor.Progression;p.Classes=Enumerable.Range(0,21).Select(i=>new ClassData{CharacterClass=new Blueprint{AssetGuid=i.ToString("x32")}}).ToList();});
   Refuse("17 archetypes refuse within bound",h=>h.actors[0].Descriptor.Progression.Classes[0].Archetypes=Enumerable.Range(0,17).Select(i=>new Blueprint{AssetGuid=i.ToString("x32")}).ToList());
   Refuse("duplicate archetypes refuse",h=>{var a=h.actors[0].Descriptor.Progression.Classes[0].Archetypes;a.Add(new Blueprint());a.Add(new Blueprint());});
   Refuse("foreign party-character refuses",h=>h.game.Player.PartyCharacters.Add(new UnitReference(new UnitEntityData())));
   Refuse("duplicate party-character refuses",h=>h.game.Player.PartyCharacters.Add(h.game.Player.MainCharacter));
   Refuse("unresolved party-character refuses",h=>h.game.Player.PartyCharacters.Add(new UnitReference(null)));
   Refuse("empty party-character list refuses",h=>h.game.Player.PartyCharacters.Clear());
   Refuse("descriptor duplicate refuses without native allocation",h=>h.manager.Values.Add(new SaveInfo()));
   Refuse("descriptor bound stops at 33",h=>h.manager.Values=Enumerable.Range(0,40).Select(i=>new SaveInfo()).ToList(),h=>Need(h.manager.Enumerated==33,"unbounded enumeration"));
   Refuse("empty descriptor inventory refuses",h=>h.manager.Values.Clear());
   Refuse("foreign descriptor file refuses",h=>h.manager.Values[0].FileName="Auto_2.zks");
   Refuse("foreign descriptor name refuses",h=>h.manager.Values[0].Name="other");
   Refuse("foreign descriptor campaign refuses",h=>h.manager.Values[0].GameId="other");
   Refuse("foreign descriptor game-name refuses",h=>h.manager.Values[0].GameName="other");
   Refuse("foreign descriptor area refuses",h=>h.manager.Values[0].Area=new Blueprint());
   Refuse("unexpected native level-up descriptor refuses",h=>h.manager.Values[0].IsAutoLevelupSave=true);
   Refuse("wrong descriptor category refuses",h=>h.manager.Values[0].Type=SaveInfo.SaveType.Manual);
   Refuse("unfinished descriptor refuses",h=>h.manager.Values[0].OperationState=SaveInfo.StateType.Saving);
   Refuse("descriptor without disk file refuses",h=>h.manager.Values[0].HasFileOnDisk=false);
   Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);return fail==0?0:1;
  }
 }
}
