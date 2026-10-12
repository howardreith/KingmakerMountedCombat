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
