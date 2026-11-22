[CmdletBinding()]
param()
# Production mutation check of the toolbar lifecycle and its adapter wiring
# (docs/MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md section E; review R1-F14). Two campaigns, each
# copied into a fresh temp directory and compiled there (the worktree is never written):
# - lifecycle: the production component (src/.../Domain/MountedToolbarLifecycle.cs, MountedControlPlacementIntent.cs) with
#   its behavioural tests (tests/MountedControlPlacementIntentTests.cs, tests/TestRunner.cs) and a generated entry point;
# - wiring: the actual adapter file src/.../Integration/NativeMountedControlService.Toolbar.cs with the same component and
#   tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs (labeled Kingmaker facades and the service's main-file members),
#   driving the service's own entry points.
# Each unmutated copy must pass every check. Each mutation then edits ONE production line in the temp copy, recompiles,
# reruns and must make every named check fail. Classes: pre-cleanup observation skipped; a required actor omitted from a
# sanitation or observation sweep; outgoing-world lifetime mishandled (abandon, replacement, unfenced disposal, a missing
# player during a load); a player edit confused with native invalidation; a disconnected adapter boundary or roster input.
# Round 3 (review of 6251885) adds one mutation per repaired boundary: capture authority and preparation (M5, W6), the
# public-binding predicate (W7), per-invocation native operations and their method-specific evidence (M6), deferred
# archive selection and alias provenance (M7), retirement of an unreadable bar (M8), and the bar owner (W8, W9).
# Not native evidence.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$lifecycle='src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs'
$intent='src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs'
$adapter='src/KingmakerMountedCombat/Integration/NativeMountedControlService.Toolbar.cs'
$domain=@($lifecycle,$intent,'src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs','src/KingmakerMountedCombat/Domain/NativeMountedControl.cs')
$all=@($domain+@($adapter,'src/KingmakerMountedCombat/Domain/CleanupTrigger.cs','tests/MountedControlPlacementIntentTests.cs','tests/TestRunner.cs',
    'tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs'))
$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-toolbar-mutations-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$original=@{}
foreach($path in $all){
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
$campaigns=@(
    @{Name='lifecycle';Prefix='FAIL toolbar: ';Minimum=30;Entry=$main
      Sources=@($domain+@('tests/MountedControlPlacementIntentTests.cs','tests/TestRunner.cs'))},
    @{Name='wiring';Prefix='FAIL wiring: ';Minimum=5;Entry=$null
      Sources=@($domain+@($adapter,'src/KingmakerMountedCombat/Domain/CleanupTrigger.cs','tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs'))}
)

function Invoke-Variant([hashtable]$Campaign,[string]$Label,[hashtable]$Texts){
    $dir=Join-Path $root ($Campaign.Name+'-'+$Label);[void][IO.Directory]::CreateDirectory($dir)
    $files=@()
    foreach($path in $Campaign.Sources){$target=Join-Path $dir ([IO.Path]::GetFileName($path));[IO.File]::WriteAllText($target,$Texts[$path],[Text.UTF8Encoding]::new($false));$files+=$target}
    if($null-ne$Campaign.Entry){$entry=Join-Path $dir 'ToolbarMutationMain.cs';[IO.File]::WriteAllText($entry,$Campaign.Entry,[Text.UTF8Encoding]::new($false));$files+=$entry}
    $exe=Join-Path $dir 'ToolbarMutation.exe'
    $references=@('mscorlib.dll','System.dll','System.Core.dll'|ForEach-Object{'/reference:'+(Join-Path $framework $_)})
    $compile=@(& $compiler /noconfig /nologo /target:exe /langversion:7.3 /nostdlib+ /warnaserror+ ('/out:'+$exe) @references @files 2>&1)
    if($LASTEXITCODE-ne0){$compile|ForEach-Object{Write-Host ('  '+$_)};throw ($Campaign.Name+' '+$Label+' did not compile.')}
    $output=@(& $exe 2>&1);$exit=$LASTEXITCODE
    $failed=@($output|ForEach-Object{[string]$_}|Where-Object{$_-cmatch'^FAIL '})
    $passed=@($output|ForEach-Object{[string]$_}|Where-Object{$_-cmatch'^PASS '})
    [pscustomobject]@{Exit=$exit;Failed=$failed;Passed=$passed.Count;Output=$output}
}

# Campaign; name; file; exact production text (must occur once); its replacement; checks that must fail.
$mutations=@(
    @{Campaign='lifecycle';Name='M1a pre-cleanup observation skipped at a sweep boundary (area unload, save scope)';File=$lifecycle
      Needle="            foreach (var unit in owners) Observe(unit);`n";Replace="            // mutation: no observation before the sweep`n"
      Expect=@('D1 a move or removal right before')},
    @{Campaign='lifecycle';Name='M1b pre-cleanup observation skipped before a native write is applied';File=$lifecycle
      Needle="            Observe(unit, operation.View);`n"
      Replace="            // mutation: no observation before the native write`n"
      Expect=@('D6 native invalidation returns the control once','AC-R22-F3')},
    @{Campaign='lifecycle';Name='M2a persistent pets omitted from the sanitation sweep';File=$lifecycle
      Needle="            foreach (var owner in result.ToArray()) AddUnique(result, host.PersistentPet(owner));`n";Replace="            // mutation: pets omitted`n"
      Expect=@('AC-R26-F4','review R1-F5')},
    @{Campaign='lifecycle';Name='M2b reserve owners omitted from the observation (placement) sweep';File=$lifecycle
      Needle="            foreach (var unit in reserve) AddUnique(result, unit);`n            return result;`n        }`n`n        // Sanitation coverage"
      Replace="            return result;`n        }`n`n        // Sanitation coverage"
      Expect=@('reserve bars of older saves','AC-R22-F3')},
    @{Campaign='lifecycle';Name='M3a an abandoned load releases the outgoing world''s records';File=$lifecycle
      Needle="            loadOwner = null;`n            SyncWorld();`n";Replace="            loadOwner = null;`n            records = null;`n            SyncWorld();`n"
      Expect=@('D4/D5 an abandoned load','AC-R26-F1','AC-R26-F2')},
    @{Campaign='lifecycle';Name='M3b a replaced world keeps the outgoing world''s records';File=$lifecycle
      Needle="            if (records != null && records.BelongsTo(world)) return;`n";Replace="            if (records != null) return;`n"
      Expect=@('AC-R26-F2')},
    @{Campaign='lifecycle';Name='M3c an unfenced disposal (main menu) keeps the records';File=$lifecycle
      Needle="                if (loadOwner == null) ReleaseRecords();`n";Replace="                // mutation: nothing released without a world`n"
      Expect=@('AC-R26-F2','disable, disabled saves and world changes')},
    @{Campaign='lifecycle';Name='M3d a missing player during an open load is treated as disposal';File=$lifecycle
      Needle="            if (host.CurrentWorld == null && loadOwner != null) return;`n";Replace="            // mutation: no hold while the load is open`n"
      Expect=@('review R1-F2')},
    @{Campaign='lifecycle';Name='M3e a thrown native write is dropped at a missing player during an open load';File=$lifecycle
      Needle="            if (host.CurrentWorld == null && loadOwner != null) return;  // held through the open load's missing player`n";Replace="            // mutation: no hold for the open load`n"
      Expect=@('review R2-F4')},
    @{Campaign='lifecycle';Name='M2c a former instance in pair custody authors its actor''s arrangement';File=$lifecycle
      Needle="            if (!Authors(unit, riders, reserve))`n";Replace="            if (!Authors(unit, riders, reserve) && unit == null)`n"
      Expect=@('review R1-F10')},
    @{Campaign='lifecycle';Name='M4a a player removal after an aborted native operation is read as native emptying';File=$lifecycle
      Needle="                else if (!bound) { if (host.IsNativeEmptySlot(unit, entry.Index)) emptied.Add(entry.Index); }`n";Replace="                else if (!bound) emptied.Add(entry.Index);`n"
      Expect=@('D6 native operations that throw')},
    @{Campaign='lifecycle';Name='M4b a native emptying is read as the player''s removal (no pending slot)';File=$lifecycle
      Needle="                    else if (!arrangement.Pending) { arrangement.Pending = true; NativeInvalidationCount++; }`n"
      Replace="                    else if (!arrangement.Pending) { NativeInvalidationCount++; }`n"
      Expect=@('D6 native invalidation returns the control once','AC-R22-F3','a save header during an interruption')},
    @{Campaign='lifecycle';Name='M4c a player removal is kept as if native had emptied the slot';File=$intent
      Needle="            else if (!arrangement.Pending) arrangement.Slot = OffBar;`n";Replace="            else if (!arrangement.Pending) arrangement.Pending = true;`n"
      Expect=@('a deliberate removal survives','D6 native invalidation returns the control once')},
    @{Campaign='wiring';Name='W1 the service stops forwarding the save scope to the lifecycle';File=$adapter
      Needle="            toolbar.BeginSave();`n";Replace="            // mutation: save scope not forwarded`n"
      Expect=@('a remote owner in pair custody')},
    @{Campaign='wiring';Name='W2 the roster adapter omits the detached roster';File=$adapter
      Needle="new[] { player.RemoteCompanions, player.DetachedPartyCharacters }";Replace="new[] { player.RemoteCompanions }"
      Expect=@('a remote owner in pair custody')},
    @{Campaign='wiring';Name='W3 the native slot prefix stops forwarding to the lifecycle';File=$adapter
      Needle="            return toolbar.BeginNativeSlotOperation(owner, slotIndex);`n";Replace="            return null; // mutation: native prefix not forwarded`n"
      Expect=@('native GetSlot through the service brackets')},
    @{Campaign='wiring';Name='W4 the host returns no persistent pet';File=$adapter
      Needle="                return pet?.Descriptor != null && pet.Descriptor.IsPet && ReferenceEquals(pet.Descriptor.Master.Value, owner) ? pet : null;`n"
      Replace="                return pet?.Descriptor != null && pet.Descriptor.IsPet && pet.Descriptor.Master.Value == null ? pet : null;`n"
      Expect=@('a remote owner in pair custody')},
    @{Campaign='wiring';Name='W5 the host reports no reserve or detached owners';File=$adapter
      Needle="public IList<UnitEntityData> ReserveOwners() => ReserveRosterUnits(service.CollectCurrentCandidateUnits());"
      Replace="public IList<UnitEntityData> ReserveOwners() => new List<UnitEntityData>();"
      Expect=@('a remote owner in pair custody')},
    # Round-3 repairs (review of 6251885): each mutation disconnects one repaired boundary.
    @{Campaign='lifecycle';Name='M5a an in-world capture takes rows from a non-authoring former instance (R1-F10)';File=$lifecycle
      Needle="                    if (unit != null && !host.IsPet(unit) && Authors(unit, riders, reserve)) CaptureLiveRows(unit, rows, written);`n"
      Replace="                    if (unit != null && !host.IsPet(unit)) CaptureLiveRows(unit, rows, written);`n"
      Expect=@('review R1-F10 (round 3)')},
    @{Campaign='lifecycle';Name='M5b a capture writes rows from cached arrangements without preparing (R1-F10, R3-F1)';File=$lifecycle
      Needle="            if (prepared)`n                foreach (var unit in PlacementUnits(riders)) Observe(unit);`n";Replace="            // mutation: no capture preparation`n"
      Expect=@('review R1-F10 (round 3)')},
    @{Campaign='lifecycle';Name='M6a a postfix that opened nothing consumes the held thrown write (R2-F4)';File=$lifecycle
      Needle="            if (operation == null || !ReferenceEquals(operation, openOperation)) return;`n"
      Replace="            if (operation == null || !ReferenceEquals(operation, openOperation)) { thrownOperations.Clear(); return; }`n"
      Expect=@('review R2-F4 (round 3)')},
    @{Campaign='lifecycle';Name='M6b a thrown GetSlot is credited with a rebind it cannot write (R2-F5)';File=$lifecycle
      Needle="                else if (!operation.SingleSlot && entry.RebindData != null && ReferenceEquals(host.BoundData(unit, entry.Index), entry.RebindData))`n"
      Replace="                else if (entry.RebindData != null && ReferenceEquals(host.BoundData(unit, entry.Index), entry.RebindData))`n"
      Expect=@('review R2-F5 (round 3)')},
    @{Campaign='lifecycle';Name='M6c a thrown cleanup''s rebind is judged by the fact''s later state (R2-F5)';File=$lifecycle
      Needle="                else if (!operation.SingleSlot && entry.RebindData != null && ReferenceEquals(host.BoundData(unit, entry.Index), entry.RebindData))`n"
      Replace="                else if (!operation.SingleSlot && host.HoldsLiveBinding(unit, entry.Index))`n"
      Expect=@('review R2-F5 (round 3)')},
    @{Campaign='lifecycle';Name='M6d a thrown operation is read against the occupancy at settlement (R2-F5)';File=$lifecycle
      Needle="            return view.Occupied.Contains(index) ? MountedToolbarSlot.Ordinary : MountedToolbarSlot.Empty;`n"
      Replace="            return IsPublic(host.ReadSlot(unit, index)) ? MountedToolbarSlot.Empty : host.ReadSlot(unit, index);`n"
      Expect=@('review R2-F5 (round 3)')},
    @{Campaign='lifecycle';Name='M7a a fresh load chooses among its rows while the bar is unreadable (R2-F1)';File=$lifecycle
      Needle="                var unreadable = loadedWorld && publicRows.Count > 1 && (unit == null || host.SlotCount(unit) == 0);`n"
      Replace="                var unreadable = loadedWorld && publicRows.Count > 1 && unit == null;`n"
      Expect=@('review R2-F1 (round 3)')},
    @{Campaign='lifecycle';Name='M7b a leftover alias read before seeding overrides the archive (R2-F2)';File=$lifecycle
      Needle="                    if (loadedWorld && existing.ObservedBeforeRows && existing.ChoiceIsAlias)`n"
      Replace="                    if (loadedWorld && existing.ObservedBeforeRows && existing.ChoiceIsAlias && unit == null)`n"
      Expect=@('review R2-F2 (round 3)')},
    @{Campaign='lifecycle';Name='M8a a bar unreadable at retirement is not sanitized when it returns (R2-F3)';File=$lifecycle
      Needle="            if (records.TakeRetiredUnread(id)) ClearPublicBindings(unit, false);`n";Replace="            // mutation: no sanitation of a bar retired unread`n"
      Expect=@('review R2-F3 (round 3)')},
    @{Campaign='wiring';Name='W6 other managed rows come from every candidate, not the authoring units (R1-F10)';File=$adapter
      Needle="            CaptureOtherLiveSlots(result, legacySchema, authors);`n";Replace="            CaptureOtherLiveSlots(result, legacySchema, CollectCurrentCandidateUnits());`n"
      Expect=@('review R1-F10 a former rider object')},
    @{Campaign='wiring';Name='W7 the host''s public-binding predicate accepts any callable KMC command (R3-F1)';File=$adapter
      Needle="                return MountedControlSlotPolicy.IsPublicControlKind(kind) && service.IsCallableLiveBinding(unit, slot, kind);`n"
      Replace="                return service.IsCallableLiveBinding(unit, slot, kind);`n"
      Expect=@('review R3-F1')},
    @{Campaign='wiring';Name='W8 the bar owner is derived from a slot''s contents (R3-F2)';File=$adapter
      Needle="            var owner = settings.Owner?.Unit;`n"
      Replace="            var owner = settings.Slots.OfType<MechanicActionBarSlotAbility>().FirstOrDefault(slot => slot.Ability != null)?.Unit;`n"
      Expect=@('review R3-F2')},
    @{Campaign='wiring';Name='W9 native GetSlot''s unit argument is ignored (R3-F2)';File=$adapter
      Needle="            if (unit != null && ReferenceEquals(unit.UISettings, settings)) return unit;`n";Replace="            // mutation: unit argument ignored`n"
      Expect=@('review R3-F2')}
)

$problems=New-Object 'System.Collections.Generic.List[string]'
$baselines=@{}
foreach($campaign in $campaigns){
    $result=Invoke-Variant $campaign 'baseline' $original
    $baselines[$campaign.Name]=$result.Passed
    Write-Output ('BASELINE '+$campaign.Name+' exit='+$result.Exit+' pass='+$result.Passed+' fail='+$result.Failed.Count)
    $result.Failed|ForEach-Object{Write-Output ('  '+$_)}
    if($result.Exit-ne0-or$result.Failed.Count-ne0-or$result.Passed-lt$campaign.Minimum){$problems.Add('the unmutated '+$campaign.Name+' copy does not pass every check')}
}
$index=0
foreach($mutation in $mutations){
    $index++
    $campaign=$campaigns|Where-Object{$_.Name-ceq$mutation.Campaign}
    $text=$original[$mutation.File]
    $first=$text.IndexOf($mutation.Needle,[StringComparison]::Ordinal)
    if($first-lt0-or$first-ne$text.LastIndexOf($mutation.Needle,[StringComparison]::Ordinal)){$problems.Add($mutation.Name+': production text absent or not unique');Write-Output ('MUTATION '+$mutation.Name+' NOT APPLIED (text absent or not unique)');continue}
    $texts=@{};foreach($path in $all){$texts[$path]=$original[$path]}
    $texts[$mutation.File]=$text.Substring(0,$first)+$mutation.Replace+$text.Substring($first+$mutation.Needle.Length)
    $result=Invoke-Variant $campaign ('m'+$index) $texts
    $missing=@($mutation.Expect|Where-Object{$expected=$_;-not($result.Failed|Where-Object{$_-clike($campaign.Prefix+$expected+'*')})})
    $verdict=if($result.Exit-ne0-and$missing.Count-eq0){'KILLED'}else{'SURVIVED'}
    Write-Output ('MUTATION '+$mutation.Name+' -> '+$verdict+' (exit='+$result.Exit+', failing checks='+$result.Failed.Count+')')
    $result.Failed|ForEach-Object{Write-Output ('  '+($_ -replace '^(FAIL [^:]+:[^:]+):.*$','$1'))}
    if($verdict-ne'KILLED'){$problems.Add($mutation.Name+': expected failures missing: '+($missing-join'; '))}
}
# The worktree's production files are unchanged (the mutations lived in the temp copies only).
foreach($path in $all){
    if([IO.File]::ReadAllText((Join-Path $repo $path)).Replace("`r`n","`n")-cne$original[$path]){$problems.Add('worktree source changed during the run: '+$path)}
}
Remove-Item -LiteralPath $root -Recurse -Force
if($problems.Count-ne0){$problems|ForEach-Object{Write-Output ('PROBLEM '+$_)};Write-Output 'TOOLBAR MUTATIONS FAIL';exit 1}
Write-Output ('TOOLBAR MUTATIONS PASS: baselines lifecycle '+$baselines['lifecycle']+' and wiring '+$baselines['wiring']+' checks pass; '+$mutations.Count+
    ' production mutations each killed by their named checks; worktree sources unchanged; not native evidence')
