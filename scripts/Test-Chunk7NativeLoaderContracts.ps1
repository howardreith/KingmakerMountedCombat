param([string]$ReaderPath,[string]$OutputDirectory,[string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if([string]::IsNullOrWhiteSpace($RepositoryRoot)){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
if([string]::IsNullOrWhiteSpace($ReaderPath)){$ReaderPath=Join-Path $RepositoryRoot 'scripts/runtime/Chunk7WorldEvidence.ps1'}
if([string]::IsNullOrWhiteSpace($OutputDirectory)){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-continuation/loader-contract-tests-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. $ReaderPath
$passed=0;$rows=New-Object 'Collections.Generic.List[object]'
function Check([string]$Name,[scriptblock]$TrialBody){& $TrialBody;$script:passed++;$rows.Add([pscustomobject]@{name=$Name;status='PASS'});Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$TrialBody){$refused=$false;try{& $TrialBody}catch{$refused=$true};if(-not$refused){throw ('Unexpected acceptance: '+$Name)};$script:passed++;$rows.Add([pscustomobject]@{name=$Name;status='PASS'});Write-Host ('PASS refuses '+$Name)}
function Copy-TestValue($Value){return ($Value|ConvertTo-Json -Depth 40 -Compress|ConvertFrom-Json)}
$request=[pscustomobject]@{schemaVersion=2;scenario='chunk7-two-pair-exploration';runId='synthetic-c7-loader-reader';branch='codex/mounted-combat-chunk7-foundation';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333'}
function New-Artifact([string]$Kind,$Observations){$value=[ordered]@{schemaVersion=1;evidenceKind=$Kind;createdAtUtc='2026-10-12T00:00:00Z';status='PASS';errors=@();observations=$Observations};foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$value[$field]=$request.$field};return [pscustomobject]$value}
# Unchanged historical inventory cases; no world-scenario filesystem setup.
$dependencies=[pscustomobject]@{gameRoot='C:\SyntheticKingmaker';modsRoot='C:\SyntheticKingmaker\Mods';files=@()}
$assemblyRows=@([pscustomobject]@{name='KingmakerMountedCombat';fullName='KingmakerMountedCombat';dynamic=$false;location='C:\SyntheticKingmaker\Mods\KingmakerMountedCombat\KingmakerMountedCombat.dll';sha256=$request.dllSha256;mvid=$request.dllMvid})
foreach($relative in @('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll')){
    $dependencies.files+=[pscustomobject]@{relativePath=$relative;sha256='8'*64};$assemblyRows+=[pscustomobject]@{name=([IO.Path]::GetFileNameWithoutExtension($relative));fullName=$relative;dynamic=$false;location=(Join-Path $dependencies.gameRoot $relative);sha256='8'*64;mvid='99999999-9999-4999-8999-999999999999'}
}
$inventory=New-Artifact 'runtime-loader-inventory' ([pscustomobject]@{capturedInNativeProcess=$true;processId=100;modEntries=@([pscustomobject]@{id='KingmakerMountedCombat';active=$true;enabled=$true});loadedAssemblies=$assemblyRows})
Check 'exact native base-only loader inventory' {Assert-KmcBaseGameNativeInventory $request $inventory $dependencies}
function New-TestAssembly([string]$Name,[string]$Location) {
    return [pscustomobject]@{name=$Name;fullName=$Name;dynamic=$false;location=$Location;sha256='7'*64;mvid='77777777-7777-4777-8777-777777777777'}
}
Check 'additional native Managed assembly is recorded with hash and MVID' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'System' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\System.dll'
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.fileBackedAssemblyCount-ne5-or$scope.completeStockAssemblyAllowlistProven-ne$false){throw 'Directory allowance was mistaken for a complete stock identity allowlist.'}
}
Check 'additional native UMM dependency has a file-backed identity' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=New-TestAssembly 'dnlib' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\UnityModManager\dnlib.dll'
    Assert-KmcBaseGameNativeInventory $request $extra $dependencies
}
Check 'dynamic assembly identities retained without qualification claim' {
    $extra=Copy-TestValue $inventory;$extra.observations.loadedAssemblies+=[pscustomobject]@{name='DynamicNative';fullName='DynamicNative, Version=0.0.0.0';dynamic=$true;location=$null;sha256=$null;mvid=$null}
    $scope=Assert-KmcBaseGameNativeInventory $request $extra $dependencies -PassThru
    if($scope.dynamicAssemblyCount-ne1-or$scope.dynamicAssemblies[0].fullName-cne'DynamicNative, Version=0.0.0.0'-or
        $scope.dynamicAssembliesQualified-ne$false-or$scope.completeStockAssemblyAllowlistProven-ne$false-or[string]::IsNullOrWhiteSpace($scope.limitation)){throw 'Dynamic evidence limit was dropped.'}
}
$inventoryMutations=@(@('foreign active mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$true;enabled=$true}}),
    @('foreign disabled mod',{param($v)$v.observations.modEntries+=[pscustomobject]@{id='ForeignMod';active=$false;enabled=$false}}),@('KMC inactive',{param($v)$v.observations.modEntries[0].active=$false}),
    @('KMC enabled string',{param($v)$v.observations.modEntries[0].enabled='true'}),@('missing loaded Harmony',{param($v)$v.observations.loadedAssemblies=@($v.observations.loadedAssemblies[0..2])}),
    @('changed loaded KMC',{param($v)$v.observations.loadedAssemblies[0].sha256='1'*64}),@('changed loaded gameplay',{param($v)$v.observations.loadedAssemblies[1].sha256='1'*64}),
    @('foreign Mods assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Mods\ForeignMod\Foreign.dll'}),
    @('external file-backed assembly',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\ForeignMods\Foreign.dll'}),
    @('Managed sibling prefix path',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed-foreign\Foreign.dll'}),
    @('Managed nested foreign directory',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\Foreign\Foreign.dll'}),
    @('Managed traversal outside exact roots',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'ForeignMod' 'C:\SyntheticKingmaker\Kingmaker_Data\Managed\..\Foreign.dll'}),
    @('non-dynamic assembly without location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' $null}),
    @('non-dynamic assembly with whitespace location',{param($v)$v.observations.loadedAssemblies+=New-TestAssembly 'Locationless' ' '}),
    @('relative file-backed assembly location',{param($v)$v.observations.loadedAssemblies[1].location='Kingmaker_Data\Managed\Assembly-CSharp.dll'}),
    @('KMC loaded from Managed instead of candidate path',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Kingmaker_Data\Managed\KingmakerMountedCombat.dll'}),
    @('KMC loaded from another Mods directory',{param($v)$v.observations.loadedAssemblies[0].location='C:\SyntheticKingmaker\Mods\ForeignMod\KingmakerMountedCombat.dll'}),
    @('missing observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256=$null}),
    @('malformed observed assembly hash',{param($v)$v.observations.loadedAssemblies[1].sha256='not-a-hash'}),
    @('missing observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid=$null}),
    @('malformed observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='not-a-guid'}),
    @('empty observed MVID',{param($v)$v.observations.loadedAssemblies[1].mvid='00000000-0000-0000-0000-000000000000'}),
    @('missing assembly identity field',{param($v)$v.observations.loadedAssemblies[1].PSObject.Properties.Remove('fullName')}),
    @('assembly observation error hidden by PASS',{param($v)$v.observations.loadedAssemblies[1]|Add-Member observationError 'hash unavailable'}),
    @('dynamic assembly claims a file identity',{param($v)$v.observations.loadedAssemblies[1].dynamic=$true}),
    @('dynamic KMC cannot stand in for candidate',{param($v)$v.observations.loadedAssemblies[0].dynamic=$true;$v.observations.loadedAssemblies[0].location=$null;$v.observations.loadedAssemblies[0].sha256=$null;$v.observations.loadedAssemblies[0].mvid=$null}),
    @('duplicated required loaded gameplay assembly',{param($v)$v.observations.loadedAssemblies+=(Copy-TestValue $v.observations.loadedAssemblies[1])}),
    @('process ID string',{param($v)$v.observations.processId='100'}))
foreach($mutation in $inventoryMutations){$changed=Copy-TestValue $inventory;& $mutation[1] $changed;Reject $mutation[0] {Assert-KmcBaseGameNativeInventory $request $changed $dependencies}}

$exact=Copy-TestValue $inventory;$pinned=Copy-TestValue $dependencies
$harmonyHash='5f7ad0df1eb08e81f66d4006d3a9e5cb40fc05e381f36d7bd0513f334f6a251f';$ummHash='75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39'
$harmonyRelative='Kingmaker_Data/Managed/UnityModManager/0Harmony.dll';$ummRelative='Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll'
$pinned.files+=@([pscustomobject]@{relativePath=$harmonyRelative;sha256=$harmonyHash});($pinned.files|Where-Object relativePath -CEQ $ummRelative).sha256=$ummHash
$umm=$exact.observations.loadedAssemblies|Where-Object name -CEQ 'UnityModManager';$umm.sha256=$ummHash;$umm.fullName='UnityModManager, Version=0.28.2.0, Culture=neutral, PublicKeyToken=null';$umm.mvid='edf4be29-0c88-4482-876d-e946a0f9e363'
$exact.observations.loadedAssemblies+=@([pscustomobject]@{name='0Harmony';fullName='0Harmony, Version=2.3.1.1, Culture=neutral, PublicKeyToken=null';dynamic=$false;location=(Join-Path $pinned.gameRoot $harmonyRelative);mvid='6c906b4f-1de4-40f3-a86c-b0ebb3f48ea6';sha256=$harmonyHash})
$exact.observations.loadedAssemblies+=@([pscustomobject]@{name='HarmonySharedState';fullName='HarmonySharedState, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null';dynamic=$false;location='';mvid='b2f05641-73ed-4a8d-aa45-8bb46a4b7e2f';sha256=$null})
($exact.observations.loadedAssemblies|Where-Object name -CEQ 'KingmakerMountedCombat').location+='.51087.cache'
Check 'exact pinned Harmony memory identity and UMM candidate cache' {
 $scope=Assert-KmcBaseGameNativeInventory $request $exact $pinned -PassThru
 if($scope.fileBackedAssemblyCount-ne5-or$scope.inMemoryAssemblyCount-ne1-or$scope.dynamicAssemblyCount-ne0-or
  $scope.kmcLoadedFromNativeCache-ne$true-or$scope.inMemoryAssemblyCreationObserved-ne$false-or$scope.inMemoryAssemblyContentCertified-ne$false-or
  $scope.inMemoryAssemblies[0].identityShapeVerified-ne$true-or$scope.inMemoryAssemblies[0].creationObserved-ne$false-or$scope.inMemoryAssemblies[0].contentCertified-ne$false-or$scope.completeStockAssemblyAllowlistProven-ne$false){throw 'In-memory or cache evidence was mislabeled as byte-backed/native creation proof.'}
}
foreach($suffix in @('0','65535')){Check ('native UInt16 cache suffix '+$suffix) {$v=Copy-TestValue $exact;($v.observations.loadedAssemblies|Where-Object name -CEQ 'KingmakerMountedCombat').location=(Join-Path $pinned.modsRoot ('KingmakerMountedCombat/KingmakerMountedCombat.dll.'+$suffix+'.cache'));Assert-KmcBaseGameNativeInventory $request $v $pinned}}
$memoryMutations=@(
 @('memory unknown name',{param($m)$m.name='UnknownMemory'}),@('memory version',{param($m)$m.fullName=$m.fullName.Replace('0.0.0.0','1.0.0.0')}),
 @('memory public key',{param($m)$m.fullName=$m.fullName.Replace('PublicKeyToken=null','PublicKeyToken=0000000000000000')}),@('memory dynamic flag',{param($m)$m.dynamic=$true}),
 @('memory null location',{param($m)$m.location=$null}),@('memory whitespace location',{param($m)$m.location=' '}),@('memory relative location',{param($m)$m.location='HarmonySharedState.dll'}),
 @('memory claimed hash',{param($m)$m.sha256='9'*64}),@('memory empty hash string',{param($m)$m.sha256=''}),@('memory missing MVID',{param($m)$m.mvid=$null}),
 @('memory zero MVID',{param($m)$m.mvid='00000000-0000-0000-0000-000000000000'}),@('memory MVID case',{param($m)$m.mvid=$m.mvid.ToUpperInvariant()}),
 @('memory MVID aliases candidate',{param($m)$m.mvid=$request.dllMvid}),@('memory extra observation field',{param($m)$m|Add-Member observedBy 'unproven'})
)
foreach($case in $memoryMutations){$v=Copy-TestValue $exact;& $case[1] ($v.observations.loadedAssemblies|Where-Object name -CEQ 'HarmonySharedState');Reject $case[0] {Assert-KmcBaseGameNativeInventory $request $v $pinned}}
Reject 'duplicate known memory module' {$v=Copy-TestValue $exact;$v.observations.loadedAssemblies+=Copy-TestValue ($v.observations.loadedAssemblies|Where-Object name -CEQ 'HarmonySharedState');Assert-KmcBaseGameNativeInventory $request $v $pinned}
foreach($creator in @('0Harmony','UnityModManager')){
 foreach($field in @('name','fullName','mvid','sha256','location')){$v=Copy-TestValue $exact;$row=$v.observations.loadedAssemblies|Where-Object name -CEQ $creator;$row.$field='changed';Reject ($creator+' creator '+$field) {Assert-KmcBaseGameNativeInventory $request $v $pinned}}
 foreach($field in @('name','fullName','mvid','sha256')){$v=Copy-TestValue $exact;$row=$v.observations.loadedAssemblies|Where-Object name -CEQ $creator;$row.$field=@($row.$field);Reject ($creator+' creator singleton array '+$field) {Assert-KmcBaseGameNativeInventory $request $v $pinned}}
 Reject ($creator+' missing creator') {$v=Copy-TestValue $exact;$v.observations.loadedAssemblies=@($v.observations.loadedAssemblies|Where-Object name -CNE $creator);Assert-KmcBaseGameNativeInventory $request $v $pinned}
 Reject ($creator+' duplicate creator') {$v=Copy-TestValue $exact;$v.observations.loadedAssemblies+=Copy-TestValue ($v.observations.loadedAssemblies|Where-Object name -CEQ $creator);Assert-KmcBaseGameNativeInventory $request $v $pinned}
 $relative=if($creator-ceq'0Harmony'){$harmonyRelative}else{$ummRelative}
 Reject ($creator+' missing dependency role') {$deps=Copy-TestValue $pinned;$deps.files=@($deps.files|Where-Object relativePath -CNE $relative);Assert-KmcBaseGameNativeInventory $request $exact $deps}
 Reject ($creator+' changed dependency bytes') {$deps=Copy-TestValue $pinned;($deps.files|Where-Object relativePath -CEQ $relative).sha256='9'*64;Assert-KmcBaseGameNativeInventory $request $exact $deps}
 Reject ($creator+' duplicate dependency role') {$deps=Copy-TestValue $pinned;$deps.files+=Copy-TestValue ($deps.files|Where-Object relativePath -CEQ $relative);Assert-KmcBaseGameNativeInventory $request $exact $deps}
 foreach($field in @('relativePath','sha256')){Reject ($creator+' dependency singleton array '+$field) {$deps=Copy-TestValue $pinned;$row=$deps.files|Where-Object relativePath -CEQ $relative;$row.$field=@($row.$field);Assert-KmcBaseGameNativeInventory $request $exact $deps}}
}
foreach($suffix in @('-1','65536','0001','1.0','1e2','x','1.cache.pdb')){Reject ('invalid native cache suffix '+$suffix) {$v=Copy-TestValue $exact;($v.observations.loadedAssemblies|Where-Object name -CEQ 'KingmakerMountedCombat').location=(Join-Path $pinned.modsRoot ('KingmakerMountedCombat/KingmakerMountedCombat.dll.'+$suffix+'.cache'));Assert-KmcBaseGameNativeInventory $request $v $pinned}}
foreach($field in @('sha256','mvid')){Reject ('cache foreign candidate '+$field) {$v=Copy-TestValue $exact;($v.observations.loadedAssemblies|Where-Object name -CEQ 'KingmakerMountedCombat').$field=if($field-ceq'sha256'){'9'*64}else{'99999999-9999-4999-8999-999999999999'};Assert-KmcBaseGameNativeInventory $request $v $pinned}}
Reject 'cache outside exact candidate directory' {$v=Copy-TestValue $exact;($v.observations.loadedAssemblies|Where-Object name -CEQ 'KingmakerMountedCombat').location=(Join-Path $pinned.modsRoot 'Foreign/KingmakerMountedCombat.dll.51087.cache');Assert-KmcBaseGameNativeInventory $request $v $pinned}
$syntax=$null;$tokens=$null;[void][Management.Automation.Language.Parser]::ParseFile($ReaderPath,[ref]$tokens,[ref]$syntax);Check 'reader parses' {if($syntax.Count){throw ($syntax|Out-String)}}
$receipt=@{status='PASS';pass=$passed;fail=0;reader=@{path=$ReaderPath;sha256=(Get-FileHash -LiteralPath $ReaderPath).Hash.ToLowerInvariant()};cases=@($rows.ToArray());nativeProof=$false;gameplayProof=$false}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false));Write-Host ('LOADER CONTRACT PASS '+$passed+' FAIL 0')
