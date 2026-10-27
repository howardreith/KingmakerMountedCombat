# The owner-seed legacy checkpoint starts as a genuine current native write.
# Only its closed historical cold consumer admits a receipted metadata copy.
Set-StrictMode -Version Latest
function Test-KmcOwnedSchema2ColdRequest($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 (Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)-and$null-ne$Request.PSObject.Properties['persistenceCase']-and$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual'
}
function Assert-KmcOwnedSchema2Request($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 $has=$null-ne$Request.PSObject.Properties['schema2DerivationSha256']
 if(Test-KmcOwnedSchema2ColdRequest $Request){
  Assert-KmcChunk7OwnedPairsSelection $Request
  if(-not$has-or$Request.schema2DerivationSha256-isnot[string]-or$Request.schema2DerivationSha256-cnotmatch'\A[a-f0-9]{64}\z'){throw 'Owner-seed schema2 cold input requires its exact metadata-copy receipt.'}
  if($null-ne$Request.PSObject.Properties['schema1DerivationSha256']){throw 'Owner-seed schema2 input cannot select a schema1 derivative.'}
 }elseif($has){throw 'Schema2 derivation belongs only to the owner-seed pending historical cold consumer.'}
}
function ConvertTo-KmcOwnedSchema2Metadata($Archive,$Request){
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)-and-not(Test-KmcOwnedSchema2ColdRequest $Request)){throw 'Schema2 projection requires its exact owner-seed source/cold route.'}
 if($Request.persistenceCase-cne'legacy-mounted-pending-tb-manual'){throw 'Schema2 projection cannot select another checkpoint.'}
 Assert-KmcChunk7CombatArchive $Archive $Request
 $pair=$Archive.Pairs[0];$part=$Archive.Participations[0]
 Assert-KmcBootstrapInventoryString $pair.ProfileId 'medium-humanoid-horse-v1'
 foreach($slot in $Archive.Slots){Assert-KmcOrdinaryInteger $slot.Kind 1 4}
 # No source object is mutated. The unchanged historical decoder will derive
 # its own owner id/generation; all native identity, debt and pending state stay.
 $combat=$Archive.Combat|ConvertTo-Json -Depth 64 -Compress|ConvertFrom-Json
 $combat.Paired=$part.State|ConvertTo-Json -Depth 32 -Compress|ConvertFrom-Json
 [pscustomobject][ordered]@{SchemaVersion=2;CampaignId=$Archive.CampaignId;AreaId=$Archive.AreaId;GameTimeTicks=$Archive.GameTimeTicks;Policy=$Archive.Policy;RulesId=$Archive.RulesId;Mounted=$true;ProfileId=$pair.ProfileId;Rider=$pair.Rider;Mount=$pair.Mount;Slots=$Archive.Slots;Combat=$combat}
}
function ConvertFrom-KmcOwnedSchema2Metadata($Metadata,$Request){
 Assert-KmcOwnedSchema1Object $Metadata @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots','Combat') 'owned historical schema2 metadata'
 Assert-KmcOrdinaryInteger $Metadata.SchemaVersion 2 2;Assert-KmcChunk7Boolean $Metadata.Mounted $true
 $owner=Get-KmcChunk7Schema1Owner $Metadata
 $combat=$Metadata.Combat|ConvertTo-Json -Depth 64 -Compress|ConvertFrom-Json;$combat.Paired=$null
 $state=$Metadata.Combat.Paired|ConvertTo-Json -Depth 32 -Compress|ConvertFrom-Json
 $result=[pscustomobject][ordered]@{SchemaVersion=3;CampaignId=$Metadata.CampaignId;AreaId=$Metadata.AreaId;GameTimeTicks=$Metadata.GameTimeTicks;Policy=$Metadata.Policy;RulesId=$Metadata.RulesId;Pairs=@([pscustomobject][ordered]@{PairId=$owner;Generation=1;ProfileId=$Metadata.ProfileId;Rider=$Metadata.Rider;Mount=$Metadata.Mount});Participations=@([pscustomobject][ordered]@{OwnerId=$owner;Generation=1;MountedPairId=$owner;State=$state});Slots=$Metadata.Slots;Combat=$combat}
 Assert-KmcChunk7CombatArchive $result $Request;Assert-KmcChunk7LegacyPendingMigration $Metadata $result
 $result
}
function Assert-KmcOwnedSchema2Migration($Request,$Observations){
 Assert-KmcOwnedSchema2Request $Request
 if(-not(Test-KmcOwnedSchema2ColdRequest $Request)){throw 'Synthetic schema2 evidence is restricted to its closed historical cold consumer.'}
 $m=$Observations.schema2Migration
 Assert-KmcChunk7Boolean $m.syntheticMetadata $true
 Assert-KmcBootstrapInventoryString $m.derivationReceiptSha256 $Request.schema2DerivationSha256
 if($m.originalNativeArchiveSha256-isnot[string]-or$m.originalNativeArchiveSha256-cnotmatch'\A[a-f0-9]{64}\z'-or$m.originalNativeArchiveSha256-ceq$Request.persistenceLoad.sha256){throw 'Schema2 derivative must identify its distinct genuine native schema3 source.'}
 Assert-KmcChunk7LegacyPendingMigration $m.nativeInputMetadata $Observations.loadedArchive
}
