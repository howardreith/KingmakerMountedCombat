[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$AuditSourcePath,[string]$LeaseSourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('SourcePath')){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs'}
if(-not$PSBoundParameters.ContainsKey('AuditSourcePath')){$AuditSourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/HorseCompanionBlueprintRegistrationAuditService.cs'}
if(-not$PSBoundParameters.ContainsKey('LeaseSourcePath')){$LeaseSourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Domain/ExactAppendOnlyArrayLease.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/RangerSelectionContractTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-continuation/ranger-contract-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-ActualMethod([string]$Text,[string]$Pattern){
 $match=[regex]::Match($Text,$Pattern);if(-not$match.Success){throw ('Actual Ranger method missing: '+$Pattern)}
 $start=$match.Index;$open=$Text.IndexOf('{',$start);$end=$open+1;$depth=1
 while($depth-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++}
 if($depth){throw 'Unbalanced actual Ranger method.'};return $Text.Substring($start,$end-$start)
}
$source=[IO.File]::ReadAllText($SourcePath);$audit=[IO.File]::ReadAllText($AuditSourcePath)
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*GUARD*/',(Get-ActualMethod $source '(?:private|internal) static BlueprintFeature\[\] RequireExactRangerOptions'))
$body=$body.Replace('/*NATIVE*/',(Get-ActualMethod $source 'internal static void RequireNativeRangerOptions'))
$body=$body.Replace('/*APPEND*/',(Get-ActualMethod $audit 'private static bool HasExactHorseAppend'))
$body=$body.Replace('/*RESTORE*/',(Get-ActualMethod $audit 'private static bool HasExactStockRestore'))
$body=$body.Replace('/*FEATURES*/',(Get-ActualMethod $audit 'private static bool FeaturesStayedUntouched'))
$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$executable=Join-Path $OutputDirectory 'actual.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$executable" $generated $LeaseSourcePath
if($LASTEXITCODE-ne0){throw 'Actual Ranger selection guard compile failed.'}
&$executable
$exitCode=$LASTEXITCODE
@{source=$SourcePath;sha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();auditSha256=(Get-FileHash -LiteralPath $AuditSourcePath).Hash.ToLowerInvariant();leaseSha256=(Get-FileHash -LiteralPath $LeaseSourcePath).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();exitCode=$exitCode;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($exitCode-ne0){throw 'Actual Ranger selection guard regression failed.'}
