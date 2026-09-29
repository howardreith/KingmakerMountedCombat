. (Join-Path $PSScriptRoot 'Chunk6aLegacyTestFixtures.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aArtifactRowsEvidence.ps1')
$game=Get-Content -LiteralPath (Join-Path $root 'runtime-game-result.json') -Raw|ConvertFrom-Json
$record=Get-Content -LiteralPath (Join-Path $root 'combat-scenario-evidence.jsonl') -Raw|ConvertFrom-Json
$fixtures=Join-Path $scratch ('row-fixtures-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtures -Force|Out-Null
$script:checks=0;$errors=@()
function Clone($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Check([string]$name,[string]$raw,[bool]$pass,$context=$game,[string]$scenario='mounted-mammoth-primary-hit-tb'){
 $dir=Join-Path $fixtures $name;New-Item -ItemType Directory -Path $dir|Out-Null
 $path=Join-Path $dir 'combat-scenario-evidence.jsonl';[IO.File]::WriteAllText($path,$raw,[Text.UTF8Encoding]::new($false))
 $accepted=$true;$text=$null;$rows=@();try{$rows=@(Get-KmcChunk6aArtifactRows $path $scenario $context)}catch{$accepted=$false;$text=$_.Exception.Message}
 if($accepted-ne$pass){throw ('Unexpected row parser verdict: '+$name+' '+$text)}
 if($pass-and($rows.Count-ne1-or$rows[0].name-cne$scenario)){throw 'Projected row identity changed'}
 $script:checks++;Write-Host ('PASS '+$name)
 return ,$rows
}
$raw=$record|ConvertTo-Json -Depth 100 -Compress
$null=Check original $raw $true
$bad=Clone $record;$bad.status='FAIL';$bad.assertionFailCount=1;$bad.errors=@('Exact failed assertion with [x] * literal punctuation.')
$rows=Check failure ($bad|ConvertTo-Json -Depth 100 -Compress) $true
if($rows[0].status-cne'FAIL'-or$rows[0].assertionFailCount-ne1-or$rows[0].errors[0]-cne$bad.errors[0]){throw 'Failure evidence changed in projection'};$script:checks++
$null=Check missing '' $false
$null=Check duplicate ($raw+"`n"+$raw) $false
$null=Check duplicate-property ($raw.Replace('"schemaVersion":57','"schemaVersion":57,"schemaVersion":57')) $false
foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','row','artifactKind','status')){
 $bad=Clone $record;$bad.$field='wrong';$null=Check $field ($bad|ConvertTo-Json -Depth 100 -Compress) $false
}
foreach($field in @('schemaVersion','assertionPassCount','assertionFailCount')){
 $bad=Clone $record;$bad.$field=-1;$null=Check $field ($bad|ConvertTo-Json -Depth 100 -Compress) $false
}
$bad=Clone $record;$bad.errors='string';$null=Check errors ($bad|ConvertTo-Json -Depth 100 -Compress) $false
$null=Check scenario-context $raw $false $game 'chunk6a-mount-approach'
# Normal JSON PASS keeps rows[] only, while FAIL lookup includes subscenarioResults as before.
$p=Join-Path $fixtures 'ordinary.json';[IO.File]::WriteAllText($p,'{"rows":[{"name":"row"}],"subscenarioResults":[{"name":"sub"}]}')
$rows=@(Get-KmcChunk6aArtifactRows $p test $game -PassRowsOnly)
$all=@(Get-KmcChunk6aArtifactRows $p test $game)
if($rows.Count-ne1-or$rows[0].name-cne'row'-or$all.Count-ne2){throw 'Ordinary JSON projection changed'};$script:checks++
[pscustomobject]@{status='PASS';pass=$script:checks;fail=0;fixtures=$fixtures;scope='Synthetic row projection and synthetic modern parser regression only'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $scratch 'Rows-DRAFT-receipt.json') -Encoding UTF8
Write-Host ('ROW PROJECTION READER PASS='+$script:checks+' FAIL=0')

Remove-LegacyTestFixture
