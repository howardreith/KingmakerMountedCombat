[CmdletBinding()]
param(
    # Further genuine UnityModManager.dll files to inspect, held outside this repository. Also read from the
    # semicolon-separated UnityModManagerReferencePaths of LocalGamePaths.props. They are only read.
    [string[]]$AdditionalReference=@(),
    # The product build whose loader member references are bound (bin\<Configuration>).
    [ValidateSet('Debug','Release')][string]$Configuration='Release'
)
# Checks the loader checker (Test-UmmLoaderContracts.ps1) and reports the loader matrix.
#   - A genuine reference passes identity under its own reviewed profile and fails under any other, while
#     its API result is unchanged: identity and API never stand in for each other.
#   - An unknown binary fails identity even when its API passes.
#   - A missing member, another signature, a retyped callback and a callback that cannot be assigned each
#     fail the API. Those loaders are SYNTHETIC stubs compiled here for the purpose; they are not genuine
#     Unity Mod Manager builds and are never installed.
#   - A reference that is not on this machine is reported unavailable and unassessed, never passed.
# Every reviewed profile and every additional genuine reference gets one matrix row. Nothing is launched,
# installed or replaced, and no loader binary is copied.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$managed=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data\Managed'
. (Join-Path $PSScriptRoot 'UmmLoaderContracts.ps1')
$checker=Join-Path $PSScriptRoot 'Test-UmmLoaderContracts.ps1'
$passed=0;$unassessed=New-Object 'System.Collections.Generic.List[string]';$errors=New-Object 'System.Collections.Generic.List[string]'
$work=Join-Path $labRoot ('analysis-cache\umm-loader-contract-checks\'+[Guid]::NewGuid().ToString('N'))
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Unassessed([string]$Name){$script:unassessed.Add($Name);Write-Host ('UNASSESSED '+$Name)}
function Need([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
function Invoke-Checker([string[]]$Arguments){
    # A refusal the checker writes to its error stream is a result to read here, not an error of this script.
    $preference=$ErrorActionPreference;$ErrorActionPreference='Continue'
    # Every case states its own profile, so the machine-local profile setting is never read here.
    $Arguments=@('-Configuration',$Configuration,'-IgnoreLocalProfile')+@($Arguments)
    try{$output=@(& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $checker @Arguments 2>&1|ForEach-Object{[string]$_});$code=$LASTEXITCODE}finally{$ErrorActionPreference=$preference}
    $line={param([string]$Prefix)$found=@($output|Where-Object{$_.StartsWith($Prefix,[StringComparison]::Ordinal)});if($found.Count){$found[$found.Count-1]}else{''}}
    [pscustomobject]@{exit=$code;lines=$output;identity=(& $line 'UMM IDENTITY ');api=(& $line 'UMM API ');failures=@($output|Where-Object{$_.StartsWith('FAIL ',[StringComparison]::Ordinal)})}
}
# What the checker said, for a failure message. When a result line is missing, the checker's own output is kept.
function Get-ChildDetail($Result){@($Result.lines|Where-Object{$_.Trim()-and-not$_.StartsWith('PASS ',[StringComparison]::Ordinal)-and-not$_.StartsWith('UMM DECLARED MINIMUM ',[StringComparison]::Ordinal)})}
function Format-Result($Result){'exit '+$Result.exit+'; '+$Result.identity+'; '+$Result.api+$(if($Result.failures.Count){'; '+($Result.failures-join' | ')}else{''})+$(if(-not$Result.identity-or-not$Result.api){'; checker output: '+((Get-ChildDetail $Result)-join' / ')}else{''})}
# One -ApiOnly inspection of a loader that has no reviewed profile ends in exactly one of four ways:
#   api-pass     the inspection completed and the API passed;
#   api-fail     the inspection completed and the API failed. That is valid data about that loader, not a failed
#                check of this script: an unprofiled or below-minimum loader is not required to be compatible;
#   unavailable  the reference is not there;
#   error        the inspection produced no usable result.
# The exit code and the result lines must agree. Anything else is an error, never a blank cell.
function Get-UnprofiledInspection($Result){
    $ordinal=[StringComparison]::Ordinal
    $notAsserted=$Result.identity.StartsWith('UMM IDENTITY NOT ASSERTED ',$ordinal)
    if($Result.exit-eq3-and@($Result.lines|Where-Object{$_.StartsWith('UMM REFERENCE UNAVAILABLE ',$ordinal)}).Count-eq1){return 'unavailable'}
    if($Result.exit-eq0-and$notAsserted-and$Result.api.StartsWith('UMM API PASS ',$ordinal)-and$Result.failures.Count-eq0){return 'api-pass'}
    if($Result.exit-eq1-and$notAsserted-and$Result.api.StartsWith('UMM API FAIL ',$ordinal)-and$Result.failures.Count-ge1){return 'api-fail'}
    'error'
}
function Get-UnprofiledMatrixRow([string]$Path,$Result){
    $outcome=Get-UnprofiledInspection $Result
    $referenceLine=[string](@($Result.lines|Where-Object{$_.StartsWith('UMM REFERENCE ',[StringComparison]::Ordinal)-and$_-match' version [0-9.]+ '})|Select-Object -First 1)
    $version=if($referenceLine){$referenceLine-replace'^.* version ([0-9.]+) .*$','$1'}else{'<version not read>'}
    $row=switch($outcome){
        'api-pass' {'UMM MATRIX unprofiled '+$version+' | reference '+$Path+' | '+$Result.identity+' | '+$Result.api}
        'api-fail' {'UMM MATRIX unprofiled '+$version+' | reference '+$Path+' | '+$Result.identity+' | '+$Result.api+' | incompatible members: '+(($Result.failures|ForEach-Object{$_-replace'^FAIL UMM API ',''})-join'; ')}
        'unavailable' {'UMM MATRIX unprofiled | reference UNAVAILABLE '+$Path+' | identity UNASSESSED | API UNASSESSED'}
        default {'UMM MATRIX unprofiled '+$version+' | reference '+$Path+' | INSPECTION ERROR (checker exit '+$Result.exit+', no usable result) | identity UNASSESSED | API UNASSESSED'}
    }
    [pscustomobject]@{outcome=$outcome;row=$row;detail=@(if($outcome-ceq'error'){Get-ChildDetail $Result})}
}

$document=Get-KmcUmmLoaderProfiles $repoRoot
$installed=Join-Path $managed 'UnityModManager\UnityModManager.dll'
Check 'the declared minimum is 0.31.1 in Info.json and in the profile record, separately from runtime identity' {
    Need ((Get-KmcUmmDeclaredMinimum $repoRoot)-ceq'0.31.1') 'Info.json ManagerVersion must match the owner-selected UMM 0.31.1 baseline.'
    Need ([string]$document.declaredMinimum.value-ceq'0.31.1') 'The profile record states another declared minimum.'
}

# ---- Genuine references on this machine --------------------------------------------------------
$extra=@($AdditionalReference)+@((Get-KmcUmmLocalSetting $repoRoot 'UnityModManagerReferencePaths')-split';')
$buildReference=Join-Path $repoRoot 'obj\loader-references\UnityModManager.dll'
$candidates=@(@($installed,$buildReference)+$extra|ForEach-Object{([string]$_).Trim()}|Where-Object{$_}|Select-Object -Unique)
$references=@();$absent=@()
foreach($path in $candidates){
    if(Test-Path -LiteralPath $path -PathType Leaf){$references+=[pscustomobject]@{path=[IO.Path]::GetFullPath($path);sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant()}}else{$absent+=$path}
}
$installedReference=@($references|Where-Object{$_.path-ieq[IO.Path]::GetFullPath($installed)})
if($installedReference.Count-ne1){
    Unassessed ('the installed loader is not present at '+$installed+'; the genuine-reference cases cannot run')
}else{
    $own=@($document.profiles|Where-Object sha256 -CEQ $installedReference[0].sha256)
    $unselected=Invoke-Checker @()
    Check 'the installed loader is a reviewed profile and passes identity and API with no profile selected' {
        Need ($own.Count-eq1) ('The installed loader is no reviewed profile (SHA-256 '+$installedReference[0].sha256+'). Add a profile from this genuine binary with its provenance; do not select another.')
        Need ($unselected.exit-eq0-and$unselected.identity-ceq('UMM IDENTITY PASS profile '+$own[0].id+' (identified from the reference)')-and$unselected.api-clike'UMM API PASS*') (Format-Result $unselected)
    }
    if($own.Count-eq1){
        Check ('selecting its own profile '+$own[0].id+' explicitly passes') {$r=Invoke-Checker @('-Profile',$own[0].id);Need ($r.exit-eq0-and$r.identity-ceq('UMM IDENTITY PASS profile '+$own[0].id+' (parameter)')) (Format-Result $r)}
        foreach($other in @($document.profiles|Where-Object id -CNE $own[0].id)){
            Check ('selecting '+$other.id+' for the installed '+$own[0].id+' fails identity, and only identity') {
                $r=Invoke-Checker @('-Profile',$other.id)
                Need ($r.exit-eq1-and$r.identity-ceq('UMM IDENTITY FAIL profile '+$other.id+' (parameter)')) (Format-Result $r)
                Need ($r.api-ceq$unselected.api) ('The API result changed with the selected profile: '+$r.api)
                Need (@($r.failures|Where-Object{-not$_.StartsWith('FAIL UMM identity ['+$other.id+'] ',[StringComparison]::Ordinal)}).Count-eq0-and@($r.failures|Where-Object{$_-clike'* exact version'-or$_-clike'* SHA-256'-or$_-clike'* MVID'}).Count-eq3) ('Identity failures are not exactly the version, SHA-256 and MVID pins plus tokens: '+($r.failures-join' | '))
            }
        }
    }
    Check 'an unknown profile name is refused instead of falling back to another profile' {$r=Invoke-Checker @('-Profile','umm-9.9.9');Need ($r.exit-eq2-and$r.identity-ceq''-and$r.api-ceq''-and@($r.lines|Where-Object{$_.StartsWith('UMM PROFILE REFUSED Unknown loader profile umm-9.9.9.',[StringComparison]::Ordinal)}).Count-eq1) (Format-Result $r)}
}
Check 'a reference that is not on this machine is unavailable and unassessed, never passed' {
    $r=Invoke-Checker @('-Reference',(Join-Path $labRoot 'analysis-cache\no-such-loader\UnityModManager.dll'),'-Profile','umm-0.28.2')
    Need ($r.exit-eq3-and$r.identity-ceq'UMM IDENTITY UNASSESSED no reference binary'-and$r.api-ceq'UMM API UNASSESSED no reference binary') (Format-Result $r)
    Need (@($r.lines|Where-Object{$_-match'\bPASS\b'-and$_-notmatch'^TOTAL PASS=0 '}).Count-eq0-and@($r.lines|Where-Object{$_-clike'UMM REFERENCE UNAVAILABLE *'}).Count-eq1) 'An unavailable reference reported a pass.'
}

# ---- Synthetic loaders: the API checks still fail ----------------------------------------------
$stub=@'
using System;
namespace UnityModManagerNet {
 public class UnityModManager {
  public class ModInfo { public string Id; public string DisplayName; public string Version; }
  public class ModEntry {
   public readonly ModInfo Info; public readonly ModLogger Logger;
   public Action<ModEntry> OnGUI; public Action<ModEntry> OnSessionStop;
   public /*ONUPDATE*/Action<ModEntry,float> OnUpdate;
   public Func<ModEntry,bool> OnUnload;
   /*ONTOGGLE*/public Func<ModEntry,bool,bool> OnToggle;/*END*/
   public bool Active { get { return true; } }
   public class ModLogger {
    public void Log(/*LOGPARAMETER*/string/*END*/ message) {} public void Warning(string message) {} public void Error(string message) {}
    public void LogException(string key, Exception exception) {}
   }
  }
  public class UI : UnityEngine.MonoBehaviour {
   public static UI Instance { get { return null; } } public bool Opened { get { return false; } } public int tabId;
   /*TOGGLEWINDOW*/public void ToggleWindow(bool open) {}/*END*/
  }
  public static ModEntry FindMod(string id) { return null; }
 }
}
'@
$vswhere=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio\Installer\vswhere.exe'
$compiler=if(Test-Path -LiteralPath $vswhere -PathType Leaf){[string](@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe')|Select-Object -First 1)}else{''}
$unityCore=Join-Path $managed 'UnityEngine.CoreModule.dll'
$product=Join-Path $repoRoot ('bin\'+$Configuration+'\KingmakerMountedCombat.dll');if(-not(Test-Path -LiteralPath $product -PathType Leaf)){$product=$null}
Check 'a product named explicitly that does not exist is refused, not reported as unassessed' {
    $r=Invoke-Checker @('-ProductDll',(Join-Path $labRoot 'analysis-cache\no-such-product\KingmakerMountedCombat.dll'))
    Need ($r.exit-eq2-and$r.api-ceq''-and@($r.lines|Where-Object{$_.StartsWith('UMM PRODUCT REFUSED The named product does not exist',[StringComparison]::Ordinal)}).Count-eq1) (Format-Result $r)
}
if([string]::IsNullOrWhiteSpace($compiler)-or-not(Test-Path -LiteralPath $unityCore -PathType Leaf)){
    Unassessed 'synthetic loader cases: the C# compiler or UnityEngine.CoreModule is not available'
}else{
    function New-SyntheticLoader([string]$Name,[string]$Source){
        $directory=Join-Path $work $Name;[void][IO.Directory]::CreateDirectory($directory)
        $file=Join-Path $directory 'SyntheticLoader.cs';[IO.File]::WriteAllText($file,$Source)
        $out=Join-Path $directory 'UnityModManager.dll'
        $log=@(& $compiler /nologo /target:library /nowarn:0649 "/out:$out" "/reference:$unityCore" $file 2>&1)
        if($LASTEXITCODE-ne0-or-not(Test-Path -LiteralPath $out -PathType Leaf)){throw ('Synthetic loader '+$Name+' did not compile: '+($log-join' '))}
        $out
    }
    $complete=New-SyntheticLoader 'complete' $stub
    Check 'SYNTHETIC complete loader: the API passes and identity is reported as not asserted' {
        $r=Invoke-Checker @('-Reference',$complete,'-ApiOnly');Need ($r.exit-eq0-and$r.api-clike'UMM API PASS*'-and$r.identity-clike'UMM IDENTITY NOT ASSERTED*') (Format-Result $r)
    }
    Check 'SYNTHETIC complete loader: a passing API does not hide that it is no reviewed loader' {
        $r=Invoke-Checker @('-Reference',$complete)
        Need ($r.exit-eq1-and$r.api-clike'UMM API PASS*'-and$r.identity-ceq'UMM IDENTITY FAIL profile <none matches> (identified from the reference)'-and$r.failures.Count-eq1-and$r.failures[0]-clike'FAIL UMM identity: the reference is no reviewed loader profile*') (Format-Result $r)
    }
    # name, source change, the explicit check that must fail, whether the product's own reference also fails to bind
    $breaks=@(
        @('a missing member (no ModEntry.OnToggle)',{param($s)[regex]::Replace($s,'/\*ONTOGGLE\*/.*?/\*END\*/','')},'FAIL UMM API ModEntry.OnToggle *',$true),
        @('an incompatible signature (ModLogger.Log takes an Int32)',{param($s)$s.Replace('/*LOGPARAMETER*/string/*END*/','int')},'FAIL UMM API ModLogger.Log(System.String) void',$true),
        @('a callback that cannot be assigned (readonly ModEntry.OnUpdate)',{param($s)$s.Replace('/*ONUPDATE*/','readonly ')},'FAIL UMM API ModEntry.OnUpdate *',$false),
        @('a retyped callback (OnToggle without its second argument)',{param($s)[regex]::Replace($s,'/\*ONTOGGLE\*/.*?/\*END\*/','public Func<ModEntry,bool> OnToggle;')},'FAIL UMM API ModEntry.OnToggle *',$true),
        @('a missing capture member (no UI.ToggleWindow)',{param($s)[regex]::Replace($s,'/\*TOGGLEWINDOW\*/.*?/\*END\*/','')},'FAIL UMM API UI.ToggleWindow(System.Boolean) void',$true),
        # Only the compiled product's own references show this one: no explicit check names these members.
        @('members the product calls made non-public (internal ModEntry.Active, private UI.tabId)',{param($s)$s.Replace('public bool Active','internal bool Active').Replace('public int tabId','private int tabId')},'FAIL UMM API every loader member the compiled product references binds by name and signature*',$true))
    $index=0;$incompatible=$null
    foreach($break in $breaks){
        $index++;$changed=& $break[1] $stub
        if($changed-ceq$stub){throw ('Synthetic loader change did not apply: '+$break[0])}
        $loader=New-SyntheticLoader ('break-'+$index) $changed
        if($index-eq1){$incompatible=$loader}
        if($null-eq$product-and$break[2]-clike'*compiled product references*'){Unassessed ('SYNTHETIC loader with '+$break[0]+': no '+$Configuration+' build of the product');continue}
        Check ('SYNTHETIC loader with '+$break[0]+' fails the API') {
            $r=Invoke-Checker @('-Reference',$loader,'-ApiOnly')
            Need ($r.exit-eq1-and$r.api-clike'UMM API FAIL*'-and@($r.failures|Where-Object{$_-clike$break[2]}).Count-eq1) (Format-Result $r)
            if($null-ne$product){
                $unbound=@($r.failures|Where-Object{$_-clike'FAIL UMM API every loader member the compiled product references binds by name and signature*'}).Count
                Need ($unbound-eq$(if($break[3]){1}else{0})) ('The compiled product binding result is not the expected one: '+(Format-Result $r))
            }
        }
    }
    if($null-eq$product){Unassessed 'synthetic loader cases: the compiled product binding sweep (no build)'}
    Check 'matrix row for a completed SYNTHETIC inspection: API PASS and API FAIL are both data, and neither is an error' {
        $pass=Get-UnprofiledMatrixRow $complete (Invoke-Checker @('-Reference',$complete,'-ApiOnly'))
        $fail=Get-UnprofiledMatrixRow $incompatible (Invoke-Checker @('-Reference',$incompatible,'-ApiOnly'))
        Need ($pass.outcome-ceq'api-pass'-and$pass.row-clike'* | UMM IDENTITY NOT ASSERTED * | UMM API PASS *'-and$pass.detail.Count-eq0) $pass.row
        Need ($fail.outcome-ceq'api-fail'-and$fail.row-clike'* | UMM API FAIL * | incompatible members: ModEntry.OnToggle *'-and$fail.detail.Count-eq0) $fail.row
    }
}

# ---- The matrix never shows an incomplete inspection as a finished row ---------------------------
Check 'matrix outcomes: a pass, an incompatibility, an unavailable reference and a result-less inspection are four different things' {
    $make={param([int]$Exit,[string[]]$Lines)$found={param($prefix)$hit=@($Lines|Where-Object{$_.StartsWith($prefix,[StringComparison]::Ordinal)});if($hit.Count){$hit[$hit.Count-1]}else{''}}
        [pscustomobject]@{exit=$Exit;lines=@($Lines);identity=(& $found 'UMM IDENTITY ');api=(& $found 'UMM API ');failures=@($Lines|Where-Object{$_.StartsWith('FAIL ',[StringComparison]::Ordinal)})}}
    $notAsserted='UMM IDENTITY NOT ASSERTED no reviewed profile was applied (-ApiOnly); this is not an identity pass'
    foreach($case in @(
        @('api-pass',0,@($notAsserted,'UMM API PASS reference 0.31.1.0')),
        @('api-fail',1,@('FAIL UMM API ModEntry.OnToggle x',$notAsserted,'UMM API FAIL reference 0.1.0.0')),
        @('unavailable',3,@('UMM REFERENCE UNAVAILABLE X:\none\UnityModManager.dll','UMM IDENTITY UNASSESSED no reference binary','UMM API UNASSESSED no reference binary')),
        @('error',1,@('Exception calling "ReflectionOnlyLoadFrom"')),
        @('error',0,@($notAsserted)),
        @('error',0,@($notAsserted,'UMM API FAIL reference 0.1.0.0')),
        @('error',1,@($notAsserted,'UMM API PASS reference 0.1.0.0')),
        @('error',1,@($notAsserted,'UMM API FAIL reference 0.1.0.0')),
        @('error',2,@('UMM PRODUCT REFUSED The named product does not exist')),
        @('error',3,@('UMM IDENTITY UNASSESSED no reference binary','UMM API UNASSESSED no reference binary')),
        @('error',-1,@()))){
        $actual=Get-UnprofiledInspection (& $make $case[1] $case[2])
        Need ($actual-ceq$case[0]) ('exit '+$case[1]+' with ['+($case[2]-join' | ')+'] was read as '+$actual+', not '+$case[0])
    }
}
[void][IO.Directory]::CreateDirectory((Join-Path $work 'unreadable'))
$unreadable=Join-Path $work 'unreadable\UnityModManager.dll';[IO.File]::WriteAllText($unreadable,'This file is not an assembly. It stands for a reference the checker cannot inspect.')
Check 'matrix row for an inspection that produced no result: an explicit error that keeps the checker''s own failure, never blank cells' {
    $result=Invoke-Checker @('-Reference',$unreadable,'-ApiOnly')
    Need ($result.exit-ne0-and$result.identity-ceq''-and$result.api-ceq'') ('The unreadable reference did not reproduce a result-less inspection: '+(Format-Result $result))
    $row=Get-UnprofiledMatrixRow $unreadable $result
    Need ($row.outcome-ceq'error'-and$row.row-clike'* | INSPECTION ERROR (checker exit *, no usable result) | identity UNASSESSED | API UNASSESSED') $row.row
    Need ($row.row-notmatch'\|\s*\|'-and$row.row-notmatch'\|\s*$') ('The row has a blank cell: '+$row.row)
    Need (@($row.detail|Where-Object{$_-match'ReflectionOnlyLoadFrom|assembly manifest|BadImageFormat'}).Count-ge1) ('The checker''s own failure was not kept: '+($row.detail-join' / '))
}
Check 'matrix row for a reference that is not there: unavailable, not an error and not a result' {
    $gone=Join-Path $work 'absent\UnityModManager.dll'
    $row=Get-UnprofiledMatrixRow $gone (Invoke-Checker @('-Reference',$gone,'-ApiOnly'))
    Need ($row.outcome-ceq'unavailable'-and$row.row-ceq('UMM MATRIX unprofiled | reference UNAVAILABLE '+$gone+' | identity UNASSESSED | API UNASSESSED')) $row.row
}

# ---- Loader matrix -------------------------------------------------------------------------------
Write-Host ''
Write-Host ('UMM MATRIX declared minimum '+(Get-KmcUmmDeclaredMinimum $repoRoot)+' (Info.json ManagerVersion); native execution: not run for any row')
$known=@{}
foreach($profile in $document.profiles){
    $holder=@($references|Where-Object sha256 -CEQ $profile.sha256)|Select-Object -First 1
    if($null-eq$holder){
        Unassessed ('profile '+$profile.id+': no genuine binary with its recorded SHA-256 is on this machine')
        Write-Host ('UMM MATRIX '+$profile.id+' | reference UNAVAILABLE | identity UNASSESSED | API UNASSESSED')
        continue
    }
    $known[$holder.sha256]=$true
    $r=Invoke-Checker @('-Reference',$holder.path,'-Profile',$profile.id)
    Check ('profile '+$profile.id+': its genuine binary passes identity and API') {Need ($r.exit-eq0-and$r.identity-clike'UMM IDENTITY PASS*'-and$r.api-clike'UMM API PASS*') (Format-Result $r)}
    Write-Host ('UMM MATRIX '+$profile.id+' | reference '+$holder.path+' | '+$r.identity+' | '+$r.api)
}
# Unprofiled loaders are inspected for their API only. An incompatible one is a row of data. An inspection that
# gave no result is an error with the checker's own output, and the matrix is then reported as incomplete.
foreach($reference in @($references|Where-Object{-not$known.ContainsKey($_.sha256)})){
    $row=Get-UnprofiledMatrixRow $reference.path (Invoke-Checker @('-Reference',$reference.path,'-ApiOnly'))
    Write-Host $row.row
    if($row.outcome-ceq'unavailable'){$unassessed.Add('unprofiled reference became unavailable: '+$reference.path)}
    if($row.outcome-ceq'error'){
        $errors.Add($reference.path)
        foreach($line in $row.detail){Write-Host ('UMM MATRIX ERROR DETAIL '+$line)}
        if($row.detail.Count-eq0){Write-Host 'UMM MATRIX ERROR DETAIL the checker wrote nothing'}
    }
}
foreach($path in $absent){Write-Host ('UMM MATRIX configured reference missing | '+$path+' | identity UNASSESSED | API UNASSESSED');$unassessed.Add('configured reference missing: '+$path)}
Write-Host ('UMM LOADER CONTRACT CHECKS PASS='+$passed+' FAIL=0 UNASSESSED='+$unassessed.Count+' ERRORS='+$errors.Count+$(if($errors.Count){'; THE MATRIX IS INCOMPLETE: '+$errors.Count+' inspection(s) produced no result'}else{''})+'; offline inspection of loader binaries, not native execution.')
if($errors.Count-ne0){exit 1}
