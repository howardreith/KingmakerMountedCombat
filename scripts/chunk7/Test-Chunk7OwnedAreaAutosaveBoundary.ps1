[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
# Actual-method boundary check for the owned area autosave observers. The six
# production members are extracted verbatim from the current source and compiled
# against the template's native facades; no game, archive or lab root is touched.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedAreaAutosaveBoundaryTemplate.cs'}
$analysis=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../../analysis-cache'))
if(-not$OutputDirectory){$OutputDirectory=Join-Path $analysis ('chunk7-continuation/owned-area-boundary-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $OutputDirectory)){throw 'Fresh analysis-cache output required.'}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-BracedMember([string]$Text,[string]$Signature){
 $s=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($s-lt0-or$Text.LastIndexOf($Signature,[StringComparison]::Ordinal)-ne$s){throw ('Actual member signature is absent or ambiguous: '+$Signature)}
 $o=$Text.IndexOf('{',$s);if($o-lt0){throw 'Member has no body.'};$d=1;$e=$o+1
 while($d-gt0-and$e-lt$Text.Length){if($Text[$e]-ceq'{'){$d++}elseif($Text[$e]-ceq'}'){$d--};$e++}
 if($d-ne0){throw ('Unbalanced member: '+$Signature)}
 # Expression-bodied members end with the statement terminator after the closing brace.
 if($e-lt$Text.Length-and$Text[$e]-ceq';'){$e++}
 return $Text.Substring($s,$e-$s)
}
$serviceSource=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs';$service=[IO.File]::ReadAllText($serviceSource)
$ownedSource=Join-Path $DiagnosticSourceDirectory 'Chunk7PersistenceScenario.OwnedPairs.cs';$owned=[IO.File]::ReadAllText($ownedSource)
$extracted=[ordered]@{
 SERVICE_OBSERVER=Get-BracedMember $service '        internal void ObserveActiveSaveHeader(out object operation, out SaveInfo requested, out SaveInfo prepared)'
 COMPLETION_DTO=Get-BracedMember $service '    internal sealed class NativeSaveCompletion'
 NATIVE_DESCRIPTOR=Get-BracedMember $owned '        private static JObject OwnedNativeDescriptor(SaveInfo value)'
 HEADER_OBSERVER=Get-BracedMember $owned '        private JObject ObserveOwnedAreaHeader(bool staged)'
 COMPLETION_OBSERVER=Get-BracedMember $owned '        private void ObserveOwnedAreaSaveCompletion()'
 RELEASE_OBSERVER=Get-BracedMember $owned '        private void ReleaseOwnedAreaObservation()'
}
$template=[IO.File]::ReadAllText($TestSource)
foreach($key in $extracted.Keys){$marker='@@'+$key+'@@';if(([regex]::Matches($template,[regex]::Escape($marker))).Count-ne1){throw ('Template placeholder absent or duplicated: '+$marker)};$template=$template.Replace($marker,$extracted[$key])}
if($template.Contains('@@')){throw 'Template retains an unfilled placeholder.'}
$actual=Join-Path $OutputDirectory 'ActualOwnedAreaAutosaveBoundary.cs';[IO.File]::WriteAllText($actual,$template,[Text.UTF8Encoding]::new($false))
$identity=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll'
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$exe=Join-Path $OutputDirectory 'OwnedAreaAutosaveBoundary.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$timer=[Diagnostics.Stopwatch]::StartNew()
$compilerArguments=@('/nologo','/target:exe','/langversion:7.3','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Diagnostics.Chunk7PersistenceScenario',('/out:'+$exe),('/reference:'+$json))
$compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArguments+@($actual,$identity))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$output=@(&$compiler /noconfig ('@'+$rsp) 2>&1);$compileExit=$LASTEXITCODE;$output|Set-Content -LiteralPath (Join-Path $OutputDirectory 'compile.log') -Encoding UTF8
if($compileExit-ne0){$output|ForEach-Object{Write-Output $_}}
$testExit=$null;$test=@()
if($compileExit-eq0){Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll');$test=@(&$exe 2>&1);$testExit=$LASTEXITCODE;$test|Set-Content -LiteralPath (Join-Path $OutputDirectory 'test.log') -Encoding UTF8;$test|ForEach-Object{Write-Output $_}}
$passed=@($test|Where-Object{$_-cmatch'^PASS '}).Count;$failed=@($test|Where-Object{$_-cmatch'^FAIL '}).Count
$ok=$compileExit-eq0-and$testExit-eq0-and$passed-gt0-and$failed-eq0
$receipt=[ordered]@{schemaVersion=1;status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;compileExit=$compileExit;testExit=$testExit;elapsedSeconds=$timer.Elapsed.TotalSeconds;
 extractedMembers=@($extracted.Keys);serviceSourceSha256=(Get-FileHash -LiteralPath $serviceSource).Hash.ToLowerInvariant();ownedSourceSha256=(Get-FileHash -LiteralPath $ownedSource).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TestSource).Hash.ToLowerInvariant();actualSha256=(Get-FileHash -LiteralPath $actual).Hash.ToLowerInvariant();
 inputs=@(@($identity,$json)|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeInvoked=$false;archivesRead=0;
 limits='Six exact production members (service header observer, completion DTO, descriptor projection, header/completion/release observers) run against template facades for Game, SaveInfo, the persistence scope, inherited fixture and authority. No native autosave, worker, load or cleanup delivery is executed.'}
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),($receipt|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ('OWNED AREA BOUNDARY PASS='+$passed+' FAIL='+$failed+'; actual extracted members over template facades, no native claim.')
if(-not$ok){throw 'Owned area autosave actual-boundary regression failed; preserve outputs.'}
