Set-StrictMode -Version Latest
function Read-KmcChunk7ManualDerivedSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Manual-derived selection';Assert-KmcNotReparsePoint $path 'Manual-derived selection';Assert-KmcNotHardLink $path 'Manual-derived selection'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt32KB){throw 'Manual-derived selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Manual-derived selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7ManualDerivedSelection ([pscustomobject]@{scenario='chunk7-manual-derived-acquisition';runId=$RunId;manualDerived=$selection})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Manual-derived selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
function Get-KmcChunk7ManualDerivedSource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7ManualDerivedSelection ([pscustomobject]@{scenario='chunk7-manual-derived-acquisition';runId='unassigned-manual-derived';manualDerived=$Selection})
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'qualified Manual profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified Manual profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'qualified Manual evidence';Assert-KmcNotReparsePoint $root 'qualified Manual evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-manual-seed-evidence.json')){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified Manual receipt';Assert-KmcNotHardLink $path 'qualified Manual receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified Manual receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified Manual owner';Assert-KmcNotHardLink $ownerPath 'qualified Manual owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified Manual owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-bootstrap-manual-seed';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 Assert-KmcChunk7ManualSeedDescriptor $request
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 # Complete bootstrap and cold source qualification is repeated unchanged. Decoded
 # metadata gives no load/write/restore authority; the separate runtime owns that.
 $lineage=Get-KmcChunk7ManualSeedSource $request.manualSeed $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $lineage.descriptor)){throw 'Manual source did not load the exact qualified bootstrap descriptor.'}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $request.manualSeed.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Manual source owner unexpectedly selects another persistence case.'}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Manual-derived acquisition requires an error-free completed Manual seed.'}
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
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7ManualSeedArtifact $request $manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-manual-seed-evidence.json' 'chunk7-manual-seed-evidence'
 $artifactPath=Join-Path $root 'chunk7-manual-seed-evidence.json';$file=Get-Item -LiteralPath $artifactPath
 if($file.Length-le0-or$file.Length-gt16MB-or$files[$artifactPath]-cne$Selection.sourceArtifactSha256){throw 'Caller-pinned successful Manual seed differs.'}
 Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
 Assert-KmcChunk7ManualSeedOutput $request $artifact;Assert-KmcManualDerivedSourcePins $Selection $artifact.observations
 $w=$artifact.observations.nativeWrite
 $descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcChunk7ManualDerivedDescriptor ([pscustomobject]@{scenario='chunk7-manual-derived-acquisition';runId='unassigned-manual-derived';manualDerived=$Selection;persistenceLoad=$descriptor;fixture=$Fixture})
 foreach($path in $files.Keys){if((Get-KmcSha256 $path)-cne$files[$path]){throw 'Manual qualification receipt changed during intake.'}}
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Manual source bytes changed during qualification.'}
 [pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
  bootstrapRunId=$request.manualSeed.sourceRunId;coldInventoryRunId=$request.manualSeed.inventoryRunId;manualRunId=$request.runId}
}
function Copy-KmcChunk7ManualDerivedSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'Manual-derived profile';Assert-KmcDirectoryTreeCloneable $run 'Manual-derived profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-manual-derived-acquisition'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Manual-derived copy lacks its exact locked profile.'}
 $destination=Join-Path $run 'manual-derived-source.json';if(Test-Path -LiteralPath $destination){throw 'Manual-derived lineage destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'Manual-derived lineage';Assert-KmcNotHardLink $Source.artifactPath 'Manual-derived lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Manual-derived source receipt changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Manual-derived lineage changed during locked copy.'}
}
function Assert-KmcChunk7ManualDerivedOutput($Request,$Artifact){
 $w=$Artifact.observations.nativeWrite;$root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $Request.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$Request.runId)) 'Saved Games'
 $path=Assert-KmcChildPath $w.path $root 'native Manual-derived output';Assert-KmcBootstrapInventoryString $path (Join-Path $root 'Auto_1.zks')
 Assert-KmcNotReparsePoint $path 'native Manual-derived output';Assert-KmcNotHardLink $path 'native Manual-derived output';$file=Get-Item -LiteralPath $path
 if($file.Length-ne$w.length-or$file.LastWriteTimeUtc.Ticks-ne$w.lastWriteTimeUtcTicks-or(Get-KmcSha256 $path)-cne$w.sha256){throw 'Native terminal Auto output differs from its settled receipt.'}
 $input=Join-Path $root $Request.persistenceLoad.fileName;Assert-KmcNotReparsePoint $input 'immutable Manual input';Assert-KmcNotHardLink $input 'immutable Manual input'
 if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length){throw 'Manual-derived acquisition changed its immutable input.'}
}
