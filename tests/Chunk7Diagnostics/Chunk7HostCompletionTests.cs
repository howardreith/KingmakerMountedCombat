using System;using System.Collections.Generic;using KingmakerMountedCombat.Diagnostics;
static class Program {
 static int passed;
 static void Check(bool value,string name){if(!value)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
 static void Main(){
  var gate=new Chunk7HostCompletionGate();var order=new List<string>();var faults=new List<string>();bool worker=false,ownerA=false,ownerB=true,world=true;
  Func<bool> w=()=>{order.Add("worker");return worker;};Func<bool> a=()=>{order.Add("persistence");return ownerA;};Func<bool> b=()=>{order.Add("combat");return ownerB;};Func<bool> x=()=>{order.Add("world");return world;};
  bool rejected=false;try{gate.Poll(0,false,w,a,b,x,faults.Add);}catch(InvalidOperationException){rejected=true;}Check(rejected,"unrequested completion refuses");
  gate.Request(0);Check(!gate.Poll(1,true,w,a,b,x,faults.Add)&&order.Count==0,"loading preserves every owner and observer");
  Check(!gate.Poll(2,false,w,a,b,x,faults.Add)&&string.Join(",",order)=="worker","native worker blocks actor and engine cleanup");order.Clear();
  worker=true;Check(!gate.Poll(3,false,w,a,b,x,faults.Add)&&string.Join(",",order)=="worker,persistence,combat","unfinished owner does not skip independent neighbour");order.Clear();
  a=()=>{order.Add("persistence");throw new InvalidOperationException("retained target");};
  Check(!gate.Poll(4,false,w,a,b,x,faults.Add)&&string.Join(",",order)=="worker,persistence,combat"&&faults.Count==1,"throwing owner remains retained while independent cleanup runs");order.Clear();
  gate.Poll(5,false,w,a,b,x,faults.Add);Check(faults.Count==1,"repeated identical failure preserved once");order.Clear();
  gate.Request(40);Check(!gate.Poll(46,false,w,a,b,x,faults.Add)&&gate.DeadlineExceeded&&!gate.Settled&&faults.Count==2,"repeated completion request cannot restart deadline or authorize quit");
  Check(!gate.Poll(47,false,w,a,b,x,faults.Add)&&faults.Count==2,"expired custody stays closed without duplicate terminal failure");order.Clear();
  a=()=>{order.Add("persistence");return true;};world=false;Check(!gate.Poll(48,false,w,a,b,x,faults.Add)&&string.Join(",",order)=="worker,persistence,combat,world","settled engines still require all-pair cleanup");order.Clear();
  worker=false;Check(!gate.Poll(49,false,w,a,b,x,faults.Add)&&string.Join(",",order)=="worker","late worker ownership closes cleanup on every retry");order.Clear();
  worker=true;world=true;Check(gate.Poll(50,false,w,a,b,x,faults.Add)&&gate.Settled&&!gate.Pending&&gate.DeadlineExceeded,"later physical settlement retains bounded failure history");order.Clear();
  Check(gate.Poll(51,false,w,a,b,x,faults.Add)&&order.Count==0,"settled completion does not replay cleanup");
  new HostAdapter().Run();passed+=HostAdapter.CheckCount;
  Console.WriteLine("CHUNK7 HOST CUSTODY PASS="+passed+" FAIL=0; production gate and extracted host terminal adapter; no game launch");
 }
}
