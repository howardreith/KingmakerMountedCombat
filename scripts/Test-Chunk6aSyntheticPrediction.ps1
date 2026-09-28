# Test-only schema projection. The caller has already read a private in-memory copy
# of an archived RT artifact. This is synthetic fixture data, never native evidence.
function Add-KmcSyntheticPredictionFields($Artifact) {
    foreach($proof in @($Artifact.observations.chunk6aCommandProofs)) {
        if($proof.resourceWindow.turnBased -eq $true){throw 'A synthetic TB fixture must declare its actual prediction samples.'}
        $proof|Add-Member -NotePropertyName predictionCommands -NotePropertyValue ([pscustomobject]@{contract='native-speculative-init-separated-from-one-committed-request';commands=@();pass=$true})
        foreach($sample in @($proof.samples)){$sample|Add-Member -NotePropertyName simulatingClick -NotePropertyValue $false}
    }
}
