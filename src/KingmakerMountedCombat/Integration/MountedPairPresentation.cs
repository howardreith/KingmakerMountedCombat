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
    // the rows list it was found in, the pair identity, the hovering unit), released exactly before every later native hover
    // pass and at every boundary that ends it. (R2): the badge follows the native per-frame portrait refresh
    // (CharacterBase.Update -> UpdateCrippled) and the portrait's bind (Initialize); every world boundary, disable and
    // disposal removes every badge object.
    internal sealed class MountedPairPresentation : IDisposable
    {
        internal const string BadgeName = "KMC_MountedBadge";
        internal const int BadgeCapacity = 32;
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

        // The one folded-mount redirect KMC owns. Weak references and value identities only.
        private sealed class HoverLease
        {
            internal WeakReference Tracker, Entry, Rows;
            internal MountedPairPresentationIdentity Pair;
            internal string Source;
        }

        private readonly MountedWorldService world;
        private readonly Func<Sprite> badgeSprite;
        private readonly IModLogger logger;
        private readonly List<WeakReference> badges = new List<WeakReference>();
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
        internal string LastHoverRelease { get; private set; }
        internal int TrackedBadgeCount => badges.Count;
        internal bool HasHoverLease => hoverLease != null;
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

        // Before native's hover pass (true or false, any unit, any tracker): the redirect KMC owns is released exactly, so
        // native's pass is authoritative. A pass nested inside KMC's own apply leaves native alone.
        internal void BeforeTrackerHover(object tracker, UnitEntityView view, bool isHover)
        {
            if (applyingHover) return;
            ReleaseHover(isHover ? "hover-change" : "hover-ended");
        }

        // After native's hover pass found no entry of the hovered unit's own: a folded mount's hover highlights its rider's
        // existing entry, through native's own hover setters, and KMC owns that redirect until it is released.
        internal void RedirectFoldedMountHover(object tracker, UnitEntityView view, bool isHover)
        {
            if (applyingHover || !isHover || view == null || disposed || !Enabled) return;
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
            var lease = new HoverLease
            {
                Tracker = new WeakReference(tracker), Entry = new WeakReference(entry), Rows = new WeakReference(rows),
                Pair = pair, Source = hovered.UniqueId
            };
            applyingHover = true;
            try
            {
                // Owned from the first write, so a failure part-way is released exactly.
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

        // A tracker rebuild, a pair cleanup or a boundary: the lease stays only while every identity it was taken on holds.
        internal void ValidateHoverLease(string reason)
        {
            var lease = hoverLease;
            if (lease == null) return;
            var tracker = lease.Tracker.Target;
            var entry = lease.Entry.Target;
            var rows = tracker == null ? null : TrackerUnits.GetValue(tracker) as IList;
            var current = false;
            if (Enabled && !disposed && entry != null && rows != null && ReferenceEquals(rows, lease.Rows.Target) && rows.Contains(entry))
                foreach (var fact in Facts())
                    if (fact.Mounted && fact.MountEntryFolded && fact.Owner == lease.Pair.Owner && fact.Generation == lease.Pair.Generation &&
                        fact.RiderId == lease.Pair.RiderId && fact.MountId == lease.Pair.MountId && fact.MountId == lease.Source &&
                        (TrackerUnitUnit.GetValue(entry) as UnitEntityData)?.UniqueId == lease.Pair.RiderId)
                        current = true;
            if (!current) ReleaseHover(reason);
        }

        // Exact release: false on the one entry KMC highlighted (only while it is still a row of its tracker; a rebuilt
        // tracker's old entry is never touched again), and HoveredUnit cleared only if it is still that entry.
        internal void ReleaseHover(string reason)
        {
            var lease = hoverLease;
            if (lease == null) return;
            hoverLease = null;
            HoverReleaseCount++;
            LastHoverRelease = reason;
            var tracker = lease.Tracker.Target;
            var entry = lease.Entry.Target;
            if (tracker == null || entry == null) return;
            var applying = applyingHover;
            applyingHover = true;
            try
            {
                if (ReferenceEquals(HoveredEntry(tracker), entry)) SetHovered(tracker, null);
                var rows = TrackerUnits.GetValue(tracker) as IList;
                if (rows != null && ReferenceEquals(rows, lease.Rows.Target) && rows.Contains(entry))
                    TrackerUnitHover.Invoke(entry, new object[] { false });
            }
            finally { applyingHover = applying; }
        }

        private void HandlePairCleanup(GameMountedRelationshipService relationship, CleanupTrigger trigger)
        {
            // Dismount, death, load, area unload, disable and teardown all arrive here, possibly before the state changes.
            try { ReleaseHover("pair-cleanup:" + trigger); }
            catch (Exception exception) { Report("Mounted hover release at pair cleanup", exception); }
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

        // Every native portrait refresh and bind: the badge is active exactly while the portrait's unit is the rider of a
        // live mounted relationship, and inactive otherwise (including a rebind to another unit or to none).
        internal void RefreshPortrait(GroupCharacterPortraitController portrait)
        {
            if (portrait == null || disposed) return;
            var surface = BadgeSurface;
            var identity = Enabled ? PortraitBadge(PortraitUnit.GetValue(portrait) as UnitEntityData) : null;
            var existing = surface.Find(portrait);
            if (identity == null)
            {
                if (existing != null && surface.IsActive(existing)) surface.SetActive(existing, false);
                return;
            }
            if (existing == null) existing = CreateBadge(portrait);
            if (existing != null && !surface.IsActive(existing)) surface.SetActive(existing, true);
        }

        private object CreateBadge(GroupCharacterPortraitController portrait)
        {
            var surface = BadgeSurface;
            badges.RemoveAll(reference => { var badge = reference.Target; return badge == null || !surface.IsAlive(badge); });
            var sprite = badgeSprite();
            if (sprite == null || badges.Count >= BadgeCapacity) return null;
            var created = surface.Create(portrait, sprite);
            if (created != null) badges.Add(new WeakReference(created));
            return created;
        }

        // Disable, disposal and every world boundary: no badge object of this presentation remains. One failed removal
        // never stops the others, a failing logger never escapes, and the bound always empties.
        internal void Reset()
        {
            try
            {
                foreach (var reference in badges.ToArray())
                {
                    try
                    {
                        var badge = reference.Target;
                        if (badge != null && BadgeSurface.IsAlive(badge)) BadgeSurface.Destroy(badge);
                    }
                    catch (Exception exception) { Report("Mounted badge removal", exception); }
                }
            }
            finally { badges.Clear(); }
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
    // engine.
    internal sealed class MountedBadgeSurface
    {
        private static readonly FieldInfo PortraitArt = typeof(GroupCharacterPortraitController).GetField("Portrait",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal Func<GroupCharacterPortraitController, object> Find;
        internal Func<GroupCharacterPortraitController, Sprite, object> Create;
        internal Func<object, bool> IsAlive;
        internal Func<object, bool> IsActive;
        internal Action<object, bool> SetActive;
        internal Action<object> Destroy;

        internal static MountedBadgeSurface Unity() => new MountedBadgeSurface
        {
            Find = portrait =>
            {
                var art = PortraitArt.GetValue(portrait) as Component;
                var child = art == null ? null : art.transform.Find(MountedPairPresentation.BadgeName);
                return child == null ? null : child.gameObject;
            },
            Create = CreateUnityBadge,
            IsAlive = badge => badge is GameObject alive && alive != null,
            IsActive = badge => ((GameObject)badge).activeSelf,
            SetActive = (badge, active) => ((GameObject)badge).SetActive(active),
            Destroy = badge => UnityEngine.Object.Destroy((GameObject)badge),
        };

        private static object CreateUnityBadge(GroupCharacterPortraitController portrait, Sprite sprite)
        {
            var art = PortraitArt.GetValue(portrait) as Component;
            if (art == null) return null;
            var badge = new GameObject(MountedPairPresentation.BadgeName, typeof(RectTransform));
            try
            {
                var rect = (RectTransform)badge.transform;
                rect.SetParent(art.transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(22f, 22f);
                rect.anchoredPosition = new Vector2(-2f, 2f);
                var image = badge.AddComponent(PortraitArt.FieldType);
                PortraitArt.FieldType.GetProperty("sprite").SetValue(image, sprite, null);
                PortraitArt.FieldType.GetProperty("raycastTarget").SetValue(image, false, null);
                return badge;
            }
            catch
            {
                // A badge that could not be completed is never left behind.
                UnityEngine.Object.Destroy(badge);
                throw;
            }
        }
    }
}
