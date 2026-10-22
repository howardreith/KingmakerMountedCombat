using System;
using Kingmaker.Enums;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests
{
 public static class PaidBuildContractTests
 {
  private static int pass,fail;
  private static void Check(string name,Action action,bool refuses=false){try{action();if(refuses)throw new Exception("unexpected acceptance");pass++;Console.WriteLine("PASS "+name);}catch(Exception e){if(refuses&&e.Message!="unexpected acceptance"){pass++;Console.WriteLine("PASS refuses "+name);}else{fail++;Console.WriteLine("FAIL "+name+": "+e.Message);}}}
  private static bool Emitted(Chunk7ManualDerivedSelection selection)=>__ACTUAL_SAVE_ORDER_EXPRESSION__;
  private static Chunk7OrdinaryBuildPins Pins(Chunk7OrdinaryBuildMode mode,Size size)=>new Chunk7OrdinaryBuildPins("synthetic-build",new string('a',64),mode,new string('b',32),new string('c',32),"SyntheticObservedRig",Gender.Female,false,4,size);
  private static void Flag(string bodyCase,bool expected){if(Emitted(new Chunk7ManualDerivedSelection{Case=bodyCase})!=expected)throw new Exception("wrong emitted native Manual-after-field provenance");}
  public static int Run()
  {
   pass=fail=0;
   Check("existing emits native Manual after settled acquired field",()=>Flag(Chunk7ManualDerivedSelection.ExistingCase,true));
   Check("paid Small emits native Manual after field selector",()=>Flag(Chunk7ManualDerivedSelection.PaidSmallCase,true));
   Check("paid Medium emits native Manual after field selector",()=>Flag(Chunk7ManualDerivedSelection.PaidMediumCase,true));
   Check("Medium build admits exact Medium size",()=>Pins(Chunk7OrdinaryBuildMode.NewMediumCustomCompanion,Size.Medium));
   Check("Small build admits exact Small size",()=>Pins(Chunk7OrdinaryBuildMode.NewSmallCustomCompanion,Size.Small));
   Check("Medium build refuses Small size",()=>Pins(Chunk7OrdinaryBuildMode.NewMediumCustomCompanion,Size.Small),true);
   Check("Small build refuses Medium size",()=>Pins(Chunk7OrdinaryBuildMode.NewSmallCustomCompanion,Size.Medium),true);
   Check("Medium build refuses Large size",()=>Pins(Chunk7OrdinaryBuildMode.NewMediumCustomCompanion,Size.Large),true);
   Check("Small build refuses Large size",()=>Pins(Chunk7OrdinaryBuildMode.NewSmallCustomCompanion,Size.Large),true);
   Check("Horse respec preserves Small size support",()=>Pins(Chunk7OrdinaryBuildMode.NativeRespecHorse,Size.Small));
   Check("Horse respec preserves Medium size support",()=>Pins(Chunk7OrdinaryBuildMode.NativeRespecHorse,Size.Medium));
   Check("Mammoth respec preserves Medium size support",()=>Pins(Chunk7OrdinaryBuildMode.NativeRespecMammoth,Size.Medium));
   Check("respec still refuses Large body",()=>Pins(Chunk7OrdinaryBuildMode.NativeRespecHorse,Size.Large),true);
   Check("unknown native build mode refuses",()=>Pins((Chunk7OrdinaryBuildMode)999,Size.Medium),true);
   Check("both paid modes own native creation",()=>{if(!Pins(Chunk7OrdinaryBuildMode.NewSmallCustomCompanion,Size.Small).NewPaidCompanion||!Pins(Chunk7OrdinaryBuildMode.NewMediumCustomCompanion,Size.Medium).NewPaidCompanion)throw new Exception("paid mode");});
   Check("existing and respec do not own native creation",()=>{if(Pins(Chunk7OrdinaryBuildMode.ExistingRangerLevels,Size.Medium).NewPaidCompanion||Pins(Chunk7OrdinaryBuildMode.NativeRespecHorse,Size.Medium).NewPaidCompanion)throw new Exception("paid mode");});
   Check("stock Mammoth fixture admits exactly Ranger ten",()=>new Chunk7OrdinaryBuildPins("synthetic-build",new string('a',64),Chunk7OrdinaryBuildMode.ExistingRangerLevels,null,null,null,Gender.Male,false,10,Size.Medium,false,true));
   Check("stock Mammoth fixture refuses Ranger nine",()=>new Chunk7OrdinaryBuildPins("synthetic-build",new string('a',64),Chunk7OrdinaryBuildMode.ExistingRangerLevels,null,null,null,Gender.Male,false,9,Size.Medium,false,true),true);
   Check("stock Mammoth fixture refuses Small rider",()=>new Chunk7OrdinaryBuildPins("synthetic-build",new string('a',64),Chunk7OrdinaryBuildMode.ExistingRangerLevels,null,null,null,Gender.Male,false,10,Size.Small,false,true),true);
   Check("stock Mammoth fixture refuses respec substitution",()=>new Chunk7OrdinaryBuildPins("synthetic-build",new string('a',64),Chunk7OrdinaryBuildMode.NativeRespecMammoth,new string('b',32),new string('c',32),"SyntheticObservedRig",Gender.Female,false,10,Size.Medium,false,true),true);
   Check("stock Mammoth native builder admits exact XP threshold",()=>Chunk7StockMammothBuildAdmission.RequireExactXp(105000,105000,10));
   Check("stock Mammoth native builder refuses short XP",()=>Chunk7StockMammothBuildAdmission.RequireExactXp(75000,105000,9),true);
   Check("stock Mammoth native builder refuses extra ready level",()=>Chunk7StockMammothBuildAdmission.RequireExactXp(155000,105000,11),true);
   Check("stock Mammoth native builder refuses changed installed table",()=>Chunk7StockMammothBuildAdmission.RequireExactXp(105000,110000,10),true);
   Console.WriteLine("RESULT "+pass+" PASS / "+fail+" FAIL");return fail;
  }
 }
}
