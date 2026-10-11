[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Runs record consistency against the actual immutable artifacts, never completion.
. (Join-Path $PSScriptRoot 'Test-Chunk6aLedger.ps1')
$literalChecks=0
foreach($case in @(
    @{expected='failure {"path":[1,2],"inFlight":"<none>"}';errors=@('failure {"path":[1,2],"inFlight":"<none>"}');pass=$true},
    @{expected='rider [x]';errors=@('rider x');pass=$false},
    @{expected='rider [x]';errors=@('prefix rider [x] suffix');pass=$true},
    @{expected='cost *';errors=@('cost 3');pass=$false},
    @{expected='cost ?';errors=@('cost 3');pass=$false},
    @{expected='Exact';errors=@('exact');pass=$false},
    @{expected='';errors=@('anything');pass=$false}
)) {
    if((Test-KmcLiteralAssertion $case.expected $case.errors) -ne $case.pass){throw 'Failure assertion matching is not literal ordinal text.'}
    $literalChecks++
}
Write-Host "CHUNK6A LITERAL ASSERTION PASS=$literalChecks FAIL=0; original structured assertions preserved"
