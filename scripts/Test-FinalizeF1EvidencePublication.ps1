$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot;$helper=Join-Path $lab 'codex-policy/Push-KingmakerMountedCombat.ps1'
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($helper,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Publishing helper parse error'}
foreach($name in @('Get-KmcReviewedOriginalAssetSha256','Test-KmcReviewedOriginalAsset')){
 $node=$ast.Find({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true)
 if($null-eq$node){throw 'Exact reviewed asset function missing'}
 . ([scriptblock]::Create($node.Extent.Text))
}
$manifest=Read-KmcJson (Join-Path (Get-KmcRepositoryRoot) 'docs/evidence/finalize-f1/native-evidence.json');$count=0
$finish=Read-KmcJson (Join-Path (Get-KmcRepositoryRoot) 'docs/evidence/finalize-f1/finish-native-evidence.json')
$f1d=Read-KmcJson (Join-Path (Get-KmcRepositoryRoot) 'docs/evidence/finalize-f1/f1d-native-evidence.json')
foreach($row in @($manifest.images)+@($finish.images)+@($f1d.images)){
 $sha=Get-KmcSha256 (Join-Path (Get-KmcRepositoryRoot) $row.path)
 if(-not(Test-KmcReviewedOriginalAsset $row.path $sha)-or$sha-cne$row.sha256){throw 'Declared native PNG not admitted'};$count++
 if(Test-KmcReviewedOriginalAsset $row.path ('0'*64)){throw 'Modified native PNG admitted'};$count++
}
if(Test-KmcReviewedOriginalAsset 'docs/evidence/finalize-f1/captures/unselected.png' $manifest.images[0].sha256){throw 'Unselected evidence path admitted'};$count++
if(-not(Test-KmcReviewedOriginalAsset 'src/KingmakerMountedCombat/Assets/MountedCombatFeat.png' '328f8e6ab637cdce11bf518cd19b2bf886382eb70e4ba63f104717e99c2ced9a')){throw 'Existing original-art registration changed'};$count++
$c=Join-Path $lab 'artifacts/KmcRemovalObserver-0.1.0-observer.8-finalize-f1d-diagnostic.zip'
[void](Assert-KmcNormalPlayerObserverManifest $c -FrozenF1Observer);$count++
$refused=$false;try{Assert-KmcNormalPlayerObserverManifest $c|Out-Null}catch{$refused=$true}
if(-not$refused){throw 'Legacy observer admission silently relaxed'};$count++
$refused=$false;try{Assert-KmcNormalPlayerObserverManifest (Join-Path $lab 'artifacts/KmcRemovalObserver-0.1.0-observer.7-finalize-f1-finish-c-diagnostic.zip') -FrozenF1Observer|Out-Null}catch{$refused=$true}
if(-not$refused){throw 'Another observer package admitted as frozen F1'};$count++
Write-Host ('F1 EVIDENCE/ADMISSION PASS='+$count+' FAIL=0; helper functions only, no push/game/staging.')
