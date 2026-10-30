[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceProfileProtection.ps1')
$testRoot=Assert-KmcChildPath (Join-Path (Get-KmcRepositoryRoot) ('obj/base-game-isolation-tests/'+[Guid]::NewGuid().ToString('N'))) (Join-Path (Get-KmcRepositoryRoot) 'obj') 'base-game isolation tests'
[void][IO.Directory]::CreateDirectory($testRoot)
$passed=0;$failed=0
function Check([string]$Name,[scriptblock]$TestBody){try{&$TestBody;$script:passed++;Write-Host ('PASS '+$Name)}catch{$script:failed++;Write-Host ('FAIL '+$Name+': '+$_.Exception.Message)}}
function Reject([string]$Name,[scriptblock]$RejectedBody){Check $Name { $rejected=$false;try{&$RejectedBody}catch{$rejected=$true};if(!$rejected){throw 'unsafe input was accepted'} }}
function Need([bool]$Condition,[string]$Message){if(!$Condition){throw $Message}}
function New-Fixture([string]$Name){
    $fixtureRoot=Assert-KmcChildPath (Join-Path $testRoot $Name) $testRoot 'fake installation fixture'
    $game=Join-Path $fixtureRoot 'game';$mods=Join-Path $game 'Mods';$loader=Join-Path $game 'Kingmaker_Data/Managed/UnityModManager'
    foreach($folder in @($mods,$loader,(Join-Path $mods 'ForeignOne/cache'),(Join-Path $mods 'ForeignTwo'))){[void][IO.Directory]::CreateDirectory($folder)}
    [IO.File]::WriteAllText((Join-Path $mods 'ForeignOne/Info.json'),'foreign opaque metadata')
    [IO.File]::WriteAllText((Join-Path $mods 'ForeignOne/cache/owned.bin'),'foreign mutable bytes')
    [IO.File]::WriteAllText((Join-Path $mods 'ForeignTwo/assembly.dll'),'foreign opaque code')
    [IO.File]::WriteAllText((Join-Path $mods 'root-note.txt'),'foreign root bytes')
    [IO.File]::WriteAllText((Join-Path $loader 'Config.xml'),'<Config><ModsDirectory>Mods</ModsDirectory><ModInfo>Info.json</ModInfo><GameExe>Kingmaker.exe</GameExe></Config>')
    $params='<Param><ShowOnStart>1</ShowOnStart><ModParams><Mod Id="ForeignOne" Enabled="true"/><Mod Id="KingmakerMountedCombat" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod><Mod Id="ForeignTwo" Enabled="false"/></ModParams></Param>'
    [IO.File]::WriteAllText((Join-Path $loader 'Params.xml'),$params)
    foreach($name in @('UnityModManager.dll','0Harmony12.dll','0Harmony-1.2.dll','0Harmony.dll','dnlib.dll')){[IO.File]::WriteAllText((Join-Path $loader $name),('fixture-'+$name))}
    $gameAssembly=Join-Path $game 'Kingmaker_Data/Managed/Assembly-CSharp.dll';[IO.File]::WriteAllText($gameAssembly,'fixture-gameplay')
    $pins=@(foreach($pair in @(@('gameplayAssembly',$gameAssembly),@('umm',(Join-Path $loader 'UnityModManager.dll')),@('harmony',(Join-Path $loader '0Harmony12.dll')))){
        [pscustomobject]@{role=$pair[0];length=(Get-Item -LiteralPath $pair[1]).Length;sha256=(Get-KmcSha256 $pair[1])}
    })
    return [pscustomobject]@{home=$fixtureRoot;game=$game;mods=$mods;loader=$loader;parameters=$params;fingerprint=[pscustomobject]@{kingmaker=[pscustomobject]@{files=$pins}}}
}
$f=New-Fixture 'main'
$receipt=Get-KmcBaseGameDependencies -GameRoot $f.game -Fingerprint $f.fingerprint
Check 'dependency receipt records exact loader set without moving it' {Need (@($receipt.files).Count-eq6) 'dependency count';Assert-KmcBaseGameDependencyReceipt $receipt -VerifyFiles}
Check 'dependency identity is stable across receipt serialization' {Assert-KmcBaseGameDependencyReceipt ($receipt|ConvertTo-Json -Depth 8|ConvertFrom-Json) -VerifyFiles}
$beforeDependencies=Get-KmcDirectoryManifest (Split-Path $f.loader)
$bad=New-Fixture 'wrong-fingerprint';$bad.fingerprint.kingmaker.files[1].sha256='0'*64
Reject 'changed pinned UMM rejected' {Get-KmcBaseGameDependencies $bad.game $bad.fingerprint|Out-Null}
$bad2=New-Fixture 'missing-harmony';[IO.File]::Delete((Join-Path $bad2.loader '0Harmony12.dll'))
Reject 'missing required Harmony rejected' {Get-KmcBaseGameDependencies $bad2.game $bad2.fingerprint|Out-Null}
$bad3=New-Fixture 'unknown-loader';[IO.File]::WriteAllText((Join-Path $bad3.loader 'ForeignPlugin.dll'),'foreign')
Reject 'foreign loader DLL rejected' {Get-KmcBaseGameDependencies $bad3.game $bad3.fingerprint|Out-Null}
$bad4=New-Fixture 'redirected-mods';[IO.File]::WriteAllText((Join-Path $bad4.loader 'Config.xml'),'<Config><ModsDirectory>../Mods</ModsDirectory><ModInfo>Info.json</ModInfo><GameExe>Kingmaker.exe</GameExe></Config>')
Reject 'redirected Mods layout rejected' {Get-KmcBaseGameDependencies $bad4.game $bad4.fingerprint|Out-Null}
$bad5=New-Fixture 'disabled-kmc';[IO.File]::WriteAllText((Join-Path $bad5.loader 'Params.xml'),$bad5.parameters.Replace('Id="KingmakerMountedCombat" Enabled="true"','Id="KingmakerMountedCombat" Enabled="false"'))
Reject 'owner-disabled KMC never enabled by isolation' {Get-KmcBaseGameDependencies $bad5.game $bad5.fingerprint|Out-Null}
Reject 'DTD loader XML rejected' {Read-KmcBaseGameXml '<!DOCTYPE Config [<!ENTITY x "Mods">]><Config><ModsDirectory>&x;</ModsDirectory></Config>'|Out-Null}
$duplicate=$f.parameters.Replace('</ModParams>','<Mod Id="kingmakermountedcombat" Enabled="true"/></ModParams>')
Reject 'case-ambiguous mod parameters rejected' {Assert-KmcBaseGameParameters $duplicate|Out-Null}
$copy=$receipt|ConvertTo-Json -Depth 8|ConvertFrom-Json;$copy.dependencyDigest='0'*64
Reject 'tampered dependency receipt rejected' {Assert-KmcBaseGameDependencyReceipt $copy}
# The dependency digest must not follow the checkout's line endings (a CRLF worktree once computed another digest and
# recovery refused): load the actual source normalised to CRLF and to LF and require the explicit LF-joined digest from both.
Check 'dependency digest is identical from CRLF and LF checkouts and equals the LF-joined rows' {
    $sourceText=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'runtime/BaseGameIsolation.ps1'),[Text.Encoding]::UTF8)
    $lf=$sourceText.Replace("`r`n","`n");$crlf=$lf.Replace("`n","`r`n")
    [string[]]$rows=@($receipt.files|ForEach-Object {'{0}|{1}|{2}' -f $_.relativePath,$_.length,$_.sha256});[Array]::Sort($rows,[StringComparer]::Ordinal)
    $expected=Get-KmcTextSha256 ((@([string]$receipt.gameRoot,[string]$receipt.configSha256)+$rows)-join "`n")
    foreach($variant in @(@('lf',$lf),@('crlf',$crlf))){
        $copy=Join-Path $testRoot ('BaseGameIsolation.'+$variant[0]+'.ps1');[IO.File]::WriteAllText($copy,$variant[1],[Text.UTF8Encoding]::new($false))
        $digest=& {param($path,$value). $path;Get-KmcBaseGameDependencyDigest $value} $copy $receipt
        Need ($digest-ceq$expected) ('The '+$variant[0]+' checkout computes another dependency digest.')
    }
    Need ((Get-KmcBaseGameDependencyDigest $receipt)-ceq$expected) 'The loaded dependency digest differs from the LF-joined rows.'
}
$dependency=Join-Path $f.loader 'dnlib.dll';$saved=[IO.File]::ReadAllBytes($dependency)
try{[IO.File]::WriteAllText($dependency,'changed');Reject 'dependency changed between intake and staging rejected' {Assert-KmcBaseGameDependencyReceipt $receipt -VerifyFiles}}
finally{[IO.File]::WriteAllBytes($dependency,$saved)}

# UMM parameter admission and startup/exit delta. Pure parser checks: they run before
# any guarded transaction so a live game or installer cannot hide them.
$reduced=$f.parameters.Replace('<Mod Id="ForeignOne" Enabled="true"/>','').Replace('<Mod Id="ForeignTwo" Enabled="false"/>','')
Check 'base-only UMM admits exact absent-mod removal' {Assert-KmcNativeUmmStartupDelta $f.parameters $reduced -BaseGameOnly}
Check 'base-only UMM admits unchanged parameters' {Assert-KmcNativeUmmStartupDelta $f.parameters $f.parameters -BaseGameOnly}
Reject 'historical UMM mode still refuses foreign-entry removal' {Assert-KmcNativeUmmStartupDelta $f.parameters $reduced}
Reject 'base-only and removal-observer authorities cannot combine' {Assert-KmcNativeUmmStartupDelta $f.parameters $reduced -BaseGameOnly -RemovalObserver}
foreach($case in @(
    @('disabled KMC',$reduced.Replace('Enabled="true"','Enabled="false"')),
    @('other setting changed',$reduced.Replace('<ShowOnStart>1','<ShowOnStart>0')),
    @('new foreign entry',$reduced.Replace('</ModParams>','<Mod Id="New" Enabled="true"/></ModParams>')),
    @('partial foreign removal',$f.parameters.Replace('<Mod Id="ForeignOne" Enabled="true"/>','')),
    @('KMC hotkey changed',$reduced.Replace('<keyCode>None','<keyCode>A')))){
    Reject ('base-only UMM refuses '+$case[0]) {Assert-KmcNativeUmmStartupDelta $f.parameters $case[1] -BaseGameOnly}
}
# Installed UMM 0.33.2.0 shape: an owner who plays without KMC has no KMC entry; on a
# normal exit UMM rewrites ModParams from the mods it found (KMC alone in the ready tree).
$stamp='2026-10-16T19:45:27.4249334-04:00'
function New-OwnerParams([string]$Mods,[string]$Stamp=$script:stamp,[string]$Show='1'){
    return ("<?xml version=`"1.0`" encoding=`"utf-8`"?>`n<Param xmlns:xsd=`"http://www.w3.org/2001/XMLSchema`" xmlns:xsi=`"http://www.w3.org/2001/XMLSchema-instance`">`n  <Hotkey>`n    <keyCode>None</keyCode>`n    <modifiers>0</modifiers>`n  </Hotkey>`n  <CheckUpdates>1</CheckUpdates>`n  <ShowOnStart>"+$Show+"</ShowOnStart>`n  <UIScale>1</UIScale>`n  <LastUpdateCheck>"+$Stamp+"</LastUpdateCheck>`n  <ModParams>"+$Mods+"</ModParams>`n</Param>")
}
$foreignMods="`n    <Mod Id=`"ForeignOne`" Enabled=`"true`">`n      <Hotkey>`n        <keyCode>None</keyCode>`n        <modifiers>0</modifiers>`n      </Hotkey>`n    </Mod>`n    <Mod Id=`"ForeignTwo`" Enabled=`"false`"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>`n  "
$kmcAppended="`n    <Mod Id=`"KingmakerMountedCombat`" Enabled=`"true`">`n      <Hotkey>`n        <keyCode>None</keyCode>`n        <modifiers>0</modifiers>`n      </Hotkey>`n    </Mod>`n  "
$ownerParams=New-OwnerParams $foreignMods
$absent=New-Fixture 'absent-kmc';[IO.File]::WriteAllText((Join-Path $absent.loader 'Params.xml'),$ownerParams)
Check 'absent KMC entry admitted (UMM 0.33.2.0 enables an unlisted mod by default)' {Need (@((Get-KmcBaseGameDependencies $absent.game $absent.fingerprint).files).Count-eq6) 'dependency count'}
Reject 'duplicate exact KMC entries refused' {Assert-KmcBaseGameParameters $f.parameters.Replace('</ModParams>','<Mod Id="KingmakerMountedCombat" Enabled="true"/></ModParams>')|Out-Null}
Reject 'lone case-variant KMC entry refused' {Assert-KmcBaseGameParameters (New-OwnerParams ($foreignMods+'<Mod Id="kingmakermountedcombat" Enabled="true"/>'))|Out-Null}
Reject 'present KMC entry without Enabled refused' {Assert-KmcBaseGameParameters (New-OwnerParams ($foreignMods+'<Mod Id="KingmakerMountedCombat"/>'))|Out-Null}
Check 'exact default KMC append accepted when the original had none' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams $kmcAppended) -BaseGameOnly}
Check 'exact KMC append accepted in compact whitespace' {Assert-KmcNativeUmmStartupDelta $ownerParams ((New-OwnerParams '<Mod Id="KingmakerMountedCombat" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>') -replace "`n\s*",'') -BaseGameOnly}
Check 'exact KMC append with a forward LastUpdateCheck accepted' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams $kmcAppended '2026-10-17T09:41:01.4626062-04:00') -BaseGameOnly}
Check 'LastUpdateCheck-only forward change accepted' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams $foreignMods '2026-10-17T13:00:00.0000000Z') -BaseGameOnly}
Check 'KMC absent after a run without OnDestroy accepted (unchanged)' {Assert-KmcNativeUmmStartupDelta $ownerParams $ownerParams -BaseGameOnly}
Check 'KMC absent after a ModParams rewrite without KMC accepted' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams '') -BaseGameOnly}
# Sprint 1 UPG-01: retained dependencies staged beside KMC gain default entries in the loader's own order.
$cotwAppended="`n    <Mod Id=`"CallOfTheWild`" Enabled=`"true`"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>`n  "
$cmiAppended="`n    <Mod Id=`"CraftMagicItems`" Enabled=`"true`"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>`n  "
$depIds=@('CallOfTheWild','CraftMagicItems')
Check 'retained dependencies: KMC kept, defaults appended' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('</ModParams>',($cotwAppended+$cmiAppended+'</ModParams>'))) -BaseGameOnly -StagedModIds $depIds}
Check 'retained dependencies: loader load order (dependencies before the kept KMC entry)' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('<ModParams>',('<ModParams>'+$cotwAppended+$cmiAppended))) -BaseGameOnly -StagedModIds $depIds}
Check 'retained dependencies: all three appended when the original had none' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($cmiAppended+$kmcAppended+$cotwAppended)) -BaseGameOnly -StagedModIds $depIds}
Check 'retained dependencies: unchanged parameters still accepted' {Assert-KmcNativeUmmStartupDelta $ownerParams $ownerParams -BaseGameOnly -StagedModIds $depIds}
Reject 'retained dependencies: an undeclared third entry refused' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('</ModParams>',($cotwAppended+$cmiAppended+'<Mod Id="New" Enabled="true"/></ModParams>'))) -BaseGameOnly -StagedModIds $depIds}
Reject 'retained dependencies: a disabled dependency entry refused' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('</ModParams>',($cotwAppended.Replace('Enabled="true"','Enabled="false"')+$cmiAppended+'</ModParams>'))) -BaseGameOnly -StagedModIds $depIds}
Reject 'retained dependencies: appends without the staged declaration refused' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('</ModParams>',($cotwAppended+$cmiAppended+'</ModParams>'))) -BaseGameOnly}
Check 'runtime write classification admits writes confined to a retained dependency folder' {
    $staged=@([pscustomobject]@{kind='directory';path='CallOfTheWild';length=0;sha256=$null},[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Info.json';length=2;sha256=('b'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/loaded_blueprints.txt';length=1;sha256=('c'*64)})
    $runtime=[pscustomobject]@{entries=@([pscustomobject]@{kind='directory';path='CallOfTheWild';length=0;sha256=$null},[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Info.json';length=2;sha256=('b'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/loaded_blueprints.txt';length=5;sha256=('d'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Settings.xml';length=4;sha256=('e'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=3;sha256=('a'*64)})}
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $staged -RuntimeManifest $runtime -RetainedDependencies @([pscustomobject]@{id='CallOfTheWild'})
    Need (@($c.unexpected).Count-eq0) ('unexpected: '+(@($c.unexpected|ForEach-Object{$_.change+' '+$_.path})-join ','))
    Need (@($c.expected|Where-Object change -CEQ 'dependency-runtime-change').Count-eq1-and@($c.expected|Where-Object change -CEQ 'dependency-runtime-write').Count-eq1-and@($c.expected|Where-Object change -CEQ 'added').Count-eq1) 'expected classes'
}
Check 'runtime write classification still refuses dependency assembly, Info.json and foreign writes' {
    $staged=@([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Info.json';length=2;sha256=('b'*64)})
    $runtime=[pscustomobject]@{entries=@([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll';length=3;sha256=('f'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Info.json';length=9;sha256=('b'*64)},[pscustomobject]@{kind='file';path='Other/note.txt';length=1;sha256=('c'*64)})}
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $staged -RuntimeManifest $runtime -RetainedDependencies @([pscustomobject]@{id='CallOfTheWild'})
    Need (@($c.unexpected).Count-eq3-and@($c.expected).Count-eq0) 'three unexpected'
    $none=Get-KmcModsRuntimeWriteClassification -StagedEntries $staged -RuntimeManifest $runtime
    Need (@($none.unexpected).Count-eq3) 'without declaration'
}
# Sprint 1 iteration 6: a UMM loader cache in a dependency folder passes only the byte-copy rule, never the folder exception.
$cacheStaged=@([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Info.json';length=2;sha256=('b'*64)})
function New-CacheRuntime([object[]]$Added){[pscustomobject]@{entries=@($cacheStaged+$Added)}}
$cacheDep=@([pscustomobject]@{id='CallOfTheWild'})
Check 'dependency DLL cache: a byte-exact copy is accepted by the byte-copy rule alone' {
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $cacheStaged -RuntimeManifest (New-CacheRuntime @([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=3;sha256=('a'*64)})) -RetainedDependencies $cacheDep
    Need (@($c.unexpected).Count-eq0-and@($c.expected).Count-eq1-and$c.expected[0].change-ceq'added'-and$c.expected[0].path-ceq'CallOfTheWild/CallOfTheWild.dll.1234.cache') 'byte-exact cache classified added'
}
Check 'dependency DLL cache: a copy whose bytes differ is refused' {
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $cacheStaged -RuntimeManifest (New-CacheRuntime @([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=3;sha256=('f'*64)})) -RetainedDependencies $cacheDep
    Need (@($c.expected).Count-eq0-and@($c.unexpected).Count-eq1-and$c.unexpected[0].change-ceq'added') 'mismatching cache unexpected'
}
Check 'dependency DLL cache: a copy whose length differs is refused' {
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $cacheStaged -RuntimeManifest (New-CacheRuntime @([pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=4;sha256=('a'*64)})) -RetainedDependencies $cacheDep
    Need (@($c.expected).Count-eq0-and@($c.unexpected).Count-eq1) 'wrong-length cache unexpected'
}
Check 'dependency DLL cache: an orphaned cache, a bare .cache and a DLL-named stray are refused' {
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $cacheStaged -RuntimeManifest (New-CacheRuntime @([pscustomobject]@{kind='file';path='CallOfTheWild/Other.dll.5.cache';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/data.cache';length=1;sha256=('c'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.bak';length=3;sha256=('a'*64)})) -RetainedDependencies $cacheDep
    Need (@($c.expected).Count-eq0-and@($c.unexpected).Count-eq3) ('three unexpected: '+(@($c.unexpected|ForEach-Object{$_.change+' '+$_.path})-join ','))
}
Check 'dependency DLL cache: a changed staged cache and a new executable are refused, data writes still admitted' {
    $staged=@($cacheStaged+[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=3;sha256=('a'*64)})
    $runtime=[pscustomobject]@{entries=@($cacheStaged+[pscustomobject]@{kind='file';path='CallOfTheWild/CallOfTheWild.dll.1234.cache';length=3;sha256=('f'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Extra.dll';length=3;sha256=('a'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/dumps/trace.txt';length=1;sha256=('c'*64)},[pscustomobject]@{kind='file';path='CallOfTheWild/Settings.xml';length=4;sha256=('e'*64)})}
    $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $staged -RuntimeManifest $runtime -RetainedDependencies $cacheDep
    Need (@($c.unexpected).Count-eq2-and@($c.unexpected|Where-Object change -CEQ 'changed').Count-eq1-and@($c.unexpected|Where-Object path -CEQ 'CallOfTheWild/Extra.dll').Count-eq1) ('unexpected: '+(@($c.unexpected|ForEach-Object{$_.change+' '+$_.path})-join ','))
    Need (@($c.expected).Count-eq2-and@($c.expected|Where-Object change -CEQ 'dependency-runtime-write').Count-eq2) 'nested data writes admitted'
}
foreach($case in @(
    @('disabled KMC append',(New-OwnerParams $kmcAppended.Replace('Enabled="true"','Enabled="false"'))),
    @('foreign append beside KMC',(New-OwnerParams ($kmcAppended+'<Mod Id="New" Enabled="true"/>'))),
    @('foreign-only append',(New-OwnerParams '<Mod Id="New" Enabled="true"/>')),
    @('KMC appended while foreign entries remain',(New-OwnerParams ($foreignMods+$kmcAppended))),
    @('KMC append with an extra attribute',(New-OwnerParams $kmcAppended.Replace('Enabled="true"','Enabled="true" Extra="1"'))),
    @('KMC append with a changed hotkey',(New-OwnerParams $kmcAppended.Replace('<keyCode>None','<keyCode>F10'))),
    @('KMC append with an extra element',(New-OwnerParams $kmcAppended.Replace('</Hotkey>','</Hotkey><Extra/>'))),
    @('KMC append without its hotkey',(New-OwnerParams '<Mod Id="KingmakerMountedCombat" Enabled="true"/>')),
    @('LastUpdateCheck moved backwards',(New-OwnerParams $kmcAppended '2026-10-15T00:00:00.0000000Z')),
    @('LastUpdateCheck not an xs:dateTime',(New-OwnerParams $foreignMods 'yesterday')),
    @('LastUpdateCheck removed',(New-OwnerParams $foreignMods).Replace("<LastUpdateCheck>$stamp</LastUpdateCheck>",'')),
    @('LastUpdateCheck with an attribute',(New-OwnerParams $foreignMods '2026-10-17T13:00:00Z').Replace('<LastUpdateCheck>','<LastUpdateCheck x="1">')),
    @('other element change',(New-OwnerParams $kmcAppended $stamp '0')),
    @('other element added',(New-OwnerParams $kmcAppended).Replace('<UIScale>1</UIScale>','<UIScale>1</UIScale><UIFont>Arial</UIFont>')),
    @('root attribute changed',(New-OwnerParams $kmcAppended).Replace('xmlns:xsd="http://www.w3.org/2001/XMLSchema"','')))){
    Reject ('base-only UMM refuses '+$case[0]) {Assert-KmcNativeUmmStartupDelta $ownerParams $case[1] -BaseGameOnly}
}
Reject 'base-only UMM refuses a default KMC append replacing an existing KMC entry hotkey' {Assert-KmcNativeUmmStartupDelta $f.parameters.Replace('<keyCode>None','<keyCode>F10') $reduced -BaseGameOnly}

$stage=Join-Path $f.home 'staging';$backups=Join-Path $f.home 'backups';$state=Join-Path $f.home 'state'
foreach($dir in @($stage,$backups,$state)){[void][IO.Directory]::CreateDirectory($dir)}
$packageSource=Join-Path $f.home 'package-source/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory($packageSource)
[IO.File]::WriteAllText((Join-Path $packageSource 'Info.json'),'{"Id":"KingmakerMountedCombat"}')
[IO.File]::WriteAllText((Join-Path $packageSource 'KingmakerMountedCombat.dll'),'original test-only product')
$package=Join-Path $f.home 'package.zip';Compress-Archive -LiteralPath $packageSource -DestinationPath $package
$before=Get-KmcDirectoryManifest $f.mods
$preview=Join-Path $stage 'preview';[void][IO.Directory]::CreateDirectory($preview)
$previewClone=Join-Path $preview 'Mods-live-clone';[void](Copy-KmcDirectoryTreeExact $f.mods $previewClone)
$previewPackage=Join-Path $preview 'package-overlay/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory((Split-Path $previewPackage));[void](Copy-KmcDirectoryTreeExact $packageSource $previewPackage)
$previewBefore=Get-KmcDirectoryManifest $preview
Check 'ready-tree WhatIf validates without mutating staging or actual fixture' {
    New-KmcBaseGameReadyTree -StagingRun $preview -CloneRoot $previewClone -ReadyRoot (Join-Path $preview 'Mods-ready') -PackageModRoot $previewPackage -Before $before -WhatIf
    Assert-KmcDirectoryManifestsEqual $previewBefore (Get-KmcDirectoryManifest $preview) 'WhatIf staging purity'
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'WhatIf original fixture purity'
}
Reject 'ready tree outside its owned staging run rejected before creation' {New-KmcBaseGameReadyTree -StagingRun $preview -CloneRoot $previewClone -ReadyRoot (Join-Path $f.home 'outside') -PackageModRoot $previewPackage -Before $before -Confirm:$false}
[IO.File]::WriteAllText((Join-Path $previewPackage 'Foreign.dll'),'foreign')
Reject 'foreign package payload rejected before ready-tree creation' {New-KmcBaseGameReadyTree -StagingRun $preview -CloneRoot $previewClone -ReadyRoot (Join-Path $preview 'Mods-ready') -PackageModRoot $previewPackage -Before $before -Confirm:$false}
# Sprint 1 UPG-01: retained foreign dependencies staged beside KMC in the ready tree.
$depPreview=Join-Path $stage 'preview-deps';[void][IO.Directory]::CreateDirectory($depPreview)
$depClone=Join-Path $depPreview 'Mods-live-clone';[void](Copy-KmcDirectoryTreeExact $f.mods $depClone)
$depPackage=Join-Path $depPreview 'package-overlay/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory((Split-Path $depPackage));[void](Copy-KmcDirectoryTreeExact $packageSource $depPackage)
$depSource=Join-Path $f.home 'retained/CallOfTheWild';[void][IO.Directory]::CreateDirectory((Join-Path $depSource 'Data'))
[IO.File]::WriteAllText((Join-Path $depSource 'info.json'),'{"Id":"CallOfTheWild","Version":"1.14.4c-2.1","AssemblyName":"CallOfTheWild.dll"}')
[IO.File]::WriteAllText((Join-Path $depSource 'CallOfTheWild.dll'),'retained dependency assembly')
[IO.File]::WriteAllText((Join-Path $depSource 'CallOfTheWild.dll.10016.cache'),'stale loader cache')
[IO.File]::WriteAllText((Join-Path $depSource 'Data/settings.json'),'{"a":1}')
$depManifest=Get-KmcRetainedDependencyManifest $depSource
$dep=[pscustomobject]@{id='CallOfTheWild';version='1.14.4c-2.1';sourceRunId='test';sourcePath=$depSource;dllName='CallOfTheWild.dll';dllSha256=(Get-KmcSha256 (Join-Path $depSource 'CallOfTheWild.dll'));digest=$depManifest.digest;fileCount=$depManifest.fileCount}
Check 'retained dependency manifest excludes the loader cache and pins every other file' {Need ($depManifest.fileCount-eq3-and@($depManifest.excludedCacheFiles).Count-eq1-and$depManifest.excludedCacheFiles[0]-ceq'CallOfTheWild.dll.10016.cache') 'manifest shape'}
Check 'ready tree stages the retained dependency beside KMC without its loader cache' {
    New-KmcBaseGameReadyTree -StagingRun $depPreview -CloneRoot $depClone -ReadyRoot (Join-Path $depPreview 'Mods-ready') -PackageModRoot $depPackage -Before $before -RetainedDependencies @($dep) -Confirm:$false
    $ready=Join-Path $depPreview 'Mods-ready'
    $top=@(Get-ChildItem -LiteralPath $ready -Force|ForEach-Object Name|Sort-Object)
    Need (($top-join '|')-ceq'CallOfTheWild|KingmakerMountedCombat') ('top-level set: '+($top-join '|'))
    Need (-not(Test-Path -LiteralPath (Join-Path $ready 'CallOfTheWild/CallOfTheWild.dll.10016.cache'))) 'cache copied'
    Need ((Get-KmcRetainedDependencyManifest (Join-Path $ready 'CallOfTheWild')).digest-ceq$dep.digest) 'staged digest'
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $depClone) 'preserved clone'
}
foreach($fault in @(
    @('a reserved id',{param($d)$d.id='KingmakerMountedCombat'}),
    @('a changed assembly pin',{param($d)$d.dllSha256='0'*64}),
    @('a changed folder digest',{param($d)$d.digest='1'*64}),
    @('a source outside the lab',{param($d)$d.sourcePath='C:\Windows\Temp\CallOfTheWild'}))){
    $script:faultName=$fault[0];$script:mutate=$fault[1]
    Reject ('ready tree refuses a retained dependency with '+$script:faultName) {
        $bad=$dep.PSObject.Copy();&$script:mutate $bad
        $run=Join-Path $stage ('preview-fault-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($run)
        $clone=Join-Path $run 'Mods-live-clone';[void](Copy-KmcDirectoryTreeExact $f.mods $clone)
        $pkg=Join-Path $run 'package-overlay/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory((Split-Path $pkg));[void](Copy-KmcDirectoryTreeExact $packageSource $pkg)
        New-KmcBaseGameReadyTree -StagingRun $run -CloneRoot $clone -ReadyRoot (Join-Path $run 'Mods-ready') -PackageModRoot $pkg -Before $before -RetainedDependencies @($bad) -Confirm:$false
    }
}

$lock=Open-KmcRuntimeLock $state 'base-only-positive'
try{
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc -BaseGameDependencies $receipt
    $record=Read-KmcJson $transaction
    Check 'explicit base-only mode uses schema 4 and preserves the complete live clone' {
        Need ($record.schemaVersion-eq4-and$record.stagingMode-ceq'base-game-only-kmc') 'new receipt identity'
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $record.preservedClone) 'complete preserved clone'
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $record.originalBackup) 'untouched original snapshot'
    }
    Check 'staged Mods contains only KMC and transaction sentinel' {
        $actual=@((Get-KmcDirectoryManifest $f.mods).entries|ForEach-Object {'{0}|{1}'-f$_.kind,$_.path}|Sort-Object)
        $expected=@('directory|KingmakerMountedCombat','file|.kmc-runtime-sentinel.json','file|KingmakerMountedCombat/Info.json','file|KingmakerMountedCombat/KingmakerMountedCombat.dll')|Sort-Object
        Need (($actual-join ';')-ceq($expected-join ';')) 'foreign content remains in ready tree'
        Assert-KmcDirectoryManifestsEqual $beforeDependencies (Get-KmcDirectoryManifest (Split-Path $f.loader)) 'installed loader unchanged'
    }
    $originalRecord=[IO.File]::ReadAllBytes($transaction)
    try{
        $record.baseGameDependencies.dependencyDigest='0'*64;Write-KmcJsonAtomic $transaction $record
        Reject 'altered dependency receipt cannot authorize restoration' {Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage|Out-Null}
    }finally{[IO.File]::WriteAllBytes($transaction,$originalRecord)}
    try{
        $record=Read-KmcJson $transaction;$record.schemaVersion=3;Write-KmcJsonAtomic $transaction $record
        Reject 'historical schema cannot claim new mode' {Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage|Out-Null}
    }finally{[IO.File]::WriteAllBytes($transaction,$originalRecord)}
    [IO.File]::WriteAllText((Join-Path $f.mods 'runtime-created.txt'),'owned staged output')
    [void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    Check 'runtime output quarantined and actual original tree restored exactly' {
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'restored original'
        $settled=Read-KmcJson $transaction
        Need ($settled.phase-ceq'restored'-and$settled.restoredDigest-ceq$before.digest-and$settled.stagedTreeChangedAtRuntime) 'settled receipt'
        Need (Test-Path -LiteralPath (Join-Path $settled.stagedAfter 'runtime-created.txt')) 'runtime evidence lost'
    }
    Check 'restoration retry is idempotent' {[void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}
}finally{
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){
        [void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    }
    Close-KmcRuntimeLock $lock
}
$lock=Open-KmcRuntimeLock $state 'legacy-default-positive'
try{
    Reject 'dependency receipt does not silently select base-only mode' {Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -BaseGameDependencies $receipt|Out-Null}
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage
    Check 'ordinary default retains foreign mods and historical schema 3' {
        Need ((Read-KmcJson $transaction).schemaVersion-eq3) 'ordinary receipt changed'
        Need (Test-Path -LiteralPath (Join-Path $f.mods 'ForeignOne/cache/owned.bin')) 'ordinary foreign tree removed'
        Need ((Get-KmcSha256 (Join-Path $f.mods 'ForeignTwo/assembly.dll'))-ceq(Get-KmcTextSha256 'foreign opaque code')) 'foreign code changed'
    }
}finally{
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){[void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}
    Close-KmcRuntimeLock $lock
}
# Simulate the guarded original-moved crash window in this synthetic installation.
$lock=Open-KmcRuntimeLock $state 'base-only-crash-window'
try{
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc -BaseGameDependencies $receipt
    $record=Read-KmcJson $transaction
    $ownedLive=Assert-KmcChildPath $f.mods $f.home 'synthetic crash-window Mods'
    $ownedReady=Assert-KmcChildPath $record.stagedReady (Join-Path $stage $lock.RunId) 'synthetic crash-window ready tree'
    Move-Item -LiteralPath $ownedLive -Destination $ownedReady
    $record.phase='original-moved';Write-KmcJsonAtomic $transaction $record
    [void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    Check 'original-moved crash window restores missing Mods from exact original snapshot' {
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'crash-window original restored'
        Need ((Read-KmcJson $transaction).phase-ceq'restored') 'crash-window state not settled'
    }
}finally{
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){[void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}
    Close-KmcRuntimeLock $lock
}
$settledBase=Read-KmcJson (Get-KmcTransactionStatePath $state 'base-only-positive')
Check 'settings authority comes from the settled exact owned base-only receipt' {
    Assert-KmcBaseGameSettingsAuthority $settledBase $settledBase.runId $settledBase.token $f.game
}
foreach($case in @(@('runId','other-run'),@('token',('0'*64)),@('phase','staged'),@('schemaVersion',3),@('stagingMode','live-clone-plus-kmc-overlay'),@('liveModsRoot',(Join-Path $f.home 'unrelated')))){
    $badState=$settledBase|ConvertTo-Json -Depth 8|ConvertFrom-Json;$badState.($case[0])=$case[1]
    Reject ('settings restoration refuses mismatched '+$case[0]) {Assert-KmcBaseGameSettingsAuthority $badState $settledBase.runId $settledBase.token $f.game}
}
Reject 'settings restoration refuses another installation' {Assert-KmcBaseGameSettingsAuthority $settledBase $settledBase.runId $settledBase.token (Join-Path $f.home 'another-game')}

# Request-free normal-player control: the isolated tree holds exactly KMC and the
# external observer. Every other mode stays closed to the second package.
$observerAppended="`n    <Mod Id=`"KmcRemovalObserver`" Enabled=`"true`">`n      <Hotkey>`n        <keyCode>None</keyCode>`n        <modifiers>0</modifiers>`n      </Hotkey>`n    </Mod>`n  "
Check 'co-staged UMM admits KMC then observer default appends' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($kmcAppended+$observerAppended)) -BaseGameOnly -WithObserver}
Check 'co-staged UMM admits observer then KMC default appends' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($observerAppended+$kmcAppended)) -BaseGameOnly -WithObserver}
Check 'co-staged UMM admits an observer append beside an existing KMC entry' {Assert-KmcNativeUmmStartupDelta $f.parameters ($reduced.Replace('</ModParams>','<Mod Id="KmcRemovalObserver" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod></ModParams>')) -BaseGameOnly -WithObserver}
# Batch191 native shape: the owner's settled parameters already held an observer entry;
# UMM rewrote the found pair in its own load order, KMC before the observer.
$ownerWithObserver=New-OwnerParams ($foreignMods+$observerAppended)
Check 'co-staged UMM admits the native load-order rewrite of a kept observer and appended KMC' {Assert-KmcNativeUmmStartupDelta $ownerWithObserver (New-OwnerParams ($kmcAppended+$observerAppended) '2026-10-23T00:13:25.335868-04:00') -BaseGameOnly -WithObserver}
Check 'co-staged UMM admits the same pair in kept-then-appended order' {Assert-KmcNativeUmmStartupDelta $ownerWithObserver (New-OwnerParams ($observerAppended+$kmcAppended)) -BaseGameOnly -WithObserver}
Reject 'co-staged UMM refuses the reordered pair with a changed observer entry' {Assert-KmcNativeUmmStartupDelta $ownerWithObserver (New-OwnerParams ($kmcAppended+$observerAppended.Replace('<keyCode>None','<keyCode>F9'))) -BaseGameOnly -WithObserver}
Reject 'KMC-only base mode still refuses the reordered pair' {Assert-KmcNativeUmmStartupDelta $ownerWithObserver (New-OwnerParams ($kmcAppended+$observerAppended)) -BaseGameOnly}
Reject 'KMC-only base mode refuses an observer append' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($kmcAppended+$observerAppended)) -BaseGameOnly}
Reject 'co-staged UMM refuses a third appended entry' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($kmcAppended+$observerAppended+'<Mod Id="New" Enabled="true"/>')) -BaseGameOnly -WithObserver}
Reject 'co-staged UMM refuses a disabled observer append' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams ($kmcAppended+$observerAppended.Replace('Enabled="true"','Enabled="false"'))) -BaseGameOnly -WithObserver}
Reject 'co-staged authority cannot be used outside base-only mode' {Assert-KmcNativeUmmStartupDelta $ownerParams (New-OwnerParams $kmcAppended) -WithObserver}
$observerSource=Join-Path $f.home 'observer-source/KmcRemovalObserver';[void][IO.Directory]::CreateDirectory($observerSource)
[IO.File]::WriteAllText((Join-Path $observerSource 'Info.json'),'{"Id":"KmcRemovalObserver"}')
[IO.File]::WriteAllText((Join-Path $observerSource 'KmcRemovalObserver.dll'),'test-only observer')
$observerPackage=Join-Path $f.home 'observer.zip';Compress-Archive -LiteralPath $observerSource -DestinationPath $observerPackage
$foreignObserverSource=Join-Path $f.home 'observer-foreign/KmcRemovalObserver';[void](Copy-KmcDirectoryTreeExact $observerSource $foreignObserverSource)
[IO.File]::WriteAllText((Join-Path $foreignObserverSource 'Extra.dll'),'foreign')
$foreignObserverPackage=Join-Path $f.home 'observer-foreign.zip';Compress-Archive -LiteralPath $foreignObserverSource -DestinationPath $foreignObserverPackage
$lock=Open-KmcRuntimeLock $state 'co-staged-refusals'
try{
    Reject 'base-only KMC mode refuses a second observer package' {Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc -BaseGameDependencies $receipt -ObserverPackagePath $observerPackage|Out-Null}
    Reject 'co-staging mode refuses a missing observer package' {Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc-observer -BaseGameDependencies $receipt|Out-Null}
    Reject 'removal-observer mode refuses co-staging' {Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $observerPackage -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode live-clone-minus-kmc-plus-observer -ObserverPackagePath $observerPackage|Out-Null}
    Check 'contract refusals left the original tree exact and no transaction state' {
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'refused co-staging original'
        Need (-not(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId))) 'refusal left state'
    }
}finally{Close-KmcRuntimeLock $lock}
$lock=Open-KmcRuntimeLock $state 'co-staged-partial-failure'
try{
    Reject 'foreign observer payload refuses co-staging before activation' {Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc-observer -BaseGameDependencies $receipt -ObserverPackagePath $foreignObserverPackage|Out-Null}
    Check 'partial co-staging failure leaves the live tree exactly original' {
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'partial co-staging original'
        Need ($null-eq(Read-KmcLiveSentinel $f.mods)) 'live sentinel after refused co-staging'
    }
}finally{Close-KmcRuntimeLock $lock}
$lock=Open-KmcRuntimeLock $state 'co-staged-positive'
try{
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc-observer -BaseGameDependencies $receipt -ObserverPackagePath $observerPackage
    $record=Read-KmcJson $transaction
    Check 'co-staged mode records schema 4, its frozen observer package and the complete live clone' {
        Need ($record.schemaVersion-eq4-and$record.stagingMode-ceq'base-game-only-kmc-observer') 'co-staged receipt identity'
        Need ($record.observerPackageSha256-ceq(Get-KmcSha256 $observerPackage)-and(Get-KmcSha256 $record.frozenObserverPackage)-ceq$record.observerPackageSha256) 'frozen observer identity'
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $record.preservedClone) 'co-staged preserved clone'
    }
    Check 'co-staged Mods contains exactly KMC, the observer and the sentinel' {
        $actual=@((Get-KmcDirectoryManifest $f.mods).entries|ForEach-Object {'{0}|{1}'-f$_.kind,$_.path}|Sort-Object)
        $expected=@('directory|KingmakerMountedCombat','directory|KmcRemovalObserver','file|.kmc-runtime-sentinel.json','file|KingmakerMountedCombat/Info.json',
            'file|KingmakerMountedCombat/KingmakerMountedCombat.dll','file|KmcRemovalObserver/Info.json','file|KmcRemovalObserver/KmcRemovalObserver.dll')|Sort-Object
        Need (($actual-join ';')-ceq($expected-join ';')) ('co-staged tree differs: '+($actual-join ';'))
    }
    $originalRecord=[IO.File]::ReadAllBytes($transaction)
    try{
        $record=Read-KmcJson $transaction;$record.PSObject.Properties.Remove('observerPackageSha256');Write-KmcJsonAtomic $transaction $record
        Reject 'co-staged state without its observer identity cannot restore' {Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage|Out-Null}
    }finally{[IO.File]::WriteAllBytes($transaction,$originalRecord)}
    try{
        $record=Read-KmcJson $transaction;$record.frozenObserverPackage=(Join-Path $f.home 'elsewhere.zip');Write-KmcJsonAtomic $transaction $record
        Reject 'co-staged state naming a foreign frozen observer cannot restore' {Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage|Out-Null}
    }finally{[IO.File]::WriteAllBytes($transaction,$originalRecord)}
    [IO.File]::WriteAllText((Join-Path $f.mods 'KmcRemovalObserver/runtime.log'),'observer output')
    [void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    Check 'co-staged runtime output quarantined and the original restored exactly' {
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'co-staged restored original'
        $settled=Read-KmcJson $transaction
        Need ($settled.phase-ceq'restored'-and(Test-Path -LiteralPath (Join-Path $settled.stagedAfter 'KmcRemovalObserver/runtime.log'))) 'co-staged settlement'
    }
    $coSettled=Read-KmcJson $transaction
    Check 'settings authority accepts the settled co-staged receipt' {Assert-KmcBaseGameSettingsAuthority $coSettled $coSettled.runId $coSettled.token $f.game}
}finally{
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){[void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}
    Close-KmcRuntimeLock $lock
}
# F12 carry (7E X5): Enter declares the carried previous-install cache in the state and
# stages it; Restore validates that declaration and passes it to the classifier, which names
# UMM's removal of exactly that cache and admits the candidate's new cache.
$lock=Open-KmcRuntimeLock $state 'co-staged-carry'
$labRootBeforeCarry=${function:Get-KmcLabRoot}
try{
    Set-Item function:Get-KmcLabRoot ([scriptblock]::Create("'"+$f.home+"'"))
    $carryDir=Join-Path $f.home 'runtime-staging/u1-carry/Mods-staged-after/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory($carryDir)
    $carryFile=Join-Path $carryDir 'KingmakerMountedCombat.dll.4242.cache';[IO.File]::WriteAllText($carryFile,'previous package dll bytes')
    $carried=@([pscustomobject][ordered]@{name='KingmakerMountedCombat.dll.4242.cache';sourcePath=$carryFile;length=[long](Get-Item -LiteralPath $carryFile).Length;sha256=(Get-KmcSha256 $carryFile)})
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc-observer -BaseGameDependencies $receipt -ObserverPackagePath $observerPackage -KmcCarryFiles $carried
    $record=Read-KmcJson $transaction
    Check 'carry: Enter declares exactly the carried cache in the state and stages it beside the package' {
        $declared=@($record.kmcCarriedFiles)
        Need ($declared.Count-eq1-and[string]$declared[0].name-ceq$carried[0].name-and[string]$declared[0].sha256-ceq$carried[0].sha256-and[long]$declared[0].length-eq$carried[0].length) 'declared carry differs'
        Need ((Get-KmcSha256 (Join-Path $f.mods 'KingmakerMountedCombat/KingmakerMountedCombat.dll.4242.cache'))-ceq$carried[0].sha256) 'carried cache not staged'
    }
    $originalRecord=[IO.File]::ReadAllBytes($transaction)
    foreach($bad in @(@('a declared file outside the cache shape',{param($r)$r.kmcCarriedFiles[0].name='Settings.xml'},'outside the closed UMM cache shape'),@('an unbounded declaration',{param($r)$r.kmcCarriedFiles=@($r.kmcCarriedFiles[0])*5},'Declared carried KMC folder files are unbounded'),
        @('an empty declaration',{param($r)$r.kmcCarriedFiles=@()},'Declared carried KMC folder files are unbounded'),@('a declaration with an extra member',{param($r)$r.kmcCarriedFiles[0]|Add-Member extra 1},'declared carried KMC file property set is not exact'))){
        try{
            $r=Read-KmcJson $transaction;&$bad[1] $r;Write-KmcJsonAtomic $transaction $r
            Check ('carry: Restore refuses '+$bad[0]) {$message=$null;try{Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage|Out-Null}catch{$message=$_.Exception.Message};Need ([string]$message-cmatch[regex]::Escape($bad[2])) ('refusal differs: '+$message)}
        }finally{[IO.File]::WriteAllBytes($transaction,$originalRecord)}
    }
    # UMM's own cache miss: it deletes every '*.cache*' in the mod folder, then copies the DLL.
    Remove-Item -LiteralPath (Join-Path $f.mods 'KingmakerMountedCombat/KingmakerMountedCombat.dll.4242.cache')
    Copy-Item -LiteralPath (Join-Path $f.mods 'KingmakerMountedCombat/KingmakerMountedCombat.dll') -Destination (Join-Path $f.mods 'KingmakerMountedCombat/KingmakerMountedCombat.dll.777.cache')
    [void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    Check 'carry: Restore names the removal of the declared cache and admits the new candidate cache, then restores exactly' {
        $settled=Read-KmcJson $transaction
        Need ($settled.phase-ceq'restored'-and@($settled.runtimeUnexpectedWrites).Count-eq0) ('unexpected writes: '+(@($settled.runtimeUnexpectedWrites)|ConvertTo-Json -Compress -Depth 4))
        $changes=@(@($settled.runtimeExpectedWrites)|ForEach-Object{[string]$_.change+':'+[string]$_.path}|Sort-Object)
        Need (($changes-join'|')-ceq'added:KingmakerMountedCombat/KingmakerMountedCombat.dll.777.cache|removed-carried-cache:KingmakerMountedCombat/KingmakerMountedCombat.dll.4242.cache') ('expected writes: '+($changes-join'|'))
        Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'carry restored original'
    }
}finally{
    Set-Item function:Get-KmcLabRoot $labRootBeforeCarry
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){$s=Read-KmcJson (Get-KmcTransactionStatePath $state $lock.RunId);if([string]$s.phase-cne'restored'){[void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}}
    Close-KmcRuntimeLock $lock
}
$lock=Open-KmcRuntimeLock $state 'co-staged-crash-window'
try{
    $transaction=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $f.mods -PackagePath $package -StateRoot $state -BackupRoot $backups -StagingRoot $stage -StagingMode base-game-only-kmc-observer -BaseGameDependencies $receipt -ObserverPackagePath $observerPackage
    $record=Read-KmcJson $transaction
    Move-Item -LiteralPath (Assert-KmcChildPath $f.mods $f.home 'co-staged crash Mods') -Destination (Assert-KmcChildPath $record.stagedReady (Join-Path $stage $lock.RunId) 'co-staged crash ready')
    $record.phase='original-moved';Write-KmcJsonAtomic $transaction $record
    [void](Restore-KmcModsTransaction -Lock $lock -StatePath $transaction -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)
    Check 'co-staged original-moved crash window restores the exact original' {Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'co-staged crash restored'}
}finally{
    if(Test-Path -LiteralPath (Get-KmcTransactionStatePath $state $lock.RunId)){[void](Restore-KmcModsTransaction -Lock $lock -StatePath (Get-KmcTransactionStatePath $state $lock.RunId) -LiveModsRoot $f.mods -BackupRoot $backups -StagingRoot $stage)}
    Close-KmcRuntimeLock $lock
}
Check 'changed runtime scripts parse without executing a launcher' {
    foreach($script in @('runtime/BaseGameIsolation.ps1','runtime/RuntimeHarness.Common.ps1','runtime/Invoke-KingmakerRuntimeScenario.ps1','runtime/PersistenceProfileProtection.ps1','runtime/Recover-KingmakerRuntimeTransaction.ps1','runtime/Recover-KmcPersistenceProfile.ps1')){
        $tokens=$null;$parseErrors=$null
        [void][Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $script),[ref]$tokens,[ref]$parseErrors)
        if(@($parseErrors).Count-ne0){throw ('Parser errors in '+$script+': '+($parseErrors|Out-String))}
    }
}
Check 'all offline fixture original bytes remain exact' {Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $f.mods) 'final original fixture';Assert-KmcDirectoryManifestsEqual $beforeDependencies (Get-KmcDirectoryManifest (Split-Path $f.loader)) 'final loader fixture'}
Write-Host ('Base-game isolation: '+$passed+' PASS / '+$failed+' FAIL; evidence '+$testRoot)
if($failed-ne0){exit 1}
