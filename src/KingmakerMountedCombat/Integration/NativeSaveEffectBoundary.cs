using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;

namespace KingmakerMountedCombat.Integration
{
    // A projectile reaches its target in one controller and resolves effects in
    // another. A save barrier cannot infer completed effects from IsHit alone.
    internal static class NativeSaveEffectBoundary
    {
        private static readonly System.Reflection.FieldInfo Active =
            NativeCombatActorPersistence.Field(typeof(ProjectileController), "m_Projectiles",
                0x04005E2B, typeof(HashSet<Projectile>));
        private static readonly System.Reflection.FieldInfo Pending =
            NativeCombatActorPersistence.Field(typeof(ProjectileController), "m_NewProjectiles",
                0x04005E2C, typeof(List<Projectile>));
        private static readonly System.Reflection.FieldInfo Abilities =
            NativeCombatActorPersistence.Field(typeof(AbilityExecutionController), "m_Abilities",
                0x04005D50, typeof(List<AbilityExecutionProcess>));
        private static ConditionalWeakTable<Projectile, object> completed = new ConditionalWeakTable<Projectile, object>();
        private static readonly object completedMarker = new object();

        internal static void HitCompleted(Projectile projectile)
        {
            if (projectile != null) completed.GetValue(projectile, p => completedMarker);
        }
        internal static void Clear() => completed = new ConditionalWeakTable<Projectile, object>();

        internal static bool HasUnresolvedProjectiles() => HasUnresolvedProjectiles(Game.Instance?.ProjectileController);

        internal static bool HasUnresolvedProjectiles(ProjectileController controller) =>
            FirstUnresolvedProjectile(controller, out _) != null;

        // The single projectile predicate: the save barrier and its diagnostics
        // read the same first offender. pending is true when it is still in
        // m_NewProjectiles (launched, not yet ticked into the active set).
        internal static Projectile FirstUnresolvedProjectile(ProjectileController controller, out bool pending)
        {
            pending = false;
            if (controller == null) { Clear(); return null; }
            // Game-thread reads only. The public Projectiles iterator changes
            // m_Updating, even when nested inside an existing native iteration.
            var active = ((HashSet<Projectile>)Active.GetValue(controller)).FirstOrDefault(Unresolved);
            if (active != null) return active;
            var queued = ((List<Projectile>)Pending.GetValue(controller)).FirstOrDefault(Unresolved);
            pending = queued != null;
            return queued;
        }

        internal static Projectile FirstUnresolvedProjectile(out bool pending) =>
            FirstUnresolvedProjectile(Game.Instance?.ProjectileController, out pending);

        internal static Projectile[] CaptureUnresolvedProjectiles(ProjectileController controller)
        {
            if (controller == null) return new Projectile[0];
            return ((HashSet<Projectile>)Active.GetValue(controller))
                .Concat((List<Projectile>)Pending.GetValue(controller)).Distinct().Where(Unresolved).ToArray();
        }

        // Native spell delivery and spreading effects may outlive UnitUseAbility.
        // Observe their controller-owned process; never advance or reconstruct it.
        internal static bool HasUnresolvedAbilities() => HasUnresolvedAbilities(Game.Instance?.AbilityExecutor);
        internal static bool HasUnresolvedAbilities(AbilityExecutionController controller) =>
            FirstUnresolvedAbility(controller) != null;
        internal static AbilityExecutionProcess FirstUnresolvedAbility(AbilityExecutionController controller) =>
            controller == null ? null : ((List<AbilityExecutionProcess>)Abilities.GetValue(controller)).FirstOrDefault(p => !p.IsEnded);
        internal static AbilityExecutionProcess FirstUnresolvedAbility() => FirstUnresolvedAbility(Game.Instance?.AbilityExecutor);

        // The exact original executor may own a process before RuleCastSpell or
        // UnitUseAbility receives it. Read only; no tick, removal or state write.
        internal static AbilityExecutionProcess[] CaptureAbilities(AbilityExecutionController controller, AbilityExecutionContext context) =>
            ((List<AbilityExecutionProcess>)Abilities.GetValue(controller)).Where(p => ReferenceEquals(p.Context, context)).ToArray();

        internal static bool CommandNeedsSettlement(UnitCommand command) =>
            command != null && !command.IsFinished && command.GetType() != typeof(UnitMoveContiniously) &&
            // The mounted wrapper starts to own approach before UnitAttack.OnStart.
            // Match native unmounted approach: position/debt can snapshot until
            // the real attack sequence begins; never classify its effects as movement.
            (!(command is MountedPairAttackCommand mounted) || mounted.NativeSequenceStarted) &&
            (command.IsRunning || command is UnitAttackOfOpportunity || IsCommittedTouchDelivery(command));

        // Native reaction debt is charged when Run queues this command, before
        // Start. It must be allowed to deliver before a save can snapshot it.
        internal static bool MayStartDuringWait(UnitCommand command) =>
            command is UnitAttackOfOpportunity || IsCommittedTouchDelivery(command);

        // AbilityEffectStickyTouch spends the source spell, creates the actor's
        // native UnitPartTouch and queues its exact Ability.Data for delivery.
        // This is the second half of that commitment, not a new selected spell.
        internal static bool IsCommittedTouchDelivery(UnitCommand command) =>
            command is UnitUseAbility cast && !cast.IsFinished && cast.Spell != null &&
            ReferenceEquals(cast.Spell, cast.Executor?.Get<UnitPartTouch>()?.Ability?.Data);

        private static bool Unresolved(Projectile projectile) =>
            !projectile.Cleared && !completed.TryGetValue(projectile, out var marker);

        // Read-only descriptions for the deferred-save diagnostics. Field and
        // property reads only: nothing here ticks, removes, enumerates through a
        // native iterator or keeps a reference past the call.
        internal static string NameOf(UnitEntityData unit) =>
            unit == null ? "<none>" : (unit.CharacterName ?? "<unnamed>") + "(" + (unit.UniqueId ?? "?") + ")";

        // UnityEngine.Object.name is an engine internal call. Read it only for a
        // real asset and never inlined into the describers, so the describers
        // themselves stay callable (and testable) wherever the asset is absent.
        internal static string AssetName(UnityEngine.Object asset, string absent) =>
            ReferenceEquals(asset, null) ? absent : ReadAssetName(asset) ?? absent;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ReadAssetName(UnityEngine.Object asset) => asset.name;

        internal static string DescribeTarget(Kingmaker.Utility.TargetWrapper target)
        {
            if (target == null) return "<none>";
            return target.Unit != null ? NameOf(target.Unit) : "point";
        }

        internal static string SummarizeProjectile(Projectile projectile) =>
            (projectile.Launcher?.CharacterName ?? "an unknown launcher") + "'s " + AssetName(projectile.Blueprint, "unnamed") + " projectile";

        internal static string DescribeProjectile(Projectile projectile, bool pending) =>
            "blueprint=" + AssetName(projectile.Blueprint, "<none>") + "; launcher=" + NameOf(projectile.Launcher) +
            "; target=" + DescribeTarget(projectile.Target) + "; membership=" + (pending ? "pending" : "active") +
            "; isHit=" + projectile.IsHit + "; cleared=" + projectile.Cleared + "; hitObserved=" +
            completed.TryGetValue(projectile, out var marker) + "; passedTime=" + projectile.PassedTime.ToString("0.00") +
            "; timeAfterHit=" + projectile.TimeAfterHit.ToString("0.00");

        // The refusing condition without timing (see ConditionOfReadiness).
        internal static string ConditionOfProjectile(Projectile projectile, bool pending) =>
            "membership=" + (pending ? "pending" : "active") + "; isHit=" + projectile.IsHit + "; cleared=" + projectile.Cleared +
            "; hitObserved=" + completed.TryGetValue(projectile, out var marker);

        internal static string ConditionOfCommand(UnitCommand command, bool queued) =>
            "ownership=" + (queued ? "queue" : "raw") + "; started=" + command.IsStarted + "; running=" + command.IsRunning +
            "; acted=" + command.IsActed + "; finished=" + command.IsFinished + "; result=" + command.Result +
            (command is MountedPairAttackCommand mounted ? "; mountedNativeSequence=" + mounted.NativeSequenceStarted : string.Empty);

        internal static string SummarizeAbility(AbilityExecutionProcess process)
        {
            var context = process.Context;
            return AssetName(context?.AbilityBlueprint, "an unnamed ability") + " cast by " + (context?.Caster?.CharacterName ?? "an unknown caster");
        }

        internal static string DescribeAbility(AbilityExecutionProcess process)
        {
            var context = process.Context;
            return "ability=" + AssetName(context?.AbilityBlueprint, "<none>") + "; caster=" + NameOf(context?.Caster) +
                "; ended=" + process.IsEnded + "; context=" + (context == null ? "<none>" :
                    RuntimeHelpers.GetHashCode(context).ToString("x8"));
        }

        internal static string SummarizeCommand(UnitCommand command) =>
            (command.Executor?.CharacterName ?? "an unknown unit") + "'s " + command.GetType().Name;

        internal static string DescribeCommand(UnitEntityData owner, UnitCommand command, bool queued)
        {
            var mounted = command as MountedPairAttackCommand;
            return "type=" + command.GetType().FullName + "; owner=" + NameOf(owner) + "; executor=" + NameOf(command.Executor) +
                "; ownership=" + (queued ? "queue" : "raw") + "; slot=" + command.Type + "; target=" + DescribeTarget(command.Target) +
                "; started=" + command.IsStarted + "; running=" + command.IsRunning + "; acted=" + command.IsActed +
                "; finished=" + command.IsFinished + "; result=" + command.Result + "; sinceStart=" +
                command.TimeSinceStart.ToString("0.00") + "s; reaction=" + (command is UnitAttackOfOpportunity) +
                "; committedTouch=" + IsCommittedTouchDelivery(command) +
                (mounted == null ? string.Empty : "; mountedNativeSequence=" + mounted.NativeSequenceStarted);
        }
    }
}
