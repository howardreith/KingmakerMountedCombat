using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using Kingmaker.Visual.CharacterSystem;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // The native merge (Character.EnsureBones) stops mapping a body part at the first bone the avatar's hierarchy lacks, leaving
    // every later bone of that part on the merged list's first bone. The female doll lacks L_back_weapon_slot_10, so the lower half
    // of each part's list, which holds every R_ leg bone, stays rigid on the pelvis: run h1 saw the right leg hang straight down
    // while its bones splayed, with no mount in the render. Standing, that is close to the bind pose and reads as normal.
    // A seat poses the legs, so a Small rider's view gets the missing bones and rebuilds through the native path. They carry no
    // geometry and never reach a save. They stay for the life of the view, so a rider standing again keeps its correct leg.
    internal sealed class SmallRiderRigCompletion : IDisposable
    {
        private readonly Character avatar;
        private readonly IModLogger logger;

        internal SmallRiderRigCompletion(Character exactAvatar, IModLogger exactLogger)
        {
            avatar = exactAvatar ?? throw new ArgumentNullException(nameof(exactAvatar));
            logger = exactLogger;
            avatar.OnUpdated += Reconcile;
            Reconcile(avatar);
        }

        // Later gear can bring its own part list, so each native rebuild is checked. A completed rig finds nothing and ends here.
        private void Reconcile(Character updated)
        {
            try
            {
                var source = new Dictionary<string, Transform>(StringComparer.Ordinal);
                foreach (var part in avatar.EquipmentEntities.Where(entity => entity != null).SelectMany(entity => entity.BodyParts).Where(part => part != null))
                {
                    var renderer = part.SkinnedRenderer;
                    if (renderer == null) continue;
                    foreach (var bone in renderer.bones)
                        if (bone != null && !source.ContainsKey(bone.name)) source[bone.name] = bone;
                }
                var live = new Dictionary<string, Transform>(StringComparer.Ordinal);
                foreach (var node in avatar.GetComponentsInChildren<Transform>(true))
                    if (!live.ContainsKey(node.name)) live[node.name] = node;
                var missing = NativeRigMergePolicy.MissingBones(source.Keys, new HashSet<string>(live.Keys, StringComparer.Ordinal));
                if (missing.Count == 0) return;
                foreach (var name in missing)
                {
                    var original = source[name];
                    Transform parent;
                    if (original.parent == null || !live.TryGetValue(original.parent.name, out parent)) parent = avatar.transform;
                    var added = new GameObject(name).transform;
                    added.SetParent(parent, false);
                    added.localPosition = original.localPosition; added.localRotation = original.localRotation; added.localScale = original.localScale;
                    live[name] = added;
                }
                avatar.IsDirty = true;
                logger?.Info("Small rider rig completed for the native merge: " + string.Join(", ", missing) + ".");
            }
            catch (Exception exception)
            {
                // Presentation only: the seat and every mechanic keep working with the native merged mesh.
                logger?.Warning("Small rider rig completion failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }

        public void Dispose()
        {
            if (avatar != null) avatar.OnUpdated -= Reconcile;
        }
    }
}
