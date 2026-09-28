param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '../..'))
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aNativeMammothEvidence.ps1')
$tokens=$null;$parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count -ne 0){throw $parseErrors[0]}
foreach($name in @('Get-KmcPhase3dHorseRuntimeRows','Assert-KmcChunk6aCombatMountEvidence')){
 $def=@($ast.FindAll({param($n)$n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq $name},$true))
 if($def.Count -ne 1){throw $name};. ([scriptblock]::Create($def[0].Extent.Text))
}
function Copy-Draft($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=(Join-Path $labRoot 'runtime-evidence/c6a-fixtures-a-approach/phase3d-horse-scenario-evidence.json')
$originalHash=Get-KmcSha256 $original
$child=Read-KmcJson $original;$child.scenario='chunk6a-mammoth-mount-rt'
$proof=@($child.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
$pair=[pscustomobject]@{riderId=$proof.identity.casterId;mountId=$proof.identity.targetId;riderObject=101;mountObject=102;mountBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';petObject=102;petId=$proof.identity.targetId;masterObject=101;masterId=$proof.identity.casterId;riderInState=$true;mountInState=$true;riderIsPartyMember=$true;samePlayerPartyGroup=$true}
$root=Join-Path (Join-Path $labRoot 'analysis-cache/chunk6a-causal') ('synthetic-artifacts-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($root)|Out-Null
$request=[pscustomobject]@{evidenceRoot=$root}
$profile=[pscustomobject]@{schemaVersion=1;evidenceKind='chunk6a-native-mammoth-profile';status='PASS';errors=@();createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o');observations=[pscustomobject]@{
 before=$pair;after=(Copy-Draft $pair);restored=$true;selectionBefore=@('original');selectionAfter=@('original');pauseBefore=$true;pauseAfter=$true;turnBasedBefore=$false;turnBasedAfter=$false;pairedBefore=$true;pairedAfter=$true;childScenario=$child.scenario;childRows=@($child.rows|ForEach-Object {[pscustomobject]@{name=$_.name;status=$_.status}})}}
foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){
 $profile|Add-Member -NotePropertyName $field -NotePropertyValue $child.$field
 $request|Add-Member -NotePropertyName $field -NotePropertyValue $child.$field
}
$profilePath=Join-Path $root 'chunk6a-native-mammoth-profile.json';$childPath=Join-Path $root 'phase3d-horse-scenario-evidence.json'
function Save-Artifact($p,$a){[IO.File]::WriteAllText($p,($a|ConvertTo-Json -Depth 100 -Compress),[Text.UTF8Encoding]::new($false))}
Save-Artifact $childPath $child
function Get-Record($p,$kind){[pscustomobject]@{relativePath=[IO.Path]::GetFileName($p);kind=$kind;length=(Get-Item -LiteralPath $p).Length;sha256=(Get-KmcSha256 $p)}}
function Get-Manifest{[pscustomobject]@{artifacts=@((Get-Record $profilePath 'chunk6a-native-mammoth-profile'),(Get-Record $childPath 'phase3d-horse-scenario-evidence'))}}
$script:checks=0
function Reject-Artifact([scriptblock]$mutate){
 $p=Copy-Draft $profile;$q=Copy-Draft $request;Save-Artifact $profilePath $p;$m=Get-Manifest
 & $mutate $p $q $m
 Save-Artifact $profilePath $p
 if(-not $script:keepBrokenHash){$m.artifacts[0]=Get-Record $profilePath 'chunk6a-native-mammoth-profile'}
 $script:keepBrokenHash=$false
 $rejected=$false;try{Assert-KmcNativeMammothArtifact $q $m 'PASS'}catch{$rejected=$true}
 if(-not $rejected){throw 'Corrupt Mammoth artifact envelope accepted'};$script:checks++
}
$script:keepBrokenHash=$false
Save-Artifact $profilePath $profile
Assert-KmcNativeMammothArtifact $request (Get-Manifest) 'PASS';$script:checks++
foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){Reject-Artifact {param($p,$q,$m)$p.$field='foreign'}}
Reject-Artifact {param($p,$q,$m)$p.status='FAIL'}
Reject-Artifact {param($p,$q,$m)$p.schemaVersion=2}
Reject-Artifact {param($p,$q,$m)$p.createdAtUtc='invalid'}
Reject-Artifact {param($p,$q,$m)$p.observations.after.petId='foreign'}
Reject-Artifact {param($p,$q,$m)$p.observations.restored=$false}
Reject-Artifact {param($p,$q,$m)$p.observations.before.mountId='foreign'}
Reject-Artifact {param($p,$q,$m)$m.artifacts[0].sha256=('0'*64);$script:keepBrokenHash=$true}
Reject-Artifact {param($p,$q,$m)$m.artifacts[0].length++;$script:keepBrokenHash=$true}
Reject-Artifact {param($p,$q,$m)$m.artifacts[0].relativePath='../outside.json';$script:keepBrokenHash=$true}
Reject-Artifact {param($p,$q,$m)$m.artifacts+=@($m.artifacts[0]);$script:keepBrokenHash=$true}
Reject-Artifact {param($p,$q,$m)$m.artifacts=@($m.artifacts[1]);$script:keepBrokenHash=$true}
Reject-Artifact {param($p,$q,$m)$m.artifacts=@($m.artifacts[0])}
Reject-Artifact {param($p,$q,$m)$m.artifacts[1].sha256=('0'*64)}
Reject-Artifact {param($p,$q,$m)$q.scenario='chunk6a-mount-approach'}
Save-Artifact $profilePath $profile
foreach($field in @('cost','rider-reaction','mount-reaction','reaction-cooldown','initiative-order')){
 $bad=Copy-Draft $child;$b=@($bad.observations.chunk6aCommandProofs|Where-Object window -CEQ 'positive-mount')[0]
 if($field -eq 'cost'){@($b.resourceWindow.events|Where-Object boundary -CEQ 'cost-after')[0].state.move+=3}
 elseif($field -eq 'rider-reaction'){$b.samples[-1].state.rider.reactions--}
 elseif($field -eq 'mount-reaction'){$b.samples[-1].state.mount.reactions++}
 elseif($field -eq 'reaction-cooldown'){$b.samples[-1].state.mount.reactionCooldown+=1}
 else{$b.samples[-1].state.rider.initiativeOrder++}
 Save-Artifact $childPath $bad;$rejected=$false
 try{Assert-KmcNativeMammothArtifact $request (Get-Manifest) 'PASS'}catch{$rejected=$true}
 if(-not $rejected){throw 'Mammoth envelope lost exact resource proof'};$script:checks++
}
Save-Artifact $childPath $child
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
$manifestPath=Join-Path $root 'runtime-artifacts.json'
Save-Artifact $manifestPath (Get-Manifest)
$binding=[pscustomobject]@{artifactManifestSha256=Get-KmcSha256 $manifestPath;profileSha256=Get-KmcSha256 $profilePath}
Assert-KmcBoundMammothProfile $binding $request $root;$script:checks++
foreach($field in @('artifactManifestSha256','profileSha256')) {
    $copy=Copy-Draft $binding;$copy.$field=('0'*64);$rejected=$false
    try{Assert-KmcBoundMammothProfile $copy $request $root}catch{$rejected=$true}
    if(-not $rejected){throw 'Mammoth qualification accepted unbound profile bytes'};$script:checks++
}
if((Get-KmcSha256 $original) -cne $originalHash){throw 'Historical original modified'}
Write-Output ('NATIVE MAMMOTH ARTIFACT PASS='+$script:checks+' FAIL=0; synthetic copied shape only; no native credit')
