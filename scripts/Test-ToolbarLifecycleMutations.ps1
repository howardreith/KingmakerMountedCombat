[CmdletBinding()]
param()
# Production mutation check of the toolbar lifecycle (docs/MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md
# section E). The production component (src/.../Domain/MountedToolbarLifecycle.cs and MountedControlPlacementIntent.cs) and
# its behavioural tests (tests/MountedControlPlacementIntentTests.cs, with tests/TestRunner.cs) are copied into a fresh temp
# directory and compiled with a generated entry point. The unmutated copy must pass every test. Each mutation then edits
# ONE production line in the temp copy only (the worktree is never written), recompiles, runs the same tests and must make
# every named test fail. Mutation classes: pre-cleanup observation skipped; a required actor omitted from a sanitation or
# observation sweep; outgoing-world lifetime mishandled (abandon, replacement, unfenced disposal); a player edit confused with
# native invalidation. Not native evidence.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$lifecycle='src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs'
$intent='src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs'
$sources=@($lifecycle,$intent,'src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs','src/KingmakerMountedCombat/Domain/NativeMountedControl.cs',
    'tests/MountedControlPlacementIntentTests.cs','tests/TestRunner.cs')
$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-toolbar-mutations-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$original=@{}
foreach($path in $sources){
    $full=Join-Path $repo $path
    if(-not(Test-Path -LiteralPath $full -PathType Leaf)){throw "Mutation input is missing: $path"}
    $original[$path]=[IO.File]::ReadAllText($full).Replace("`r`n","`n")
    Write-Output ('INPUT '+$path+' sha256='+(Get-FileHash -Algorithm SHA256 -LiteralPath $full).Hash.ToLowerInvariant())
}
$main=@'
namespace KingmakerMountedCombat.Tests
{
    internal static class ToolbarMutationMain
    {
        public static int Main()
        {
            var runner = new TestRunner();
            MountedControlPlacementIntentTests.Register(runner);
            return runner.Complete();
        }
    }
}
'@

function Invoke-Variant([string]$Label,[hashtable]$Texts){
    $dir=Join-Path $root $Label;[void][IO.Directory]::CreateDirectory($dir)
    $files=@()
    foreach($path in $sources){$target=Join-Path $dir ([IO.Path]::GetFileName($path));[IO.File]::WriteAllText($target,$Texts[$path],[Text.UTF8Encoding]::new($false));$files+=$target}
    $entry=Join-Path $dir 'ToolbarMutationMain.cs';[IO.File]::WriteAllText($entry,$main,[Text.UTF8Encoding]::new($false));$files+=$entry
    $exe=Join-Path $dir 'ToolbarMutation.exe'
    $references=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{'/reference:'+(Join-Path $framework $_)})
    $compile=@(& $compiler /noconfig /nologo /target:exe /langversion:7.3 /nostdlib+ /warnaserror+ ('/out:'+$exe) @references @files 2>&1)
    if($LASTEXITCODE-ne0){$compile|ForEach-Object{Write-Output ('  '+$_)};throw "$Label did not compile."}
    $output=@(& $exe 2>&1);$exit=$LASTEXITCODE
    $failed=@($output|ForEach-Object{[string]$_}|Where-Object{$_-cmatch'^FAIL '})
    $passed=@($output|ForEach-Object{[string]$_}|Where-Object{$_-cmatch'^PASS '})
    [pscustomobject]@{Exit=$exit;Failed=$failed;Passed=$passed.Count;Output=$output}
}

$baseline=Invoke-Variant 'baseline' $original
Write-Output ('BASELINE exit='+$baseline.Exit+' pass='+$baseline.Passed+' fail='+$baseline.Failed.Count)
$baseline.Failed|ForEach-Object{Write-Output ('  '+$_)}
$problems=New-Object 'System.Collections.Generic.List[string]'
if($baseline.Exit-ne0-or$baseline.Failed.Count-ne0-or$baseline.Passed-lt20){$problems.Add('the unmutated production copy does not pass every test')}

# Name; file; exact production text (must occur once); its replacement; tests that must fail.
$mutations=@(
    @{Name='M1a pre-cleanup observation skipped at a sweep boundary (area unload, save scope)';File=$lifecycle
      Needle="            foreach (var unit in owners) Observe(unit);`n";Replace="            // mutation: no observation before the sweep`n"
      Expect=@('D1 a move or removal right before')},
    @{Name='M1b pre-cleanup observation skipped before native invalidation is recorded';File=$lifecycle
      Needle="            Observe(unit, before);`n";Replace="            // mutation: no observation before the native emptying`n"
      Expect=@('D6 native invalidation returns the control once','AC-R22-F3')},
    @{Name='M2a reserve owners'' persistent pets omitted from the sanitation sweep';File=$lifecycle
      Needle="                if (owner != null) AddUnique(result, host.PersistentPet(owner));`n";Replace="                // mutation: pets omitted`n"
      Expect=@('AC-R26-F4')},
    @{Name='M2b reserve owners omitted from the observation (placement) sweep';File=$lifecycle
      Needle="            foreach (var unit in host.ReserveOwners() ?? new List<TUnit>()) AddUnique(result, unit);`n            return result;`n        }`n`n        // Sanitation coverage";
      Replace="            return result;`n        }`n`n        // Sanitation coverage"
      Expect=@('reserve bars of older saves','AC-R22-F3')},
    @{Name='M3a an abandoned load releases the outgoing world''s records';File=$lifecycle
      Needle="            loadOwner = null;`n            SyncWorld();`n";Replace="            loadOwner = null;`n            records = null;`n            SyncWorld();`n"
      Expect=@('D4/D5 an abandoned load','AC-R26-F1','AC-R26-F2')},
    @{Name='M3b a replaced world keeps the outgoing world''s records';File=$lifecycle
      Needle="            if (records != null && records.BelongsTo(world)) return;`n";Replace="            if (records != null) return;`n"
      Expect=@('AC-R26-F2')},
    @{Name='M3c an unfenced disposal (main menu) keeps the records';File=$lifecycle
      Needle="                if (loadOwner == null) ReleaseRecords();`n";Replace="                // mutation: nothing released without a world`n"
      Expect=@('AC-R26-F2','disable, disabled saves and world changes')},
    @{Name='M4a a player removal after an aborted native operation is read as native emptying';File=$lifecycle
      Needle="                if (!returned && !host.IsNativeEmptySlot(unit, entry.Index)) continue;`n";Replace="                // mutation: every change counts as native`n"
      Expect=@('D6 native operations that throw')},
    @{Name='M4b a native emptying is read as the player''s removal (no pending slot)';File=$lifecycle
      Needle="                arrangement.Pending.Add(index);`n                NativeInvalidationCount++;`n";Replace="                NativeInvalidationCount++;`n"
      Expect=@('D6 native invalidation returns the control once','AC-R22-F3','a save header during an interruption')},
    @{Name='M4c a player removal is kept as if native had emptied the slot';File=$intent
      Needle="            var kept = arrangement.Slots.Where(index => !arrangement.Pending.Contains(index) && present.Contains(index)).ToList();`n";
      Replace="            var kept = arrangement.Slots.ToList();`n"
      Expect=@('a deliberate removal survives','D6 native invalidation returns the control once')}
)

foreach($mutation in $mutations){
    $text=$original[$mutation.File]
    $first=$text.IndexOf($mutation.Needle,[StringComparison]::Ordinal)
    if($first-lt0-or$first-ne$text.LastIndexOf($mutation.Needle,[StringComparison]::Ordinal)){$problems.Add($mutation.Name+': production text absent or not unique');Write-Output ('MUTATION '+$mutation.Name+' NOT APPLIED (text absent or not unique)');continue}
    $texts=@{};foreach($path in $sources){$texts[$path]=$original[$path]}
    $texts[$mutation.File]=$text.Substring(0,$first)+$mutation.Replace+$text.Substring($first+$mutation.Needle.Length)
    $label='m'+([array]::IndexOf($mutations,$mutation)+1)
    $result=Invoke-Variant $label $texts
    $missing=@($mutation.Expect|Where-Object{$expected=$_;-not($result.Failed|Where-Object{$_-clike('FAIL toolbar: '+$expected+'*')})})
    $verdict=if($result.Exit-ne0-and$missing.Count-eq0){'KILLED'}else{'SURVIVED'}
    Write-Output ('MUTATION '+$mutation.Name+' -> '+$verdict+' (exit='+$result.Exit+', failing tests='+$result.Failed.Count+')')
    $result.Failed|ForEach-Object{Write-Output ('  '+($_ -replace '^(FAIL [^:]+:[^:]+):.*$','$1'))}
    if($verdict-ne'KILLED'){$problems.Add($mutation.Name+': expected failures missing: '+($missing-join'; '))}
}
# The worktree's production files are unchanged (the mutations lived in the temp copies only).
foreach($path in $sources){
    if([IO.File]::ReadAllText((Join-Path $repo $path)).Replace("`r`n","`n")-cne$original[$path]){$problems.Add('worktree source changed during the run: '+$path)}
}
Remove-Item -LiteralPath $root -Recurse -Force
if($problems.Count-ne0){$problems|ForEach-Object{Write-Output ('PROBLEM '+$_)};Write-Output 'TOOLBAR MUTATIONS FAIL';exit 1}
Write-Output ('TOOLBAR MUTATIONS PASS: baseline '+$baseline.Passed+' tests pass; '+$mutations.Count+' production mutations each killed by their named tests; worktree sources unchanged; not native evidence')
