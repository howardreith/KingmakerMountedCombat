using System;
using System.Linq;
using KingmakerMountedCombat.Domain;
using HorseCompanionBlueprintService = RangerSelectionContractAdapter;
public sealed class BlueprintFeature { public string AssetGuid; }
public static class RangerSelectionContractAdapter
{
    private const string DogFeatureGuid = "f894e003d31461f48a02f5caec4e3359";
    private const string MammothFeatureGuid = "6adc3aab7cde56b40aa189a797254271";
    private const string HorseGuid = "7db7c50677e39f09feef56f3831fc723";
    private const string FeatureGuid = HorseGuid;
    /*GUARD*/
    /*NATIVE*/
    /*APPEND*/
    /*RESTORE*/
    /*FEATURES*/
    private static readonly string[] Historical = { "472091361cf118049a2b4339c4ea836a", DogFeatureGuid,
        "e992949eba096644784592dc7f51a5c7", "aa92fea676be33d4dafd176d699d7996", "2ee2ba60850dd064e8b98bf5c2c946ba",
        "ece6bde3dfc76ba4791376428e70621a", "67a9dc42b15d0954ca4689b13e8dedea" };
    // The exact ordered native preflight facts are retained independently of the registrar.
    private static readonly string[] Native = { "472091361cf118049a2b4339c4ea836a", "f6f1cdcc404f10c4493dc1e51208fd6f",
        "afb817d80b843cc4fa7b12289e6ebe3d", "f9ef7717531f5914a9b6ecacfad63f46", DogFeatureGuid,
        "e992949eba096644784592dc7f51a5c7", "aa92fea676be33d4dafd176d699d7996", "2ee2ba60850dd064e8b98bf5c2c946ba",
        "6adc3aab7cde56b40aa189a797254271", "ece6bde3dfc76ba4791376428e70621a", "126712ef923ab204983d6f107629c895",
        "67a9dc42b15d0954ca4689b13e8dedea" };
    private static int passed, failed;
    private static BlueprintFeature[] Values(string[] guids) { return guids.Select(id => new BlueprintFeature { AssetGuid = id }).ToArray(); }
    private static void Check(bool condition, string name) { Console.WriteLine((condition ? "PASS " : "FAIL ") + name); if(condition) passed++; else failed++; }
    private static BlueprintFeature[] Admit(BlueprintFeature[] values, out string refusal) {
        try { refusal = null; return RequireExactRangerOptions(values, "AllFeatures"); }
        catch(InvalidOperationException error) { refusal = error.Message; return null; }
    }
    private static string Refusal(BlueprintFeature[] values) { string error; Admit(values, out error); return error; }
    private static bool NativeAccepted(BlueprintFeature[] values, BlueprintFeature horse) {
        try { RequireNativeRangerOptions(values, horse); return true; }
        catch (InvalidOperationException) { return false; }
    }
    private static void VerifyShape(string name, string[] ids) {
        var values = Values(ids); string refusal;
        var admitted = Admit(values, out refusal);
        Check(ReferenceEquals(values, admitted), name + " exact original array is admitted by reference");
        var horse = new BlueprintFeature { AssetGuid = HorseGuid };
        var lease = admitted == null ? null : new ExactAppendOnlyArrayLease<BlueprintFeature>(admitted, horse);
        var appended = lease == null ? null : lease.CreateAppendedValue();
        Check(lease != null && lease.OriginalCount == ids.Length && lease.AppendedCount == ids.Length+1 &&
            appended.Take(ids.Length).SequenceEqual(values) && ReferenceEquals(appended[ids.Length], horse), name + " exact one-item append preserves every original reference");
        BlueprintFeature[] restored = null; string error = null;
        var restoreSucceeded = lease != null && lease.TryRestore(appended, out restored, out error);
        Check(restoreSucceeded && restored.SequenceEqual(values) && lease.MatchesOriginal(restored), name + " disable restores exact original references");
        var foreign = Values(ids).Concat(new[]{horse}).ToArray();
        Check(lease != null && !lease.TryRestore(foreign, out restored, out error) && restored == null && error != null, name + " equal-GUID replacement refuses destructive restoration");
        var reordered = values.Reverse().Concat(new[]{horse}).ToArray();
        Check(lease != null && !lease.TryRestore(reordered, out restored, out error) && restored == null, name + " reordered current references refuse restoration");
        var expectedAppend = values.Concat(new[]{horse}).ToArray();
        Check(HasExactHorseAppend(expectedAppend, horse), name + " audit accepts exact prefix and exact appended Horse");
        Check(HasExactStockRestore(values, horse), name + " audit accepts exact original restore");
        Check(!HasExactHorseAppend(expectedAppend, new BlueprintFeature { AssetGuid = HorseGuid }), name + " audit refuses equal-GUID foreign Horse object");
        Check(FeaturesStayedUntouched(values, values, (BlueprintFeature[])values.Clone(), horse), name + " Features reference and contents remain untouched");
        Check(!FeaturesStayedUntouched((BlueprintFeature[])values.Clone(), values, values, horse), name + " Features replacement refuses");
        Check(Refusal(expectedAppend) != null, name + " already appended surface is not readmitted as original");
        Check(Refusal(values.Concat(new[]{new BlueprintFeature { AssetGuid = new string('a',32) }}).ToArray()) != null, name + " additional unknown entry refuses");
        for(var index=0;index<ids.Length;index++) {
            var mutation=Values(ids); mutation[index]=null; Check(Refusal(mutation)!=null, name+" null entry "+index+" refuses");
            mutation=Values(ids); mutation[index]=mutation[(index+1)%ids.Length]; Check(Refusal(mutation)!=null, name+" duplicate entry "+index+" refuses");
            mutation=Values(ids); mutation[index].AssetGuid=new string('a',32); Check(Refusal(mutation)!=null, name+" foreign entry "+index+" refuses");
            Check(!HasExactHorseAppend(mutation.Concat(new[]{horse}).ToArray(),horse), name+" audit refuses changed prefix "+index);
            Check(!HasExactStockRestore(mutation,horse), name+" audit refuses changed restore "+index);
            mutation=Values(ids); mutation[index].AssetGuid=mutation[index].AssetGuid.ToUpperInvariant(); Check(Refusal(mutation)!=null, name+" changed casing "+index+" refuses");
            mutation=Values(ids); var next=(index+1)%ids.Length; var swap=mutation[index]; mutation[index]=mutation[next]; mutation[next]=swap; Check(Refusal(mutation)!=null, name+" changed order "+index+" refuses");
        }
    }
    public static int Main() {
        VerifyShape("historical7", Historical); VerifyShape("native12", Native);
        var nativeHorse = new BlueprintFeature { AssetGuid = HorseGuid };
        var nativeAppend = Values(Native).Concat(new[]{nativeHorse}).ToArray();
        Check(NativeAccepted(nativeAppend,nativeHorse),"base-only gate accepts exact native twelve plus owned Horse");
        Check(!NativeAccepted(Values(Historical).Concat(new[]{nativeHorse}).ToArray(),nativeHorse),"base-only gate refuses historical seven plus Horse before dispatch");
        Check(!NativeAccepted(nativeAppend,new BlueprintFeature{AssetGuid=HorseGuid}),"base-only gate refuses a foreign Horse object");
        Check(!NativeAccepted(Values(Native),nativeHorse),"base-only gate refuses missing append");
        Check(!NativeAccepted(nativeAppend.Concat(new[]{nativeHorse}).ToArray(),nativeHorse),"base-only gate refuses extra appended entry");
        Check(!NativeAccepted(null,nativeHorse)&&!NativeAccepted(nativeAppend,null),"base-only gate refuses absent selection or Horse");
        for(var index=0;index<Native.Length;index++){
            var changed=Values(Native).Concat(new[]{nativeHorse}).ToArray();changed[index].AssetGuid=new string('a',32);
            Check(!NativeAccepted(changed,nativeHorse),"base-only gate refuses changed native prefix "+index);
        }
        var nullMessage=Refusal(null); Check(nullMessage!=null && nullMessage.Contains("observedCount=null") && nullMessage.Contains("observedFirst32=<null>"),"null surface diagnostic is explicit");
        Check(Refusal(new BlueprintFeature[0])!=null,"empty original surface refuses");
        var many=Enumerable.Range(0,1000).Select(index=>new BlueprintFeature{AssetGuid="observed-"+index.ToString("D4")}).ToArray();
        var message=Refusal(many); Check(message!=null&&message.Contains("observedCount=1000")&&message.Contains("observed-0031")&&!message.Contains("observed-0032"),"failure retains count and bounds emitted identities at32");
        Check(message!=null&&message.Length<800,"large foreign surface produces bounded diagnostic text");
        Check(!HasExactHorseAppend(null,null),"null appended surface refuses");
        Check(!HasExactStockRestore(null,null),"null restored surface refuses");
        Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed); return failed==0?0:1;
    }
}
