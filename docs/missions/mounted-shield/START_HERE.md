# Mounted Shield — Claude mission package

Prepared for Howie Reith · 23 November 2026 · Pathfinder: Kingmaker Mounted Combat

## Outcome

Implement one passive **Mounted Shield** combat feat with the supplied original icon. The finished feature protects the current mount and improves only the rider's existing Mounted Combat hit-negation check. The complete contract is in `MISSION.md`.

This package contains the mission, current source investigation, precise factory workflow, behavioral acceptance matrix, original artwork, ready-to-embed PNG, evidence templates and a read-only integrity checker. It is preparation material; it contains no compiled mod or claim of in-game verification.

## Start Claude

Extract this ZIP to a new mission directory in the Mounted Combat lab. Give the main interactive Claude Opus/high conversation `CLAUDE_LAUNCH_PROMPT.txt` and access to the extracted directory. Claude should follow `FACTORY_WORKFLOW.md`, enroll once in **foreground** mode, and continue normal correction batches without requiring you to relay the review.

The intended endpoint is a draft PR at the controller's actual `AWAITING_OWNER_REVIEW` state. A genuine access, platform, restoration or budget blocker is reported explicitly. The mission does not authorize merging, tagging, publishing, changing the installed controller or installing the mod for normal play.

## Read order

1. `MISSION.md` — complete governing implementation and workflow contract.
2. Current repository `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md` and `AUTONOMOUS-RESUME.md`.
3. `FACTORY_WORKFLOW.md` and `PRE_NATIVE_REVIEW.md` — checked foreground workflow, scoped review before game execution, and final factory handoff.
4. `RULES_AND_DECISIONS.md`, `SOURCE_MAP.md` and `ACCEPTANCE.md`.
5. `ART.md`, `assets/icon-manifest.json` and the report/evidence templates.

## Main materials

| File | Use |
| --- | --- |
| `assets/MountedShieldFeat.png` | Final 64×64, 8-bit RGBA runtime icon. Copy byte-for-byte. |
| `assets/MountedShieldFeat.512.png` | Larger export of the same artwork. |
| `assets/MountedShieldFeat.master.png` | Final full-resolution master with uniform reference ink. |
| `assets/MountedShieldFeat.generated.png` | Selected ImageGen color-correction result, preserved without modification. |
| `assets/MountedShieldFeat-review.png` | Actual-size 32/48/64px inspection on dark and parchment fields. |
| `assets/prompts/01-generation.txt` | Exact original ImageGen prompt. |
| `templates/developer-report.v1.schema.json` | Exact factory developer-report schema from the reviewed source. |
| `templates/developer-report.UNEXECUTED.json` | Deliberately unexecuted starter; replace with actual evidence. |
| `templates/ACCEPTANCE_LEDGER.json` | Behavioral rows, initially all NOT_RUN. |
| `templates/OWNER_HANDOFF.md` | Source, artifact, review, validation and restoration handoff fields. |
| `CHECKSUMS.sha256` and `tools/verify_packet.py` | Package integrity and runtime PNG checks. |

From the extracted package directory, optional local integrity verification is:

```text
python tools/verify_packet.py
```

This checker uses Python's standard library. It does not install, enroll, contact a service, or modify files. No new runtime or build dependency is introduced into the mod.

## Baseline and honest limits

Research used Mounted Combat `main` at `fb743911200f150af407198c6c28519e522ef58c` and the factory source at `41a185a71eff60ea83b8245ff367603109969429`. Claude must re-read current source and the installed package before executing. The recorded 0.2.15 baseline is a save/door diagnostic checkpoint, not a repair; its recorded native platform/fixture limitations remain distinct from this feat.

The supplied icon has passed file-format and desktop visual inspection. Native blueprint loading, feat UI and gameplay remain implementation acceptance work. Owner aesthetic approval is separate from those technical checks and does not require stopping source implementation.
