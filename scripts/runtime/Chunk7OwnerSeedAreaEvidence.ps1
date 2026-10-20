# Closed owner-seed controlled cross-entry prerequisite. Navigation is disclosed
# assistance; actual native entry and AfterEntry Auto supply the measured boundary.
Set-StrictMode -Version Latest
function Assert-KmcChunk7OwnerSeedAreaEntryObservation($Request,$Value){
 Assert-KmcChunk7OwnerSeedAreaTarget $Request.ownedPairs $Request.persistenceCase $Request.persistenceAreaTarget
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)){throw 'Visited-area prerequisite belongs only to its owner-seed area producer.'}
 $return=Test-KmcChunk7ManticoreReturnArea $Request.ownedPairs
 $fields=@('contract','sourceArea','destinationArea','locationGuid','locationName','seen','revealed','explored','closed','entryGuid','entryAreaGuid','entryName','locationCountBefore','locationCountAfter','destinationIsCapital','existingLocationOnly','entryEqualsRegisteredBlueprint','navigationAssisted','ordinaryTravelProof','authoredExitProof','nativeMode','transferRequests','saveRequests')
 if($return){$fields+='sourceManualSha256'}
 Assert-KmcExactProperties $Value $fields 'owner-seed visited area entry'
 $source=if($return){'314e8c3b15b896a4f847c85f9250f02b'}else{'b10e86516b6437d45a629931c30936fe'}
 $destination=if($return){'b10e86516b6437d45a629931c30936fe'}else{'ead426a6c23d39548a670ee515d77df4'}
 $location=if($return){'1ee73aa52dab0264ca0b671d9a593c02'}else{'758559f44d15fc844bf30a10a83154d5'}
 $entry=if($return){'00becd5302e6d1d4a8ad93b6c612b927'}else{'fd225a93828d26449803a4847947f8c1'}
 foreach($spec in @(@('contract',$(if($return){'manticore-visited-spider-return-v1'}else{'owner-seed-visited-area-entry-v1'})),@('sourceArea',$source),@('destinationArea',$destination),@('locationGuid',$location),@('entryGuid',$entry),@('entryAreaGuid',$destination),@('nativeMode','AfterEntry'))){Assert-KmcBootstrapInventoryString $Value.($spec[0]) $spec[1]}
 if($return){Assert-KmcBootstrapInventoryString $Value.sourceManualSha256 $Request.ownedPairs.sourceArchiveSha256}
 foreach($field in @('locationName','entryName')){if($Value.$field-isnot[string]-or[string]::IsNullOrWhiteSpace($Value.$field)-or$Value.$field.Length-gt256-or$Value.$field-match'[\x00-\x1f\x7f]'){throw 'Visited stock entry names must be bounded native observations.'}}
 foreach($field in @('seen','revealed','explored','existingLocationOnly','entryEqualsRegisteredBlueprint','navigationAssisted')){Assert-KmcChunk7Boolean $Value.$field $true}
 foreach($field in @('closed','destinationIsCapital','ordinaryTravelProof','authoredExitProof')){Assert-KmcChunk7Boolean $Value.$field $false}
 Assert-KmcOrdinaryInteger $Value.locationCountBefore 1 4096;Assert-KmcOrdinaryInteger $Value.locationCountAfter $Value.locationCountBefore $Value.locationCountBefore
 foreach($field in @('transferRequests','saveRequests')){Assert-KmcOrdinaryInteger $Value.$field 0 0}
}
