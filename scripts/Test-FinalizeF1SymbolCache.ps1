[CmdletBinding()] param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
function Entry([string]$Path,[string]$Hash,[long]$Length){[pscustomobject]@{kind='file';path=$Path;sha256=$Hash;length=$Length}}
$staged=@((Entry 'Scroll/Scroll.dll' ('a'*64) 100),(Entry 'Scroll/Scroll.pdb' ('b'*64) 20))
$baseRuntime=@($staged)+@((Entry 'Scroll/Scroll.dll.12.cache' ('a'*64) 100))
$symbol=Entry 'Scroll/Scroll.dll.12.cache.pdb' ('b'*64) 20
$valid=Get-KmcModsRuntimeWriteClassification $staged ([pscustomobject]@{entries=@($baseRuntime)+@($symbol)})
if(@($valid.unexpected).Count-ne0-or@($valid.expected).Count-ne2){throw 'Exact DLL and symbol copies refused'}
Write-Host 'PASS exact DLL and PDB loader copies admitted'
$cases=@(
 @{name='mismatching symbol';staged=$staged;runtime=@($baseRuntime)+@((Entry $symbol.path ('c'*64) 20))},
 @{name='wrong symbol size';staged=$staged;runtime=@($baseRuntime)+@((Entry $symbol.path ('b'*64) 21))},
 @{name='missing original symbol';staged=@($staged[0]);runtime=@($baseRuntime)+@($symbol)},
 @{name='changed original symbol';staged=$staged;runtime=@($staged[0],(Entry 'Scroll/Scroll.pdb' ('c'*64) 20),$baseRuntime[2],$symbol)},
 @{name='orphan symbol without DLL cache';staged=$staged;runtime=@($staged)+@($symbol)},
 @{name='mismatching DLL cache';staged=$staged;runtime=@($staged)+@((Entry 'Scroll/Scroll.dll.12.cache' ('c'*64) 100),$symbol)},
 @{name='different cache stem';staged=$staged;runtime=@($baseRuntime)+@((Entry 'Scroll/Other.dll.12.cache.pdb' ('b'*64) 20))})
foreach($case in $cases){
 $r=Get-KmcModsRuntimeWriteClassification $case.staged ([pscustomobject]@{entries=$case.runtime})
 if(@($r.unexpected).Count-eq0){throw "Invalid symbol admitted: $($case.name)"}
 Write-Host "PASS $($case.name) refused"
}
Write-Host 'TOTAL PASS=8 FAIL=0'
