# Kingmaker native-test reservation (protocol 1)

A machine-wide convention for the Kingmaker labs on one Windows machine that run native game tests. Several labs share
resources no single lab owns: the owner's Pathfinder: Kingmaker profile (saves, player log, settings), Steam's
remote-storage mirror, the live installation and the set of running `Kingmaker` processes. Each lab owns its own
transaction lock and journal; this reservation makes those transactions mutually exclusive across labs.

It is a convention, not a dependency. Each participating lab carries its own copy of this document and its own
implementation, built only from Windows and .NET facilities. No lab needs another lab's repository, installation,
directories, scripts, binaries or configuration to exist, and no service or shared package is involved. A lab installed
alone acquires and releases the reservation exactly as it does with peers present.

## Identity

| Item | Value |
| --- | --- |
| Protocol | `kingmaker-native-test-reservation/1` |
| Mutex | `Global\KingmakerNativeTest.Reservation.v1` (named mutex, Global namespace: every session on the machine) |
| Lease record | `%ProgramData%\KingmakerNativeTest\reservation.v1.json` (directory created on demand by any participant) |

## Record

UTF-8 JSON object, written to a temporary file in the same directory and moved into place (never partially visible):

- `protocol`: `kingmaker-native-test-reservation/1`
- `lab`: the holder's lab identity (for example `KingmakerFlight`, `KingmakerBuffPlanner`, `KingmakerMountedCombat`,
  `KingmakerWakingRune`)
- `runId`, `purpose`: the holder's transaction
- `token`: a fresh random value per acquisition; ownership is the pair (`lab`, `token`)
- `holderProcessId`, `holderStartUtc`, `host`, `createdUtc`: diagnostics, and for the holder's own lab to decide
  whether its writer still runs
- optional fields a lab adds for itself (for example `adopted`); other labs ignore them

## Rules

1. **Order.** A participating entrypoint acquires the reservation before it reads a mutable baseline, snapshots,
   prepares, stages, launches or restores anything, and only then takes its own lab lock. Preparation, scenarios and
   recovery all use this order. It releases in reverse order: its own lock first, the reservation last.
2. **Atomic acquisition.** Acquire, adopt and release only while holding the mutex (a bounded wait; a busy or
   inaccessible mutex refuses). Under the mutex: no record means free, so write one; any record means reserved, so
   refuse and name the holder. Whoever holds the reservation, a lab never decides on its own that no peer is present.
   The mutex is held only for these few steps, never across a transaction, so a transaction can span processes.
3. **Hold for the whole transaction.** Hold the reservation until the transaction's restoration is verified, including
   periods when no game process exists. An entrypoint whose transaction outlives its process (a multi-step tool, or a
   deployment restored later) keeps the record until its own restore step releases it.
4. **Unresolved stays reserved.** A record is a reservation whether or not its writer still runs. A mutex abandoned by
   a dead holder, or a record whose holder has exited, is not proof that the transaction restored. Only the lab named in
   the record may adopt or remove it, and only through its own recovery, ownership and journal rules. Other labs refuse.
5. **Ambiguity refuses.** A record that is unreadable or names another protocol is never removed by a participant; it is
   left for the owner.
6. **Loss of ownership is an incident.** A holder that finds the record gone or replaced (another writer, the owner)
   stops the affected native path, leaves the new record alone and preserves its evidence.
7. **Existing checks stay.** The reservation adds to each lab's own lock, journal and game-process checks. Optional
   discovery of other labs (for example checking a peer's lock file when that peer is installed) may remain; a missing
   peer is the normal standalone configuration.

## Scope

The reservation coordinates participating automation only. It does not make unexpected owner activity or a
nonparticipating process attributable to any lab. Entrypoints that do not implement it, such as older copies of a lab's
scripts, are nonparticipating and must not be used while another lab's native transaction runs.
