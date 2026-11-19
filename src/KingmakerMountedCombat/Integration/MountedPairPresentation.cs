using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 7 (U04): the rider's mounted badge on its party portrait, and the turn tracker's hover of a mount
    // whose own entry is folded into its pair's live paired activation. Both are projections of the current relationships,
    // computed afresh at every native call: nothing is saved, no selection, initiative, scheduler, turn or tracker entry
    // changes, and no strong reference to a view, VM, unit or pair is kept. The badge objects are children of the portrait
    // art, held here only weakly so they can be removed at disable and disposal.
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
        private static readonly FieldInfo PortraitUnit = ExactField(typeof(Kingmaker.UI.Group.GroupCharacterPortraitController), "m_Unit", 0x04002AF6);
        private static readonly FieldInfo PortraitArt = ExactField(typeof(Kingmaker.UI.Group.GroupCharacterPortraitController), "Portrait", 0x04002AEF);

        private readonly MountedWorldService world;
        private readonly Func<Sprite> badgeSprite;
        private readonly IModLogger logger;
        private readonly List<WeakReference> badges = new List<WeakReference>();
        private bool disposed;

        internal MountedPairPresentation(MountedWorldService world, Func<Sprite> badgeSprite, IModLogger logger)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.badgeSprite = badgeSprite ?? throw new ArgumentNullException(nameof(badgeSprite));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal bool Enabled { get; set; }
        internal int HoverRedirectCount { get; private set; }
        internal int TrackedBadgeCount => badges.Count;

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

        // The tracker entry index a hover of a folded mount highlights, or -1. Read only.
        internal int FoldedMountHoverEntry(object tracker, UnitEntityData hovered)
        {
            if (tracker == null || hovered == null || !TrackerType.IsInstanceOfType(tracker)) return -1;
            var units = TrackerUnits.GetValue(tracker) as IList;
            var hoveredProperty = TrackerHovered.GetValue(tracker);
            if (units == null || hoveredProperty == null) return -1;
            var nativeFound = hoveredProperty.GetType().GetProperty("Value")?.GetValue(hoveredProperty, null) != null;
            var ids = new List<string>(units.Count);
            foreach (var entry in units) ids.Add((TrackerUnitUnit.GetValue(entry) as UnitEntityData)?.UniqueId);
            return MountedPairPresentationPolicy.FoldedMountHoverEntry(hovered.UniqueId, nativeFound, Facts(), ids);
        }

        // After native's hover found no entry of the hovered unit's own: a folded mount's hover highlights its rider's
        // existing entry, through native's own hover setters. Nothing is added, removed or reordered.
        internal void RedirectFoldedMountHover(object tracker, UnitEntityView view, bool isHover)
        {
            if (!isHover || view == null || disposed || !Enabled) return;
            var index = FoldedMountHoverEntry(tracker, view.EntityData);
            if (index < 0) return;
            var entry = ((IList)TrackerUnits.GetValue(tracker))[index];
            var hoveredProperty = TrackerHovered.GetValue(tracker);
            hoveredProperty.GetType().GetProperty("Value").SetValue(hoveredProperty, entry, null);
            TrackerUnitHover.Invoke(entry, new object[] { true });
            HoverRedirectCount++;
        }

        // Every native portrait refresh: the badge is shown exactly while the portrait's unit is the rider of a live
        // mounted relationship, and hidden otherwise.
        internal void RefreshPortrait(Kingmaker.UI.Group.GroupCharacterPortraitController portrait)
        {
            if (portrait == null || disposed) return;
            var identity = Enabled ? PortraitBadge(PortraitUnit.GetValue(portrait) as UnitEntityData) : null;
            var art = PortraitArt.GetValue(portrait) as Component;
            if (art == null) return;
            var existing = art.transform.Find(BadgeName);
            if (identity == null)
            {
                if (existing != null && existing.gameObject.activeSelf) existing.gameObject.SetActive(false);
                return;
            }
            if (existing == null) existing = CreateBadge(art);
            if (existing != null && !existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
        }

        private Transform CreateBadge(Component art)
        {
            badges.RemoveAll(reference => !(reference.Target is GameObject alive) || alive == null);
            var sprite = badgeSprite();
            if (sprite == null || badges.Count >= BadgeCapacity) return null;
            var badge = new GameObject(BadgeName, typeof(RectTransform));
            var rect = (RectTransform)badge.transform;
            rect.SetParent(art.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(22f, 22f);
            rect.anchoredPosition = new Vector2(-2f, 2f);
            // The portrait art's own image type: the badge is drawn like the portrait, and never takes a pointer event, so
            // portrait clicks, hover and selection stay native.
            var image = badge.AddComponent(PortraitArt.FieldType);
            PortraitArt.FieldType.GetProperty("sprite").SetValue(image, sprite, null);
            PortraitArt.FieldType.GetProperty("raycastTarget").SetValue(image, false, null);
            badges.Add(new WeakReference(badge));
            return badge.transform;
        }

        // Disable, disposal and each world boundary: no badge object of this presentation remains.
        internal void Reset()
        {
            foreach (var reference in badges)
            {
                try { if (reference.Target is GameObject badge && badge != null) UnityEngine.Object.Destroy(badge); }
                catch (Exception exception) { logger.Exception("Mounted badge removal", exception); }
            }
            badges.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            Enabled = false;
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
}
