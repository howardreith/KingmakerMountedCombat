$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
$source=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7WorldScenarioEngine.cs'
$text=[IO.File]::ReadAllText($source)
$start=$text.IndexOf('        private static bool ResourcesConserved(')
$end=$text.IndexOf('        private JObject CaptureWorld(',$start)
if($start-lt0-or$end-le$start){throw 'Actual world resource method extraction failed.'}
$method=$text.Substring($start,$end-$start)
$probe=Join-Path $chunk7AnalysisRoot ('world-resources-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($probe)
$template=[IO.File]::ReadAllText((Join-Path $chunk7TestSource 'WorldResourcesAdapterTemplate.cs'))
$adapter=Join-Path $probe 'WorldResourcesAdapter.cs'
[IO.File]::WriteAllText($adapter,$template.Replace('__METHOD__',$method))
$policy=Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RelationshipCommandIdentity.cs'
[ordered]@{source=$source;sourceSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant();policySha256=(Get-FileHash -LiteralPath $policy -Algorithm SHA256).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $probe 'inputs.json')
$layout=(Get-Content -LiteralPath (Join-Path (Get-KmcLabRoot) 'environment-intake.json') -Raw|ConvertFrom-Json).requestedLayout
$json=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$vs=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$dll=Join-Path $probe 'WorldResourcesAdapter.dll'
& $compiler /nologo /target:library /langversion:7.3 "/out:$dll" "/reference:$json" $adapter $policy
if($LASTEXITCODE-ne0){throw 'World resources observer adapter compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($json)
[void][Reflection.Assembly]::LoadFrom($dll)
[WorldResourcesAdapter]::Verify()
