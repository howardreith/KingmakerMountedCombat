# Producer selection only. Completed-source lineage has its separate three-field reader.
Set-StrictMode -Version Latest
function Read-KmcChunk7AssistedPreparationSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'assisted preparation selection';Assert-KmcNotReparsePoint $path 'assisted preparation selection';Assert-KmcNotHardLink $path 'assisted preparation selection'
 $file=Get-Item -LiteralPath $path;if($file.Length-le0-or$file.Length-gt16KB){throw 'Assisted preparation selection is empty or oversized.'}
 $sha=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'assisted preparation selection';$selection=Read-KmcJson $path
 Assert-KmcChunk7AssistedFixtureSelection ([pscustomobject]@{scenario='chunk7-assisted-fixture-preparation';runId=$RunId;assistedFixture=$selection})
 if((Get-KmcSha256 $path)-cne$sha-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks){throw 'Assisted preparation selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$sha;selection=$selection}
}
