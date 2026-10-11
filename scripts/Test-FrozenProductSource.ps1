[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
$lab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../obj'))) ('frozen-source-test-'+[Guid]::NewGuid().ToString('N'))
$repo=Join-Path $lab 'repo/Kmc'
$source=Join-Path $lab 'worktrees/product'
$analysis=Join-Path $lab 'analysis-cache'
New-Item -ItemType Directory -Path $repo,$analysis,(Join-Path $lab 'worktrees'),(Join-Path $lab 'artifacts') | Out-Null
function WriteUtf8([string]$path,[string]$text) { [IO.File]::WriteAllText($path,$text,[Text.UTF8Encoding]::new($false)) }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Invoke-TestGit([string[]]$Arguments) {
    $output=@(& git.exe -c user.name=KmcTest -c user.email=kmc-test.invalid -C $repo @Arguments 2>&1)
    if($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    $output
}
$passes=0
function Check([string]$name,[scriptblock]$body) { & $body; $script:passes++; Write-Host ('PASS '+$name) }
function Refuses([string]$name,[scriptblock]$body,[string]$reason) {
    $caught=$null
    try { & $body | Out-Null } catch { $caught=$_.Exception.Message }
    if($null -eq $caught -or $caught -notlike ('*'+$reason+'*')) { throw ($name+': expected refusal '+$reason+', got '+$caught) }
    $script:passes++; Write-Host ('PASS '+$name)
}
$tokens=$null;$parseErrors=$null
$validatorAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Validate-Source.ps1'),[ref]$tokens,[ref]$parseErrors)
$assignments=@($validatorAst.FindAll({param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -cin @('$captureClassBody','$captureMembers','$captureBareFields')},$true))
if($parseErrors.Count -ne 0 -or $assignments.Count -ne 3) { throw 'Actual source capture contract was not selected exactly.' }
$captureScript=[scriptblock]::Create((($assignments | ForEach-Object {$_.Extent.Text}) -join "`n")+"`n"+'[pscustomobject]@{properties=$captureMembers.Count;fields=$captureBareFields.Count}')
$captureText=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/KingmakerMountedCombat/Diagnostics/HorseCompanionUnmountedScenarioEngine.cs'))
$lf=$captureText.Replace("`r`n","`n")
foreach($ending in @("`n","`r`n")) {
    Check ('actual source validator recognizes properties with '+$(if($ending.Length -eq 1){'LF'}else{'CRLF'})) {
        $unmountedEngineText=$lf.Replace("`n",$ending)
        $shape=& $captureScript
        if($shape.properties -lt 45 -or $shape.fields -ne 0) { throw 'Valid capture properties were refused.' }
        $unmountedEngineText=$unmountedEngineText.Replace(' { get; set; }',';')
        $shape=& $captureScript
        if($shape.properties -ne 0 -or $shape.fields -lt 45) { throw 'Bare capture fields were not detected.' }
    }
}
Invoke-TestGit @('init','--quiet') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $repo 'src') | Out-Null
WriteUtf8 (Join-Path $repo 'src/Product.cs') 'class Product {}'
Invoke-TestGit @('add','src/Product.cs') | Out-Null
Invoke-TestGit @('commit','--quiet','-m','Frozen production') | Out-Null
$commit=[string](Invoke-TestGit @('rev-parse','HEAD'))
$tree=[string](Invoke-TestGit @('rev-parse','HEAD^{tree}'))
Invoke-TestGit @('worktree','add','--quiet','--detach',$source,$commit) | Out-Null
WriteUtf8 (Join-Path $repo 'AGENTS.md') 'Current policy'
Invoke-TestGit @('add','AGENTS.md') | Out-Null
Invoke-TestGit @('commit','--quiet','-m','Later harness and policy') | Out-Null
$package=Join-Path $lab 'artifacts/product.zip'
WriteUtf8 $package 'opaque test payload'
$manifestPath=$package+'.manifest.json'
$suitePath=Join-Path $analysis 'suite.json'
$manifest=[ordered]@{commit=$commit;branch='codex/product';packageSha256=(Hash $package);dllSha256=('1'*64);dllMvid='11111111-1111-1111-1111-111111111111'}
WriteUtf8 $manifestPath ($manifest|ConvertTo-Json)
$suite=[ordered]@{repository=[ordered]@{root=$repo;commit=$commit;branch='codex/product'};package=[ordered]@{sha256=(Hash $package)}}
WriteUtf8 $suitePath ($suite|ConvertTo-Json -Depth 5)
$freeze=[ordered]@{source=$commit;tree=$tree;remote=$commit;branch='codex/product';clean=$true;product=$manifest;package=@{path=$package;sha256=(Hash $package)};manifest=@{path=$manifestPath;sha256=(Hash $manifestPath)};suite=@{path=$suitePath;sha256=(Hash $suitePath)}}
$freezePath=Join-Path $analysis 'freeze.json'
WriteUtf8 $freezePath ($freeze|ConvertTo-Json -Depth 8)
$pins=@{RepositoryRoot=$repo;SourceRoot=$source;FreezePath=$freezePath;ExpectedFreezeSha256=(Hash $freezePath)}
Check 'archived production retains its identity under a later clean harness' {
    $actual=Get-KmcFrozenProductSource @pins
    if($actual.source -cne $commit -or $actual.harnessHead -ceq $commit -or $actual.branch -cne 'codex/product') { throw 'Product and harness identities were conflated.' }
}
$context=Get-KmcFrozenProductSource @pins
$packageArguments=@{Context=$context;Manifest=([pscustomobject]$manifest);PackagePath=$package;ManifestPath=$manifestPath;Generator='scripts/Package.ps1';RepositoryRoot=$repo}
Check 'package admission rechecks the archived product without relabeling it' {
    $actual=Assert-KmcFrozenProductManifest @packageArguments
    if($actual.source -cne $commit -or $actual.harnessHead -ceq $commit) { throw 'Package admission lost the identity split.' }
}
$badArguments=$packageArguments.Clone(); $badArguments.ManifestPath=Join-Path $analysis 'unbound-manifest.json'
Refuses 'another manifest path cannot borrow archived admission' {Assert-KmcFrozenProductManifest @badArguments} 'exact archived candidate'
$wrongContext=$context | Select-Object *
$wrongContext.harnessHead='0'*40
$badArguments=$packageArguments.Clone();$badArguments.Context=$wrongContext
Refuses 'changed harness cannot keep an earlier admission' {Assert-KmcFrozenProductManifest @badArguments} 'harness changed'
$original=[IO.File]::ReadAllBytes($package)
WriteUtf8 $package 'changed payload'
Refuses 'changed package is refused' {Get-KmcFrozenProductSource @pins} 'bytes changed'
[IO.File]::WriteAllBytes($package,$original)
WriteUtf8 (Join-Path $source 'foreign.txt') 'unknown state'
Refuses 'dirty archived worktree is refused' {Get-KmcFrozenProductSource @pins} 'both worktrees clean'
Remove-Item -LiteralPath (Join-Path $source 'foreign.txt')
WriteUtf8 (Join-Path $repo 'foreign.txt') 'unknown state'
Refuses 'dirty execution worktree is refused' {Get-KmcFrozenProductSource @pins} 'both worktrees clean'
Remove-Item -LiteralPath (Join-Path $repo 'foreign.txt')
$badPins=$pins.Clone(); $badPins.ExpectedFreezeSha256='0'*64
Refuses 'changed original freeze is refused' {Get-KmcFrozenProductSource @badPins} 'record changed'
$badPins=$pins.Clone(); $badPins.SourceRoot=$repo
Refuses 'execution checkout cannot impersonate archived source' {Get-KmcFrozenProductSource @badPins} 'separate worktree'
$oldFreeze=[IO.File]::ReadAllBytes($freezePath)
$freeze.tree='0'*40
WriteUtf8 $freezePath ($freeze|ConvertTo-Json -Depth 8)
$badPins=$pins.Clone();$badPins.ExpectedFreezeSha256=Hash $freezePath
Refuses 'wrong frozen tree is refused even with a matching record hash' {Get-KmcFrozenProductSource @badPins} 'exact frozen source'
[IO.File]::WriteAllBytes($freezePath,$oldFreeze)
$originalSuite=[IO.File]::ReadAllBytes($suitePath)
$suite.repository.root=Join-Path $lab 'other-repository'
WriteUtf8 $suitePath ($suite|ConvertTo-Json -Depth 5)
Refuses 'changed suite bytes are refused' {Get-KmcFrozenProductSource @pins} 'bytes changed'
[IO.File]::WriteAllBytes($suitePath,$originalSuite)
WriteUtf8 (Join-Path $repo 'src/Product.cs') 'class ChangedProduct {}'
Invoke-TestGit @('add','src/Product.cs') | Out-Null
Invoke-TestGit @('commit','--quiet','-m','Changed native production') | Out-Null
Refuses 'committed production change requires another candidate' {Get-KmcFrozenProductSource @pins} 'new product candidate'
$launcher=Join-Path $PSScriptRoot 'runtime/Invoke-KingmakerRuntimeScenario.ps1'
$preambleLog=Join-Path $analysis 'incomplete-pins.log'
$ErrorActionPreference='Continue'
try { & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launcher -FrozenSourceRoot $source *> $preambleLog; $preambleExit=$LASTEXITCODE }
finally { $ErrorActionPreference='Stop' }
if($preambleExit -eq 0 -or -not(Select-String -LiteralPath $preambleLog -SimpleMatch 'complete source and original-freeze pin set')) { throw 'Actual launcher did not refuse incomplete pins at its entry.' }
$passes++; Write-Host 'PASS actual launcher refuses incomplete archived-source pins before preflight'
Write-Host ('FROZEN PRODUCT SOURCE PASS='+$passes+' FAIL=0; disposable fixture retained at '+$lab)
