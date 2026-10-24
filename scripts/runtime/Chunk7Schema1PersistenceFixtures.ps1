Set-StrictMode -Version Latest
# Only the existing P06 qualifier/deriver reads or changes archive members.
# This adapter binds their immutable JSON receipt to the new cold request.
function Assert-KmcChunk7Schema1Derivation($Receipt,$Descriptor,[string]$SourceRunId,[string]$SourceSha256) {
    Assert-KmcExactProperties $Receipt @('schemaVersion','purpose','case','sourceRun','sourceSha256','foreignSourceRun','foreignSha256','descriptor','nativeMembersVerified','members') 'schema-1 derivation receipt'
    if(-not(Test-KmcExactJsonInteger $Receipt.schemaVersion)-or$Receipt.schemaVersion-ne1-or
       $Receipt.purpose-cne'P06 offline metadata-only validation copy'-or$Receipt.case-cne'schema1'-or
       $Receipt.sourceRun-cne$SourceRunId-or$Receipt.sourceSha256-cne$SourceSha256-or
       $SourceRunId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'-or$SourceRunId-cin@('.','..')-or$SourceSha256-cnotmatch'^[0-9a-f]{64}$'-or
       $null-ne$Receipt.foreignSourceRun-or$null-ne$Receipt.foreignSha256-or
       -not(Test-KmcExactJsonInteger $Receipt.nativeMembersVerified)-or$Receipt.nativeMembersVerified-lt1-or$Receipt.nativeMembersVerified-gt1023-or
       (ConvertTo-KmcChunk7Canonical $Receipt.descriptor)-cne(ConvertTo-KmcChunk7Canonical $Descriptor)-or
       $Descriptor.fileName-cne'Manual_812_KMC_P06.zks'-or$Descriptor.internalName-cne'KMC_P01'-or$Descriptor.sha256-ceq$SourceSha256) {
        throw 'Schema-1 derivative was not produced by the exact unchanged P06 metadata-only contract.'
    }
    $members=@($Receipt.members.PSObject.Properties)
    if($members.Count-ne($Receipt.nativeMembersVerified+1)-or@($members|Where-Object Name -CEQ 'kmc-mounted-state').Count-ne1-or
       @($members|Where-Object Name -CEQ 'header.json').Count-ne1){throw 'Schema-1 native member receipt is incomplete.'}
    foreach($member in $members){if($member.Value-isnot[string]-or$member.Value-cnotmatch'^[0-9a-f]{64}$'){throw 'Schema-1 member hash is not exact.'}}
}
function New-KmcChunk7Schema1Input([string]$SourceRunId,[string]$ExpectedSha256,$Fixture) {
    $source=Get-KmcPersistenceValidationSource $SourceRunId $ExpectedSha256 $Fixture -Case schema1
    $variant=New-KmcPersistenceValidationCopy $SourceRunId $ExpectedSha256 $Fixture -Case schema1
    $receiptPath=Join-Path (Split-Path -Parent $variant.path) 'owner.json'
    Assert-KmcNotReparsePoint $receiptPath 'schema-1 derivation receipt';Assert-KmcNotHardLink $receiptPath 'schema-1 derivation receipt'
    Assert-KmcManifestJsonMembersUnique $receiptPath 'schema-1 derivation receipt'
    $receipt=Read-KmcJson $receiptPath
    Assert-KmcChunk7Schema1Derivation $receipt $variant.descriptor $SourceRunId $ExpectedSha256
    if((Get-KmcSha256 $source.path)-cne$ExpectedSha256-or(Get-KmcSha256 $variant.path)-cne$variant.descriptor.sha256){throw 'Qualified schema-1 input changed after derivation.'}
    return [pscustomobject]@{path=$variant.path;descriptor=$variant.descriptor;receiptPath=$receiptPath;receiptSha256=(Get-KmcSha256 $receiptPath)}
}
function Assert-KmcChunk7Schema1ColdSource($SourceRunId,$Request,$ColdArtifact) {
    Assert-KmcChunk7Schema1Request $Request
    $lab=Get-KmcLabRoot
    $profile=Assert-KmcChildPath (Join-Path $lab ('runtime-staging/persistence-'+$Request.runId)) (Join-Path $lab 'runtime-staging') 'schema-1 isolated profile'
    $path=Join-Path $profile 'validation-copy.json'
    Assert-KmcNotReparsePoint $path 'schema-1 derivation receipt';Assert-KmcNotHardLink $path 'schema-1 derivation receipt'
    Assert-KmcManifestJsonMembersUnique $path 'schema-1 derivation receipt'
    if((Get-KmcSha256 $path)-cne$Request.schema1DerivationSha256){throw 'Request-bound schema-1 derivation receipt changed.'}
    $receipt=Read-KmcJson $path
    Assert-KmcChunk7Schema1Derivation $receipt $Request.persistenceLoad $SourceRunId $receipt.sourceSha256
    # Reuses the same historical qualifier, including completed native evidence,
    # restoration receipts and the exact original archive identity.
    $source=Get-KmcPersistenceValidationSource $SourceRunId $receipt.sourceSha256 $Request.fixture -Case schema1
    if((Get-KmcSha256 $source.path)-cne$receipt.sourceSha256){throw 'Original qualified source changed during schema-1 migration proof.'}
    $input=Join-Path (Join-Path $profile 'Saved Games') $Request.persistenceLoad.fileName
    Assert-KmcNotReparsePoint $input 'schema-1 input';Assert-KmcNotHardLink $input 'schema-1 input'
    if((Get-KmcSha256 $input)-cne$Request.persistenceLoad.sha256-or(Get-Item -LiteralPath $input).Length-ne$Request.persistenceLoad.length-or
       $ColdArtifact.observations.schema1Migration.derivationReceiptSha256-cne$Request.schema1DerivationSha256){throw 'Native schema-1 input or provenance changed during the isolated process.'}
}
