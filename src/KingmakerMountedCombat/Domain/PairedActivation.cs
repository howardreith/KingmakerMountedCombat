using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Entitlement is tied to an encounter and an observed native principal boundary.
    // Costs are observations of native actors, never a spendable parallel bank.
    public sealed class PairedActivation<TActor, TBoundary> where TActor : class where TBoundary : class
    {
        public sealed class ActorState
        {
            public TActor Actor { get; internal set; }
            public bool Granted { get; internal set; }
            public bool Prepared { get; internal set; }
            public bool Ended { get; internal set; }
            public float StandardSpent { get; private set; }
            public float MoveSpent { get; private set; }
            public float SwiftSpent { get; private set; }

            public void Observe(float standard, float move, float swift)
            {
                if (!Granted) throw new InvalidOperationException("An observation cannot create entitlement.");
                StandardSpent = Math.Max(StandardSpent, standard);
                MoveSpent = Math.Max(MoveSpent, move);
                SwiftSpent = Math.Max(SwiftSpent, swift);
            }
        }

        private readonly HashSet<TBoundary> boundaries = new HashSet<TBoundary>();
        public Guid EncounterId { get; } = Guid.NewGuid();
        public long Sequence { get; private set; }
        public TBoundary Boundary { get; private set; }
        public TActor Principal { get; }
        public TActor Partner { get; }
        public ActorState Rider { get; private set; }
        public ActorState Mount { get; private set; }
        public bool Ending { get; private set; }
        public bool Split { get; private set; }
        public bool Open => Rider != null && Rider.Prepared && Mount.Prepared && !Ending;
        public string Identity => EncounterId.ToString("N") + ":" + Sequence;

        public PairedActivation(TActor principal, TActor partner)
        {
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
            Partner = partner ?? throw new ArgumentNullException(nameof(partner));
            if (ReferenceEquals(principal, partner)) throw new ArgumentException("Two distinct native actors are required.");
        }

        public bool Begin(TBoundary boundary)
        {
            if (boundary == null || Split || boundaries.Contains(boundary)) return false;
            if (Boundary != null && (!Rider.Ended || !Mount.Ended))
                throw new InvalidOperationException("Previous paired activation has not ended.");
            boundaries.Add(boundary);
            Boundary = boundary;
            Sequence++;
            Rider = new ActorState { Actor = Principal };
            Mount = new ActorState { Actor = Partner };
            Ending = false;
            return true;
        }

        public bool BeginActorPreparation(TActor actor, TBoundary boundary)
        {
            var state = State(actor);
            if (!ReferenceEquals(Boundary, boundary) || state == null || state.Granted || Ending) return false;
            state.Granted = true;
            return true;
        }

        public void FinishActorPreparation(TActor actor)
        {
            var state = State(actor);
            if (state == null || !state.Granted || state.Prepared)
                throw new InvalidOperationException("Preparation completion requires one reserved actor grant.");
            state.Prepared = true;
        }

        public ActorState State(TActor actor) => ReferenceEquals(actor, Principal) ? Rider :
            ReferenceEquals(actor, Partner) ? Mount : null;

        public bool CanAddress(TActor actor, TBoundary boundary) => Open && !Split &&
            ReferenceEquals(Boundary, boundary) && State(actor) != null && !State(actor).Ended;

        public void BeginEnding() { Ending = true; }
        public void EndActor(TActor actor)
        {
            var state = State(actor);
            if (state == null || !state.Granted) return;
            state.Ended = true;
        }
        public void Detach() { Split = true; }
    }
}
