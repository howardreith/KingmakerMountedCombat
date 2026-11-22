<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=status-45-paused part=1/1 sha256=3d010e39ba2b9785c7aec61458ce450627aac0aa47d22a00203fb3baafa5851a -->
### Workflow — paused — checkpoint final

developer paused: I am returning paused: the owner-authorized stop file appeared during round 16. Its text reads 'owner-authorized safe stop to activate the reviewer effort max workflow version (2026-11-21)'. The round's work was already finished. I committed it as d4b3c2d4, pushed it through the guarded helper, and left the worktree clean. The required focused gate was NOT run on d4b3c2d4, because the stop file was found after the push; it must run on this commit when work resumes.

Changes in d4b3c2d4:
- R14-F2 and R15-F1 (shared root cause): while a chosen slot is interrupted, a surviving older alias or stale canonical copy at another index is treated as a secondary copy (MountedControlPlacementIntent.IsSecondaryBinding with IsStaleOrAlias).
  - Bar observation keeps the interrupted choice (ObserveBindingAtBoundary).
  - Capture classification and the live archive writer skip the copy, so the remembered writer saves the chosen index.
  - ReconcilePublicControlSlots removes the migrated or rebound copy when the chosen slot is free, so the control returns there as one shortcut.
  - A binding that holds the live fact's data, such as a real player placement, is never treated as secondary.
- R16-F1: TryInitialPlacement now records the placed slot and its companion immediately (ObserveBound, or ObserveOffBar when the bar is full). A direct change of companions before the next observation therefore retires the placement.

Three new component regressions cover these sequences, and the handoff doc has a Round 16 section.

Development run on the uncommitted tree: components 1483/0, Validate-Source 153/0, assembly contracts 822/0, archive bound 9/0, focused 5/5. PASS lines were temporarily silenced in tests/TestRunner.cs for this run; that change was reverted and is not in the commit. A mutation run failed exactly the new regressions plus round 15's save-between-reads test.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

- Enrollment: `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`
- Phase: `develop`; rounds completed for this checkpoint: 16/30
- Inspect: `python C:/Dev/agent-review/agent_review.py status --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`

_Posted by the agent-review controller through the owner's GitHub account. Automated status; not a human review._
