[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$probe=Join-Path $repo 'obj/persistence-data-tests'
[void][IO.Directory]::CreateDirectory($probe)
$vs=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler=@(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe')|Select-Object -First 1
$dll=Join-Path $probe 'PersistenceDataTests.dll'
& $compiler /nologo /target:library /langversion:7.3 "/out:$dll" "/reference:$managed/Newtonsoft.Json.dll" (Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedSaveData.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Integration/SavedCombatData.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Domain/PairedActivationSnapshot.cs') (Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedLoadAdmissionPolicy.cs') (Join-Path $repo 'tests/PersistenceDataTests.cs')
if($LASTEXITCODE-ne0){throw 'Persistence data probe compilation failed'}
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))
[void][Reflection.Assembly]::LoadFrom($dll)
[PersistenceDataTests]::Run()
