[CmdletBinding()]
param([string]$RepositoryRoot,[string]$PackageScriptDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
if(-not$PackageScriptDirectory){$PackageScriptDirectory=$PSScriptRoot}
$labRoot=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))
$analysis=[IO.Path]::GetFullPath((Join-Path $labRoot 'analysis-cache')).TrimEnd('\')+'\'
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('qualification-packaging-'+[Guid]::NewGuid().ToString('N'))}
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$output.StartsWith($analysis,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $output)){throw 'Fresh private analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($output)
$buildMock=@'
[CmdletBinding()]
param([ValidateSet('Release')][string]$Configuration)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$case=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$config=Get-Content -LiteralPath (Join-Path $case 'config.json') -Raw|ConvertFrom-Json
Add-Content -LiteralPath (Join-Path $case 'build-count.txt') -Value 'build'
if(-not$config.missingMain){$path=Join-Path $repo 'bin/Release';[void][IO.Directory]::CreateDirectory($path);[IO.File]::WriteAllText((Join-Path $path 'KingmakerMountedCombat.dll'),'opaque-main-from-this-build',[Text.UTF8Encoding]::new($false))}
if(-not$config.missingObserver){$path=Join-Path $repo 'bin/Observer/Release';[void][IO.Directory]::CreateDirectory($path);[IO.File]::WriteAllText((Join-Path $path 'KmcRemovalObserver.dll'),'opaque-observer-from-this-build',[Text.UTF8Encoding]::new($false))}
$global:LASTEXITCODE=[int]$config.buildExit
'@
$validatorMock=@'
[CmdletBinding()]
param([string]$PackagePath,[switch]$PassThru)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$case=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$config=Get-Content -LiteralPath (Join-Path $case 'config.json') -Raw|ConvertFrom-Json
$observer=$PSCommandPath.EndsWith('Validate-ObserverPackage.ps1',[StringComparison]::OrdinalIgnoreCase)
if($observer-and$config.validatorFailure){throw 'Explicit observer assembly-validator boundary refusal.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($PackagePath)
try{
 $name=if($observer){'KmcRemovalObserver'}else{'KingmakerMountedCombat'}
 if($zip.Entries.Count-ne2){throw 'Actual ZIP entry count differs.'}
 $entries=@($zip.Entries|ForEach-Object{$_.FullName.Replace('\','/')})
 if(@($entries|Where-Object{$_-ceq($name+'/Info.json')}).Count-ne1-or@($entries|Where-Object{$_-ceq($name+'/'+$name+'.dll')}).Count-ne1){throw 'Actual ZIP member identity differs.'}
 $dll=$zip.Entries|Where-Object{$_.FullName.Replace('\','/')-ceq($name+'/'+$name+'.dll')}
 $stream=$dll.Open();$hasher=[Security.Cryptography.SHA256]::Create()
 try{$dllHash=([BitConverter]::ToString($hasher.ComputeHash($stream))).Replace('-','').ToLowerInvariant()}finally{$hasher.Dispose();$stream.Dispose()}
}finally{$zip.Dispose()}
if(-not$observer-and$config.dirtyDuringPackaging){[IO.File]::WriteAllText((Join-Path $case 'dirty-marker.txt'),'dirty')}
if(-not$observer-and$config.tamperOtherOutput){[IO.File]::WriteAllText((Join-Path $repo 'bin/Observer/Release/KmcRemovalObserver.dll'),'unexpected-other-output')}
if($observer-and$config.tamperEarlierOutput){[IO.File]::WriteAllText((Join-Path $repo 'bin/Release/KingmakerMountedCombat.dll'),'unexpected-earlier-output')}
if($config.validatorHashMismatch){$dllHash=[string]::new([char]'f',64)}
if($PassThru){[pscustomobject]@{packageSha256=(Get-FileHash -LiteralPath $PackagePath).Hash.ToLowerInvariant();dllSha256=$dllHash;dllMvid='11111111-1111-4111-8111-111111111111';entries=$entries}}
'@
$childRunner=@'
[CmdletBinding()]
param([string]$CaseRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$global:TestCaseRoot=$CaseRoot
$global:Config=Get-Content -LiteralPath (Join-Path $CaseRoot 'config.json') -Raw|ConvertFrom-Json
function global:git {
 $values=@($args);$global:LASTEXITCODE=0
 $built=Test-Path -LiteralPath (Join-Path $global:TestCaseRoot 'build-count.txt')
 if($values-contains'rev-parse'){if($built-and$global:Config.commitAfter){return('b'*40)};return('a'*40)}
 if($values-contains'branch'){if($built-and$global:Config.branchAfter){return 'codex/changed'};return 'codex/package-test'}
 if($values-contains'status'){if($global:Config.dirtyBefore-or($built-and$global:Config.dirtyAfter)-or(Test-Path -LiteralPath (Join-Path $global:TestCaseRoot 'dirty-marker.txt'))){return ' M tracked.cs'};return}
 throw 'Unexpected git boundary call.'
}
$repo=Join-Path $CaseRoot 'repo/KingmakerMountedCombat'
$entry=if($global:Config.entry-eq'Observer'){'Package-Observer.ps1'}else{'Package.ps1'}
$parameters=@{}
if(-not$global:Config.omitQualifier){$parameters.ArtifactQualifier=$global:Config.qualifier}
if($global:Config.both){$parameters.IncludeRemovalObserver=$true}
if($global:Config.skip){$parameters.SkipBuild=$true}
$success=$false;$failure=$null
try{& (Join-Path $repo ('scripts/'+$entry)) @parameters;$success=$true}catch{$failure=$_.Exception.Message}
$buildCount=if(Test-Path -LiteralPath (Join-Path $CaseRoot 'build-count.txt')){@(Get-Content -LiteralPath (Join-Path $CaseRoot 'build-count.txt')).Count}else{0}
$result=[ordered]@{success=$success;error=$failure;builds=$buildCount;nativeInvoked=$false}
[IO.File]::WriteAllText((Join-Path $CaseRoot 'result.json'),($result|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
'@
function Write-Text([string]$Path,[string]$Value){[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path));[IO.File]::WriteAllText($Path,$Value,[Text.UTF8Encoding]::new($false))}
$cases=@(
    @{name='both-one-build';both=$true;success=$true;packages=2;builds=1},
    @{name='main-default';success=$true;packages=1;builds=1},
    @{name='observer-standalone';entry='Observer';success=$true;packages=1;builds=1},
    @{name='main-unqualified-default';omitQualifier=$true;success=$true;packages=1;builds=1},
    @{name='both-unqualified-default';both=$true;omitQualifier=$true;success=$true;packages=2;builds=1},
    @{name='skip-main';skip=$true;builds=0;expectedError='-SkipBuild is prohibited for qualification packages.'},
    @{name='skip-observer';entry='Observer';skip=$true;builds=0;expectedError='-SkipBuild is prohibited for qualification packages.'},
    @{name='dirty-before';both=$true;dirtyBefore=$true;builds=0;expectedError='Qualification packaging requires a clean worktree before build.'},
    @{name='commit-changed';both=$true;commitAfter=$true;builds=1;expectedError='Source identity changed during qualification build or packaging.'},
    @{name='branch-changed';both=$true;branchAfter=$true;builds=1;expectedError='Source identity changed during qualification build or packaging.'},
    @{name='dirty-after-build';both=$true;dirtyAfter=$true;builds=1;expectedError='Source identity changed during qualification build or packaging.'},
    @{name='failed-build-with-stale-dlls';both=$true;buildExit=7;builds=1;stale=$true;expectedError='Qualification solution build failed.'},
    @{name='observer-build-output-missing';both=$true;missingObserver=$true;builds=1;expectedError='Clean solution build did not produce KmcRemovalObserver.dll.'},
    @{name='main-build-output-missing';both=$true;missingMain=$true;builds=1;expectedError='Clean solution build did not produce KingmakerMountedCombat.dll.'},
    @{name='qualified-main-immutable';both=$true;preexisting='KingmakerMountedCombat';builds=1;expectedError='Qualified artifact already exists and is immutable:*'},
    @{name='qualified-observer-immutable';both=$true;preexisting='KmcRemovalObserver';builds=1;expectedError='Qualified artifact already exists and is immutable:*'},
    @{name='observer-validator-refusal';both=$true;validatorFailure=$true;builds=1;expectedError='Explicit observer assembly-validator boundary refusal.'},
    @{name='validator-hash-substitution';both=$true;validatorHashMismatch=$true;builds=1;expectedError='Packaged DLL differs from this clean build output.'},
    @{name='output-changed-between-packages';both=$true;tamperOtherOutput=$true;builds=1;expectedError='Clean build output changed before packaging.'},
    @{name='earlier-output-changed-before-manifests';both=$true;tamperEarlierOutput=$true;builds=1;expectedError='A verified build output or package changed before qualification manifests were written.'},
    @{name='source-changed-during-packaging';both=$true;dirtyDuringPackaging=$true;builds=1;expectedError='Source identity changed during qualification build or packaging.'},
    @{name='qualifier-path-escape';both=$true;qualifier='../escape';builds=0;expectedError='*ArtifactQualifier*'}
)
$results=@();$timer=[Diagnostics.Stopwatch]::StartNew()
foreach($case in $cases){
    $caseRoot=Join-Path $output $case.name
    $repo=Join-Path $caseRoot 'repo/KingmakerMountedCombat'
    $config=[ordered]@{entry='Main';qualifier='private';omitQualifier=$false;both=$false;skip=$false;dirtyBefore=$false;commitAfter=$false;branchAfter=$false;dirtyAfter=$false;dirtyDuringPackaging=$false;buildExit=0;missingMain=$false;missingObserver=$false;validatorFailure=$false;validatorHashMismatch=$false;tamperOtherOutput=$false;tamperEarlierOutput=$false}
    foreach($key in $case.Keys){if($config.Contains($key)){$config[$key]=$case[$key]}}
    Write-Text (Join-Path $caseRoot 'config.json') ($config|ConvertTo-Json)
    foreach($name in @('Package.ps1','Package-Observer.ps1','QualificationPackaging.ps1')){Write-Text (Join-Path $repo ('scripts/'+$name)) ([IO.File]::ReadAllText((Join-Path $PackageScriptDirectory $name)))}
    Write-Text (Join-Path $repo 'scripts/Build-Local.ps1') $buildMock
    foreach($name in @('Validate-Package.ps1','Validate-ObserverPackage.ps1')){Write-Text (Join-Path $repo ('scripts/'+$name)) $validatorMock}
    Write-Text (Join-Path $repo 'version.json') '{"productVersion":"0.1.0-private"}'
    Write-Text (Join-Path $repo 'Info.json') '{"Id":"KingmakerMountedCombat"}'
    Write-Text (Join-Path $repo 'tools/KmcRemovalObserver/Info.json') '{"Id":"KmcRemovalObserver"}'
    Write-Text (Join-Path $repo 'tools/KmcRemovalObserver/ObserverIdentity.cs') 'internal const string ProductVersion = "0.1.0-private";'
    Write-Text (Join-Path $caseRoot 'Run-Case.ps1') $childRunner
    if($case.ContainsKey('stale')){Write-Text (Join-Path $repo 'bin/Release/KingmakerMountedCombat.dll') 'opaque-old-main';Write-Text (Join-Path $repo 'bin/Observer/Release/KmcRemovalObserver.dll') 'opaque-old-observer'}
    $preexistingPath=$null;$preexistingHash=$null
    if($case.ContainsKey('preexisting')){$preexistingPath=Join-Path $caseRoot ('artifacts/'+$case.preexisting+'-0.1.0-private-private-diagnostic.zip');Write-Text $preexistingPath 'immutable-existing-qualified-artifact';$preexistingHash=(Get-FileHash -LiteralPath $preexistingPath).Hash}
    &powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $caseRoot 'Run-Case.ps1') -CaseRoot $caseRoot *> (Join-Path $caseRoot 'run.log')
    $childExit=$LASTEXITCODE
    $passed=$false;$errorText=$null
    try{
        if($childExit-ne0){throw 'Private child runner itself failed.'}
        $actual=Get-Content -LiteralPath (Join-Path $caseRoot 'result.json') -Raw|ConvertFrom-Json
        $expectedSuccess=$case.ContainsKey('success')-and$case.success
        if($actual.success-ne$expectedSuccess-or$actual.builds-ne$case.builds){throw ('Wrong admission/build count: '+($actual|ConvertTo-Json -Compress))}
        if(-not$expectedSuccess-and$actual.error-notlike$case.expectedError){throw ('Wrong refusal boundary: '+$actual.error+' expected '+$case.expectedError)}
        $manifests=@(Get-ChildItem -LiteralPath $caseRoot -File -Recurse|Where-Object Name -Like '*.manifest.json')
        if(-not$expectedSuccess-and$manifests.Count-ne0){throw 'A failed build/guard minted a qualification manifest.'}
        if($expectedSuccess){
            if($manifests.Count-ne$case.packages){throw 'Wrong package manifest count.'}
            foreach($manifestFile in $manifests){
                $manifest=Get-Content -LiteralPath $manifestFile.FullName -Raw|ConvertFrom-Json
                $isObserver=$manifestFile.Name.StartsWith('KmcRemovalObserver-',[StringComparison]::Ordinal)
                $expectedGenerator=if($isObserver){'scripts/Package-Observer.ps1'}else{'scripts/Package.ps1'}
                if(@($manifest.PSObject.Properties).Count-ne13-or$manifest.schemaVersion-ne2-or$manifest.generator-cne$expectedGenerator-or$manifest.commit-cne('a'*40)-or$manifest.branch-cne'codex/package-test'-or-not$manifest.worktreeClean-or-not$manifest.qualificationEligible){throw 'Existing strict manifest schema/generator/source contract changed.'}
                $dll=if($isObserver){Join-Path $repo 'bin/Observer/Release/KmcRemovalObserver.dll'}else{Join-Path $repo 'bin/Release/KingmakerMountedCombat.dll'}
                if($manifest.dllSha256-cne(Get-FileHash -LiteralPath $dll).Hash.ToLowerInvariant()-or$manifest.packageSha256-cne(Get-FileHash -LiteralPath $manifest.packagePath).Hash.ToLowerInvariant()-or@($manifest.entries).Count-ne2){throw 'Actual ZIP/build hashes or entry contract differ.'}
            }
        }
        if($preexistingPath-and(Get-FileHash -LiteralPath $preexistingPath).Hash-cne$preexistingHash){throw 'Qualified immutable artifact was overwritten.'}
        if($case.name-eq'qualified-observer-immutable'-and(Test-Path -LiteralPath (Join-Path $caseRoot 'artifacts/KingmakerMountedCombat-0.1.0-private-private-diagnostic.zip'))){throw 'Observer collision wrote an unrequested partial main package before preflight.'}
        if($case.name-eq'main-default'-and@($manifests|Where-Object Name -Like 'KmcRemovalObserver-*').Count-ne0){throw 'Default main packaging emitted observer output.'}
        if($case.name-eq'observer-standalone'-and@($manifests|Where-Object Name -Like 'KingmakerMountedCombat-*').Count-ne0){throw 'Observer-only entry emitted main output.'}
        $passed=$true
    }catch{$errorText=$_.Exception.Message}
    $results+=@([pscustomobject]@{name=$case.name;status=$(if($passed){'PASS'}else{'FAIL'});error=$errorText;childExit=$childExit;path=$caseRoot})
    Write-Output ($(if($passed){'PASS '}else{'FAIL '})+$case.name+$(if($errorText){': '+$errorText}else{''}))
}
$failed=@($results|Where-Object status -EQ 'FAIL').Count
$receipt=[ordered]@{schemaVersion=1;status=$(if($failed-eq0){'PASS'}else{'FAIL'});passed=($results.Count-$failed);failed=$failed;elapsedSeconds=$timer.Elapsed.TotalSeconds;cases=$results;inputs=@(@('Package.ps1','Package-Observer.ps1','QualificationPackaging.ps1')|ForEach-Object{$path=Join-Path $PackageScriptDirectory $_;[ordered]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}});scriptSha256=(Get-FileHash -LiteralPath $PSCommandPath).Hash.ToLowerInvariant();nativeInvoked=$false;actualBuildInvoked=$false;realArtifactsWritten=$false;limits='Actual package entry points and full orchestration, real bounded staging/ZIP IO/hashes/manifests. Git source-state, Build-Local and native-assembly package validators are explicit boundary facades over opaque test DLL leaves. Fixtures and artifacts live only under fresh private analysis-cache directories; no true qualification/package identity or native behavior is claimed.'}
[IO.File]::WriteAllText((Join-Path $output 'receipt.json'),($receipt|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if($failed-ne0){throw 'Private qualification packaging guard check failed; preserve every fixture/output.'}
