# Native supported-profile identity plus exact command/action/reaction proof.
Set-StrictMode -Version Latest
function Assert-KmcNativeMammothPair($Before,$After) {
    if($null -eq $Before -or $null -eq $After){throw 'Native Mammoth pair window is missing.'}
    foreach($p in @($Before,$After)) {
        if($p.mountBlueprint -cne 'e7aa96d15a45238438ae4cfb476f6bb9' -or [string]::IsNullOrEmpty($p.riderId) -or
            [string]::IsNullOrEmpty($p.mountId) -or $p.riderId -ceq $p.mountId -or $null -eq $p.riderObject -or $p.riderObject -eq 0 -or
            $null -eq $p.mountObject -or $p.mountObject -eq 0 -or $p.petObject -ne $p.mountObject -or $p.masterObject -ne $p.riderObject -or
            $p.petId -cne $p.mountId -or $p.masterId -cne $p.riderId -or $p.riderInState -ne $true -or $p.mountInState -ne $true -or
            $p.riderIsPartyMember -ne $true -or $p.samePlayerPartyGroup -ne $true) {throw 'Native Mammoth ownership/profile is not exact and reciprocal.'}
    }
    if(($Before|ConvertTo-Json -Depth 10 -Compress) -cne ($After|ConvertTo-Json -Depth 10 -Compress)){throw 'Native Mammoth pair changed through the scenario.'}
}
function Assert-KmcNativeMammothProfile($Profile,$CommandProof) {
    if($Profile.status -cne 'PASS' -or $Profile.evidenceKind -cne 'chunk6a-native-mammoth-profile' -or
        $Profile.scenario -cnotin @('chunk6a-mammoth-mount-rt','chunk6a-mammoth-mount-tb') -or $Profile.schemaVersion -ne 1 -or @($Profile.errors).Count -ne 0) {throw 'Native Mammoth profile artifact incomplete.'}
    $o=$Profile.observations
    Assert-KmcNativeMammothPair $o.before $o.after
    if($o.before.riderId -cne $CommandProof.identity.casterId -or $o.before.mountId -cne $CommandProof.identity.targetId -or
        $CommandProof.mountId -cne $o.before.mountId -or $CommandProof.pass -ne $true) {throw 'Native Mammoth profile belongs to another positive command.'}
    if($o.restored -ne $true -or ($o.selectionBefore|ConvertTo-Json -Compress) -cne ($o.selectionAfter|ConvertTo-Json -Compress) -or
        $o.pauseBefore -ne $o.pauseAfter -or $o.turnBasedBefore -ne $o.turnBasedAfter -or $o.pairedBefore -ne $o.pairedAfter) {throw 'Native Mammoth outer fixture restoration differs.'}
    if($o.childScenario -cne $Profile.scenario){throw 'Native Mammoth child transaction differs.'}
    foreach($row in @('CM01-exploration-free','CM01-combat-mount-accepted','CM02-approach-arrival','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations')){
        $match=@($o.childRows|Where-Object name -CEQ $row)
        if($match.Count -ne 1 -or $match[0].status -cne 'PASS'){throw 'Native Mammoth child omitted a mandatory exact window.'}
    }
}

function Assert-KmcNativeMammothArtifact {
    param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Manifest,[AllowNull()][string]$Status)
    $leaf='chunk6a-native-mammoth-profile.json';$kind='chunk6a-native-mammoth-profile'
    $records=@($Manifest.artifacts|Where-Object { $_.relativePath -ceq $leaf -or $_.kind -ceq $kind })
    if($Request.scenario -cnotin @('chunk6a-mammoth-mount-rt','chunk6a-mammoth-mount-tb','chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb','chunk6d-staged-rt','chunk6d-staged-tb','chunk6e-reaction-rt','chunk6e-reaction-tb')) {
        if($records.Count -ne 0){throw 'Another scenario manifested a native Mammoth profile.'}
        return
    }
    if($records.Count -eq 0 -and $Status -cne 'PASS'){return}
    if($records.Count -ne 1){throw 'Native Mammoth scenario requires exactly one profile record.'}
    function Read-ProfileArtifact([string]$Relative,[string]$Kind) {
        $r=@($Manifest.artifacts|Where-Object {$_.relativePath -ceq $Relative -or $_.kind -ceq $Kind})
        if($r.Count -ne 1 -or $r[0].relativePath -cne $Relative -or $r[0].kind -cne $Kind){throw 'Native Mammoth artifact manifest record differs.'}
        Assert-KmcExactProperties $r[0] @('relativePath','kind','length','sha256') 'Native Mammoth artifact record'
        $root=[IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
        $path=Assert-KmcChildPath (Join-Path $root $Relative) $root 'Native Mammoth artifact'
        Assert-KmcNotReparsePoint $path 'Native Mammoth artifact'
        Assert-KmcNotHardLink $path 'Native Mammoth artifact'
        $f=Get-Item -LiteralPath $path -Force
        if($f.Length -ne [long]$r[0].length -or [string]$r[0].sha256 -cnotmatch '^[0-9a-f]{64}$' -or (Get-KmcSha256 $path) -cne $r[0].sha256){throw 'Native Mammoth artifact differs from its immutable manifest.'}
        $a=Read-KmcJson $path
        $after=Get-Item -LiteralPath $path -Force
        if($f.Length -ne $after.Length -or $f.LastWriteTimeUtc.Ticks -ne $after.LastWriteTimeUtc.Ticks -or (Get-KmcSha256 $path) -cne $r[0].sha256){throw 'Native Mammoth artifact changed during validation.'}
        foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){
            if($a.$field -isnot [string] -or $a.$field -cne $Request.$field){throw ('Native Mammoth artifact payload identity differs: '+$field)}
        }
        if($a.evidenceKind -cne $Kind -or $a.status -cnotin @('PASS','FAIL')){throw 'Native Mammoth artifact kind/status invalid.'}
        return $a
    }
    $profile=Read-ProfileArtifact $leaf $kind
    Assert-KmcExactProperties $profile @('schemaVersion','evidenceKind','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','createdAtUtc','status','observations','errors') 'Native Mammoth profile'
    $date=[DateTimeOffset]::MinValue
    if(-not(Test-KmcExactJsonInteger $profile.schemaVersion) -or $profile.schemaVersion -ne 1 -or $profile.errors -isnot [Array] -or
        $profile.createdAtUtc -isnot [string] -or -not[DateTimeOffset]::TryParse($profile.createdAtUtc,[ref]$date)){throw 'Native Mammoth profile schema/timestamp/errors differs.'}
    if($Status -ceq 'PASS' -and $profile.status -cne 'PASS'){throw 'PASS run has a failed native Mammoth profile.'}
    if($profile.status -cne 'PASS'){return}
    $child=Read-ProfileArtifact 'phase3d-horse-scenario-evidence.json' 'phase3d-horse-scenario-evidence'
    if($Request.scenario -cin @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb','chunk6d-staged-rt','chunk6d-staged-tb','chunk6e-reaction-rt','chunk6e-reaction-tb')) {
        # The casting-fixture families (6C, 6D staged, 6E reaction) share the Mammoth-engine outer
        # profile; each child artifact carries its own schema, observation key and dedicated validator.
        $family=if($Request.scenario -clike 'chunk6d-*'){@{schema=45;key='chunk6dStaged';validator='Assert-KmcChunk6dStagedEvidence'}}elseif($Request.scenario -clike 'chunk6e-*'){@{schema=46;key='chunk6eReaction';validator='Assert-KmcChunk6eReactionEvidence'}}else{@{schema=44;key='chunk6cCasting';validator='Assert-KmcChunk6cCastingEvidence'}}
        if($child.status -cne 'PASS' -or $child.schemaVersion -ne$family.schema -or $profile.status-cne'PASS' -or @($profile.errors).Count-ne0){throw 'Native casting profile is incomplete.'}
        $o=$profile.observations
        Assert-KmcNativeMammothPair $o.before $o.after
        $childObservation=$child.observations.($family.key)
        if($null-eq$childObservation-or$o.before.riderId-cne$childObservation.rider-or$o.before.mountId-cne$childObservation.mount-or$o.childScenario-cne$Request.scenario){throw 'Native casting original pair binding differs.'}
        if($o.restored-ne$true-or($o.selectionBefore|ConvertTo-Json -Compress)-cne($o.selectionAfter|ConvertTo-Json -Compress)-or$o.pauseBefore-ne$o.pauseAfter-or$o.turnBasedBefore-ne$o.turnBasedAfter-or$o.pairedBefore-ne$o.pairedAfter){throw 'Native casting outer fixture was not restored.'}
        & $family.validator -Request $Request -Artifact $child -Status $Status
        return
    }
    if($child.status -cne 'PASS' -or $child.schemaVersion -ne 30){throw 'Native Mammoth profile depends on an incomplete command artifact.'}
    $proofs=@($child.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')
    if($proofs.Count -ne 1){throw 'Native Mammoth profile needs one exact positive Mount.'}
    Assert-KmcNativeMammothProfile $profile $proofs[0]
    Assert-KmcChunk6aCombatMountEvidence -Request $Request -Artifact $child -Status 'PASS'
}
