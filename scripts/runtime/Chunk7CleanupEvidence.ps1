# Optional immutable timeout receipts; these are failures and never success evidence.
Set-StrictMode -Version Latest
function Assert-KmcChunk7CleanupArtifacts($Request,$Manifest,[string]$Status='PASS') {
    if($Status-cnotin@('PASS','FAIL')){throw 'Cleanup envelope requires an exact terminal status.'}
    $families=@(
        @{name='bootstrap-inventory';scenarios=@('chunk7-bootstrap-cold-inventory')},
        @{name='native-bootstrap';scenarios=@('chunk7-native-main-bootstrap')},
        @{name='small-calibration';scenarios=@('chunk7-small-profile-calibration')},
        @{name='progression';scenarios=@('chunk7-native-progression')},
        @{name='ordinary-field';scenarios=@('chunk7-ordinary-field-admission')},
        @{name='ordinary';scenarios=@('chunk7-ordinary-acquisition')},
        @{name='world';scenarios=@('chunk7-two-pair-exploration')},
        @{name='persistence';scenarios=@('chunk7-persistence-save','chunk7-persistence-load')},
        @{name='combat';scenarios=@('chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first','chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge','chunk7-two-pair-native-life-cleanup')}
    )
    foreach($family in $families){
        $kind='chunk7-'+$family.name+'-cleanup-pending';$leaf=$kind+'.json'
        $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$leaf-or$_.kind-ceq$kind})
        if($present.Count-eq0){continue}
        if($Request.scenario-cnotin$family.scenarios){throw 'Foreign scenario supplied retained cleanup evidence.'}
        if($Status-cne'FAIL'){throw 'Retained cleanup timeout cannot be promoted to PASS.'}
        $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest $leaf $kind
        if(-not(Test-KmcExactJsonInteger $artifact.schemaVersion)-or$artifact.schemaVersion-ne1-or$artifact.status-cne'FAIL'-or$artifact.errors.Count-eq0){throw 'Retained cleanup receipt must be a versioned FAIL with nonempty errors.'}
        foreach($error in $artifact.errors){if($error-isnot[string]-or[string]::IsNullOrWhiteSpace($error)){throw 'Retained cleanup error must be a nonempty string.'}}
        if($null-eq$artifact.observations-or$artifact.observations-isnot[pscustomobject]){throw 'Retained cleanup observations are missing.'}
        if($family.name-ceq'combat'){
            $p=$artifact.observations.cleanupPending
            Assert-KmcExactProperties $p @('reason','serializationBusy','loading','queuedLoads','pendingNativeWrites','preparationPending','relationshipTransitionPending','settled') 'Retained combat cleanup'
            if($p.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($p.reason)-or$p.queuedLoads-isnot[Array]){throw 'Retained combat cleanup reason or queue is invalid.'}
            foreach($name in $p.queuedLoads){if($name-isnot[string]-or[string]::IsNullOrWhiteSpace($name)){throw 'Retained combat queue identity invalid.'}}
            foreach($flag in @('serializationBusy','loading','pendingNativeWrites','preparationPending','relationshipTransitionPending')){if($p.$flag-isnot[bool]){throw 'Retained combat boundary flag is not boolean.'}}
            Assert-KmcChunk7Boolean $p.settled $false
        } else {Assert-KmcChunk7Boolean $artifact.observations.cleanupPendingAtHostBoundary $true}
    }
}
