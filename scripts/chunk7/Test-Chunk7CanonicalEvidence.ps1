$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7PersistenceEvidence.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){&$Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Different([string]$Name,[string]$A,[string]$B){Check $Name {if((ConvertTo-KmcChunk7Canonical (ConvertFrom-Json $A))-ceq(ConvertTo-KmcChunk7Canonical (ConvertFrom-Json $B))){throw 'Unequal JSON values compared equal.'}}}
function Reject([string]$Name,[scriptblock]$Body){$rejected=$false;try{&$Body}catch{$rejected=$true};Check $Name {if(-not$rejected){throw 'Invalid JSON value accepted.'}}}
Different 'equal-length scalar substitution' '"actor-a"' '"actor-b"'
Different 'equal-length scalar array substitution' '["a","b"]' '["b","b"]'
Different 'actor order changes' '["aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa","bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"]' '["bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb","aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"]'
Different 'nested owner actor substitution' '{"owners":[{"actor":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"}]}' '{"owners":[{"actor":"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"}]}'
Different 'numeric debt substitution' '{"debt":[1.25,4]}' '{"debt":[1.75,4]}'
Different 'boolean substitution' '{"prepared":true}' '{"prepared":false}'
Different 'string versus number' '{"id":"12"}' '{"id":12}'
Different 'empty string versus null' '{"id":""}' '{"id":null}'
Different 'null array element versus absent' '{"actors":[null]}' '{"actors":[]}'
Different 'empty object versus empty array' '{"a":{}}' '{"a":[]}'
Different 'escaped text differs' '{"a":"line\nnext"}' '{"a":"line next"}'
Check 'property order canonicalized recursively' {$a=ConvertFrom-Json '{"z":[{"b":2,"a":"x"}],"a":true}';$b=ConvertFrom-Json '{"a":true,"z":[{"a":"x","b":2}]}';if((ConvertTo-KmcChunk7Canonical $a)-cne(ConvertTo-KmcChunk7Canonical $b)){throw 'Object property order changed semantic comparison.'}}
Check 'dictionary equals parsed JSON object' {$a=[ordered]@{z=@('a','b');a=1};$b=ConvertFrom-Json '{"a":1,"z":["a","b"]}';if((ConvertTo-KmcChunk7Canonical $a)-cne(ConvertTo-KmcChunk7Canonical $b)){throw 'Dictionary and JSON representations differ.'}}
Check 'wrapped JSON primitive values remain their exact values' {$a=ConvertFrom-Json '{"actor":"abcdef","debt":1.5,"prepared":false}';if((ConvertTo-KmcChunk7Canonical $a)-cne'{"actor":"abcdef","debt":1.5,"prepared":false}'){throw 'Wrapped scalar value expanded its .NET properties.'}}
Reject 'NaN refused' {ConvertTo-KmcChunk7Canonical ([double]::NaN)}
Reject 'infinity refused' {ConvertTo-KmcChunk7Canonical ([double]::PositiveInfinity)}
Reject 'native object refused' {ConvertTo-KmcChunk7Canonical ([DateTime]::UtcNow)}
Reject 'nonstring dictionary key refused' {ConvertTo-KmcChunk7Canonical @{1='a'}}
Reject 'non-JSON multidimensional array refused' {ConvertTo-KmcChunk7Canonical ([Array]::CreateInstance([int],2,2))}
Reject 'cycle exceeds bound instead of hanging' {$a=[pscustomobject]@{self=$null};$a.self=$a;ConvertTo-KmcChunk7Canonical $a}
Write-Host ('CHUNK7 CANONICAL PASS='+$passed+' FAIL=0; semantic reader regressions only; historical comparator unchanged')
