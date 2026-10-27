[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ProbeSource,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ProbeSource){$ProbeSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SaveBoundaryProbe.cs'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SaveBoundaryHeadersTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/save-boundary-headers-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualBoundaryMethod([string]$Text,[string]$Signature){$a=$Text.IndexOf($Signature);if($a-lt0){throw ('Missing exact method '+$Signature)};$o=$Text.IndexOf('{',$a);$semi=$Text.IndexOf(';',$a);$arrow=$Text.IndexOf('=>',$a);if($arrow-ge0-and$arrow-lt$o-and$semi-lt$o){return $Text.Substring($a,$semi+1-$a)};$d=1;$e=$o+1;while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-eq'{'){$d++};if($Text[$e]-eq'}'){$d--};$e++};if($d-ne0){throw 'Unbalanced actual method'};if($arrow-ge0-and$arrow-lt$o){$e=$Text.IndexOf(';',$e)+1;if($e-le0){throw 'Missing expression terminator'}};$Text.Substring($a,$e-$a)}
$source=[IO.File]::ReadAllText($ProbeSource);$methods=''
foreach($sig in @('internal Chunk7SaveBoundaryProbe(','private void Observe(','private void Starting()','private void Staged()','private void Native(','internal bool Complete =>','internal bool HeadersComplete(','internal JObject Capture() =>','private static void Require(')){$methods+=[Environment]::NewLine+(Get-ActualBoundaryMethod $source $sig)}
$generated=Join-Path $OutputDirectory 'ActualSaveBoundaryHeaders.cs';[IO.File]::WriteAllText($generated,([IO.File]::ReadAllText($TestSource).Replace('__METHODS__',$methods)),[Text.UTF8Encoding]::new($false))
[xml]$paths=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'LocalGamePaths.props'));$install=$paths.SelectSingleNode('//*[local-name()="KingmakerInstallDir"]').InnerText;$managed=$paths.SelectSingleNode('//*[local-name()="KingmakerManagedDir"]').InnerText.Replace('$(KingmakerInstallDir)',$install);$json=Join-Path $managed 'Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'HeaderTests.exe'
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 /reference:Microsoft.CSharp.dll ("/reference:"+$json) ("/out:"+$exe) $generated;if($LASTEXITCODE-ne0){throw 'Actual header method compile failed.'}
Copy-Item -LiteralPath $json -Destination $OutputDirectory
&$exe;if($LASTEXITCODE-ne0){throw 'Actual header method check failed.'}
[ordered]@{sourceSha256=(Get-FileHash -LiteralPath $ProbeSource).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TestSource).Hash.ToLowerInvariant();generatedSha256=(Get-FileHash -LiteralPath $generated).Hash.ToLowerInvariant();nativeInstallationSubstituted=$true;nativeEvidence=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
