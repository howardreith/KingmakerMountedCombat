using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace UnityEngine
{
    class Transform
    {
        static int next; readonly int id=++next;
        public Transform parent; public int childCount, index;
        public GameObject gameObject=new GameObject();
        public int GetSiblingIndex()=>index;
        public int GetInstanceID()=>id;
    }
    class GameObject { public Scene scene=new Scene(); }
    class Scene { public int handle=7,rootCount=23; }
    static class Time { public static int frameCount=13; }
}
class SixAttachmentObservationTests
{
    class Log:IModLogger
    {
        public List<JObject> Rows=new List<JObject>(); public bool Throw;
        public void Info(string line){if(Throw)throw new Exception("logger");Rows.Add(JObject.Parse(line.Substring("KMC_SIX_ATTACHMENT ".Length)));}
        public void Warning(string x){} public void Error(string x){} public void Exception(string x,Exception e){}
    }
    static readonly string[] Ids={"a","b","c","d","e","f"};
    static int pass,fail;
    static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)pass++;else fail++;}
    static void Record(string phase,string id,Transform node,Transform parent,int index,Transform anchor)
        =>SixPairAttachmentObservation.Record(phase,id,node,parent,index,anchor);
    public static int Main(string[] args)
    {
        var managed=System.IO.Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,request)=>
        {
            var name=new System.Reflection.AssemblyName(request.Name).Name;
            if(name!="UnityEngine"&&!name.StartsWith("UnityEngine.",StringComparison.Ordinal))return null;
            var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(managed,name+".dll"));
            if(!path.StartsWith(managed+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return null;
            return System.IO.File.Exists(path)?System.Reflection.Assembly.LoadFrom(path):null;
        };
        var log=new Log();var parent=new Transform{childCount=12};var anchor=new Transform();var node=new Transform{parent=parent,index=11};
        Check(!SixPairAttachmentObservation.IsActive,"integration seam is inactive before native argument access");
        Record("acquire-before","a",null,null,0,null);Check(log.Rows.Count==0,"inactive observer does not inspect native objects");
        using(var scope=new SixPairAttachmentObservation(Ids,log))
        {
            Record("acquire-before","unrelated",null,null,0,null);Check(log.Rows.Count==0,"unrelated actor does not inspect native objects");
            Record("acquire-before","a",node,null,-1,anchor);
            node.parent=anchor;node.index=0;parent.childCount=11;
            Record("acquire-after","a",node,parent,11,anchor);
            Record("restore-before","a",node,parent,11,anchor);
            node.parent=parent;node.index=6;parent.childCount=7;
            Record("restore-failed","a",node,parent,11,anchor);
            var row=log.Rows.Last();
            Check(!SixPairAttachmentObservation.IsActive,"native failure immediately disables integration seam");
            Check(log.Rows.Count==4&&(int)log.Rows[0]["capturedParentChildren"]==12&&(int)log.Rows[1]["capturedParentChildren"]==11,"captures native hierarchy counts on both sides of acquire");
            Check((int)row["capturedIndex"]==11&&(int)row["currentIndex"]==6&&(int)row["capturedParentChildren"]==7,"preserves distinguishing expected/current index and parent count");
            Check((int)row["capturedParentId"]==(int)row["currentParentId"]&&(int)row["sceneHandle"]==7,"records exact parent and scene identities");
            for(int i=0;i<100;i++){Record("restore-before","a",node,parent,11,anchor);Record("restore-failed","a",node,parent,11,anchor);}
            Check(log.Rows.Count==4,"first native failure stops retry log growth");
            Check(node.parent==parent&&node.index==6&&parent.childCount==7,"observer preserves native hierarchy");
        }
        Record("acquire-before","a",null,null,0,null);Check(log.Rows.Count==4,"disposed scope is inert");
        log=new Log();using(var scope=new SixPairAttachmentObservation(Ids,log))
        {
            for(int cycle=0;cycle<3;cycle++)foreach(var id in Ids)
            {
                Record("acquire-before",id,node,parent,11,anchor);Record("acquire-after",id,node,parent,11,anchor);
                Record("restore-before",id,node,parent,11,anchor);Record("restore-after",id,node,parent,11,anchor);
                Record("restore-before",id,node,parent,11,anchor);Record("restore-after",id,node,parent,11,anchor);
            }
            Check(log.Rows.Count==72&&log.Rows.All(r=>(int)r["cycle"]>=1&&(int)r["cycle"]<=3),"six actors three cycles remain bounded and repeated restores deduplicate");
            Record("acquire-before","a",node,parent,11,anchor);Check(log.Rows.Count==72,"undeclared fourth cycle stops observation");
        }
        log=new Log{Throw=true};using(var scope=new SixPairAttachmentObservation(Ids,log))
        {Record("acquire-before","a",node,parent,11,anchor);log.Throw=false;Record("acquire-after","a",node,parent,11,anchor);Check(log.Rows.Count==0,"logger exception cannot escape or trigger retries");}
        using(var scope=new SixPairAttachmentObservation(Ids,new Log()))
        {bool rejected=false;try{new SixPairAttachmentObservation(Ids,new Log());}catch(InvalidOperationException){rejected=true;}Check(rejected,"competing scope refused");}
        bool invalid=false;try{new SixPairAttachmentObservation(new[]{"a"},new Log());}catch(InvalidOperationException){invalid=true;}Check(invalid,"non-six scope refused");
        Console.WriteLine("SIX ATTACHMENT OBSERVER PASS="+pass+" FAIL="+fail);return fail==0?0:1;
    }
}
