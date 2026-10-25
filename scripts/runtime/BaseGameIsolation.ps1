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
    # Installed UMM 0.33.2.0 only reads Params.xml at startup; a mod found in Mods
    # without an entry is enabled by default (ModEntry ctor), and Param.Save drops
    # entries for mods it did not find. An owner who plays without KMC therefore has
    # no KMC entry, and that is admitted. A present entry must still be the exact id
    # and enabled: an explicit owner disable keeps blocking, and the harness never
    # writes Params.xml to change it.
    $kmcLike=@($mods|Where-Object {[string]::Equals([string]$_.GetAttribute('Id'),'KingmakerMountedCombat',[StringComparison]::OrdinalIgnoreCase)})
    $kmc=@($mods|Where-Object {[string]$_.GetAttribute('Id')-ceq'KingmakerMountedCombat'})
    if($kmcLike.Count-ne$kmc.Count-or$kmc.Count-gt1){throw 'UMM KMC parameter identity is duplicated or case-ambiguous.'}
    if($kmc.Count-eq1-and[string]$kmc[0].GetAttribute('Enabled')-cne'true'){throw 'Base-game mode requires a present KMC entry enabled exactly; it never changes owner settings to enable it.'}
    return $document
}

# Whitespace-independent structural shape of a parsed UMM document: node kind,
# qualified name and namespace, the exact attribute set and every non-whitespace
# text node, in document order. Anything else in the document is refused.
function Get-KmcBaseGameXmlShape {
    param([Parameter(Mandatory=$true)][Xml.XmlNode]$Node)
    switch($Node.NodeType){
        'Document' {return [ordered]@{k='document';c=@(foreach($child in $Node.ChildNodes){if($child.NodeType-notin@('Whitespace','SignificantWhitespace')){Get-KmcBaseGameXmlShape $child}})}}
        'XmlDeclaration' {return [ordered]@{k='declaration';v=[string]$Node.Value}}
        'Element' {
            [string[]]$attributes=@(foreach($attribute in $Node.Attributes){'{0}|{1}|{2}'-f$attribute.NamespaceURI,$attribute.Name,$attribute.Value})
            [Array]::Sort($attributes,[StringComparer]::Ordinal)
            return [ordered]@{k='element';n=[string]$Node.Name;ns=[string]$Node.NamespaceURI;a=@($attributes);
                c=@(foreach($child in $Node.ChildNodes){if($child.NodeType-notin@('Whitespace','SignificantWhitespace')){Get-KmcBaseGameXmlShape $child}})}
        }
        'Text' {return [ordered]@{k='text';v=[string]$Node.Value}}
        'CDATA' {return [ordered]@{k='text';v=[string]$Node.Value}}
        'Comment' {return [ordered]@{k='comment';v=[string]$Node.Value}}
        default {throw ('Base-game UMM parameters contain an unsupported node: '+$Node.NodeType)}
    }
}

function Get-KmcBaseGameXmlShapeText {
    param([Parameter(Mandatory=$true)][Xml.XmlNode]$Node)
    return (Get-KmcBaseGameXmlShape $Node|ConvertTo-Json -Depth 100 -Compress)
}

function Get-KmcBaseGameDependencyDigest {
    param([Parameter(Mandatory=$true)]$Receipt)
    [string[]]$rows=@($Receipt.files|ForEach-Object {'{0}|{1}|{2}' -f $_.relativePath,$_.length,$_.sha256})
    [Array]::Sort($rows,[StringComparer]::Ordinal)
    # An explicit LF separator: a literal line break inside the string made the digest follow the checkout's line endings
    # (a CRLF worktree computed 93f28715... where the LF trees stored db2c3901...). LF keeps every existing receipt valid.
    return Get-KmcTextSha256 ((@([string]$Receipt.gameRoot,[string]$Receipt.configSha256)+$rows)-join "`n")
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
        [Parameter(Mandatory=$true)]$Before,
        # Only the explicit normal-player observer mode adds a second exact package.
        [string]$ObserverModRoot,
        # F12 upgrade rehearsal only (with the observer): exact UMM cache files from a
        # previous install, copied beside the package files and re-hashed in place.
        $KmcCarryFiles)
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
    $observer=$null
    if(-not[string]::IsNullOrEmpty($ObserverModRoot)){
        $observer=Assert-KmcChildPath $ObserverModRoot $StagingRun 'base-game observer package root'
        if($observer-cne(Join-Path $StagingRun 'observer-overlay/KmcRemovalObserver')){throw 'Base-game observer path differs from the exact transaction layout.'}
        Assert-KmcDirectoryTreeCloneable $observer 'base-game observer package root'
        $observerMembers=@((Get-KmcDirectoryManifest $observer).entries|ForEach-Object {'{0}|{1}'-f$_.kind,$_.path}|Sort-Object)
        if(($observerMembers-join '|')-cne'file|Info.json|file|KmcRemovalObserver.dll'){throw 'Base-game observer package root contains foreign payload.'}
    }
    Assert-KmcNotReparsePoint $ready 'base-game ready tree'
    if(Test-Path -LiteralPath $ready){throw 'Base-game ready tree already exists.'}
    if(-not$PSCmdlet.ShouldProcess($ready,'create the private ready tree of exactly the selected packages; preserve the exact complete live clone')){return}
    [void][IO.Directory]::CreateDirectory($ready)
    [void](Copy-KmcDirectoryTreeExact -SourceRoot $package -DestinationRoot (Join-Path $ready 'KingmakerMountedCombat'))
    $carry=@($KmcCarryFiles|Where-Object{$null-ne$_})
    if($carry.Count-ne0-and$null-eq$observer){throw 'Only the observer co-staged tree may carry previous KMC folder files.'}
    if($carry.Count-gt4){throw 'Carried KMC folder files are unbounded.'}
    foreach($file in $carry){
        $name=[string]$file.name
        if($name-cnotmatch'^KingmakerMountedCombat\.dll\.[0-9]{1,9}\.cache$'-or[string]$file.sha256-cnotmatch'^[0-9a-f]{64}$'){throw 'A carried KMC folder file is outside the closed UMM cache shape.'}
        # Only a retained first-install staged-after tree under the lab staging root may supply them.
        $source=Assert-KmcChildPath ([string]$file.sourcePath) (Join-Path (Get-KmcLabRoot) 'runtime-staging') 'carried KMC folder file'
        Assert-KmcNotReparsePoint $source 'carried KMC folder file';Assert-KmcNotHardLink $source 'carried KMC folder file'
        $target=Join-Path (Join-Path $ready 'KingmakerMountedCombat') $name
        if(Test-Path -LiteralPath $target){throw 'A carried KMC folder file collides with a staged package file.'}
        if((Get-KmcSha256 $source)-cne[string]$file.sha256){throw 'A carried KMC folder file changed before staging.'}
        Copy-Item -LiteralPath $source -Destination $target
        if((Get-KmcSha256 $target)-cne[string]$file.sha256-or(Get-Item -LiteralPath $target).Length-ne[long]$file.length){throw 'A carried KMC folder file changed while staging.'}
        Assert-KmcNotHardLink $target 'staged carried KMC folder file'
    }
    if($null-ne$observer){[void](Copy-KmcDirectoryTreeExact -SourceRoot $observer -DestinationRoot (Join-Path $ready 'KmcRemovalObserver'))}
    $readyTop=@(Get-ChildItem -LiteralPath $ready -Force|ForEach-Object Name|Sort-Object)
    $expectedTop=@(@('KingmakerMountedCombat')+@(if($null-ne$observer){'KmcRemovalObserver'})|Sort-Object)
    if(($readyTop-join '|')-cne($expectedTop-join '|')){throw 'Base-game ready tree holds other than exactly the selected packages.'}
    Assert-KmcDirectoryManifestsEqual $Before (Get-KmcDirectoryManifest $clone) 'Preserved base-game clone after ready-tree construction'
}

# One native UMM difference shared by every startup-delta rule: the LastUpdateCheck stamp, present on both sides, may only
# move forward; the current document takes the original stamp back so the rest compares exactly.
function Step-KmcUmmLastUpdateCheck([Xml.XmlDocument]$Original,[Xml.XmlDocument]$Current){
    $beforeChecks=@($Original.SelectNodes('/Param/LastUpdateCheck'))
    $afterChecks=@($Current.SelectNodes('/Param/LastUpdateCheck'))
    if($beforeChecks.Count-eq1-and$afterChecks.Count-eq1-and$beforeChecks[0].InnerText-cne$afterChecks[0].InnerText){
        foreach($check in @($beforeChecks[0],$afterChecks[0])){
            if($check.Attributes.Count-ne0-or$check.ChildNodes.Count-ne1-or$check.FirstChild.NodeType-cne'Text'){throw 'UMM LastUpdateCheck is not a plain xs:dateTime element.'}
        }
        $beforeStamp=[DateTimeOffset]::MinValue;$afterStamp=[DateTimeOffset]::MinValue
        try{$beforeStamp=[Xml.XmlConvert]::ToDateTimeOffset([string]$beforeChecks[0].FirstChild.Value);$afterStamp=[Xml.XmlConvert]::ToDateTimeOffset([string]$afterChecks[0].FirstChild.Value)}
        catch{throw 'UMM LastUpdateCheck is not a valid xs:dateTime.'}
        if($afterStamp.UtcTicks-lt$beforeStamp.UtcTicks){throw 'UMM LastUpdateCheck moved backwards.'}
        $afterChecks[0].FirstChild.Value=$beforeChecks[0].FirstChild.Value
    }
}
# The other shared native difference: UMM rebuilds ModParams from the mods it actually found, so an entry whose mod folder
# was absent from the run's own staged Mods tree is pruned. Returns a copy without exactly those entries.
function Remove-KmcUmmAbsentModEntries([Xml.XmlDocument]$Document,[string[]]$PresentIds,[switch]$FolderNames){
    $reduced=$Document.Clone()
    foreach($mod in @($reduced.SelectNodes('/*/ModParams/Mod|/*/ModParamsList/Mod'))){
        # Staged folder names stand in for UMM ids: matched case-insensitively, so a present mod is never taken for absent.
        $absent=if($FolderNames){[string]$mod.GetAttribute('Id')-notin@($PresentIds)}else{[string]$mod.GetAttribute('Id')-cnotin@($PresentIds)}
        if($absent){[void]$mod.ParentNode.RemoveChild($mod)}
    }
    return ,$reduced
}
function Assert-KmcBaseGameUmmStartupDelta {
    param([Parameter(Mandatory=$true)][string]$Before,[Parameter(Mandatory=$true)][string]$After,
        # The normal-player observer mode found exactly KMC and the observer.
        [switch]$WithObserver)
    $original=Assert-KmcBaseGameParameters $Before
    $current=Assert-KmcBaseGameParameters $After
    # UMM 0.33.2.0 writes Params.xml only on a normal exit (UI.OnDestroy) or its Save
    # button, rebuilding ModParams from the mods it actually found. In the KMC-only
    # ready tree that is the original's KMC entry, or - only when the original had no
    # KMC entry - one default KMC entry (Enabled=true, default hotkey). A process that
    # ended without OnDestroy leaves the file unchanged. One other native difference is
    # admitted: the LastUpdateCheck stamp, present on both sides, may only move forward.
    Step-KmcUmmLastUpdateCheck $original $current
    $actual=Get-KmcBaseGameXmlShapeText $current
    $found=@('KingmakerMountedCombat')+@(if($WithObserver){'KmcRemovalObserver'})
    $reduced=Remove-KmcUmmAbsentModEntries $original $found
    $allowed=New-Object 'Collections.Generic.List[string]'
    $allowed.Add((Get-KmcBaseGameXmlShapeText $original))
    $allowed.Add((Get-KmcBaseGameXmlShapeText $reduced))
    # A found mod without an entry gains one default entry; with two found mods
    # either append order is the native rebuild of the same exact set.
    $missing=@($found|Where-Object{$id=$_;@($reduced.SelectNodes('/Param/ModParams/Mod')|Where-Object{[string]$_.GetAttribute('Id')-ceq$id}).Count-eq0})
    $orders=New-Object 'Collections.Generic.List[string[]]'
    if($missing.Count-eq2){$orders.Add([string[]]@($missing[0],$missing[1]));$orders.Add([string[]]@($missing[1],$missing[0]))}
    elseif($missing.Count-eq1){$orders.Add([string[]]@($missing[0]))}
    $rewrites=New-Object 'Collections.Generic.List[Xml.XmlDocument]';$rewrites.Add($reduced)
    foreach($order in $orders){
        $appended=$reduced.Clone();$container=$appended.SelectSingleNode('/Param/ModParams')
        foreach($id in $order){
            $entry=$appended.CreateElement('Mod');$entry.SetAttribute('Id',$id);$entry.SetAttribute('Enabled','true')
            $hotkey=$appended.CreateElement('Hotkey');$keyCode=$appended.CreateElement('keyCode');$modifiers=$appended.CreateElement('modifiers')
            [void]$keyCode.AppendChild($appended.CreateTextNode('None'));[void]$modifiers.AppendChild($appended.CreateTextNode('0'))
            [void]$hotkey.AppendChild($keyCode);[void]$hotkey.AppendChild($modifiers);[void]$entry.AppendChild($hotkey);[void]$container.AppendChild($entry)
        }
        $allowed.Add((Get-KmcBaseGameXmlShapeText $appended));$rewrites.Add($appended)
    }
    # UMM rebuilds ModParams in its own load order, so with two found mods a kept
    # entry and an appended default may be written in either order. The exact
    # entry content is unchanged; only the order of exactly two entries varies.
    if($WithObserver){
        foreach($rewrite in @($rewrites)){
            $mods=@($rewrite.SelectNodes('/Param/ModParams/Mod'))
            if($mods.Count-ne2){continue}
            $swapped=$rewrite.Clone();$container=$swapped.SelectSingleNode('/Param/ModParams');$first=@($swapped.SelectNodes('/Param/ModParams/Mod'))[0]
            [void]$container.RemoveChild($first);[void]$container.AppendChild($first)
            $allowed.Add((Get-KmcBaseGameXmlShapeText $swapped))
        }
    }
    if(-not$allowed.Contains($actual)){throw 'Base-game UMM parameters changed beyond the native rewrite of the selected packages and a forward LastUpdateCheck.'}
}

# Parameter restoration may use the base-only reduction solely after the exact
# owned original Mods tree has been put back. The authority is durable state,
# not an arbitrary boolean passed by a recovery caller.
function Assert-KmcBaseGameSettingsAuthority {
    param([Parameter(Mandatory=$true)]$State,[Parameter(Mandatory=$true)][string]$RunId,
        [Parameter(Mandatory=$true)][string]$Token,[Parameter(Mandatory=$true)][string]$GameRoot)
    if($State.schemaVersion-ne4-or[string]$State.stagingMode-cnotin@('base-game-only-kmc','base-game-only-kmc-observer')-or
        [string]$State.runId-cne$RunId-or[string]$State.token-cne$Token-or[string]$State.phase-cne'restored'-or
        [string]$State.baseGameDependencies.gameRoot-cne$GameRoot-or[string]$State.liveModsRoot-cne[string]$State.baseGameDependencies.modsRoot){
        throw 'Base-game settings restoration requires its settled exact owned Mods transaction.'
    }
    Assert-KmcBaseGameDependencyReceipt $State.baseGameDependencies
}
