[CmdletBinding()]
param(
    # The UnityModManager.dll to inspect. Default: the loader installed in the Kingmaker the build references.
    [string]$Reference='',
    # The reviewed profile (planning/UMM-LOADER-PROFILES.json) the reference must be. Default: the
    # UnityModManagerProfile of LocalGamePaths.props, else the profile the reference identifies as.
    [string]$Profile='',
    # Inspect the API of a genuine loader that has no reviewed profile. Identity is then reported as not
    # asserted; it is never reported as passed.
    [switch]$ApiOnly,
    # The compiled product whose loader member references are resolved. Default: bin\Release, else bin\Debug.
    [string]$ProductDll=''
)
# One loader reference, one process: two UnityModManager assemblies cannot be inspected side by side.
# Reports four things separately and never lets one stand in for another:
#   the declared minimum, the identity result for one profile, the API result, and native execution (not run).
# Exit 0: nothing failed. Exit 1: an identity or API check failed. Exit 2: the requested profile is not a
# reviewed one. Exit 3: the reference is unavailable, so
# nothing was assessed. The installed loader is only read; no loader binary is copied, installed or replaced.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$managed=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data\Managed'
. (Join-Path $PSScriptRoot 'UmmLoaderContracts.ps1')
if([string]::IsNullOrWhiteSpace($Reference)){$Reference=Join-Path $managed 'UnityModManager\UnityModManager.dll'}
$document=Get-KmcUmmLoaderProfiles $repoRoot
$minimum=Get-KmcUmmDeclaredMinimum $repoRoot
Write-Host ('UMM DECLARED MINIMUM '+$minimum+' (Info.json ManagerVersion; a support declaration, not assessed by inspecting another version)')
if($ApiOnly-and-not[string]::IsNullOrWhiteSpace($Profile)){throw '-ApiOnly inspects a loader without a reviewed profile; it cannot be combined with -Profile.'}
if(-not(Test-Path -LiteralPath $Reference -PathType Leaf)){
    Write-Host ('UMM REFERENCE UNAVAILABLE '+$Reference)
    Write-Host 'UMM IDENTITY UNASSESSED no reference binary'
    Write-Host 'UMM API UNASSESSED no reference binary'
    Write-Host 'UMM NATIVE EXECUTION not run by this check'
    Write-Host 'TOTAL PASS=0 FAIL=0 UNASSESSED=2'
    exit 3
}
$Reference=[IO.Path]::GetFullPath($Reference)
$passes=0;$failures=New-Object 'System.Collections.Generic.List[string]';$unassessed=New-Object 'System.Collections.Generic.List[string]'
function Assert-Contract([bool]$Condition,[string]$Message){if($Condition){$script:passes++;Write-Host "PASS $Message"}else{$script:failures.Add($Message);Write-Host "FAIL $Message"}}
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({param($sender,$eventArgs)
    $name=($eventArgs.Name-split',')[0]
    if($name-ceq'UnityModManager'){return $null}
    foreach($root in @($managed,(Join-Path $managed 'UnityModManager'))){$path=Join-Path $root ($name+'.dll');if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)}}
    try{return [Reflection.Assembly]::ReflectionOnlyLoad($eventArgs.Name)}catch{return $null}})
$assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($Reference)
Register-KmcUmmReferenceResolver $assembly
$identity=Get-KmcUmmReferenceIdentity $Reference $assembly
Write-Host ('UMM REFERENCE '+$Reference+' version '+$identity.version+' SHA-256 '+$identity.sha256+' MVID '+$identity.mvid+' length '+$identity.length)
if($ApiOnly){
    $identityLine='UMM IDENTITY NOT ASSERTED no reviewed profile was applied (-ApiOnly); this is not an identity pass'
}else{
    # A profile name that is not reviewed is refused outright; it never falls back to another profile.
    try{$selected=Resolve-KmcUmmLoaderProfile $document $identity $Profile (Get-KmcUmmLocalSetting $repoRoot 'UnityModManagerProfile')}
    catch{Write-Host ('UMM PROFILE REFUSED '+$_.Exception.Message);Write-Host 'TOTAL PASS=0 FAIL=1 UNASSESSED=0';exit 2}
    $identityOk=Invoke-KmcUmmLoaderIdentityChecks $assembly $identity $selected ${function:Assert-Contract}
    $identityLine='UMM IDENTITY '+$(if($identityOk){'PASS'}else{'FAIL'})+' profile '+$(if($null-eq$selected.profile){'<none matches>'}else{$selected.profile.id})+' ('+$selected.selection+')'
}
$before=$failures.Count
Invoke-KmcUmmLoaderApiChecks $assembly ${function:Assert-Contract}
$product=Get-KmcUmmProductAssembly $repoRoot $ProductDll;$reader=Get-KmcUmmMetadataReaderPath $managed
if($null-eq$product-or-not$reader){
    $unassessed.Add('UMM API product binding sweep: '+$(if($null-eq$product){'no compiled product (build first)'}else{'no metadata reader (dnlib beside the installed loader)'}));Write-Host ('UNASSESSED '+$unassessed[$unassessed.Count-1])
}else{
    $bindings=Invoke-KmcUmmProductBindingChecks $product $reader ${function:Assert-Contract}
    Write-Host ('INFO compiled product '+$product.Location+' MVID '+$product.ManifestModule.ModuleVersionId+' was built against '+(@($product.GetReferencedAssemblies()|Where-Object Name -CEQ 'UnityModManager')|ForEach-Object{$_.Version})+' and binds '+$bindings.resolved.Count+' loader members')
}
$apiOk=$failures.Count-eq$before
Write-Host $identityLine
Write-Host ('UMM API '+$(if($apiOk){'PASS'}else{'FAIL'})+' reference '+$identity.version+$(if($unassessed.Count){' (product binding sweep unassessed)'}else{''}))
Write-Host 'UMM NATIVE EXECUTION not run by this check'
Write-Host ('TOTAL PASS='+$passes+' FAIL='+$failures.Count+' UNASSESSED='+$unassessed.Count)
if($failures.Count-ne0){exit 1}
