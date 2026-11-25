# Owner playtest for the next private alpha

This is a template for the developer's final alpha handoff. Fill in the **actual new package/version/hash** before giving install instructions. The mission ZIP itself is not a mod package.

Use a disposable save/copy and retain the prior alpha package plus a pre-test save for rollback. Restoring an old DLL does not establish backward compatibility for a save written by a newer alpha. Keep the installed UMM version unless a separate explicitly justified change is requested; this mission preserves minimum/build reference 0.31.1.

## 1. Seating and lifecycle

Mount once from an unmounted save. Check the saddle from the side at normal zoom, walk a short distance, turn and stop, then dismount/remount at the same location/camera. The rider should sit consistently and return to a normal unmounted pose. Save the disposable mounted state and reload once; compare the seat and pair behavior again.

Record whether an issue appears on first mount, only after loading, or only during a particular animation. Matched before/after views are more useful than different camera angles.

## 2. Turn-based ordinary melee

Use a fresh eligible pair activation and a reachable enemy just beyond melee reach. Give one ordinary attack order. The pair should approach legally and attack without a separate ground click. Repeat a nearby case with the game's five-foot-step mode when eligible, then a case after movement has already been spent; the latter must not produce extra free movement or attacks.

Watch the combat log for **Horse** attacks as well as the rider. When the horse has its own eligible attack opportunity, the pair order should carry through without another mount click. It must not attack again after its actions/activation are used. A routine after a legal full-attack opportunity may differ from a single attack after moving.

## 3. Real-time engagement and responsiveness

With AI enabled, a melee weapon equipped, and no active Stop/Hold/other order, begin combat near a valid reachable enemy. The pair should acquire/approach normally. If the encounter has two eligible reachable enemies, check that it chooses the nearer one unless you already gave it another target; no extra fixture is needed just for this comparison. Then give an explicit attack order from outside reach and compare the beginning of movement with an ordinary unmounted melee character in a similar state.

Movement may begin before the strike is ready when native rules allow it; damage must still wait for legal attack timing. Check that AI-off, Stop/Hold and a deliberately selected target remain respected. If a delay remains, note whether it affects movement start or only the first swing.

## 4. Horse bite, Trip and Shield

On a plain Horse without a separate ability granting Trip, watch a bite hit. It should not automatically trip merely because it bit. The bite should have visible corresponding motion; hooves should still animate. The log should contain the real native attacks and damage once each. A wolf or riding dog with legitimate bite-trip should retain that behavior.

Check the mount's Mounted Shield AC entry and one Mounted Combat roll if it occurs. Your previous screenshots showed **+2** in both. Use the expected value for the actual shield and focus feats in this test; enhancement does not change the feat's inherited design. Removing the shield or dismounting should remove the corresponding protection without duplicates after remount/reload.

## Reporting a remaining issue

Useful details are alpha version/package identity, RT or TB, shield/weapon, whether movement/actions were already used, AI/Stop/Hold state, first mount versus reload/remount, and a short log excerpt or clip showing the command and response. Do not infer a missing horse attack solely from a subtle animation. Mark unobserved cases as unobserved rather than passing.

Final visual/gameplay/save-load acceptance remains yours; the factory's alpha readiness records the scope it actually reviewed and tested.
