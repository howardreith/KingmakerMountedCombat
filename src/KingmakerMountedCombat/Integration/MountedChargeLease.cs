using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the charge mechanics of the pair-owned Mounted Charge, in one removable place.
    //
    // Every call here is a native primitive the stock AbilityCustomCharge already makes, applied to the right
    // actor of the pair: the charging flag, the doubled speed override and the straight forced path go on the
    // MOUNT's agent because the mount is the mover, while the charge buff and the charging state go on the
    // RIDER because the rider is the attacker. Nothing is written to a cooldown, a resource, a turn or a
    // preparation, and nothing is ever refunded.
    //
    // Two facts measured on previews 156 and 157 shape the lease. A forced path lives only while the mover
    // holds a live command, so the lease is applied on top of the pair's own admitted delegated ground move and
    // is maintained while that carrier lives. UnitMovementAgent.Stop() leaves force mode latched until the next
    // OnPathComplete, exactly as the stock charge does, so Restore clears what the lease set and records the
    // latch rather than pretending to clear it.
    internal sealed class MountedChargeLease
    {
        private static readonly FieldInfo ForceModeField = ResolveForceModeField();
        private static readonly FieldInfo ChargingCounterField = ResolveChargingCounterField();
        private const float ForcedApproachRadius = 1000000f;

        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly UnitEntityData target;
        private readonly IModLogger logger;
        private readonly List<string> observations = new List<string>();

        private bool chargingBefore;
        private int chargingCounterBefore;
        private float? speedOverrideBefore;
        private bool riderChargingBefore;

        // Per-mutation ownership. Restore acts on these, never on Applied: a lease whose application
        // failed part way owns exactly the mutations that completed, and must return exactly those.
        private bool chargingOwned;
        private bool chargingReleaseObserved;
        private bool speedOverrideOwned;
        private bool riderChargingOwned;
        private bool forcedPathOwned;
        private readonly MountedChargeFactOwnership<Buff> buffOwnership = new MountedChargeFactOwnership<Buff>();
        private BuffCollection appliedBuffCollection;
        private MechanicsContext buffAcquisitionContext;
        private ModifiableValue[] buffStats = new ModifiableValue[0];
        private readonly List<ModifiableValue.Modifier> buffModifiers = new List<ModifiableValue.Modifier>();
        private readonly List<GameLogicComponent> buffComponents = new List<GameLogicComponent>();
        private string[] buffComponentTypes = new string[0];
        private MountedChargeConditionObserver buffCondition;
        private MountedChargeBuffChildren buffChildren;
        private int buffOwnerThread;
        [ThreadStatic] private static MountedChargeLease acquiringBuffLease;
        private UnitMovementAgent appliedMountAgent;
        private UnitEntityView appliedMountView;
        private UnitState appliedRiderState;
        private MountedChargeApplicationTransaction application;
        private MountedChargeCleanupLedger cleanup;

        internal MountedChargeLease(
            UnitEntityData rider,
            UnitEntityData mount,
            UnitEntityData target,
            IModLogger logger)
        {
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static FieldInfo ResolveForceModeField()
        {
            var field = typeof(UnitMovementAgent).GetField("m_IsInForceMode", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != 0x040011AE || field.FieldType != typeof(bool))
            {
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, "m_IsInForceMode");
            }

            return field;
        }

        private static FieldInfo ResolveChargingCounterField()
        {
            var field = typeof(UnitMovementAgent).GetField("m_ChargingCounter", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != 0x040011B2 || field.FieldType != typeof(int))
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, "m_ChargingCounter");
            return field;
        }

        internal bool Applied { get; private set; }

        // True when an application attempt failed and every completed mutation was undone. The carrier the
        // lease rides on is the caller's to terminate exactly; this only reports that the lease itself owns
        // nothing any more.
        // Strict: no ownership left, no failures in the last attempt, postconditions observed. Never
        // merely "the undo loop ran".
        internal bool ApplyRolledBack => RollbackComplete;

        internal bool RollbackAttempted => cleanup != null && cleanup.Attempted;

        internal bool RollbackComplete => cleanup != null && cleanup.Complete && FinalCleanupPostconditions();

        internal int CleanupAttemptCount => cleanup == null ? 0 : cleanup.AttemptCount;

        // The cleanup debt: mutations still owned because their native undo has not yet succeeded.
        internal string UnresolvedCleanup => cleanup == null ? string.Empty :
            string.Join("|", new List<string>(cleanup.Unresolved).ToArray()) +
            (FinalCleanupPostconditions() ? string.Empty : "|native-postconditions");

        internal string CleanupFailures => cleanup == null
            ? string.Empty
            : string.Join("|", new List<string>(cleanup.Failures).ToArray());

        internal string CleanupDescription => cleanup == null ? "<none>" : cleanup.Describe();

        // True while any mutation is still owned. The command and the controller treat this as debt.
        internal bool HasCleanupDebt => chargingOwned || speedOverrideOwned || riderChargingOwned ||
            forcedPathOwned || BuffOutstanding || cleanup != null && !FinalCleanupPostconditions();

        internal string ApplyFailureReason { get; private set; }

        internal string ApplyFailedStep { get; private set; }

        // True when the forced straight path had already been applied when the failure happened, so the
        // caller knows the mover was in motion and must terminate the carrier.
        internal bool ForcedPathAppliedBeforeFailure { get; private set; }

        // Whether a forced straight path is still outstanding on the mover. The caller reads this to
        // decide whether the carrier must be terminated exactly, since a forced path lives only while the
        // mover holds a live command.
        internal bool ForcedPathOutstanding => forcedPathOwned;

        internal bool Restored { get; private set; }

        // Whether this lease ever installed the native Charge buff on the rider. It is a historical
        // fact and stays true after the buff has been removed: the evidence readers ask "did the charge
        // carry the native buff", and a successful removal must not erase the answer. Ownership is a
        // separate question, asked of the retained reference below, and the cleanup ledger probes that
        // one rather than this.
        internal bool BuffApplied { get; private set; }

        // Whether the buff is still owned by this lease right now.
        internal bool BuffOutstanding => buffOwnership.Outstanding || buffChildren != null && !buffChildren.Drained;

        internal bool RiderAgentTouched { get; private set; }

        internal int ForcedPathCount { get; private set; }

        internal float SpeedOverrideApplied { get; private set; }

        internal bool ChargingAppliedExactly { get; private set; }

        internal bool ChargingObservedThroughout { get; private set; } = true;

        internal bool ForceModeAfterApply { get; private set; }

        internal bool ForceModeAtRestore { get; private set; }

        internal bool ChargingRestoredExactly { get; private set; }

        internal bool SpeedOverrideRestoredExactly { get; private set; }

        internal bool RiderChargingRestoredExactly { get; private set; }

        private UnitMovementAgent MountAgent => appliedMountAgent;

        internal bool ForceMode
        {
            get
            {
                var agent = MountAgent;
                return agent != null && (bool)ForceModeField.GetValue(agent);
            }
        }

        // The three stock calls on the mover's agent, plus the attacker's charge buff and charging state.
        // Called once, immediately after the pair's delegated ground move has proved it owns the mount Move slot.
        internal void Apply()
        {
            if (Applied)
            {
                throw new InvalidOperationException("The mounted charge lease was applied twice.");
            }

            var agent = mount.View == null ? null : mount.View.AgentASP;
            if (agent == null || rider.View == null || rider.Descriptor == null)
            {
                throw new InvalidOperationException("The mounted charge lease requires the exact live pair views.");
            }
            appliedMountAgent = agent;
            appliedMountView = mount.View;
            appliedRiderState = rider.Descriptor.State;

            var riderAgent = rider.View.AgentASP;
            var riderChargingAgentBefore = riderAgent != null && riderAgent.IsCharging;
            var riderSpeedAgentBefore = riderAgent == null ? null : riderAgent.MaxSpeedOverride;
            var riderAgentEnabledBefore = riderAgent != null && riderAgent.enabled;

            chargingBefore = agent.IsCharging;
            chargingCounterBefore = (int)ChargingCounterField.GetValue(agent);
            speedOverrideBefore = agent.MaxSpeedOverride;
            riderChargingBefore = rider.Descriptor.State.IsCharging;
            SpeedOverrideApplied = Math.Max(speedOverrideBefore ?? 0f, mount.CombatSpeedMps * 2f);

            // The five mutations, in order, each with the exact undo of its own field. The rider's charge
            // buff is first because it is the one mandatory step whose failure costs nothing to undo, and
            // the forced path is last because nothing may be in motion until everything else is in place.
            // The exact installed buff is owned through every cleanup boundary, including successful
            // terminal delivery. Pre-existing buffs and ambiguous callback failures cannot be adopted.
            application = new MountedChargeApplicationTransaction(new[]
            {
                new MountedChargeApplicationStep("charge-buff", ApplyChargeBuff, () => RequireUndo("charge-buff", TryUndoChargeBuff)),
                new MountedChargeApplicationStep("mount-charging", ApplyMountCharging, () => RequireUndo("mount-charging", TryUndoMountCharging)),
                new MountedChargeApplicationStep("mount-speed-override", ApplyMountSpeedOverride, () => RequireUndo("mount-speed-override", TryUndoMountSpeedOverride)),
                new MountedChargeApplicationStep("rider-charging-state", ApplyRiderChargingState, () => RequireUndo("rider-charging-state", TryUndoRiderChargingState)),
                new MountedChargeApplicationStep("forced-path", ApplyForcedPath, () => RequireUndo("forced-path", TryUndoForcedPath))
            });

            try
            {
                application.Apply();
            }
            catch (Exception)
            {
                ApplyFailedStep = application.FailedStep;
                ApplyFailureReason = application.FailureReason;
                // The transaction undid what it could; the ledger now proves it and discharges the rest.
                AttemptCleanup();
                ChargingRestoredExactly = agent.IsCharging == chargingBefore;
                SpeedOverrideRestoredExactly = agent.MaxSpeedOverride == speedOverrideBefore;
                RiderChargingRestoredExactly = rider.Descriptor == null ||
                    rider.Descriptor.State.IsCharging == riderChargingBefore;
                Observe("apply-rolled-back", application.Describe() +
                    ";charging=" + ChargingRestoredExactly + ";speed=" + SpeedOverrideRestoredExactly +
                    ";riderCharging=" + RiderChargingRestoredExactly + ";buffOutstanding=" + BuffOutstanding +
                    ";forcedPathApplied=" + ForcedPathAppliedBeforeFailure);
                logger.Info("Mounted charge lease application rolled back: mountId=" + mount.UniqueId +
                    "; riderId=" + rider.UniqueId + "; " + application.Describe() +
                    "; charging=" + ChargingRestoredExactly +
                    "; speedOverride=" + SpeedOverrideRestoredExactly +
                    "; riderCharging=" + RiderChargingRestoredExactly +
                    "; buffRemoved=" + !BuffOutstanding +
                    "; forcedPathApplied=" + ForcedPathAppliedBeforeFailure + ".");
                throw;
            }

            ForceModeAfterApply = ForceMode;
            ChargingAppliedExactly = agent.IsCharging;
            RiderAgentTouched = riderAgent != null &&
                (riderAgent.IsCharging != riderChargingAgentBefore ||
                 riderAgent.MaxSpeedOverride != riderSpeedAgentBefore ||
                 riderAgent.enabled != riderAgentEnabledBefore);
            Applied = true;
            Observe("applied", "speed=" + Format(SpeedOverrideApplied) + ";buff=" + BuffApplied +
                ";forceMode=" + ForceModeAfterApply + ";riderAgentTouched=" + RiderAgentTouched);
            logger.Info("Mounted charge lease applied: mountId=" + mount.UniqueId +
                "; riderId=" + rider.UniqueId + "; targetId=" + target.UniqueId +
                "; speedOverride=" + Format(SpeedOverrideApplied) +
                "; chargingBefore=" + chargingBefore + "; forceMode=" + ForceModeAfterApply + ".");
        }

        // The stock runtime routine re-forces its path whenever the target has moved; the pair carrier also
        // drops force mode when its own native approach recomputes a path. Both are handled here, and the
        // doubled speed is re-asserted exactly as the stock routine re-asserts it on every tick.
        internal void Maintain(bool carrierAlive)
        {
            if (!Applied || Restored)
            {
                return;
            }

            // A charge whose mover has no movement agent is not a charge. This was a silent return, which
            // left the transaction running with no forced path and no doubled speed.
            var agent = RequireMountAgent();
            ChargingObservedThroughout &= agent.IsCharging;
            agent.MaxSpeedOverride = Math.Max(agent.MaxSpeedOverride ?? 0f, SpeedOverrideApplied);
            if (!carrierAlive || ForceMode)
            {
                return;
            }

            ForcePathToTarget("re-force");
        }

        // The five steps. Each Apply takes exactly one mutation and records ownership of it; each Undo
        // returns exactly that field to the value captured before the attempt.
        private void ApplyChargeBuff()
        {
            var chargeBuff = BlueprintRoot.Instance == null || BlueprintRoot.Instance.SystemMechanics == null
                ? null
                : BlueprintRoot.Instance.SystemMechanics.ChargeBuff;
            if (chargeBuff == null)
            {
                throw new InvalidOperationException("The stock charge buff blueprint is unavailable.");
            }

            appliedBuffCollection = rider.Buffs;
            if (appliedBuffCollection.Enumerable.Any(buff => buff.Blueprint == chargeBuff))
                throw new InvalidOperationException("An existing Charge buff cannot be adopted by this transaction.");
            buffComponentTypes = chargeBuff.ComponentsArray.Select(component => component == null ? "<null>" : component.GetType().FullName).ToArray();
            var surface = RequireAvailableBuffSurface(chargeBuff, rider);
            buffStats = rider.Descriptor.Stats.GetList().ToArray();
            buffOwnerThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            buffAcquisitionContext = new MechanicsContext(rider, rider.Descriptor, chargeBuff);
            buffChildren = new MountedChargeBuffChildren(surface, rider, appliedBuffCollection,
                () => buffOwnership.Fact, () => buffOwnership.Acquiring,
                () => buffOwnership.ObserveNativeRemoval(buffOwnership.Fact), CaptureBuffResidue);
            var previous = acquiringBuffLease;
            acquiringBuffLease = this;
            try
            {
                buffOwnership.Acquire(() => appliedBuffCollection.AddBuff(chargeBuff, buffAcquisitionContext, 1.Rounds().Seconds));
                BuffApplied = true;
            }
            finally { acquiringBuffLease = previous; CaptureBuffResidue(); }
        }

        private static void RequireChargeBuffSurface(BlueprintBuff chargeBuff)
        {
            MountedChargeBuffSurface.Read(chargeBuff);
        }

        private static MountedChargeBuffSurface RequireAvailableBuffSurface(BlueprintBuff chargeBuff, UnitEntityData rider)
        {
            RequireChargeBuffSurface(chargeBuff);
            var surface = MountedChargeBuffSurface.Read(chargeBuff);
            foreach (var enchantment in surface.Enchantments) MountedChargeEnchantmentFx.ValidatePrefab(enchantment);
            if (rider?.Buffs == null || rider.Buffs.Enumerable.Any(buff => ReferenceEquals(buff.Blueprint, chargeBuff) ||
                surface.Children.Any(child => ReferenceEquals(child, buff.Blueprint))))
                throw new InvalidOperationException("An existing Charge lifetime buff cannot be replaced or adopted.");
            return surface;
        }

        internal static string BuffAvailabilityReason(UnitEntityData rider)
        {
            try
            {
                RequireAvailableBuffSurface(BlueprintRoot.Instance?.SystemMechanics?.ChargeBuff, rider);
                return null;
            }
            catch (Exception error) { return "Mounted Charge cannot safely own the loaded Charge buff: " + error.Message; }
        }

        // The CreateFact postfix runs before AddFact inserts/activates the fact. Match the unique
        // parent context as well as the original collection; reentrant unrelated facts are ignored.
        internal static void ObserveChargeBuffCreated(BuffCollection collection, Buff buff)
        {
            var lease = acquiringBuffLease;
            if (lease != null && ReferenceEquals(collection, lease.appliedBuffCollection) && buff != null &&
                ReferenceEquals(buff.Context?.ParentContext, lease.buffAcquisitionContext))
            {
                lease.buffOwnership.CaptureCreated(buff);
                lease.buffCondition = new MountedChargeConditionObserver(buff, lease.appliedRiderState);
            }
        }

        // Exact RemoveFact deactivates before unlinking; blueprint-wide removal unlinks first.
        // Both can leave residue after caught callbacks. Observe exact state rather than infer
        // completion from absence, and never clean inside a resumable acquisition/lifecycle scope.
        private bool TryUndoChargeBuff()
        {
            if (!BuffOutstanding) return true;
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != buffOwnerThread) return false;
            if (buffOwnership.Acquiring || !BuffLifecycleSettled(buffOwnership.Fact) || buffChildren != null && !buffChildren.ScopeSettled) return false;
            MountedChargeAdmissionFault.FireCleanup("charge-buff");
            CaptureBuffResidue();
            try { buffOwnership.TryRemove(buff => buff.Remove(), BuffCleanupPostcondition); }
            finally { buffChildren?.TryDrain(); }
            var complete = buffOwnership.TryRemove(buff => buff.Remove(), BuffCleanupPostcondition);
            if (complete) { buffCondition.Release(); buffChildren?.Release(); }
            return complete;
        }

        private void CaptureBuffResidue()
        {
            var buff = buffOwnership.Fact;
            if (buff == null) return;
            foreach (var modifier in buffStats.SelectMany(stat => stat.Modifiers).Where(item => ReferenceEquals(item.Source, buff)))
                if (!buffModifiers.Contains(modifier)) buffModifiers.Add(modifier);
            var components = typeof(Buff).Module.ResolveField(0x0400695A).GetValue(buff) as IEnumerable;
            if (components != null)
                foreach (GameLogicComponent component in components)
                    if (!buffComponents.Any(item => ReferenceEquals(item, component))) buffComponents.Add(component);
        }

        private bool BuffCleanupPostcondition(Buff buff) => BuffResiduePostcondition(buff,
            Game.Instance?.Rulebook?.Context, System.Threading.Thread.CurrentThread.ManagedThreadId);

        // Only resolution of the native singleton is outside the detached adapter probe.
        // The actual current context, thread, components and modifier lists are checked here.
        private bool BuffResiduePostcondition(Buff buff, Kingmaker.RuleSystem.RulebookEventContext context, int thread) =>
            appliedBuffCollection != null && buffCondition != null && buffCondition.Drained &&
            (buffChildren == null || buffChildren.Drained) &&
            !buffOwnership.Acquiring && BuffLifecycleSettled(buff) &&
            ChargeRuleDispatchSettled(context, thread) &&
            !appliedBuffCollection.Enumerable.Any(fact => ReferenceEquals(fact, buff)) &&
            !buff.Active && buff.IsDisposed && NativeFactListEmpty(buff, 0x04001B6D) &&
            NativeFactListEmpty(buff, 0x04001B6E) && NativeFactListEmpty(buff, 0x0400695A) &&
            typeof(Buff).Module.ResolveField(0x0400695B).GetValue(buff) == null &&
            buffComponents.All(component => !ReferenceEquals(component, null) && !component.IsListeningEvents) &&
            buffStats.All(stat => !stat.Modifiers.Any(modifier => ReferenceEquals(modifier.Source, buff))) &&
            buffModifiers.All(modifier => ReferenceEquals(modifier.AppliedTo, null) &&
                buffStats.All(stat => !stat.Modifiers.Any(item => ReferenceEquals(item, modifier))));

        private static bool BuffLifecycleSettled(Buff buff)
        {
            if (buff == null) return false;
            // Exact native Fact scopes: their finally blocks clear these flags. Reading
            // false proves return from the scope, never successful cleanup by itself.
            foreach (var token in new[] { 0x04006962, 0x04006963, 0x04006964 })
                if ((bool)typeof(Buff).Module.ResolveField(token).GetValue(buff)) return false;
            return true;
        }

        private bool ChargeRuleDispatchSettled(Kingmaker.RuleSystem.RulebookEventContext context, int thread)
        {
            return thread == buffOwnerThread && context != null && !context.EventStack.Any();
        }

        private static bool NativeFactListEmpty(Buff buff, int token)
        {
            var field = typeof(Buff).Module.ResolveField(token);
            var items = field.GetValue(buff) as IEnumerable;
            return items == null || !items.Cast<object>().Any();
        }

        private void ApplyMountCharging()
        {
            var agent = RequireMountAgent();
            chargingOwned = true;
            agent.IsCharging = true;
        }

        // Ownership is retained when the agent is unavailable: the flag may still be live on an agent
        // that comes back, and reporting it restored would be a guess.
        private bool TryUndoMountCharging()
        {
            MountedChargeAdmissionFault.FireCleanup("mount-charging");
            var agent = MountAgent;
            if (agent == null)
            {
                return false;
            }

            // IsCharging is a native reference counter, not a Boolean assignment. Observe
            // first: a setter that decremented then threw must never be decremented twice.
            if (!TryRestoreChargingCounter(agent, chargingCounterBefore, ref chargingReleaseObserved))
            {
                return false;
            }

            chargingOwned = false;
            return true;
        }

        private static bool TryRestoreChargingCounter(UnitMovementAgent agent, int before, ref bool released)
        {
            var counter = (int)ChargingCounterField.GetValue(agent);
            if (!released)
            {
                // An external owner's decrement is not proof that KMC released its increment.
                // An ambiguous count retains debt without overwriting native state.
                if (counter != before + 1) return false;
                try { agent.IsCharging = false; }
                finally { released = (int)ChargingCounterField.GetValue(agent) == before; }
            }
            return released && (int)ChargingCounterField.GetValue(agent) == before && agent.IsCharging == (before > 0);
        }

        private void ApplyMountSpeedOverride()
        {
            var agent = RequireMountAgent();
            speedOverrideOwned = true;
            agent.MaxSpeedOverride = SpeedOverrideApplied;
        }

        private bool TryUndoMountSpeedOverride()
        {
            MountedChargeAdmissionFault.FireCleanup("mount-speed-override");
            var agent = MountAgent;
            if (agent == null)
            {
                return false;
            }

            agent.MaxSpeedOverride = speedOverrideBefore;
            if (agent.MaxSpeedOverride != speedOverrideBefore)
            {
                return false;
            }

            speedOverrideOwned = false;
            return true;
        }

        private void ApplyRiderChargingState()
        {
            if (rider.Descriptor == null)
            {
                throw new InvalidOperationException("The mounted charge lease lost the rider descriptor.");
            }

            riderChargingOwned = true;
            appliedRiderState.IsCharging = true;
        }

        private bool TryUndoRiderChargingState()
        {
            MountedChargeAdmissionFault.FireCleanup("rider-charging-state");
            if (appliedRiderState == null)
            {
                return false;
            }

            appliedRiderState.IsCharging = riderChargingBefore;
            if (appliedRiderState.IsCharging != riderChargingBefore)
            {
                return false;
            }

            riderChargingOwned = false;
            return true;
        }

        private void ApplyForcedPath()
        {
            forcedPathOwned = true;
            ForcePathToTarget("initial");
            MountedChargeAdmissionFault.FireAfterLeaseAcquired();
        }

        // A forced path lives only while the mover holds a live command (preview.156/157), and the caller
        // terminates that carrier exactly. Stopping the view here keeps the mount from travelling the
        // charge line in the meantime; the force-mode latch is recorded at Restore, never faked.
        // Ownership is released only after StopMoving has actually run. The force-mode latch itself is
        // native and is recorded rather than faked, exactly as Restore has always recorded it.
        private bool TryUndoForcedPath()
        {
            MountedChargeAdmissionFault.FireCleanup("forced-path");
            ForcedPathAppliedBeforeFailure = true;
            if (appliedMountView == null || appliedMountAgent == null)
            {
                return false;
            }

            appliedMountAgent.Stop();
            if (appliedMountAgent.Path != null || appliedMountAgent.IsReallyMoving) return false;
            forcedPathOwned = false;
            return true;
        }

        // The application transaction records a step failure when an undo throws. A postcondition that
        // does not hold is the same kind of fact, so it is raised the same way.
        private void RequireUndo(string step, Func<bool> tryUndo)
        {
            if (!tryUndo())
            {
                throw new InvalidOperationException("The mounted charge lease could not undo " + step + ".");
            }
        }

        private MountedChargeCleanupLedger Cleanup()
        {
            return cleanup ?? (cleanup = new MountedChargeCleanupLedger(new[]
            {
                new MountedChargeCleanupStep("forced-path", () => forcedPathOwned, TryUndoForcedPath),
                new MountedChargeCleanupStep("rider-charging-state", () => riderChargingOwned, TryUndoRiderChargingState),
                new MountedChargeCleanupStep("mount-speed-override", () => speedOverrideOwned, TryUndoMountSpeedOverride),
                new MountedChargeCleanupStep("mount-charging", () => chargingOwned, TryUndoMountCharging),
                new MountedChargeCleanupStep("charge-buff", () => BuffOutstanding, TryUndoChargeBuff)
            }));
        }

        // One cleanup attempt. Safe to call repeatedly: a mutation already returned is skipped, and one
        // still owned is tried again. Returns true when nothing is owed any more.
        internal bool AttemptCleanup()
        {
            var ledger = Cleanup();
            ledger.Attempt();
            ChargingRestoredExactly = ChargingPostcondition();
            SpeedOverrideRestoredExactly = SpeedOverridePostcondition();
            RiderChargingRestoredExactly = RiderChargingPostcondition();
            Observe("cleanup", ledger.Describe());
            logger.Info("Mounted charge lease cleanup: mountId=" + mount.UniqueId + "; " + ledger.Describe() +
                "; charging=" + ChargingRestoredExactly + "; speedOverride=" + SpeedOverrideRestoredExactly +
                "; riderCharging=" + RiderChargingRestoredExactly + "; forceModeLatched=" + ForceMode + ".");
            return ledger.Complete && FinalCleanupPostconditions();
        }

        private bool FinalCleanupPostconditions() => ChargingPostcondition() && SpeedOverridePostcondition() &&
            RiderChargingPostcondition() && !forcedPathOwned && !BuffOutstanding;

        private bool ChargingPostcondition()
        {
            var agent = MountAgent;
            return !chargingOwned && (ReferenceEquals(agent, null) || agent.IsCharging == chargingBefore &&
                (int)ChargingCounterField.GetValue(agent) == chargingCounterBefore);
        }

        private bool SpeedOverridePostcondition()
        {
            var agent = MountAgent;
            return !speedOverrideOwned && (ReferenceEquals(agent, null) || agent.MaxSpeedOverride == speedOverrideBefore);
        }

        private bool RiderChargingPostcondition()
        {
            return !riderChargingOwned &&
                (appliedRiderState == null || appliedRiderState.IsCharging == riderChargingBefore);
        }

        private UnitMovementAgent RequireMountAgent()
        {
            var agent = MountAgent;
            if (agent == null || mount.View != appliedMountView || mount.View.AgentASP != agent)
            {
                throw new InvalidOperationException("The mounted charge lease lost the mount movement agent.");
            }

            return agent;
        }

        // Restore is now an attempt, not a declaration. It returns true only when every mutation this
        // lease owns has actually been returned and observed returned; otherwise the lease keeps its
        // cleanup debt and stays unrestored, so the command and the controller can retry or escalate.
        internal void Restore()
        {
            TryRestore();
        }

        internal bool TryRestore()
        {
            if (Restored)
            {
                return true;
            }

            ForceModeAtRestore = ForceMode;
            var complete = AttemptCleanup();
            if (!complete)
            {
                Observe("restore-incomplete", CleanupDescription);
                logger.Info("Mounted charge lease restore is incomplete; cleanup debt retained: mountId=" +
                    mount.UniqueId + "; unresolved=" + UnresolvedCleanup + "; failures=" + CleanupFailures + ".");
                return false;
            }

            // Only now: nothing is owed, every postcondition was observed.
            Restored = true;

            Observe("restored", "buffOutstanding=" + BuffOutstanding +
                ";charging=" + ChargingRestoredExactly + ";speed=" + SpeedOverrideRestoredExactly +
                ";riderCharging=" + RiderChargingRestoredExactly + ";forceModeLatched=" + ForceModeAtRestore);
            logger.Info("Mounted charge lease restored: mountId=" + mount.UniqueId +
                "; charging=" + ChargingRestoredExactly + "; speedOverride=" + SpeedOverrideRestoredExactly +
                "; riderCharging=" + RiderChargingRestoredExactly +
                "; forcedPaths=" + ForcedPathCount + "; forceModeLatched=" + ForceModeAtRestore + ".");
            return true;
        }

        private void ForcePathToTarget(string reason)
        {
            var agent = RequireMountAgent();

            agent.ForcePath(new ForcedPath(new List<Vector3> { mount.Position, target.Position }), ForcedApproachRadius);
            ForcedPathCount++;
            Observe("forced-path", reason + ";count=" + ForcedPathCount + ";forceMode=" + ForceMode +
                ";distance=" + Format(mount.DistanceTo(target)));
        }

        private void Observe(string stage, string detail)
        {
            if (observations.Count >= 64)
            {
                return;
            }

            observations.Add(stage + ":" + detail);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        internal JObject CaptureEvidence()
        {
            return new JObject
            {
                ["applied"] = Applied,
                ["applyRolledBack"] = ApplyRolledBack,
                ["applyFailedStep"] = ApplyFailedStep,
                ["applyFailureReason"] = ApplyFailureReason,
                ["applyDescription"] = application == null ? null : application.Describe(),
                ["forcedPathAppliedBeforeFailure"] = ForcedPathAppliedBeforeFailure,
                ["forcedPathOutstanding"] = ForcedPathOutstanding,
                ["buffApplied"] = BuffApplied,
                ["buffOutstanding"] = BuffOutstanding,
                ["buffAcquisitionObserved"] = buffOwnership.Fact != null,
                ["buffAcquisitionStarted"] = buffOwnership.Started,
                ["buffCallbackDebt"] = buffOwnership.Outstanding ? buffOwnership.Fault : null,
                ["buffCallbackFailureHistory"] = buffOwnership.Fault,
                ["buffComponentTypes"] = new JArray(buffComponentTypes),
                ["buffCondition"] = buffCondition?.CaptureEvidence(),
                ["buffChildren"] = buffChildren?.CaptureEvidence(),
                ["buffNative"] = MountedChargeBuffChildren.CaptureNativeFact(buffOwnership.Fact, appliedBuffCollection, buffComponents, buffStats, buffModifiers),
                ["buffRuleDispatchSettled"] = ChargeRuleDispatchSettled(Game.Instance?.Rulebook?.Context,
                    System.Threading.Thread.CurrentThread.ManagedThreadId),
                ["chargingBefore"] = chargingBefore,
                ["chargingCounterBefore"] = chargingCounterBefore,
                ["chargingCounterAfter"] = appliedMountAgent == null ? (int?)null : (int)ChargingCounterField.GetValue(appliedMountAgent),
                ["speedOverrideBefore"] = speedOverrideBefore,
                ["speedOverrideApplied"] = SpeedOverrideApplied,
                ["mountCombatSpeedMps"] = mount.CombatSpeedMps,
                ["chargingAppliedExactly"] = ChargingAppliedExactly,
                ["chargingObservedThroughout"] = ChargingObservedThroughout,
                ["forceModeAfterApply"] = ForceModeAfterApply,
                ["forcedPathCount"] = ForcedPathCount,
                ["riderAgentTouched"] = RiderAgentTouched,
                ["restored"] = Restored,
                ["chargingRestoredExactly"] = ChargingRestoredExactly,
                ["speedOverrideRestoredExactly"] = SpeedOverrideRestoredExactly,
                ["riderChargingRestoredExactly"] = RiderChargingRestoredExactly,
                ["forceModeAtRestore"] = ForceModeAtRestore,
                ["riderChargingBefore"] = riderChargingBefore,
                ["observations"] = new JArray(observations.ToArray())
            };
        }

        internal string Describe()
        {
            return "applied=" + Applied + ";rolledBack=" + ApplyRolledBack + ";restored=" + Restored + ";buff=" + BuffApplied +
                ";forcedPaths=" + ForcedPathCount + ";speedOverride=" + Format(SpeedOverrideApplied) +
                ";chargingThroughout=" + ChargingObservedThroughout +
                ";forceModeLatched=" + ForceModeAtRestore +
                ";riderAgentTouched=" + RiderAgentTouched;
        }
    }
}
