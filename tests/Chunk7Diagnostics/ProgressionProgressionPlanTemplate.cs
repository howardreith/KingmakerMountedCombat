using System;
using System.Collections.Generic;
class RuntimeSaveDescriptor { internal string GameId="campaign",GameName="name",FileName="Manual_320_KMC_C7_PROGRESSION.zks",InternalName="KMC_C7_PROGRESSION",Area="area",Sha256="source-hash"; }
class ActualPlan {
    class Entry { internal string Name,Type,Area,Hash,AdmissionArea,ForeignGameId,ForeignGameName;internal bool Writable,Writing,Bootstrap,AdmitsBeforeArea; }
    readonly object sync=new object();readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>(StringComparer.OrdinalIgnoreCase);
    string gameId="campaign",gameName="name";bool declaresBootstrap,bootstrapWindowOpen;int? maximumNativeWriteRequests;int admittedNativeWriteRequests;
/*ACTUAL_PLAN*/
    RuntimeSaveDescriptor source=new RuntimeSaveDescriptor();
    ActualPlan(bool autosave){entries.Add(source.FileName,new Entry{Name=source.InternalName,Type="Manual",Area=source.Area,Hash=source.Sha256});if(autosave){maximumNativeWriteRequests=1;entries.Add("Auto_1.zks",new Entry{Name="localized Auto",Type="Auto",Area=source.Area,Writable=true});}}
    static int passed,failed;
    static void Check(string name,bool autosave,Action<ActualPlan> change,bool expected){var p=new ActualPlan(autosave);change(p);var before=p.admittedNativeWriteRequests;bool actual=p.MatchesProgressionPlan(p.source,autosave,"localized Auto");if(actual==expected&&p.admittedNativeWriteRequests==before){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.WriteLine("FAIL "+name+" result="+actual);}}
    internal static int Main(){
        Check("exact manual source and one native autosave",true,p=>{},true);
        Check("exact read-only dismissal source",false,p=>{},true);
        Check("completed single native admission does not replay or spend",true,p=>p.admittedNativeWriteRequests=1,true);
        Check("second native admission refused",true,p=>p.admittedNativeWriteRequests=2,false);
        Check("zero-write mode with any prior write refused",false,p=>p.admittedNativeWriteRequests=1,false);
        Check("uncapped autosave plan refused",true,p=>p.maximumNativeWriteRequests=null,false);
        Check("two-write autosave plan refused",true,p=>p.maximumNativeWriteRequests=2,false);
        Check("dismissal cannot borrow a write quota",false,p=>p.maximumNativeWriteRequests=1,false);
        Check("extra write leaf refused",true,p=>p.entries.Add("extra.zks",new Entry{Writable=true}),false);
        Check("extra read leaf refused",false,p=>p.entries.Add("extra.zks",new Entry()),false);
        Check("different source hash refused",true,p=>p.entries[p.source.FileName].Hash="other",false);
        Check("writable source refused",true,p=>p.entries[p.source.FileName].Writable=true,false);
        Check("currently writing source refused",true,p=>p.entries[p.source.FileName].Writing=true,false);
        Check("native source category substitution refused",true,p=>p.entries[p.source.FileName].Type="Auto",false);
        Check("different source name refused",true,p=>p.entries[p.source.FileName].Name="other",false);
        Check("different source area refused",true,p=>p.entries[p.source.FileName].Area="other",false);
        Check("cross-area source authority refused",true,p=>p.entries[p.source.FileName].AdmissionArea="other",false);
        Check("pre-area source authority refused",true,p=>p.entries[p.source.FileName].AdmitsBeforeArea=true,false);
        Check("foreign source campaign refused",true,p=>p.entries[p.source.FileName].ForeignGameId="foreign",false);
        Check("bootstrap source authority refused",true,p=>p.entries[p.source.FileName].Bootstrap=true,false);
        Check("bootstrap declaration refused",true,p=>p.declaresBootstrap=true,false);
        Check("open bootstrap window refused",true,p=>p.bootstrapWindowOpen=true,false);
        Check("different native campaign refused",true,p=>p.gameId="foreign",false);
        Check("different native campaign name refused",true,p=>p.gameName="foreign",false);
        Check("localized autosave name substitution refused",true,p=>p.entries["Auto_1.zks"].Name="other",false);
        Check("nonwritable autosave declaration refused",true,p=>p.entries["Auto_1.zks"].Writable=false,false);
        Check("cross-area autosave declaration refused",true,p=>p.entries["Auto_1.zks"].AdmissionArea="other",false);
        Check("foreign autosave declaration refused",true,p=>p.entries["Auto_1.zks"].ForeignGameName="foreign",false);
        Check("absent source refused",true,p=>p.entries.Remove(p.source.FileName),false);
        Console.WriteLine("PROGRESSION ACTUAL READ-ONLY PLAN PASS="+passed+" FAIL="+failed);return failed==0?0:1;
    }
}
