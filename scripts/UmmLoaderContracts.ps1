# Unity Mod Manager loader checks, shared by Test-AssemblyContracts.ps1 and Test-UmmLoaderContracts.ps1.
#
# Two questions that used to be one pin are kept apart:
#   IDENTITY  Is this reference binary exactly one reviewed loader (planning/UMM-LOADER-PROFILES.json)?
#             Version, SHA-256 and MVID are all exact, against one selected profile. The profile is chosen by
#             the -UmmProfile/-Profile parameter, else by UnityModManagerProfile in LocalGamePaths.props, else
#             it is the reviewed profile the reference identifies as. A binary that is no reviewed profile
#             fails, and so does an explicitly selected profile the binary does not match.
#   API       Does this reference expose every member Mounted Combat binds, with the exact signature, and can
#             the callbacks be assigned? Checked against the reference itself, by name and signature, and by
#             resolving every UnityModManager member the compiled product references.
# A passing API result never stands in for identity, and an identity mismatch says nothing about the API.
# Neither is native execution. The declared minimum (Info.json ManagerVersion) is reported, never inferred.
Set-StrictMode -Version Latest

function Get-KmcUmmLoaderProfiles([string]$RepoRoot) {
    $path=Join-Path $RepoRoot 'planning\UMM-LOADER-PROFILES.json'
    $document=Get-Content -Raw -LiteralPath $path|ConvertFrom-Json
    if($document.schemaVersion-ne1-or@($document.profiles).Count-eq0){throw 'Loader profile record is missing or has an unknown schema.'}
    $seen=@{}
    foreach($profile in $document.profiles){
        if($profile.id-cnotmatch'^umm-[0-9]+[.][0-9]+[.][0-9]+$'-or$profile.version-cnotmatch'^[0-9]+[.][0-9]+[.][0-9]+[.][0-9]+$'-or
           $profile.sha256-cnotmatch'^[0-9a-f]{64}$'-or$profile.mvid-cnotmatch'^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$'-or
           [string]::IsNullOrWhiteSpace([string]$profile.provenance)){throw ('Loader profile is not an exact identity with provenance: '+$profile.id)}
        foreach($member in @('get_Instance','get_Opened','ToggleWindow')){if([string]$profile.uiTokens.$member-cnotmatch'^06[0-9A-F]{6}$'){throw ('Loader profile UI token is not exact: '+$profile.id)}}
        foreach($key in @($profile.id,$profile.sha256,$profile.mvid)){if($seen.ContainsKey($key)){throw ('Loader profiles repeat an identity: '+$key)};$seen[$key]=$true}
    }
    $document
}

function Get-KmcUmmDeclaredMinimum([string]$RepoRoot) {
    [string](Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'Info.json')|ConvertFrom-Json).ManagerVersion
}

# Inspect the emitted assembly reference versions; lowering only Info.json must not make an older
# higher-baseline DLL pass package validation. No installed game or loader is loaded for this check.
function Assert-KmcPinnedLoaderPackageReferences($Identity,$Pins) {
    foreach($pin in $Pins.references){
        $matches=@($Identity.referenceIdentities|Where-Object name -CEQ $pin.Name)
        if($matches.Count-ne1-or[string]$matches[0].version-cne[string]$pin.Version){
            throw ('Packaged DLL must reference the pinned '+$pin.Name+' '+$pin.Version+'. Rebuild against the baseline; a manifest-only edit is insufficient.')
        }
    }
}

# An optional property of the machine-local LocalGamePaths.props, which already names the loader directory.
function Get-KmcUmmLocalSetting([string]$RepoRoot,[string]$Name) {
    $path=Join-Path $RepoRoot 'LocalGamePaths.props'
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){return ''}
    $node=([xml](Get-Content -Raw -LiteralPath $path)).SelectSingleNode("//*[local-name()='"+$Name+"']")
    if($null-eq$node){return ''}
    ([string]$node.InnerText).Trim()
}

function Get-KmcUmmReferenceIdentity([string]$Path,$Assembly) {
    [pscustomobject]@{
        path=$Path
        version=$Assembly.GetName().Version.ToString()
        sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
        mvid=$Assembly.ManifestModule.ModuleVersionId.ToString()
        length=(Get-Item -LiteralPath $Path).Length
    }
}

# The profile identity is checked against, and how it was chosen. An explicit choice is never replaced by what
# the reference happens to be; without one, the reference must itself be a reviewed profile.
function Resolve-KmcUmmLoaderProfile($Document,$Identity,[string]$Requested,[string]$LocalSetting) {
    $explicit=if(-not[string]::IsNullOrWhiteSpace($Requested)){@($Requested,'parameter')}elseif(-not[string]::IsNullOrWhiteSpace($LocalSetting)){@($LocalSetting,'LocalGamePaths.props UnityModManagerProfile')}else{$null}
    if($null-ne$explicit){
        $found=@($Document.profiles|Where-Object id -CEQ $explicit[0])
        if($found.Count-ne1){throw ('Unknown loader profile '+$explicit[0]+'. Reviewed profiles: '+((@($Document.profiles|ForEach-Object{$_.id}))-join', ')+'.')}
        return [pscustomobject]@{profile=$found[0];selection=$explicit[1]}
    }
    $found=@($Document.profiles|Where-Object{$_.sha256-ceq$Identity.sha256-and$_.mvid-ceq$Identity.mvid-and$_.version-ceq$Identity.version})
    [pscustomobject]@{profile=$(if($found.Count-eq1){$found[0]}else{$null});selection='identified from the reference'}
}

# $Assert: {param([bool]$Condition,[string]$Message)}. Returns $true when every identity check passed.
function Invoke-KmcUmmLoaderIdentityChecks($Assembly,$Identity,$Selected,[scriptblock]$Assert) {
    if($null-eq$Selected.profile){
        & $Assert $false ('UMM identity: the reference is no reviewed loader profile (version '+$Identity.version+', SHA-256 '+$Identity.sha256+', MVID '+$Identity.mvid+')')
        return $false
    }
    $profile=$Selected.profile;$label='UMM identity ['+$profile.id+'] ';$ok=$true
    foreach($check in @(@('exact version',($Identity.version-ceq$profile.version)),@('SHA-256',($Identity.sha256-ceq$profile.sha256)),@('MVID',($Identity.mvid-ceq$profile.mvid)))){
        & $Assert ([bool]$check[1]) ($label+$check[0]);if(-not$check[1]){$ok=$false}
    }
    if($null-ne$profile.length){$same=$Identity.length-eq$profile.length;& $Assert $same ($label+'file length');if(-not$same){$ok=$false}}
    # Metadata tokens move between loader builds, so they belong to the identity, not to the API.
    $ui=$Assembly.GetType('UnityModManagerNet.UnityModManager+UI',$false)
    foreach($member in @('get_Instance','get_Opened','ToggleWindow')){
        $token=[Convert]::ToInt32([string]$profile.uiTokens.$member,16)
        $at=@(if($null-ne$ui){$ui.GetMembers([Reflection.BindingFlags]'Public,NonPublic,Instance,Static')|Where-Object MetadataToken -eq $token})
        $same=$at.Count-eq1-and[string]$at[0].Name-ceq$member
        & $Assert $same ($label+'UI token '+$profile.uiTokens.$member+' '+$member);if(-not$same){$ok=$false}
    }
    $ok
}

function Get-KmcUmmTypeShape($Type) {
    if($null-eq$Type){return '<null>'}
    if($Type.IsGenericType){return $Type.GetGenericTypeDefinition().FullName+'['+((@($Type.GetGenericArguments())|ForEach-Object{Get-KmcUmmTypeShape $_})-join',')+']'}
    [string]$Type.FullName
}

# The binding surface, by declaring type, name and exact signature. Nothing here reads a version, hash or token.
function Invoke-KmcUmmLoaderApiChecks($Assembly,[scriptblock]$Assert) {
    $entryName='UnityModManagerNet.UnityModManager+ModEntry'
    $entry=$Assembly.GetType($entryName,$false)
    foreach($expected in @(
        @('Info','UnityModManagerNet.UnityModManager+ModInfo',$false),
        @('Logger','UnityModManagerNet.UnityModManager+ModEntry+ModLogger',$false),
        @('OnGUI',('System.Action`1['+$entryName+']'),$true),
        @('OnSessionStop',('System.Action`1['+$entryName+']'),$true),
        @('OnUpdate',('System.Action`2['+$entryName+',System.Single]'),$true),
        @('OnUnload',('System.Func`2['+$entryName+',System.Boolean]'),$true),
        @('OnToggle',('System.Func`3['+$entryName+',System.Boolean,System.Boolean]'),$true))){
        $field=if($null-eq$entry){$null}else{$entry.GetField($expected[0],[Reflection.BindingFlags]'Public,Instance,DeclaredOnly')}
        & $Assert ($null-ne$field -and $field.IsPublic -and -not $field.IsStatic -and (Get-KmcUmmTypeShape $field.FieldType)-ceq$expected[1] -and (-not $expected[2] -or -not $field.IsInitOnly)) ('UMM API ModEntry.'+$expected[0]+' '+$expected[1]+$(if($expected[2]){' assignable'}else{' readable'}))
    }
    $info=$Assembly.GetType('UnityModManagerNet.UnityModManager+ModInfo',$false)
    $infoId=if($null-eq$info){$null}else{$info.GetField('Id',[Reflection.BindingFlags]'Public,Instance,DeclaredOnly')}
    & $Assert ($null-ne$infoId -and $infoId.IsPublic -and -not $infoId.IsStatic -and $infoId.FieldType.FullName-ceq'System.String') 'UMM API ModInfo.Id System.String readable'
    $logger=$Assembly.GetType('UnityModManagerNet.UnityModManager+ModEntry+ModLogger',$false)
    foreach($expected in @(@('Log','System.String'),@('Warning','System.String'),@('Error','System.String'),@('LogException','System.String,System.Exception'))){
        $methods=@(if($null-ne$logger){$logger.GetMethods([Reflection.BindingFlags]'Public,Instance,DeclaredOnly')|Where-Object{$_.Name-ceq$expected[0] -and ((@($_.GetParameters())|ForEach-Object{$_.ParameterType.FullName})-join',')-ceq$expected[1]}})
        & $Assert ($methods.Count-eq1 -and $methods[0].ReturnType.FullName-ceq'System.Void' -and -not $methods[0].IsGenericMethodDefinition) ('UMM API ModLogger.'+$expected[0]+'('+$expected[1]+') void')
    }
    # The diagnostic capture host: the loader window the scenarios close before a screenshot.
    $ui=$Assembly.GetType('UnityModManagerNet.UnityModManager+UI',$false)
    & $Assert ($null-ne$ui -and $null-ne$ui.BaseType -and $ui.BaseType.FullName-ceq'UnityEngine.MonoBehaviour') 'UMM API UI is a MonoBehaviour capture host'
    $all=[Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly'
    $instance=if($null-eq$ui){$null}else{$ui.GetMethod('get_Instance',$all,$null,[Type[]]@(),$null)}
    & $Assert ($null-ne$instance -and $instance.IsPublic -and $instance.IsStatic -and $instance.ReturnType.FullName-ceq'UnityModManagerNet.UnityModManager+UI') 'UMM API UI.Instance static UI getter'
    $opened=if($null-eq$ui){$null}else{$ui.GetMethod('get_Opened',$all,$null,[Type[]]@(),$null)}
    & $Assert ($null-ne$opened -and $opened.IsPublic -and -not $opened.IsStatic -and $opened.ReturnType.FullName-ceq'System.Boolean') 'UMM API UI.Opened Boolean getter'
    $toggle=@(if($null-ne$ui){$ui.GetMethods($all)|Where-Object{$_.Name-ceq'ToggleWindow' -and ((@($_.GetParameters())|ForEach-Object{$_.ParameterType.FullName})-join',')-ceq'System.Boolean'}})
    & $Assert ($toggle.Count-eq1 -and $toggle[0].IsPublic -and -not $toggle[0].IsStatic -and $toggle[0].ReturnType.FullName-ceq'System.Void') 'UMM API UI.ToggleWindow(System.Boolean) void'
}

# Every UnityModManager member the compiled product references must resolve in this reference by name and exact
# signature, as the runtime binder requires. The references are read from the product's own metadata with the
# dnlib that ships beside the installed loader (a metadata reader only), then each is bound through reflection
# with $Assembly as the product's UnityModManager (Register-KmcUmmReferenceResolver). Reflection alone cannot
# name a reference that fails to bind, which is why the names come from the metadata. Reflection also resolves
# members the product could not call, so a bound member must in addition be public on a visible type.
function Get-KmcUmmProductLoaderReferences([string]$ProductPath,[string]$DnlibPath) {
    [void][Reflection.Assembly]::LoadFrom($DnlibPath)
    $metadata=[dnlib.DotNet.ModuleDefMD]::Load($ProductPath,[dnlib.DotNet.ModuleCreationOptions]$null)
    try{
        @($metadata.GetMemberRefs()|Where-Object{$null-ne$_.DeclaringType-and$null-ne$_.DeclaringType.DefinitionAssembly-and[string]$_.DeclaringType.DefinitionAssembly.Name-ceq'UnityModManager'}|
            ForEach-Object{[pscustomobject]@{token=[int]$_.MDToken.Raw;name=[string]$_.FullName}})
    }finally{$metadata.Dispose()}
}

function Get-KmcUmmProductBindings($Product,$References) {
    $resolved=@();$missing=@()
    # A product that references no loader member arrives here as nothing at all, not as an empty list.
    foreach($reference in @($References|Where-Object{$null-ne$_})){
        $bound=$false
        try{
            $member=$Product.ManifestModule.ResolveMember($reference.token)
            $public=$(if($member-is[Reflection.FieldInfo]-or$member-is[Reflection.MethodBase]){$member.IsPublic}else{$false})
            $bound=$member.Module.Assembly.GetName().Name-ceq'UnityModManager'-and$member.DeclaringType.IsVisible-and$public
        }catch{$bound=$false}
        if($bound){$resolved+=$reference.name}else{$missing+=$reference.name}
    }
    [pscustomobject]@{resolved=@($resolved|Sort-Object -Unique);missing=@($missing|Sort-Object -Unique)}
}

# The production binding (Main.cs, Logging/UmmLogger.cs) must be among the references read, so an empty or
# misread metadata scan cannot pass.
function Invoke-KmcUmmProductBindingChecks($Product,[string]$DnlibPath,[scriptblock]$Assert) {
    $references=@(Get-KmcUmmProductLoaderReferences $Product.Location $DnlibPath)
    $names=@($references|ForEach-Object{$_.name})
    $seen=@($names|Where-Object{$_-cmatch'ModEntry::Info$|ModEntry::Logger$|ModLogger::Log\(System\.String\)$|ModLogger::LogException\(System\.String,System\.Exception\)$'}).Count-eq4
    & $Assert $seen ('UMM API the compiled product''s loader references were read from its metadata ('+$names.Count+' members, the production entry and logger among them)')
    $bindings=Get-KmcUmmProductBindings $Product $references
    & $Assert ($bindings.missing.Count-eq0) ('UMM API every loader member the compiled product references binds by name and signature and is public ('+$bindings.resolved.Count+' of '+$names.Count+$(if($bindings.missing.Count){'; unbound or not accessible: '+($bindings.missing-join' | ')}else{''})+')')
    $bindings
}
# Makes $Assembly the UnityModManager every later reflection-only load in this process binds to, so the product's
# member references are resolved against the reference under test and no other copy.
function Register-KmcUmmReferenceResolver($Assembly) {
    $reference=$Assembly
    [AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({param($sender,$eventArgs)if(($eventArgs.Name-split',')[0]-ceq'UnityModManager'){return $reference};return $null}.GetNewClosure())
}

# Loads the compiled product for the binding sweep: the build of exactly the named configuration, never another
# one that happens to exist. Null when that build does not exist: unassessed, never a pass. A product named
# explicitly must exist.
function Get-KmcUmmProductAssembly([string]$RepoRoot,[string]$Configuration,[string]$ProductDll='') {
    if(-not[string]::IsNullOrWhiteSpace($ProductDll)){
        if(-not(Test-Path -LiteralPath $ProductDll -PathType Leaf)){throw ('The named product does not exist: '+$ProductDll)}
        return [Reflection.Assembly]::ReflectionOnlyLoadFrom([IO.Path]::GetFullPath($ProductDll))
    }
    $path=Join-Path $RepoRoot ('bin\'+$Configuration+'\KingmakerMountedCombat.dll')
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){return $null}
    [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)
}
# The metadata reader for the product binding sweep: the dnlib beside the installed loader. Empty when absent.
function Get-KmcUmmMetadataReaderPath([string]$Managed) {
    $path=Join-Path $Managed 'UnityModManager\dnlib.dll'
    if(Test-Path -LiteralPath $path -PathType Leaf){$path}else{''}
}
