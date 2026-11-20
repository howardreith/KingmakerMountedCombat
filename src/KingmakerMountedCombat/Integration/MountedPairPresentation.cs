using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Group;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 7 (U04): the rider's mounted badge on its party portrait, and the turn tracker's hover of a mount
    // whose own entry is folded into its pair's live paired activation. Both are projections of the current relationships:
    // nothing is saved, no selection, initiative, scheduler, turn or tracker entry changes, and no strong reference to a
    // view, VM, unit or pair is kept.
    //
    // Review of 292cd23c (R1): the hover is the native world hover of a unit view (PointerController.OnHoverChanged ->
    // IDetectHover -> UnitEntityView.HandleHoverChange -> IUnitDirectHoverUIHandler -> InitiativeTrackerVM.HandleHoverChange),
    // false to the old view before true to the new one. A redirect KMC applies is one owned lease (weak tracker, weak entry,
    // the rows list it was found in, the pair identity, the hovering unit), released before every later native hover pass
    // and at every boundary that ends it. (R2): the badge follows the native per-frame portrait refresh
    // (CharacterBase.Update -> UpdateCrippled) and the portrait's bind (Initialize).
    //
    // Review of e7720f39 (R3): releasing a redirect is a cleanup obligation, not the end of ownership. Its two operations
    // (clear HoveredUnit if it is still KMC's entry; false on the exact entry KMC lit, while it is still a row of the same
    // rows list) are attempted independently, and the obligation is kept (weakly, bounded) until each has completed, its
    // target is gone or replaced, or native's own completed hover pass on that tracker has recomputed every row. (R4): a
    // badge whose removal fails stays owned as a hidden cleanup residue under the same bound, and is retried.
    //
    // Review of 89ace80f (R5): a pair's cleanup event ends the active redirect only when that relationship is exactly the
    // pair the redirect was taken for (owner, generation, rider, mount, read from the relationship as Facts() reads it). Any
    // other pair's cleanup only retries the cleanup still owed. Load, area unload, world discard, disable and disposal
    // remain global and end the active redirect whatever its pair.
    internal sealed class MountedPairPresentation : IDisposable
    {
        internal const string BadgeName = "KMC_MountedBadge";
        internal const int BadgeCapacity = 32;
        internal const int HoverCleanupCapacity = 4;
        private static readonly Type TrackerType =
            typeof(UnitEntityData).Assembly.GetType("Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM", true);
        private static readonly Type TrackerUnitType =
            typeof(UnitEntityData).Assembly.GetType("Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerUnitVM", true);
        private static readonly FieldInfo TrackerUnits = ExactField(TrackerType, "Units", 0x04003522);
        private static readonly FieldInfo TrackerHovered = ExactField(TrackerType, "HoveredUnit", 0x04003523);
        private static readonly FieldInfo TrackerUnitUnit = ExactField(TrackerUnitType, "Unit", 0x040034EC);
        private static readonly MethodInfo TrackerUnitHover = ExactMethod(TrackerUnitType, "HandleHoverChange", 0x06004ECF);
        private static readonly FieldInfo PortraitUnit = ExactField(typeof(GroupCharacterPortraitController), "m_Unit", 0x04002AF6);
        private static readonly FieldInfo PortraitArt = ExactField(typeof(GroupCharacterPortraitController), "Portrait", 0x04002AEF);

        // The one folded-mount redirect KMC owns, and after its release the cleanup it still owes. Weak references and value
        // identities only.
        private sealed class HoverLease
        {
            internal WeakReference Tracker, Entry, Rows;
            internal MountedPairPresentationIdentity Pair;
            internal string Source;
            // The cleanup operations still owed once the redirect has been released.
            internal bool HoveredOwed, RowOwed;
            internal bool Reported;
        }

        // One badge object this presentation made: in use, or a cleanup residue that is never reused as a badge.
        private sealed class BadgeRecord
        {
            internal WeakReference Badge;
            internal bool Residue, Hidden, DestroyRequested, Reported;
        }

        private readonly MountedWorldService world;
        private readonly Func<Sprite> badgeSprite;
        private readonly IModLogger logger;
        private readonly List<BadgeRecord> badges = new List<BadgeRecord>();
        private readonly List<HoverLease> hoverCleanup = new List<HoverLease>();
        private HoverLease hoverLease;
        private bool applyingHover;
        private bool disposed;

        internal MountedPairPresentation(MountedWorldService world, Func<Sprite> badgeSprite, IModLogger logger)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.badgeSprite = badgeSprite ?? throw new ArgumentNullException(nameof(badgeSprite));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            world.PairDismounting += HandlePairCleanup;
        }

        internal bool Enabled { get; set; }
        internal int HoverRedirectCount { get; private set; }
        internal int HoverReleaseCount { get; private set; }
        internal int HoverRedirectRefusedCount { get; private set; }
        internal string LastHoverRelease { get; private set; }
        // Badge objects in use and cleanup residues together, under one bound.
        internal int TrackedBadgeCount => badges.Count;
        internal int BadgeResidueCount => badges.FindAll(record => record.Residue).Count;
        internal bool HasHoverLease => hoverLease != null;
        internal int PendingHoverCleanupCount => hoverCleanup.Count;
        // The engine side of a badge object. Production draws through Unity; a detached contract substitutes it.
        internal MountedBadgeSurface BadgeSurface { get; set; } = MountedBadgeSurface.Unity();

        // The current relationships as presentation reads them: owner, generation, exact actors, and whether the mount's
        // own tracker entry is folded right now (the tracker filter's own predicate).
        internal List<MountedPairPresentationFact> Facts()
        {
            var facts = new List<MountedPairPresentationFact>();
            if (disposed || !Enabled) return facts;
            var pairs = world.Pairs;
            if (pairs == null) return facts;
            foreach (var pair in pairs)
            {
                var relationship = pair?.Relationship;
                var owner = relationship?.PairOwner;
                var rider = relationship?.Rider;
                var mount = relationship?.Mount;
                if (owner == null || rider == null || mount == null || !ReferenceEquals(owner.Rider, rider) ||
                    !ReferenceEquals(owner.Mount, mount)) continue;
                var mounted = relationship.State == RelationshipState.Mounted;
                facts.Add(new MountedPairPresentationFact(owner.Id, relationship.MountedPairGeneration, rider.UniqueId,
                    mount.UniqueId, mounted, mounted && (pair.Participant?.FoldsTrackerEntry(mount) ?? false)));
            }
            return facts;
        }

        internal MountedPairPresentationIdentity PortraitBadge(UnitEntityData unit) =>
            unit == null ? null : MountedPairPresentationPolicy.Badge(unit.UniqueId, Facts());

        // ---- Hover ----------------------------------------------------------------------------------------------------

        // The tracker entry index a hover of a folded mount highlights, or -1. Native "found" this hover only when its
        // HoveredUnit is an entry of exactly the hovered unit (native has just set it for this invocation). Read only.
        internal int FoldedMountHoverEntry(object tracker, UnitEntityData hovered)
        {
            if (tracker == null || hovered == null || !TrackerType.IsInstanceOfType(tracker)) return -1;
            var units = TrackerUnits.GetValue(tracker) as IList;
            if (units == null || TrackerHovered.GetValue(tracker) == null) return -1;
            var current = HoveredEntry(tracker);
            var nativeFound = current != null && ReferenceEquals(TrackerUnitUnit.GetValue(current), hovered);
            var ids = new List<string>(units.Count);
            foreach (var entry in units) ids.Add((TrackerUnitUnit.GetValue(entry) as UnitEntityData)?.UniqueId);
            return MountedPairPresentationPolicy.FoldedMountHoverEntry(hovered.UniqueId, nativeFound, Facts(), ids);
        }

        // Before native's hover pass (true or false, any unit, any tracker): the redirect KMC owns is released and every
        // cleanup still owed is retried, so native's pass is authoritative. Native's pass itself is never skipped. A pass
        // nested inside KMC's own apply or cleanup leaves native alone.
        internal void BeforeTrackerHover(object tracker, UnitEntityView view, bool isHover)
        {
            if (applyingHover) return;
            ReleaseHover(isHover ? "hover-change" : "hover-ended");
        }

        // After native's hover pass completed (Harmony runs no postfix after a throw): native has set HoveredUnit and called
        // every current row's own hover callback on this tracker, so any cleanup KMC still owed there is settled by native.
        // Then, for a true that native found no entry of the hovered unit's own: a folded mount's hover highlights its rider's
        // existing entry through native's own hover setters, and KMC owns that redirect until it is released.
        internal void RedirectFoldedMountHover(object tracker, UnitEntityView view, bool isHover)
        {
            if (applyingHover) return;
            SettledByNativePass(tracker);
            if (!isHover || view == null || disposed || !Enabled) return;
            var hovered = view.EntityData;
            var index = FoldedMountHoverEntry(tracker, hovered);
            if (index < 0) return;
            var rows = (IList)TrackerUnits.GetValue(tracker);
            var entry = rows[index];
            var rider = TrackerUnitUnit.GetValue(entry) as UnitEntityData;
            MountedPairPresentationIdentity pair = null;
            foreach (var fact in Facts())
                if (fact.Mounted && fact.MountEntryFolded && fact.MountId == hovered.UniqueId && fact.RiderId == rider?.UniqueId)
                    pair = new MountedPairPresentationIdentity(fact.Owner, fact.Generation, fact.RiderId, fact.MountId);
            if (pair == null) return;
            // Unfinished cleanup of earlier redirects is kept separately and bounded; a redirect that could exceed the bound is
            // not applied (native's own result stands).
            if (hoverCleanup.Count >= HoverCleanupCapacity)
            {
                HoverRedirectRefusedCount++;
                return;
            }
            var lease = new HoverLease
            {
                Tracker = new WeakReference(tracker), Entry = new WeakReference(entry), Rows = new WeakReference(rows),
                Pair = pair, Source = hovered.UniqueId
            };
            applyingHover = true;
            try
            {
                // Owned from the first write, so a failure part-way is cleaned up exactly.
                hoverLease = lease;
                SetHovered(tracker, entry);
                TrackerUnitHover.Invoke(entry, new object[] { true });
                HoverRedirectCount++;
            }
            catch
            {
                applyingHover = false;
                ReleaseHover("redirect-failed");
                throw;
            }
            finally { applyingHover = false; }
        }

        // A tracker rebuild, a pair cleanup or a boundary: the lease stays only while every identity it was taken on holds;
        // owed cleanup is retried either way.
        internal void ValidateHoverLease(string reason)
        {
            var lease = hoverLease;
            var current = false;
            if (lease != null)
            {
                var tracker = lease.Tracker.Target;
                var entry = lease.Entry.Target;
                var rows = tracker == null ? null : TrackerUnits.GetValue(tracker) as IList;
                if (Enabled && !disposed && entry != null && rows != null && ReferenceEquals(rows, lease.Rows.Target) && rows.Contains(entry))
                    foreach (var fact in Facts())
                        if (fact.Mounted && fact.MountEntryFolded && fact.Owner == lease.Pair.Owner && fact.Generation == lease.Pair.Generation &&
                            fact.RiderId == lease.Pair.RiderId && fact.MountId == lease.Pair.MountId && fact.MountId == lease.Source &&
                            (TrackerUnitUnit.GetValue(entry) as UnitEntityData)?.UniqueId == lease.Pair.RiderId)
                            current = true;
            }
            if (current) SettleHoverCleanup();
            else ReleaseHover(reason);
        }

        // Release: the live redirect (if any) becomes a cleanup obligation for exactly what KMC wrote, and every obligation
        // still owed is attempted. A failed cleanup operation never escapes.
        internal void ReleaseHover(string reason)
        {
            var lease = hoverLease;
            if (lease != null)
            {
                hoverLease = null;
                HoverReleaseCount++;
                LastHoverRelease = reason;
                lease.HoveredOwed = lease.RowOwed = true;
                hoverCleanup.Add(lease);
            }
            SettleHoverCleanup();
        }

        private void SettleHoverCleanup()
        {
            if (hoverCleanup.Count == 0) return;
            var applying = applyingHover;
            applyingHover = true;
            try
            {
                foreach (var cleanup in hoverCleanup.ToArray())
                    if (SettleHover(cleanup)) hoverCleanup.Remove(cleanup);
            }
            finally { applyingHover = applying; }
        }

        // The two owed operations, each attempted independently and each contained. A collected tracker or entry leaves
        // nothing of KMC's to touch; a replaced rows list means the old entry is no longer a row and is never touched again.
        private bool SettleHover(HoverLease cleanup)
        {
            var tracker = cleanup.Tracker.Target;
            var entry = cleanup.Entry.Target;
            if (tracker == null || entry == null) return true;
            if (cleanup.HoveredOwed)
            {
                try
                {
                    if (ReferenceEquals(HoveredEntry(tracker), entry)) SetHovered(tracker, null);
                    cleanup.HoveredOwed = false;
                }
                catch (Exception exception) { Fault(cleanup, "Mounted hover cleanup (tracker hovered entry)", exception); }
            }
            if (cleanup.RowOwed)
            {
                try
                {
                    var rows = TrackerUnits.GetValue(tracker) as IList;
                    if (rows != null && ReferenceEquals(rows, cleanup.Rows.Target) && rows.Contains(entry))
                        TrackerUnitHover.Invoke(entry, new object[] { false });
                    cleanup.RowOwed = false;
                }
                catch (Exception exception) { Fault(cleanup, "Mounted hover cleanup (rider entry)", exception); }
            }
            return !cleanup.HoveredOwed && !cleanup.RowOwed;
        }

        // Native's completed pass on this tracker set HoveredUnit and every current row itself.
        private void SettledByNativePass(object tracker)
        {
            if (hoverCleanup.Count == 0 || tracker == null) return;
            hoverCleanup.RemoveAll(cleanup =>
            {
                var owner = cleanup.Tracker.Target;
                return owner == null || ReferenceEquals(owner, tracker);
            });
        }

        private void Fault(HoverLease cleanup, string context, Exception exception)
        {
            if (cleanup.Reported) return;
            cleanup.Reported = true;
            Report(context, Unwrap(exception));
        }

        // One pair's dismount, death, load, area unload, disable or teardown cleanup, raised before its state changes. Only
        // the exact pair that owns the active redirect ends it; every pair's cleanup retries the cleanup still owed. An
        // identity that cannot be read ends nothing. Never throws.
        private void HandlePairCleanup(GameMountedRelationshipService relationship, CleanupTrigger trigger)
        {
            try
            {
                var owns = false;
                var lease = hoverLease;
                if (lease != null)
                {
                    try { owns = MountedPairPresentationPolicy.CleanupOwnsRedirect(CleanupPair(relationship), lease.Pair); }
                    catch (Exception exception) { Report("Mounted hover pair cleanup identity", exception); }
                }
                if (owns) ReleaseHover("pair-cleanup:" + trigger);
                else SettleHoverCleanup();
            }
            catch (Exception exception) { Report("Mounted hover release at pair cleanup", exception); }
        }

        // The pair identity of a relationship whose cleanup began, read as Facts() reads every pair: the owner's actors and
        // the relationship's must agree. Null when it cannot be established.
        internal static MountedPairPresentationIdentity CleanupPair(GameMountedRelationshipService relationship)
        {
            var owner = relationship?.PairOwner;
            var rider = relationship?.Rider;
            var mount = relationship?.Mount;
            if (owner == null || rider == null || mount == null || !ReferenceEquals(owner.Rider, rider) ||
                !ReferenceEquals(owner.Mount, mount)) return null;
            return new MountedPairPresentationIdentity(owner.Id, relationship.MountedPairGeneration, rider.UniqueId, mount.UniqueId);
        }

        private static object HoveredEntry(object tracker)
        {
            var property = TrackerHovered.GetValue(tracker);
            return property?.GetType().GetProperty("Value")?.GetValue(property, null);
        }

        private static void SetHovered(object tracker, object entry)
        {
            var property = TrackerHovered.GetValue(tracker);
            property?.GetType().GetProperty("Value")?.SetValue(property, entry, null);
        }

        // ---- Badge ----------------------------------------------------------------------------------------------------

        // Every native portrait refresh and bind: owed badge cleanup is retried, then the portrait's badge is active exactly
        // while its unit is the rider of a live mounted relationship, and inactive otherwise (including a rebind to another
        // unit or to none). A cleanup residue is never found as a portrait's badge.
        internal void RefreshPortrait(GroupCharacterPortraitController portrait)
        {
            if (portrait == null || disposed) return;
            SettleBadges();
            var surface = BadgeSurface;
            var identity = Enabled ? PortraitBadge(PortraitUnit.GetValue(portrait) as UnitEntityData) : null;
            var existing = surface.Find(portrait, IsUsableBadge);
            if (identity == null)
            {
                if (existing != null && surface.IsActive(existing)) surface.SetActive(existing, false);
                return;
            }
            if (existing != null && RecordOf(existing) == null && !Adopt(existing))
            {
                // A badge object this presentation does not own and cannot own within the bound is never shown.
                if (surface.IsActive(existing)) surface.SetActive(existing, false);
                return;
            }
            if (existing == null) existing = CreateBadge(portrait, surface);
            if (existing != null && !surface.IsActive(existing)) surface.SetActive(existing, true);
        }

        private bool IsUsableBadge(object badge)
        {
            var record = RecordOf(badge);
            return record == null || !record.Residue;
        }

        private BadgeRecord RecordOf(object badge)
        {
            foreach (var record in badges)
                if (ReferenceEquals(record.Badge.Target, badge)) return record;
            return null;
        }

        private bool Adopt(object badge)
        {
            if (!HasBadgeCapacity()) return false;
            badges.Add(new BadgeRecord { Badge = new WeakReference(badge) });
            return true;
        }

        private bool HasBadgeCapacity()
        {
            SettleBadges();
            return badges.Count < BadgeCapacity;
        }

        private object CreateBadge(GroupCharacterPortraitController portrait, MountedBadgeSurface surface)
        {
            var sprite = badgeSprite();
            if (sprite == null || !HasBadgeCapacity()) return null;
            var created = surface.Create(portrait);
            if (created == null) return null;
            // Owned from birth: a part-built object is never left unowned.
            var record = new BadgeRecord { Badge = new WeakReference(created) };
            badges.Add(record);
            try
            {
                surface.Build(created, portrait, sprite);
                return created;
            }
            catch
            {
                record.Residue = true;
                if (SettleBadge(record)) badges.Remove(record);
                throw;
            }
        }

        // Every residue is retried; a record whose object the engine destroyed or the collector reclaimed is pruned.
        private void SettleBadges()
        {
            if (badges.Count == 0) return;
            foreach (var record in badges.ToArray())
            {
                bool settled;
                try { settled = record.Residue ? SettleBadge(record) : !BadgeAlive(record); }
                catch (Exception exception)
                {
                    settled = false;
                    Fault(record, "Mounted badge cleanup", exception);
                }
                if (settled) badges.Remove(record);
            }
        }

        // Hide first, then destroy, each contained. The residue is settled only once the object is no longer alive.
        private bool SettleBadge(BadgeRecord record)
        {
            var surface = BadgeSurface;
            var badge = record.Badge.Target;
            if (badge == null || !BadgeAlive(record)) return true;
            try
            {
                if (surface.IsActive(badge)) surface.SetActive(badge, false);
                record.Hidden = true;
            }
            catch (Exception exception)
            {
                record.Hidden = false;
                Fault(record, "Mounted badge hiding", exception);
            }
            if (!record.DestroyRequested)
            {
                try
                {
                    surface.Destroy(badge);
                    record.DestroyRequested = true;
                }
                catch (Exception exception) { Fault(record, "Mounted badge removal", exception); }
            }
            return !BadgeAlive(record);
        }

        private bool BadgeAlive(BadgeRecord record)
        {
            var badge = record.Badge.Target;
            if (badge == null) return false;
            try { return BadgeSurface.IsAlive(badge); }
            catch (Exception exception)
            {
                Fault(record, "Mounted badge liveness", exception);
                return true;
            }
        }

        private void Fault(BadgeRecord record, string context, Exception exception)
        {
            if (record.Reported) return;
            record.Reported = true;
            Report(context, exception);
        }

        // Disable, disposal and every world boundary: every badge object of this presentation becomes a cleanup residue,
        // is hidden and destroyed; whatever the engine refused stays owned (hidden where it allowed) and is retried later.
        internal void Reset()
        {
            foreach (var record in badges) record.Residue = true;
            SettleBadges();
        }

        // Load start, area unload and world discard: the hover lease and every badge end with the world they belonged to.
        internal void ReleaseAtWorldBoundary(string reason)
        {
            try { ReleaseHover(reason); }
            catch (Exception exception) { Report("Mounted hover release at " + reason, exception); }
            Reset();
        }

        private void Report(string context, Exception exception)
        {
            try { logger.Exception(context, exception); }
            catch { }
        }

        private static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null) exception = exception.InnerException;
            return exception;
        }

        public void Dispose()
        {
            if (disposed) return;
            Enabled = false;
            try { world.PairDismounting -= HandlePairCleanup; } catch { }
            try { ReleaseHover("disposed"); }
            catch (Exception exception) { Report("Mounted hover release at disposal", exception); }
            Reset();
            disposed = true;
        }

        private static FieldInfo ExactField(Type type, string name, int token)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != token) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static MethodInfo ExactMethod(Type type, string name, int token)
        {
            var method = Array.Find(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                candidate => candidate.Name == name && candidate.MetadataToken == token);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }
    }

    // The engine operations of one badge object. Unity() draws a child image of the portrait art with the portrait art's own
    // image type, which takes no pointer events; the functions are fields only so a detached contract can stand in for the
    // engine. Create makes the bare object, which the presentation owns before Build completes it.
    internal sealed class MountedBadgeSurface
    {
        private static readonly FieldInfo PortraitArt = typeof(GroupCharacterPortraitController).GetField("Portrait",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // The portrait's badge object that the predicate accepts (a cleanup residue is never accepted), or null.
        internal Func<GroupCharacterPortraitController, Func<object, bool>, object> Find;
        internal Func<GroupCharacterPortraitController, object> Create;
        internal Action<object, GroupCharacterPortraitController, Sprite> Build;
        internal Func<object, bool> IsAlive;
        internal Func<object, bool> IsActive;
        internal Action<object, bool> SetActive;
        internal Action<object> Destroy;

        internal static MountedBadgeSurface Unity() => new MountedBadgeSurface
        {
            Find = FindUnityBadge,
            Create = CreateUnityBadge,
            Build = BuildUnityBadge,
            IsAlive = badge => badge is GameObject alive && alive != null,
            IsActive = badge => ((GameObject)badge).activeSelf,
            SetActive = (badge, active) => ((GameObject)badge).SetActive(active),
            Destroy = badge => UnityEngine.Object.Destroy((GameObject)badge),
        };

        private static object FindUnityBadge(GroupCharacterPortraitController portrait, Func<object, bool> accept)
        {
            var art = PortraitArt.GetValue(portrait) as Component;
            if (art == null) return null;
            var parent = art.transform;
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child != null && child.name == MountedPairPresentation.BadgeName && accept(child.gameObject)) return child.gameObject;
            }
            return null;
        }

        private static object CreateUnityBadge(GroupCharacterPortraitController portrait)
        {
            var art = PortraitArt.GetValue(portrait) as Component;
            return art == null ? null : new GameObject(MountedPairPresentation.BadgeName, typeof(RectTransform));
        }

        private static void BuildUnityBadge(object created, GroupCharacterPortraitController portrait, Sprite sprite)
        {
            var art = PortraitArt.GetValue(portrait) as Component;
            if (art == null) throw new InvalidOperationException("The portrait art is gone.");
            var badge = (GameObject)created;
            var rect = (RectTransform)badge.transform;
            rect.SetParent(art.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(22f, 22f);
            rect.anchoredPosition = new Vector2(-2f, 2f);
            var image = badge.AddComponent(PortraitArt.FieldType);
            PortraitArt.FieldType.GetProperty("sprite").SetValue(image, sprite, null);
            PortraitArt.FieldType.GetProperty("raycastTarget").SetValue(image, false, null);
        }
    }
}
