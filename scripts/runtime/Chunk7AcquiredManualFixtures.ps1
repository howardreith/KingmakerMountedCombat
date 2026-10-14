Set-StrictMode -Version Latest
function Read-KmcChunk7AcquiredManualSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Manual-derived selection';Assert-KmcNotReparsePoint $path 'Manual-derived selection';Assert-KmcNotHardLink $path 'Manual-derived selection'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt32KB){throw 'Manual-derived selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Manual-derived selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7AcquiredManualSelection ([pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId=$RunId;acquiredManual=$selection})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Manual-derived selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
function Get-KmcChunk7AcquiredManualSource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7AcquiredManualSelection ([pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId='unassigned-manual-derived';acquiredManual=$Selection})
 $two=$Selection.case-ceq'preserve-two-acquired-field';$sourceLeaf=if($two){'chunk7-cumulative-acquisition-evidence.json'}else{'chunk7-manual-derived-evidence.json'}
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'qualified Manual profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified Manual profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'qualified Manual evidence';Assert-KmcNotReparsePoint $root 'qualified Manual evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json',$sourceLeaf)){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified Manual receipt';Assert-KmcNotHardLink $path 'qualified Manual receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified Manual receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified Manual owner';Assert-KmcNotHardLink $ownerPath 'qualified Manual owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified Manual owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario $(if($two){'chunk7-cumulative-acquisition'}else{'chunk7-manual-derived-acquisition'});Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 if($two){Assert-KmcChunk7CumulativeDescriptor $request;$acquisition=$request.cumulativeAcquisition;Assert-KmcBootstrapInventoryString $acquisition.priorOwnerId $Selection.priorOwnerId;Assert-KmcBootstrapInventoryString $acquisition.priorPetId $Selection.priorPetId}else{Assert-KmcChunk7ManualDerivedDescriptor $request;$acquisition=$request.manualDerived}
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 # Complete bootstrap and cold source qualification is repeated unchanged. Decoded
 # metadata gives no load/write/restore authority; the separate runtime owns that.
 $lineage=if($two){Get-KmcChunk7CumulativeSource $acquisition $Fixture $Candidate}else{Get-KmcChunk7ManualDerivedSource $acquisition $Fixture $Candidate}
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $lineage.descriptor)){throw 'Acquisition source did not load the exact qualified Manual descriptor.'}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $acquisition.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Manual source owner unexpectedly selects another persistence case.'}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Manual-derived acquisition requires an error-free completed Manual-derived acquisition.'}
  Assert-KmcBootstrapInventoryString $receipt.evidenceManifestSha256 $files[(Join-Path $root 'runtime-artifacts.json')]
 }
 if($request.transactionToken-isnot[string]-or$request.transactionToken-cnotmatch'^[a-f0-9]{64}$'-or$result.restoredSaveInventoryDigest-isnot[string]-or$result.restoredSaveInventoryDigest-cnotmatch'^[a-f0-9]{64}$'){throw 'Manual source exact restoration identity is missing.'}
 Assert-KmcBootstrapInventoryString $result.gameResultSha256 $files[(Join-Path $root 'runtime-game-result.json')]
 foreach($key in @('modsRestored','workingRestored','baselineImmutable','saveProtectionPassed','saveWriteAllowlistPassed')){Assert-KmcChunk7Boolean $result.$key $true}
 foreach($key in @('assertionFailCount','subscenarioFailCount','baselineLoadRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 0 0}
 foreach($key in @('loadRequestCount','workingLoadRequestCount','saveRequestCount','workingSaveRequestCount')){Assert-KmcOrdinaryInteger $game.$key 1 1}
 Assert-KmcChunk7Boolean $game.fixtureIdentityVerified $true
 Assert-KmcBootstrapInventoryString $isolation.runId $request.runId;Assert-KmcBootstrapInventoryString $isolation.stagingMode 'base-game-only-kmc'
 foreach($key in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){Assert-KmcChunk7Boolean $isolation.$key $true}
 Assert-KmcReadOnlyArtifactManifest $request $files[(Join-Path $root 'runtime-artifacts.json')] -GameResult
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');if($two){Assert-KmcChunk7CumulativeArtifact $request $manifest 'PASS'}else{Assert-KmcChunk7ManualDerivedArtifact $request $manifest 'PASS'}
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest $sourceLeaf $(if($two){'chunk7-cumulative-acquisition-evidence'}else{'chunk7-manual-derived-evidence'})
 $artifactPath=Join-Path $root $sourceLeaf;$file=Get-Item -LiteralPath $artifactPath
 if($file.Length-le0-or$file.Length-gt16MB-or$files[$artifactPath]-cne$Selection.sourceArtifactSha256){throw 'Caller-pinned successful Manual-derived acquisition differs.'}
 Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
 if($two){Assert-KmcChunk7CumulativeOutput $request $artifact}else{Assert-KmcChunk7ManualDerivedOutput $request $artifact};Assert-KmcBootstrapInventoryString $artifact.observations.field.actorId $Selection.actorId;Assert-KmcBootstrapInventoryString $artifact.observations.field.petId $Selection.petId
 $w=$artifact.observations.nativeWrite
 $descriptor=[pscustomobject]@{internalName=$w.name;fileName=$w.leaf;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcChunk7AcquiredManualDescriptor ([pscustomobject]@{scenario='chunk7-acquired-manual-derivative';runId='unassigned-manual-derived';acquiredManual=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 foreach($path in $files.Keys){if((Get-KmcSha256 $path)-cne$files[$path]){throw 'Manual qualification receipt changed during intake.'}}
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Manual source bytes changed during qualification.'}
 $qualified=[pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
  bootstrapRunId=$lineage.bootstrapRunId;coldInventoryRunId=$lineage.coldInventoryRunId;manualRunId=$lineage.manualRunId;acquisitionRunId=$request.runId}
 if($two-and(Test-KmcAssistedCumulativeMode $acquisition)){
  foreach($key in @('sourceMode','assistedProducerRunId','assistedColdRunId','fixtureSetupWasAssisted')){Add-Member -InputObject $qualified -MemberType NoteProperty -Name $key -Value $lineage.$key}
 }
 $qualified
}
function Copy-KmcChunk7AcquiredManualSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'Manual-derived profile';Assert-KmcDirectoryTreeCloneable $run 'Manual-derived profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-acquired-manual-derivative'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Manual-derived copy lacks its exact locked profile.'}
 $destination=Join-Path $run 'acquired-manual-source.json';if(Test-Path -LiteralPath $destination){throw 'Manual-derived lineage destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'Manual-derived lineage';Assert-KmcNotHardLink $Source.artifactPath 'Manual-derived lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Manual-derived source receipt changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Manual-derived lineage changed during locked copy.'}
}
function Assert-KmcChunk7AcquiredManualOutput($Request,$Artifact){
 $w=$Artifact.observations.nativeWrite;$root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 Assert-KmcOrdinaryInteger $Artifact.observations.reservation.number 1 ([int]::MaxValue)
 $leaf='Manual_'+([int]$Artifact.observations.reservation.number).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_ACQUIRED_FIELD.zks'
 Assert-KmcBootstrapInventoryString $w.fileName $leaf
 $path=Assert-KmcChildPath $w.path $root 'native acquired Manual output';Assert-KmcBootstrapInventoryString $path (Join-Path $root $leaf)
 Assert-KmcNotReparsePoint $path 'native acquired Manual output';Assert-KmcNotHardLink $path 'native acquired Manual output';$file=Get-Item -LiteralPath $path
 if($file.Length-ne$w.length-or$file.LastWriteTimeUtc.Ticks-ne$w.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$w.sha256){throw 'Native cumulative Manual output differs from its settled receipt.'}
 $input=Join-Path $root $Request.persistenceLoad.fileName;Assert-KmcNotReparsePoint $input 'immutable acquired Auto input';Assert-KmcNotHardLink $input 'immutable acquired Auto input'
 if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length){throw 'Cumulative Manual derivative changed its immutable input.'}
}
