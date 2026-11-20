[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
# The assembly checker's loader binding sweep reads the compiled product, so it must read the build of the
# configuration it was given and no other. This runs the real Test-AssemblyContracts.ps1 in private copies of the
# repository layout whose bin folders are arranged for each risk:
#   - only a Debug build exists: a Debug run must bind it, and a run that was not told the configuration
#     (the umbrella before it forwarded -Configuration) reports the sweep unassessed;
#   - a Debug build beside a different Release file: a Debug run must not read the Release file.
# That the umbrella and the tier selector hand the configuration over is checked by Test-TierSelection.ps1,
# which runs the real Test.ps1 and Test-Tiers.ps1 with tracing children.
# The "Debug build" here is a copy of this checkout's own bin\<Configuration> product; the other Release file
# is an empty assembly compiled for the test. The installed game and loader are only read. Nothing is launched.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$labRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$built=Join-Path $repoRoot ('bin\'+$Configuration+'\KingmakerMountedCombat.dll')
if(-not(Test-Path -LiteralPath $built -PathType Leaf)){throw "Build the product first: $built is missing."}
$builtMvid=[Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($built)).ManifestModule.ModuleVersionId.ToString()
$work=Join-Path $labRoot ('analysis-cache\assembly-contract-configuration\'+[Guid]::NewGuid().ToString('N'))
$passed=0
function Check([string]$Name,[scriptblock]$Body){& $Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Need([bool]$Condition,[string]$Message){if(-not$Condition){throw $Message}}
# A private repository layout: <lab>\repo\kmc with the real scripts and records, and the given product builds.
function New-Layout([string]$Name,[hashtable]$Builds){
    $lab=Join-Path $work $Name;$repo=Join-Path $lab 'repo\kmc'
    foreach($directory in @('scripts','planning')){[void][IO.Directory]::CreateDirectory((Join-Path $repo $directory))}
    Copy-Item -LiteralPath (Join-Path $labRoot 'environment-intake.json') -Destination $lab
    foreach($script in @('Test-AssemblyContracts.ps1','UmmLoaderContracts.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination (Join-Path $repo 'scripts')}
    Copy-Item -LiteralPath (Join-Path $repoRoot 'planning\UMM-LOADER-PROFILES.json') -Destination (Join-Path $repo 'planning')
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Info.json') -Destination $repo
    foreach($build in $Builds.Keys){
        $directory=Join-Path $repo ('bin\'+$build);[void][IO.Directory]::CreateDirectory($directory)
        Copy-Item -LiteralPath $Builds[$build] -Destination (Join-Path $directory 'KingmakerMountedCombat.dll')
    }
    $repo
}
function Invoke-AssemblyChecker([string]$Repo,[string[]]$Arguments){
    # A failing run writes to its error stream; that output is a result to read here.
    $preference=$ErrorActionPreference;$ErrorActionPreference='Continue'
    try{$output=@(& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Repo 'scripts\Test-AssemblyContracts.ps1') @Arguments 2>&1|ForEach-Object{[string]$_});$code=$LASTEXITCODE}finally{$ErrorActionPreference=$preference}
    $one={param([string]$Pattern)$found=@($output|Where-Object{$_-match$Pattern});if($found.Count){$found[$found.Count-1]}else{''}}
    [pscustomobject]@{exit=$code;lines=$output;read=(& $one '^INFO Kingmaker UMM API product binding sweep read ');api=(& $one '^UMM API ')
        bound=(& $one '^(PASS|FAIL) Kingmaker UMM API every loader member the compiled product references binds');unassessed=(& $one '^UNASSESSED Kingmaker UMM API product binding sweep')
        total=(& $one '^TOTAL PASS=')}
}
function Format-Run($Run){'exit '+$Run.exit+'; '+$Run.read+'; '+$Run.bound+'; '+$Run.unassessed+'; '+$Run.api+'; '+$Run.total}
function Read-Path([string]$Repo,[string]$Build){'read the '+$Build+' build '+(Join-Path $Repo ('bin\'+$Build+'\KingmakerMountedCombat.dll'))+' MVID '}

Check 'the real assembly checker declares -Configuration and -Target, which is what makes the tier selector forward the build' {
    $parameters=(Get-Command (Join-Path $PSScriptRoot 'Test-AssemblyContracts.ps1')).Parameters
    Need ($parameters.ContainsKey('Configuration')-and$parameters.ContainsKey('Target')) 'The assembly checker no longer declares both parameters.'
    $allowed=@($parameters['Configuration'].Attributes|Where-Object{$_-is[Management.Automation.ValidateSetAttribute]})[0].ValidValues
    Need (($allowed-join',')-ceq'Debug,Release') ('The assembly checker accepts other configurations: '+($allowed-join','))
}

# Only a Debug build exists.
$onlyDebug=New-Layout 'only-debug' @{Debug=$built}
$debugRun=Invoke-AssemblyChecker $onlyDebug @('-Target','Kingmaker','-Configuration','Debug')
Check 'only a Debug build exists: a Debug run binds that build and reports no unassessed sweep' {
    Need ($debugRun.exit-eq0-and$debugRun.read.Contains((Read-Path $onlyDebug 'Debug')+$builtMvid)) (Format-Run $debugRun)
    Need ($debugRun.bound-match'^PASS .* \((\d+) of \1\)$'-and[int]$Matches[1]-ge1-and$debugRun.unassessed-ceq''-and$debugRun.api-notmatch'UNASSESSED'-and$debugRun.api-clike'UMM API PASS *') (Format-Run $debugRun)
}
$untoldRun=Invoke-AssemblyChecker $onlyDebug @('-Target','Kingmaker')
Check 'only a Debug build exists: a run that is not told the configuration reads nothing and says the sweep is unassessed' {
    Need ($untoldRun.read-ceq''-and$untoldRun.bound-ceq''-and$untoldRun.unassessed-clike'UNASSESSED Kingmaker UMM API product binding sweep: no Release build*'-and$untoldRun.api-match'UNASSESSED') (Format-Run $untoldRun)
    Need ($debugRun.total-match'PASS=(\d+) FAIL=0$') (Format-Run $debugRun);$assessed=[int]$Matches[1]
    Need ($untoldRun.total-match'PASS=(\d+) FAIL=0$'-and[int]$Matches[1]-lt$assessed) ('An unassessed sweep counted the same passes as an assessed one: '+$untoldRun.total+' against '+$debugRun.total)
}
$bothRun=Invoke-AssemblyChecker $onlyDebug @('-Target','Both','-Configuration','Debug')
Check 'the Both target hands the configuration to its Kingmaker child' {
    # Whether the Wrath half can run depends on the machine; only the Kingmaker child is read here.
    Need (@($bothRun.lines|Where-Object{$_.Contains((Read-Path $onlyDebug 'Debug')+$builtMvid)}).Count-eq1) (Format-Run $bothRun)
}

# A Debug build beside a different Release file.
$decoyDirectory=Join-Path $work 'decoy';[void][IO.Directory]::CreateDirectory($decoyDirectory)
$decoy=Join-Path $decoyDirectory 'KingmakerMountedCombat.dll'
Add-Type -TypeDefinition 'public static class KmcConfigurationDecoy { }' -OutputAssembly $decoy -OutputType Library
$different=New-Layout 'debug-beside-other-release' @{Debug=$built;Release=$decoy}
$debugBeside=Invoke-AssemblyChecker $different @('-Target','Kingmaker','-Configuration','Debug')
Check 'a Debug run beside a different Release file reads the Debug build, not the Release file' {
    Need ($debugBeside.exit-eq0-and$debugBeside.read.Contains((Read-Path $different 'Debug')+$builtMvid)-and$debugBeside.bound-clike'PASS *') (Format-Run $debugBeside)
    Need (@($debugBeside.lines|Where-Object{$_.Contains('bin\Release\')}).Count-eq0) 'The Debug run mentions the Release build.'
}
$releaseBeside=Invoke-AssemblyChecker $different @('-Target','Kingmaker','-Configuration','Release')
Check 'the Release run of the same layout reads the other file and fails on it, so the configuration alone decides' {
    Need ($releaseBeside.exit-eq1-and$releaseBeside.read.Contains((Read-Path $different 'Release'))-and-not$releaseBeside.read.Contains($builtMvid)) (Format-Run $releaseBeside)
    Need (@($releaseBeside.lines|Where-Object{$_-clike'FAIL Kingmaker UMM API the compiled product''s loader references were read from its metadata (0 members*'}).Count-eq1) (Format-Run $releaseBeside)
}
Write-Host ('ASSEMBLY CONTRACT CONFIGURATION PASS='+$passed+' FAIL=0; the real checker in private repository layouts, no game launched.')
