[CmdletBinding()]
param([string]$OutputDirectory)
# Actual-method check of Chunk7PersistenceScenario.RequireRetainedFailureScope (b195 x4c): the
# unchanged product method is extracted and run against minimal stand-ins. Offline evidence only.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('two-pair-scope-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'two-pair retained scope checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior two-pair retained scope evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.SixPair.cs'))
$start=$source.IndexOf('internal void RequireRetainedFailureScope()',[StringComparison]::Ordinal)
$end=$source.IndexOf('internal void RequireRetainedSixFailureScope()',[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start-or$source.IndexOf('internal void RequireRetainedFailureScope()',$start+1,[StringComparison]::Ordinal)-ge0){throw 'Actual two-pair retained scope method is absent or ambiguous.'}
$method=$source.Substring($start,$end-$start).TrimEnd()
if($method-notmatch'nativeScope\.RequireDeclaredAreaDestination\(replayDestination\.AssetGuidThreadSafe, replayDestination\)'){throw 'Two-pair retained scope no longer admits only the exact declared destination.'}
$template=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'TwoPairRetainedScopeTemplate.cs'))
if(-not$template.Contains('/* SCOPE */')){throw 'Template lacks its splice marker.'}
$adapter=Join-Path $OutputDirectory 'TwoPairRetainedScope.cs';[IO.File]::WriteAllText($adapter,$template.Replace('/* SCOPE */',$method))
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$exe=Join-Path $OutputDirectory 'TwoPairRetainedScope.exe'
& $compiler /nologo /target:exe /langversion:7.3 "/out:$exe" $adapter
if($LASTEXITCODE-ne0){throw 'Actual two-pair retained scope compilation failed.'}
$lines=@(& $exe);$code=$LASTEXITCODE;$lines|Write-Output
if($code-ne0-or@($lines|Where-Object{$_-cmatch'^FAIL '}).Count-ne0-or@($lines|Where-Object{$_-cmatch'^PASS '}).Count-ne16){throw 'Actual two-pair retained scope regressed.'}
