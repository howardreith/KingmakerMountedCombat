[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('six-fixture-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'six-pair fixture check'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve earlier six-pair fixture checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Method([string]$Text,[string]$Signature){
 $s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$s-ne$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)){throw ('Actual six-pair method absent or ambiguous '+$Signature)}
 $open=$Text.IndexOf('{',$s);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth-ne0){throw 'Unbalanced six-pair method'};$Text.Substring($s,$end-$s)
}
$diagnostics=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics'
$fixturePath=Join-Path $diagnostics 'Chunk7OwnedPairFixture.cs';$selectionPath=Join-Path $diagnostics 'Chunk7OwnedPairsSelection.cs';$coldPath=Join-Path $diagnostics 'Chunk7AssistedColdInventorySelection.cs'
$fixture=[IO.File]::ReadAllText($fixturePath);$selection=[IO.File]::ReadAllText($selectionPath);$cold=[IO.File]::ReadAllText($coldPath)
$template=Join-Path $chunk7TestSource 'SixPairFixtureTemplate.cs';$text=[IO.File]::ReadAllText($template)
foreach($pair in @(@('READ','private void ReadSixPairSource(JObject artifact)'),@('REMOTE','private void RequireSixPairRemote()'),@('CAPTURE_REMOTE','internal JObject CaptureSixPairRemote()'))){$text=$text.Replace(('/*ACTUAL_'+$pair[0]+'*/'),(Method $fixture $pair[1]))}
$start=$fixture.IndexOf('internal static JObject ArrangementOf(',[StringComparison]::Ordinal);$end=$fixture.IndexOf('internal JObject Capture(',$start,[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Actual arrangement builder absent.'};$text=$text.Replace('/*ACTUAL_ARRANGEMENT*/',$fixture.Substring($start,$end-$start))
$constants=@();foreach($name in @('SixPairSourceRun','SixPairSourceArchive')){$m=[regex]::Matches($selection,('public const string '+$name+' = [^;]+;'));if($m.Count-ne1){throw 'Actual source constant absent.'};$constants+=$m[0].Value}
$text=$text.Replace('/*ACTUAL_CONSTANTS*/',($constants-join[Environment]::NewLine))
$m=[regex]::Matches($selection,'internal static string\[\] SixPairRoles => new\[\] \{[^;]+;');if($m.Count-ne1){throw 'Actual six-role array absent.'};$text=$text.Replace('/*ACTUAL_ROLES*/',$m[0].Value)
$m=[regex]::Matches($cold,'public const string Scenario = [^;]+;');if($m.Count-ne1){throw 'Actual cold scenario constant absent.'};$text=$text.Replace('/*ACTUAL_COLD_SCENARIO*/',$m[0].Value)
$actual=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($actual,$text,[Text.UTF8Encoding]::new($false))
$artifact=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/chunk7-assisted-cold-inventory-evidence.json'
if((Get-KmcSha256 $artifact)-cne'012d1caff175d34614fcd255f0b6ad897e0ef1a9650791cb3c0d55ce13b47241'){throw 'Preserve exact accepted b185 bytes.'}
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$exe=Join-Path $OutputDirectory 'six-fixture.exe';$identity=Join-Path $diagnostics 'RetainedObjectIdentity.cs'
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/reference:$json" "/out:$exe" $actual $identity
if($LASTEXITCODE-ne0){throw 'Actual six-pair fixture compilation failed.'};Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
$output=@(&$exe $artifact $json);$code=$LASTEXITCODE;$output|ForEach-Object{Write-Output $_}
[ordered]@{nativeInvoked=$false;archiveRead=$false;exitCode=$code;sourcePins=@(@($fixturePath,$selectionPath,$coldPath,$template,$identity,$artifact)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-KmcSha256 $_)}});results=$output}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual six-pair fixture behavior failed.'}
