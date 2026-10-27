Set-StrictMode -Version Latest

# Test-only projection. The frozen Auto fixtures remain untouched and cannot be
# presented as native Manual evidence. This builds a synthetic Manual receipt for
# the new reader path from the corresponding Manual-derived test fixture.
function Convert-KmcSyntheticCumulativeManualFixture($Fixture, [string]$FixtureDirectory, [string]$Name, [bool]$OwnerSeed = $false) {
    $serialized = $Fixture | ConvertTo-Json -Depth 100 -Compress
    $Fixture = $serialized.Replace('Manual_7_KMC_C7_ACQUIRED_FIELD.zks', $(if($OwnerSeed){'Manual_305_KMC_C7_ACQUIRED_FIELD.zks'}else{'Manual_302_KMC_C7_ACQUIRED_FIELD.zks'})) | ConvertFrom-Json
    $manual = [IO.File]::ReadAllText((Join-Path $FixtureDirectory ('ManualDerived' + $Name + 'Fixture.json'))) | ConvertFrom-Json
    $o = $Fixture.artifact.observations
    $m = $manual.artifact.observations
    $leaf = if($OwnerSeed){'Manual_306_KMC_C7_MANUAL_DERIVED_FIELD.zks'}else{'Manual_303_KMC_C7_MANUAL_DERIVED_FIELD.zks'}
    $root = $o.beforeLoad.authority.root
    $manualRoot = $m.beforeLoad.authority.root
    $manualLeaf = $m.nativeWrite.leaf
    $Fixture.request.cumulativeAcquisition.nativeWriteLeaf = $leaf
    $o.selection.nativeWriteLeaf = $leaf
    if($OwnerSeed){
        $Fixture.request.cumulativeAcquisition | Add-Member -NotePropertyName sourceMode -NotePropertyValue 'owner-seed-one-owner'
        $o.selection | Add-Member -NotePropertyName sourceMode -NotePropertyValue 'owner-seed-one-owner'
    }
    $o.PSObject.Properties.Remove('nativeAcquisition')
    $o.slotProjection.enabledForTerminalAuto = $false

    function Copy-ManualValue($Value) {
        $json = $Value | ConvertTo-Json -Depth 100 -Compress
        $json = $json.Replace($manualRoot.Replace('\', '\\'), $root.Replace('\', '\\'))
        $json = $json.Replace($manualLeaf, $leaf)
        $json | ConvertFrom-Json
    }
    foreach ($key in @('nativeNumbering', 'saveRequested', 'nativeSave', 'nativeWrite', 'settledDescriptors', 'sourceDescriptorObject')) {
        $value = Copy-ManualValue $m.$key
        if ($null -eq $o.PSObject.Properties[$key]) { $o | Add-Member -NotePropertyName $key -NotePropertyValue $value }
        else { $o.$key = $value }
    }
    $o.nativeNumbering.number = if($OwnerSeed){306}else{303}
    $o.nativeNumbering.sourceDescriptors = @($o.fixtureReadiness.isolatedSaveDescriptors.descriptors | ForEach-Object { $_ | ConvertTo-Json -Depth 100 | ConvertFrom-Json })
    $o.nativeWrite.fileName = $leaf
    $o.settledDescriptors[0].fileName = $Fixture.request.persistenceLoad.fileName
    $o.settledDescriptors[0].internalName = $Fixture.request.persistenceLoad.internalName
    $o.nativeLoad.identities.ids = @($o.nativeLoad.identities.ids) + @(301, 302, 303, 304, 305, 306)
    $o.nativeLoad.identities.retainedCount = $o.nativeLoad.identities.ids.Count

    $saved = $o.settledWorld | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $o | Add-Member -NotePropertyName savedWorld -NotePropertyValue $saved
    $fieldWorld = $saved | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $acquired = @(
        [pscustomobject]@{id=$o.field.actorId;blueprint=@($o.field.profileInventory.party | Where-Object id -CEQ $o.field.actorId)[0].blueprintGuid;object=$o.field.actorObject;descriptor=$o.field.actorDescriptor;view=$o.field.actorView;isInGame=$true;isInState=$true;commandsEmpty=$true},
        [pscustomobject]@{id=$o.field.petId;blueprint=$o.field.horseBlueprint;object=$o.field.petObject;descriptor=$o.field.petDescriptor;view=$o.field.petView;isInGame=$true;isInState=$true;commandsEmpty=$true}
    )
    $fieldWorld.party = @($fieldWorld.party) + @($acquired | Where-Object { $id=$_.id; @($fieldWorld.party | Where-Object id -CEQ $id).Count -eq 0 })
    $o | Add-Member -NotePropertyName savedFieldWorld -NotePropertyValue $fieldWorld
    $arrangement = $o.afterChoices | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $owner = $m.afterSaveArrangement.acquired[0] | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $owner.actorId = $o.field.actorId
    $owner.blueprint = @($o.field.profileInventory.party | Where-Object id -CEQ $o.field.actorId)[0].blueprintGuid
    $owner.experience = $o.builderComplete.xp
    $owner.characterLevel = $o.builderComplete.level
    $pet = $m.afterSaveArrangement.acquired[1] | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    $pet.actorId = $o.field.petId
    $arrangement | Add-Member -NotePropertyName acquired -NotePropertyValue @($owner, $pet)
    $o | Add-Member -NotePropertyName beforeSaveArrangement -NotePropertyValue $arrangement
    $o | Add-Member -NotePropertyName afterSaveArrangement -NotePropertyValue ($arrangement | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
    return $Fixture
}
