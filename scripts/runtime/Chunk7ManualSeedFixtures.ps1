Set-StrictMode -Version Latest
function Read-KmcChunk7ManualSeedSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Manual seed selection';Assert-KmcNotReparsePoint $path 'Manual seed selection';Assert-KmcNotHardLink $path 'Manual seed selection'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt16KB){throw 'Manual seed selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Manual seed selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7ManualSeedSelection ([pscustomobject]@{scenario='chunk7-bootstrap-manual-seed';runId=$RunId;manualSeed=$selection})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Manual seed selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
function Get-KmcChunk7ManualSeedSource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7ManualSeedSelection ([pscustomobject]@{scenario='chunk7-bootstrap-manual-seed';runId='unassigned-manual-seed';manualSeed=$Selection})
 # Reuse the existing complete bootstrap qualification, including exact native
 # output metadata. This projection is input validation, never native authority.
 $bootstrapSelection=[pscustomobject]@{case='authored-readonly-inventory';sourceRunId=$Selection.sourceRunId;sourceArtifactSha256=$Selection.sourceArtifactSha256;sourceArchiveSha256=$Selection.sourceArchiveSha256;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $bootstrap=Get-KmcChunk7BootstrapInventorySource $bootstrapSelection $Fixture $Candidate
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.inventoryRunId)) (Join-Path $lab 'runtime-staging') 'qualified cold profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified cold profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.inventoryRunId)) (Join-Path $lab 'runtime-evidence') 'qualified cold evidence';Assert-KmcNotReparsePoint $root 'qualified cold evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json')){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified cold receipt';Assert-KmcNotHardLink $path 'qualified cold receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified cold receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified cold owner';Assert-KmcNotHardLink $ownerPath 'qualified cold owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified cold owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.inventoryRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-bootstrap-cold-inventory';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 Assert-KmcChunk7BootstrapInventoryDescriptor $request
 foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256')){Assert-KmcBootstrapInventoryString $request.bootstrapInventory.$key $Selection.$key}
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $bootstrap.descriptor)){throw 'Cold inventory did not load the exact qualified native bootstrap descriptor.'}
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $Selection.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Cold source owner unexpectedly selects another persistence case.'}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Manual seed requires an error-free completed cold inventory.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $files[(Join-Path $root 'runtime-artifacts.json')]
 }
 if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Cold inventory exact restoration identity is missing.'}
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 $files[(Join-Path $root 'runtime-game-result.json')]
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $true
 foreach($key in @('loadRequestCount','workingLoadRequestCount')){Assert-KmcOrdinaryInteger $game.$key 1 1}
 Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
 Assert-KmcReadOnlyArtifactManifest $request $files[(Join-Path $root 'runtime-artifacts.json')] -GameResult
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7BootstrapInventoryArtifact $request $manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-bootstrap-inventory-evidence.json' 'chunk7-bootstrap-inventory-evidence'
 $artifactPath=Join-Path $root 'chunk7-bootstrap-inventory-evidence.json';$file=Get-Item -LiteralPath $artifactPath
 if($file.Length-le0-or$file.Length-gt16MB-or(Get-KmcSha256 $artifactPath)-cne$Selection.inventoryArtifactSha256){throw 'Caller-pinned successful cold inventory differs.'}
 # No save/output descriptor is manufactured here. The only output eligibility
 # decision is observed native one-save mode; native numbering is rechecked later.
 Assert-KmcChunk7Boolean $artifact.observations.fixtureReadiness.saveSettings.onlyOneSave $false
 foreach($path in $files.Keys){if((Get-KmcSha256 $path)-cne$files[$path]){throw 'Cold qualification receipt changed during intake.'}}
 if((Get-KmcSha256 $artifactPath)-cne$Selection.inventoryArtifactSha256-or(Get-KmcSha256 $bootstrap.path)-cne$Selection.sourceArchiveSha256){throw 'Manual seed source changed during qualification.'}
 [pscustomobject]@{path=$bootstrap.path;descriptor=$bootstrap.descriptor;artifactPath=$bootstrap.artifactPath;artifactSha256=$bootstrap.artifactSha256;artifactLength=$bootstrap.artifactLength;
  inventoryPath=$artifactPath;inventorySha256=$Selection.inventoryArtifactSha256;inventoryLength=[long]$file.Length}
}
function Copy-KmcChunk7ManualSeedSourceArtifacts($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'Manual seed profile';Assert-KmcDirectoryTreeCloneable $run 'Manual seed profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-bootstrap-manual-seed'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Manual seed copy lacks its exact locked profile.'}
 foreach($copy in @(@($Source.artifactPath,$Source.artifactSha256,$Source.artifactLength,'manual-seed-bootstrap-source.json'),@($Source.inventoryPath,$Source.inventorySha256,$Source.inventoryLength,'manual-seed-cold-inventory.json'))){
  $destination=Join-Path $run $copy[3];if(Test-Path -LiteralPath $destination){throw 'Manual seed lineage destination already exists.'}
  Assert-KmcNotReparsePoint $copy[0] 'Manual seed lineage';Assert-KmcNotHardLink $copy[0] 'Manual seed lineage'
  if((Get-KmcSha256 $copy[0])-cne$copy[1]-or(Get-Item -LiteralPath $copy[0]).Length-ne$copy[2]){throw 'Manual seed lineage changed before locked copy.'}
  [IO.File]::Copy($copy[0],$destination,$false)
  if((Get-KmcSha256 $destination)-cne$copy[1]-or(Get-KmcSha256 $copy[0])-cne$copy[1]){throw 'Manual seed lineage changed during locked copy.'}
 }
}
function Assert-KmcChunk7ManualSeedOutput($Request,$Artifact){
 # Whole-file identity only. Native descriptor, scope, worker and callback evidence
 # authorizes this output claim; no archive members or action entitlement are read.
 $w=$Artifact.observations.nativeWrite;$root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 $path=Assert-KmcChildPath $w.path $root 'native Manual seed output';Assert-KmcBootstrapInventoryString $path (Join-Path $root 'Manual_1_KMC_C7_NATIVE_SEED.zks')
 Assert-KmcNotReparsePoint $path 'native Manual seed output';Assert-KmcNotHardLink $path 'native Manual seed output';$file=Get-Item -LiteralPath $path
 if($file.Length-ne$w.length-or$file.LastWriteTimeUtc.Ticks-ne$w.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$w.sha256){throw 'Native Manual output bytes differ from its settled exact worker receipt.'}
 $input=Join-Path $root $Request.persistenceLoad.fileName
 Assert-KmcNotReparsePoint $input 'immutable Manual seed input';Assert-KmcNotHardLink $input 'immutable Manual seed input'
 if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length){throw 'Manual seed changed its immutable native input.'}
}
