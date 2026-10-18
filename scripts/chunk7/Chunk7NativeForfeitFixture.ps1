# Generated (not a native recording) for the code-built synthetic TB fixture in Test-Chunk7CombatEvidence:
# the native paired-removal forfeit observed in batch 6 slot 3 (tb-pending-move-diagnosis01). Removing
# rider B runs native ForceToEnd(setCooldowns: true) on the rider turn with the mount's private turn nested
# inside it, leaving the pending mount at max(before, 6/3/6). The recorded OwnedPairsEvidenceFixtures.json
# combat fixtures carry this shape directly since 2026-10-18; this applies it to the code-built fixture.
function Set-KmcChunk7NativeForfeitFixture($Observations){
    $ids=@($Observations.actorIds);$pending=$Observations.pendingSurvivor
    $before=@($pending.before.actors|Where-Object id -CEQ $ids[3])[0];$after=@($pending.after.actors|Where-Object id -CEQ $ids[3])[0]
    foreach($pairing in @(@('standard',6.0),@('move',3.0),@('swift',6.0))){$after.($pairing[0])=[Math]::Max([double]$before.($pairing[0]),$pairing[1])}
    $events=@($Observations.allocationTrace.events);$next=[int](@($events|ForEach-Object{[int]$_.sequence})|Measure-Object -Maximum).Maximum
    $frame=$pending.after.frame
    $rows=@(@('remove-unit-before',$ids[2],$null),@('completion-force-to-end-before',$ids[2],'setCooldowns=True'),@('completion-force-to-end-before',$ids[3],'setCooldowns=True'),
        @('completion-force-to-end-after',$ids[3],'setCooldowns=True'),@('completion-force-to-end-after',$ids[2],'setCooldowns=True'),@('turn-end-after',$ids[3],$null),
        @('turn-end-after',$ids[2],$null),@('remove-unit-after',$ids[2],$null))
    foreach($row in $rows){$next++
        $events+=[pscustomobject]@{sequence=$next;boundary=$row[0];frame=$frame;detail=$row[2];command=0;commandActor=$null;commandType=$null;
            state=[pscustomobject]@{actor=$row[1];standard=6.0;move=3.0;swift=6.0;reactions=1;reactionCooldown=0.0}}}
    $Observations.allocationTrace.events=$events
}
