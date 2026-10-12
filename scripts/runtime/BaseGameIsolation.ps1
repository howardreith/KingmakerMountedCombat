Set-StrictMode -Version Latest

# This opt-in mode leaves the installed loader outside Mods untouched. The
# original Mods tree is still snapshotted and moved/restored by the existing
# transaction; only the private ready tree contains KMC alone.
function Read-KmcBaseGameXml {
    param([Parameter(Mandatory=$true)][string]$Text)
    if($Text.Length -gt 65536){throw 'Base-game loader XML exceeds its bounded contract.'}
    $settings=New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver=$null
    $xmlInput=New-Object IO.StringReader($Text)
    $reader=[Xml.XmlReader]::Create($xmlInput,$settings)
    try{$document=New-Object Xml.XmlDocument;$document.XmlResolver=$null;$document.Load($reader);return $document}
    finally{$reader.Dispose();$xmlInput.Dispose()}
}

function Assert-KmcBaseGameParameters {
    param([Parameter(Mandatory=$true)][string]$Text)
    $document=Read-KmcBaseGameXml $Text
    $containers=@($document.SelectNodes('/Param/ModParams'))
    if($containers.Count-ne1){throw 'Base-game mode requires the exact installed UMM ModParams surface.'}
    $mods=@($document.SelectNodes('/Param/ModParams/Mod'))
    if(@($document.SelectNodes('//Mod')).Count-ne$mods.Count){throw 'UMM mod entries exist outside the expected container.'}
    $ids=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($mod in $mods){if([string]::IsNullOrWhiteSpace($mod.Id)-or-not$ids.Add([string]$mod.Id)){throw 'UMM mod parameter identity is empty or ambiguous.'}}
    $kmc=@($mods|Where-Object {[string]$_.Id-ceq'KingmakerMountedCombat'})
    if($kmc.Count-ne1-or[string]$kmc[0].Enabled-cne'true'){throw 'Base-game mode requires the existing exact KMC entry enabled; it never changes owner settings to enable it.'}
    return $document
}

function Get-KmcBaseGameDependencyDigest {
    param([Parameter(Mandatory=$true)]$Receipt)
    [string[]]$rows=@($Receipt.files|ForEach-Object {'{0}|{1}|{2}' -f $_.relativePath,$_.length,$_.sha256})
    [Array]::Sort($rows,[StringComparer]::Ordinal)
    return Get-KmcTextSha256 ((@([string]$Receipt.gameRoot,[string]$Receipt.configSha256)+$rows)-join "
")
}

function Assert-KmcBaseGameDependencyReceipt {
    param([Parameter(Mandatory=$true)]$Receipt,[switch]$VerifyFiles)
    if($Receipt.schemaVersion-ne1-or[string]$Receipt.mode-cne'base-game-only-kmc'){throw 'Base-game dependency receipt schema or mode differs.'}
    $game=[IO.Path]::GetFullPath([string]$Receipt.gameRoot).TrimEnd('\')
    if($game-cne[string]$Receipt.gameRoot-or[string]$Receipt.modsRoot-cne(Join-Path $game 'Mods')-or
        [string]$Receipt.loaderRoot-cne(Join-Path $game 'Kingmaker_Data/Managed/UnityModManager')){throw 'Base-game dependency paths differ from the exact installed layout.'}
    $expected=@('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll',
        'Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony-1.2.dll',
        'Kingmaker_Data/Managed/UnityModManager/0Harmony.dll','Kingmaker_Data/Managed/UnityModManager/dnlib.dll')
    if(@($Receipt.files).Count-ne$expected.Count){throw 'Base-game dependency set differs.'}
    foreach($relative in $expected){
        $matchingFiles=@($Receipt.files|Where-Object {[string]$_.relativePath-ceq$relative})
        if($matchingFiles.Count-ne1-or[long]$matchingFiles[0].length-le0-or[string]$matchingFiles[0].sha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Base-game dependency identity is missing or ambiguous.'}
        if($VerifyFiles){
            $path=Assert-KmcChildPath (Join-Path $game $relative) $game 'base-game dependency'
            Assert-KmcNotReparsePoint $path 'base-game dependency';Assert-KmcNotHardLink $path 'base-game dependency'
            if((Get-Item -LiteralPath $path).Length-ne[long]$matchingFiles[0].length-or(Get-KmcSha256 $path)-cne[string]$matchingFiles[0].sha256){throw 'Base-game dependency changed after admission.'}
        }
    }
    if([string]$Receipt.configSha256-cnotmatch'^[0-9a-f]{64}$'-or
        [string]$Receipt.dependencyDigest-cne(Get-KmcBaseGameDependencyDigest $Receipt)){throw 'Base-game dependency receipt digest differs.'}
    if($VerifyFiles){
        $config=Join-Path ([string]$Receipt.loaderRoot) 'Config.xml'
        Assert-KmcNotReparsePoint $config 'base-game loader config';Assert-KmcNotHardLink $config 'base-game loader config'
        if((Get-KmcSha256 $config)-cne[string]$Receipt.configSha256){throw 'Base-game loader config changed after admission.'}
        $dlls=@(Get-ChildItem -LiteralPath $Receipt.loaderRoot -Force -File|Where-Object {$_.Extension-ieq'.dll'}|ForEach-Object {$_.Name}|Sort-Object)
        if(($dlls-join '|')-cne((@('0Harmony-1.2.dll','0Harmony.dll','0Harmony12.dll','dnlib.dll','UnityModManager.dll')|Sort-Object)-join '|')){throw 'Base-game loader contains an unknown or missing DLL.'}
    }
}

function Get-KmcBaseGameDependencies {
    param([Parameter(Mandatory=$true)][string]$GameRoot,[Parameter(Mandatory=$true)]$Fingerprint)
    $game=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
    $loader=Join-Path $game 'Kingmaker_Data/Managed/UnityModManager'
    Assert-KmcNotReparsePoint $loader 'base-game installed loader'
    $config=Join-Path $loader 'Config.xml'
    Assert-KmcNotReparsePoint $config 'base-game loader config';Assert-KmcNotHardLink $config 'base-game loader config'
    $xml=Read-KmcBaseGameXml ([IO.File]::ReadAllText($config))
    if([string]$xml.Config.ModsDirectory-cne'Mods'-or[string]$xml.Config.ModInfo-cne'Info.json'-or
        [string]$xml.Config.GameExe-cne'Kingmaker.exe'){throw 'Base-game mode refuses a redirected UMM loader layout.'}
    $params=Join-Path $loader 'Params.xml'
    Assert-KmcNotReparsePoint $params 'base-game UMM parameters';Assert-KmcNotHardLink $params 'base-game UMM parameters'
    [void](Assert-KmcBaseGameParameters ([IO.File]::ReadAllText($params)))
    $relativePaths=@('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll',
        'Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony-1.2.dll',
        'Kingmaker_Data/Managed/UnityModManager/0Harmony.dll','Kingmaker_Data/Managed/UnityModManager/dnlib.dll')
    $files=@(foreach($relative in $relativePaths){
        $path=Assert-KmcChildPath (Join-Path $game $relative) $game 'base-game dependency'
        Assert-KmcNotReparsePoint $path 'base-game dependency';Assert-KmcNotHardLink $path 'base-game dependency'
        [pscustomobject][ordered]@{relativePath=$relative;length=[long](Get-Item -LiteralPath $path).Length;sha256=Get-KmcSha256 $path}
    })
    foreach($pin in @(@('gameplayAssembly',$relativePaths[0]),@('umm',$relativePaths[1]),@('harmony',$relativePaths[2]))){
        $expected=@($Fingerprint.kingmaker.files|Where-Object {[string]$_.role-ceq$pin[0]})
        $actual=@($files|Where-Object {[string]$_.relativePath-ceq$pin[1]})
        if($expected.Count-ne1-or$actual.Count-ne1-or[string]$expected[0].sha256-cne[string]$actual[0].sha256-or
            [long]$expected[0].length-ne[long]$actual[0].length){throw ('Base-game dependency differs from the selected environment fingerprint: '+$pin[0])}
    }
    $receipt=[pscustomobject][ordered]@{schemaVersion=1;mode='base-game-only-kmc';gameRoot=$game;modsRoot=(Join-Path $game 'Mods');
        loaderRoot=$loader;configSha256=(Get-KmcSha256 $config);files=$files;dependencyDigest=''}
    $receipt.dependencyDigest=Get-KmcBaseGameDependencyDigest $receipt
    Assert-KmcBaseGameDependencyReceipt $receipt -VerifyFiles
    return $receipt
}

function New-KmcBaseGameReadyTree {
    [CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
    param([Parameter(Mandatory=$true)][string]$StagingRun,[Parameter(Mandatory=$true)][string]$CloneRoot,
        [Parameter(Mandatory=$true)][string]$ReadyRoot,[Parameter(Mandatory=$true)][string]$PackageModRoot,
        [Parameter(Mandatory=$true)]$Before)
    $clone=Assert-KmcChildPath $CloneRoot $StagingRun 'preserved base-game clone'
    $ready=Assert-KmcChildPath $ReadyRoot $StagingRun 'base-game ready tree'
    $package=Assert-KmcChildPath $PackageModRoot $StagingRun 'base-game package root'
    if($clone-cne(Join-Path $StagingRun 'Mods-live-clone')-or$ready-cne(Join-Path $StagingRun 'Mods-ready')-or
        $package-cne(Join-Path $StagingRun 'package-overlay/KingmakerMountedCombat')){throw 'Base-game stage paths differ from the exact transaction layout.'}
    Assert-KmcDirectoryTreeCloneable $clone 'preserved base-game clone'
    Assert-KmcDirectoryManifestsEqual $Before (Get-KmcDirectoryManifest $clone) 'Preserved base-game live clone'
    Assert-KmcDirectoryTreeCloneable $package 'base-game package root'
    $members=@((Get-KmcDirectoryManifest $package).entries|ForEach-Object {'{0}|{1}'-f$_.kind,$_.path}|Sort-Object)
    if(($members-join '|')-cne'file|Info.json|file|KingmakerMountedCombat.dll'){throw 'Base-game package root contains foreign payload.'}
    Assert-KmcNotReparsePoint $ready 'base-game ready tree'
    if(Test-Path -LiteralPath $ready){throw 'Base-game ready tree already exists.'}
    if(-not$PSCmdlet.ShouldProcess($ready,'create the private KMC-only ready tree; preserve the exact complete live clone')){return}
    [void][IO.Directory]::CreateDirectory($ready)
    [void](Copy-KmcDirectoryTreeExact -SourceRoot $package -DestinationRoot (Join-Path $ready 'KingmakerMountedCombat'))
    Assert-KmcDirectoryManifestsEqual $Before (Get-KmcDirectoryManifest $clone) 'Preserved base-game clone after ready-tree construction'
}

function Assert-KmcBaseGameUmmStartupDelta {
    param([Parameter(Mandatory=$true)][string]$Before,[Parameter(Mandatory=$true)][string]$After)
    $original=Assert-KmcBaseGameParameters $Before
    $current=Assert-KmcBaseGameParameters $After
    if($current.OuterXml-ceq$original.OuterXml){return}
    foreach($mod in @($original.SelectNodes('/Param/ModParams/Mod'))){
        if([string]$mod.Id-cne'KingmakerMountedCombat'){[void]$mod.ParentNode.RemoveChild($mod)}
    }
    if($current.OuterXml-cne$original.OuterXml){throw 'Base-game UMM startup changed more than removing absent foreign mod entries.'}
}

# Parameter restoration may use the base-only reduction solely after the exact
# owned original Mods tree has been put back. The authority is durable state,
# not an arbitrary boolean passed by a recovery caller.
function Assert-KmcBaseGameSettingsAuthority {
    param([Parameter(Mandatory=$true)]$State,[Parameter(Mandatory=$true)][string]$RunId,
        [Parameter(Mandatory=$true)][string]$Token,[Parameter(Mandatory=$true)][string]$GameRoot)
    if($State.schemaVersion-ne4-or[string]$State.stagingMode-cne'base-game-only-kmc'-or
        [string]$State.runId-cne$RunId-or[string]$State.token-cne$Token-or[string]$State.phase-cne'restored'-or
        [string]$State.baseGameDependencies.gameRoot-cne$GameRoot-or[string]$State.liveModsRoot-cne[string]$State.baseGameDependencies.modsRoot){
        throw 'Base-game settings restoration requires its settled exact owned Mods transaction.'
    }
    Assert-KmcBaseGameDependencyReceipt $State.baseGameDependencies
}
