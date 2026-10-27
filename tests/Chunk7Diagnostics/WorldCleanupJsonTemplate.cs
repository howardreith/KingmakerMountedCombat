using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
class CleanupJsonProbe
{
    readonly Dictionary<string,int> cleanupAttempts=new Dictionary<string,int>();
    readonly JObject evidence=new JObject(); int failures;
    void Fail(string row,string error){failures++;}
    __TRY_CLEANUP__
    class ArrayDictionaryConverter:JsonConverter
    {
        public override bool CanConvert(Type type)=>type==typeof(Dictionary<string,int>);
        public override void WriteJson(JsonWriter writer,object value,JsonSerializer serializer){writer.WriteStartArray();writer.WriteEndArray();}
        public override object ReadJson(JsonReader reader,Type type,object value,JsonSerializer serializer){throw new NotSupportedException();}
    }
    static int passed;
    static void Check(bool yes,string name){if(!yes)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
    public static int Main(string[] args)
    {
        var managed=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,request)=>{
            var name=new System.Reflection.AssemblyName(request.Name).Name;
            if(name!="UnityEngine"&&!name.StartsWith("UnityEngine.",StringComparison.Ordinal))return null;
            var path=Path.GetFullPath(Path.Combine(managed,name+".dll"));
            if(!path.StartsWith(managed+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return null;
            return File.Exists(path)?System.Reflection.Assembly.LoadFrom(path):null;
        };
        var previous=JsonConvert.DefaultSettings;
        try
        {
            JsonConvert.DefaultSettings=()=>new JsonSerializerSettings{Converters=new List<JsonConverter>{new ArrayDictionaryConverter()}};
            bool reproduced=false;
            try{JObject.FromObject(new Dictionary<string,int>{{"native",1}});}catch(ArgumentException){reproduced=true;}
            Check(reproduced,"installed Json.NET reproduces dictionary-array JObject failure");
            var p=new CleanupJsonProbe();Action fault=()=>{throw new InvalidOperationException("retained");};
            Check(!p.TryCleanup("pair",fault),"real cleanup failure returns false without diagnostic exception");
            Check(!p.TryCleanup("pair",fault)&&p.failures==1,"repeated native failure preserves one assertion and increments counter");
            var attempts=(JObject)p.evidence["cleanupFailureAttempts"];
            Check((int)attempts["pair|System.InvalidOperationException|retained"]==2,"wire counter retains exact repeated failure count");
            Check(!p.TryCleanup("observer",fault)&&p.failures==2,"independent cleanup failure remains distinct");
            Check(p.TryCleanup("pair",()=>{})&&p.failures==2,"successful retry remains success without discarding prior evidence");
            var wire=JObject.Parse(p.evidence.ToString(Formatting.None));
            Check(wire["cleanupFailureAttempts"].Type==JTokenType.Object&&((JObject)wire["cleanupFailureAttempts"]).Count==2,"explicit counter object survives actual serialization and parse under native-style converter");
        }
        finally{JsonConvert.DefaultSettings=previous;}
        Console.WriteLine("CLEANUP JSON PASS="+passed+" FAIL=0");return 0;
    }
}
