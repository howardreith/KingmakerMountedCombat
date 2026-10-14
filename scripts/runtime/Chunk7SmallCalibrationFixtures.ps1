Set-StrictMode -Version Latest
function Read-KmcChunk7SmallSelectionFile([string]$Path,[string]$RunId){
    $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Small explicit selection'
    Assert-KmcNotReparsePoint $path 'Small calibration selection';Assert-KmcNotHardLink $path 'Small calibration selection'
    $file=Get-Item -LiteralPath $path
    if($file.Length-le0-or$file.Length-gt16KB){throw 'Field selection is empty or oversized.'}
    $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Small calibration selection';$selection=Read-KmcJson $path
    Assert-KmcChunk7SmallSelection ([pscustomobject]@{scenario='chunk7-small-profile-calibration';runId=$RunId;smallCalibration=$selection})
    if((Get-KmcSha256 $path)-cne$hash-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Field selection changed during intake.'}
    [pscustomobject]@{path=$path;sha256=$hash;selection=$selection}
}

function Get-KmcChunk7SmallManualSource($Selection,$Fixture,$Candidate,[switch]$Assisted){
 Assert-KmcChunk7SmallManualSourceSelection $Selection 'unassigned-small-manual'
 $lab=Get-KmcLabRoot;$profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-staging') 'qualified Manual profile'
 Assert-KmcDirectoryTreeCloneable $profile 'qualified Manual profile'
 $root=Assert-KmcChildPath (Join-Path $lab ('runtime-evidence/'+$Selection.sourceRunId)) (Join-Path $lab 'runtime-evidence') 'qualified Manual evidence';Assert-KmcNotReparsePoint $root 'qualified Manual evidence';$files=@{}
 foreach($leaf in @('runtime-request.json','runtime-result.json','runtime-game-result.json','runtime-artifacts.json','base-game-isolation-result.json','chunk7-acquired-manual-evidence.json')){
  $path=Join-Path $root $leaf;Assert-KmcNotReparsePoint $path 'qualified Manual receipt';Assert-KmcNotHardLink $path 'qualified Manual receipt';Assert-KmcManifestJsonMembersUnique $path 'qualified Manual receipt';$files[$path]=Get-KmcSha256 $path
 }
 $ownerPath=Join-Path $profile 'owner.json';Assert-KmcNotReparsePoint $ownerPath 'qualified Manual owner';Assert-KmcNotHardLink $ownerPath 'qualified Manual owner';Assert-KmcManifestJsonMembersUnique $ownerPath 'qualified Manual owner';$files[$ownerPath]=Get-KmcSha256 $ownerPath
 $owner=Read-KmcJson $ownerPath;$request=Read-KmcJson (Join-Path $root 'runtime-request.json');$result=Read-KmcJson (Join-Path $root 'runtime-result.json');$game=Read-KmcJson (Join-Path $root 'runtime-game-result.json');$isolation=Read-KmcJson (Join-Path $root 'base-game-isolation-result.json')
 Assert-KmcBootstrapInventoryString $request.runId $Selection.sourceRunId;Assert-KmcBootstrapInventoryString $request.scenario 'chunk7-acquired-manual-derivative';Assert-KmcBootstrapInventoryString $request.evidenceRoot $root
 Assert-KmcChunk7AcquiredManualDescriptor $request
 Assert-KmcBootstrapInventoryString $request.acquiredManual.case $(if($Assisted){'preserve-two-acquired-field'}else{'preserve-acquired-field'})
 foreach($key in @('working','baseline')){Assert-KmcBootstrapInventoryString $request.fixture.$key.sha256 $Fixture.$key.sha256}
 foreach($key in @('commit','dllSha256','dllMvid')){Assert-KmcBootstrapInventoryString $request.$key $Candidate.$key}
 # Complete bootstrap and cold source qualification is repeated unchanged. Decoded
 # metadata gives no load/write/restore authority; the separate runtime owns that.
 $lineage=Get-KmcChunk7AcquiredManualSource $request.acquiredManual $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $request.persistenceLoad)-cne(ConvertTo-KmcChunk7Canonical $lineage.descriptor)){throw 'Manual source did not load the exact qualified bootstrap descriptor.'}
 foreach($key in @('runId','scenario','transactionToken')){Assert-KmcBootstrapInventoryString $owner.$key $request.$key}
 Assert-KmcBootstrapInventoryString $owner.sourceSha256 $request.acquiredManual.sourceArchiveSha256;Assert-KmcBootstrapInventoryString $owner.saveRoot (Join-Path $profile 'Saved Games')
 if($null-ne$owner.persistenceCase){throw 'Manual source owner unexpectedly selects another persistence case.'}
 foreach($receipt in @($result,$game)){
  foreach($key in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')){Assert-KmcBootstrapInventoryString $receipt.$key $request.$key}
  Assert-KmcBootstrapInventoryString $receipt.status 'PASS';if($receipt.errors-isnot[Array]-or$receipt.errors.Count-ne0){throw 'Small calibration requires an error-free completed acquired Manual.'}
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
 $manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json');Assert-KmcChunk7AcquiredManualArtifact $request $manifest 'PASS'
 $artifact=Read-KmcChunk7ManifestArtifact $request $manifest 'chunk7-acquired-manual-evidence.json' 'chunk7-acquired-manual-evidence'
 $artifactPath=Join-Path $root 'chunk7-acquired-manual-evidence.json';$file=Get-Item -LiteralPath $artifactPath
 if($file.Length-le0-or$file.Length-gt16MB-or$files[$artifactPath]-cne$Selection.sourceArtifactSha256){throw 'Caller-pinned successful acquired Manual differs.'}
 Assert-KmcBootstrapInventoryString $artifact.observations.nativeWrite.sha256 $Selection.sourceArchiveSha256
 Assert-KmcChunk7AcquiredManualOutput $request $artifact;Assert-KmcChunk7SmallManualSourceObservation $Selection $artifact.observations -Assisted:$Assisted
 $w=$artifact.observations.nativeWrite
 $descriptor=[pscustomobject]@{internalName=$w.internalName;fileName=$w.fileName;sha256=$w.sha256;length=$w.length;lastWriteTimeUtcTicks=$w.lastWriteTimeUtcTicks;gameId=$w.gameId;gameName=$w.gameName;area=$w.area}
 Assert-KmcChunk7SmallManualDescriptor ([pscustomobject]@{scenario='chunk7-small-profile-calibration';runId='unassigned-small-manual';smallCalibration=[pscustomobject]@{manualSource=$Selection};persistenceLoad=$descriptor;fixture=$Fixture})
 foreach($path in $files.Keys){if((Get-KmcSha256 $path)-cne$files[$path]){throw 'Manual qualification receipt changed during intake.'}}
 if((Get-KmcSha256 $w.path)-cne$Selection.sourceArchiveSha256){throw 'Manual source bytes changed during qualification.'}
 $qualified=[pscustomobject]@{path=$w.path;descriptor=$descriptor;artifactPath=$artifactPath;artifactSha256=$Selection.sourceArtifactSha256;artifactLength=[long]$file.Length;
  acquiredManualRunId=$request.runId;firstAcquisitionRunId=$request.acquiredManual.sourceRunId;bootstrapRunId=$lineage.bootstrapRunId;coldInventoryRunId=$lineage.coldInventoryRunId;manualRunId=$lineage.manualRunId}
 if($Assisted){foreach($key in @('sourceMode','assistedProducerRunId','assistedColdRunId','fixtureSetupWasAssisted')){Add-Member -InputObject $qualified -MemberType NoteProperty -Name $key -Value $lineage.$key}}
 $qualified
}
function Copy-KmcChunk7SmallManualSourceArtifact($Source,[string]$ProfileRoot,$Lock){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $run=Assert-KmcChildPath $ProfileRoot (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'Small Manual profile';Assert-KmcDirectoryTreeCloneable $run 'Small Manual profile';$owner=Read-KmcJson (Join-Path $run 'owner.json')
 foreach($pair in @(@('scenario','chunk7-small-profile-calibration'),@('runId',$Lock.RunId),@('transactionToken',$Lock.Token),@('sourceSha256',$Source.descriptor.sha256))){Assert-KmcBootstrapInventoryString $owner.($pair[0]) $pair[1]}
 if($run-cne(Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$owner.runId))){throw 'Small Manual copy lacks its exact locked profile.'}
 $destination=Join-Path $run 'small-acquired-manual-source.json';if(Test-Path -LiteralPath $destination){throw 'Small Manual source destination already exists.'}
 Assert-KmcNotReparsePoint $Source.artifactPath 'Small Manual lineage';Assert-KmcNotHardLink $Source.artifactPath 'Small Manual lineage'
 if((Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256-or(Get-Item -LiteralPath $Source.artifactPath).Length-ne$Source.artifactLength){throw 'Small Manual source changed before locked copy.'}
 [IO.File]::Copy($Source.artifactPath,$destination,$false)
 if((Get-KmcSha256 $destination)-cne$Source.artifactSha256-or(Get-KmcSha256 $Source.artifactPath)-cne$Source.artifactSha256){throw 'Small Manual source changed during locked copy.'}
}
