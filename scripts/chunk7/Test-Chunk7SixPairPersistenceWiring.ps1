[CmdletBinding()]
param()
# Actual-source wiring of the six-pair persistence routing inside the shared persistence
# engine. The six-pair partial's behavior is exercised by Test-Chunk7SixPairPersistence;
# this check pins where the shared engine hands control to it. Each hook is located
# inside its exact method, in order with its neighbours, and a self-test proves that
# removing or misplacing any hook is detected. Offline source evidence only.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.cs'
$text=[IO.File]::ReadAllText($source)
function Get-Method([string]$Text,[string]$Signature){
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal)
 if($start-lt0-or$Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual method absent or ambiguous: '+$Signature)}
 $open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1
 while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw ('Unbalanced actual method: '+$Signature)}
 $Text.Substring($start,$end-$start)
}
function Get-Expression([string]$Text,[string]$Signature){
 # Expression-bodied members end at their first semicolon.
 $start=$Text.IndexOf($Signature,[StringComparison]::Ordinal)
 if($start-lt0-or$Text.IndexOf($Signature,$start+1,[StringComparison]::Ordinal)-ge0){throw ('Actual member absent or ambiguous: '+$Signature)}
 $end=$Text.IndexOf(';',$start);$Text.Substring($start,$end-$start+1)
}
function Test-Ordered([string]$Body,[string[]]$Parts){
 $at=0;foreach($part in $Parts){$next=$Body.IndexOf($part,$at,[StringComparison]::Ordinal);if($next-lt0){return $false};$at=$next+$part.Length};$true
}
function Get-Count([string]$Body,[string]$Part){$n=0;$at=0;while(($at=$Body.IndexOf($Part,$at,[StringComparison]::Ordinal))-ge0){$n++;$at+=$Part.Length};$n}
function Get-WiringFindings([string]$Text){
 $findings=[Collections.Generic.List[string]]::new()
 try{
  $update=Get-Method $Text 'internal void Update()'
  if((Get-Count $update '|| SixPairCase ? 300 : 180)')-ne1){$findings.Add('deadline')}
  if(-not(Test-Ordered $update @('if (Cold)','ObserveCold();','if (CombatCase) { BeginCombatContinuation(); Next(20); } else if (SixPairCase) BeginSixPairContinuation(); else Next(10);','if (CombatController.IsInTurnBasedCombat()) return;'))){$findings.Add('cold stage 0 continuation')}
  if(-not(Test-Ordered $update @('if (inherited != null)','inherited.RequirePreserved(); Pass("C7-owned-pairs-inherited-custody");','if (SixPairCase) BeginSixPairMounts(); else Next(1);','break;','var acquisition = new JObject();'))){$findings.Add('producer stage 0 mounts')}
  if(-not(Test-Ordered $update @('case 4:','ObserveSaved();','if (OwnedSchema1SourceCase) { BeginCleanup(); break; }','if (CombatCase) { BeginCombatContinuation(); Next(20); } else if (SixPairCase) BeginSixPairContinuation(); else Next(10);','case 300: case 301: case 310: case 311:','UpdateSixPairPersistence(); break;','case 41:'))){$findings.Add('producer stage 4 continuation and six stages')}
  if((Get-Count $update 'BeginSixPairContinuation()')-ne2-or(Get-Count $update 'BeginSixPairMounts()')-ne1-or(Get-Count $update 'UpdateSixPairPersistence()')-ne1){$findings.Add('six dispatch multiplicity')}
  $cold=Get-Method $Text 'private void ObserveCold()'
  if(-not(Test-Ordered $cold @('if (CombatCase) { ObserveCombatCold(); return; }','if (SixPairCase) { ObserveSixPairCold(); return; }','saved = persistence.LoadedArchive;'))){$findings.Add('cold observation dispatch')}
  $owners=Get-Method $Text 'private void AssertOwners()'
  if(-not(Test-Ordered $owners @('if (CombatCase) { AssertCombatOwners(); return; }','if (SixPairCase) { AssertSixPairOwners(); return; }','Require(Mounted(firstRider, firstMount)'))){$findings.Add('owner assertion dispatch')}
  $saved=Get-Method $Text 'private void ObserveSaved()'
  if(-not(Test-Ordered $saved @('saved?.Pairs.Length == (SixPairCase ? 6 : OwnedSchema1SourceCase ? 1 : 2)','AssertOwners(); AssertSlots(); AssertDebt(saved);','snapshots.Add(Capture("native-write-complete"));','if (SixPairCase) RecordSixPairSaved();','else Pass("C7-persistence-native-category-all-pair-archive");'))){$findings.Add('save readback')}
  $name=Get-Expression $Text 'internal static string NativeName(SaveInfo.SaveType type, Chunk7OwnedPairsSelection owned) =>'
  if(-not$name.Contains('type == SaveInfo.SaveType.Manual && owned?.IsSixPairSave == true ? Chunk7OwnedPairsSelection.SixPairManualName :')){$findings.Add('native Manual name')}
  $reserved=Get-Expression $Text 'internal static string NativeName(SaveInfo.SaveType type, Chunk7OwnedPairsSelection owned, string checkpoint) =>'
  if(-not$reserved.Contains('owned?.IsSixPairSave == true) ?')-or-not$reserved.Contains('owned.OwnedManualOutputName(checkpoint)')){$findings.Add('reserved Manual name')}
  if(-not(Test-Ordered $update @('if (!withinDeadline && Cold && stage == 0 && (request.OwnedPairs?.PaidFixtureLineage == true || SixPairCase))','evidence["smallColdReadinessAtDeadline"]'))){$findings.Add('cold deadline forensics')}
 }catch{$findings.Add('extraction: '+$_.Exception.Message)}
 ,$findings
}
$pass=0;$fail=0
function Check([string]$Name,[bool]$Condition){if($Condition){$script:pass++;Write-Output ('PASS '+$Name)}else{$script:fail++;Write-Output ('FAIL '+$Name)}}
$actual=Get-WiringFindings $text
Check ('actual shared engine routes every six-pair hook in place'+$(if($actual.Count){' (missing: '+($actual-join', ')+')'}else{''})) ($actual.Count-eq0)
# Self-test: each hook removed or misplaced must be detected by its own finding.
$mutations=[ordered]@{
 'deadline'=@('|| SixPairCase ? 300 : 180)',') ? 300 : 180)')
 'cold stage 0 continuation'=@('} else if (SixPairCase) BeginSixPairContinuation(); else Next(10);
                            break;','} else Next(10);
                            break;')
 'producer stage 0 mounts'=@('if (SixPairCase) BeginSixPairMounts(); else Next(1);','Next(1);')
 'producer stage 4 continuation and six stages'=@('case 300: case 301: case 310: case 311:
                        UpdateSixPairPersistence(); break;','')
 'cold observation dispatch'=@('            if (SixPairCase) { ObserveSixPairCold(); return; }
','')
 'owner assertion dispatch'=@('            if (SixPairCase) { AssertSixPairOwners(); return; }
','')
 'save readback'=@('            if (SixPairCase) RecordSixPairSaved();
','')
 'native Manual name'=@('type == SaveInfo.SaveType.Manual && owned?.IsSixPairSave == true ? Chunk7OwnedPairsSelection.SixPairManualName :
','')
 'reserved Manual name'=@(' || owned?.IsSixPairSave == true) ?',') ?')
 'cold deadline forensics'=@(' || SixPairCase))
                {
                    var game = Game.Instance;','))
                {
                    var game = Game.Instance;')
}
foreach($entry in $mutations.GetEnumerator()){
 $old=$entry.Value[0];$new=$entry.Value[1]
 $count=Get-Count $text $old
 if($count-ne1){Check ('self-test pattern for '+$entry.Key+' is exact') $false;continue}
 $findings=Get-WiringFindings ($text.Replace($old,$new))
 Check ('wiring check detects a missing '+$entry.Key) ($findings-contains$entry.Key)
}
# Misplacing the six continuation ahead of the combat branch is also detected.
$swapped=$text.Replace('if (CombatCase) { BeginCombatContinuation(); Next(20); } else if (SixPairCase) BeginSixPairContinuation(); else Next(10);','if (SixPairCase) BeginSixPairContinuation(); else if (CombatCase) { BeginCombatContinuation(); Next(20); } else Next(10);')
Check 'wiring check detects a reordered six continuation dispatch' ((Get-WiringFindings $swapped).Count-gt0)
Write-Output ('SIX-PAIR PERSISTENCE WIRING PASS='+$pass+' FAIL='+$fail+'; actual shared-engine source only, no native behavior')
if($fail-ne0){throw 'Six-pair persistence wiring regressed.'}
