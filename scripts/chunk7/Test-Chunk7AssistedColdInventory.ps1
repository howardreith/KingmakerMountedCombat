[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/assisted-cold-composed-'+[Guid]::NewGuid().ToString('N'))}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot);$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior composed cold checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Source([string]$Relative){$p=Join-Path $RepositoryRoot $Relative;if(-not[string]::IsNullOrWhiteSpace($SourceOverlayRoot)){$o=Join-Path $SourceOverlayRoot $Relative;if(Test-Path -LiteralPath $o -PathType Leaf){$p=$o}};return $p}
$ps=Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe'
$scenario=Source 'scripts/chunk7/Test-Chunk7AssistedColdScenario.ps1';$reader=Source 'scripts/chunk7/Test-Chunk7AssistedColdInventoryEvidence.ps1'
$inputs=@($PSCommandPath,$scenario,$reader|ForEach-Object{[ordered]@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
$overlayArgs=@();if($PSBoundParameters.ContainsKey('SourceOverlayRoot')){if([string]::IsNullOrWhiteSpace($SourceOverlayRoot)){throw 'SourceOverlayRoot must not be blank.'};$overlayArgs=@('-SourceOverlayRoot',$SourceOverlayRoot)}
$stages=@(
 @{name='actual-scenario';script=$scenario;output=(Join-Path $OutputDirectory 'scenario');arguments=(@('-RepositoryRoot',$RepositoryRoot,'-OutputDirectory',(Join-Path $OutputDirectory 'scenario'))+$overlayArgs)},
 @{name='strict-reader';script=$reader;output=(Join-Path $OutputDirectory 'reader');arguments=(@('-RepositoryRoot',$RepositoryRoot,'-EmittedRoot',(Join-Path $OutputDirectory 'scenario/emitted'),'-OutputRoot',(Join-Path $OutputDirectory 'reader'))+$overlayArgs)}
)
$rows=[Collections.Generic.List[object]]::new();$passed=0;$failed=0;$errors=[Collections.Generic.List[string]]::new();$clock=[Diagnostics.Stopwatch]::StartNew()
foreach($stage in $stages){
 try{
  $childArguments=@('-NoProfile','-NonInteractive','-File',$stage.script)+@($stage.arguments);$global:LASTEXITCODE=0;$text=@(& $ps @childArguments 2>&1);$exitCode=$global:LASTEXITCODE
  [IO.File]::WriteAllText((Join-Path $OutputDirectory ($stage.name+'.log')),(($text|ForEach-Object{$_.ToString()})-join[Environment]::NewLine),[Text.UTF8Encoding]::new($false))
  $receiptPath=Join-Path $stage.output 'receipt.json';if(-not(Test-Path -LiteralPath $receiptPath -PathType Leaf)){throw ('Missing actual child receipt: '+$stage.name)}
  $r=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json
  $ok=$exitCode-eq0-and$r.status-ceq'PASS'-and$r.passed-is[int]-and$r.passed-gt0-and$r.failed-is[int]-and$r.failed-eq0
  $rows.Add([ordered]@{name=$stage.name;exitCode=$exitCode;status=$r.status;passed=$r.passed;failed=$r.failed;receipt=[ordered]@{path=$receiptPath;sha256=(Get-FileHash -LiteralPath $receiptPath -Algorithm SHA256).Hash.ToLowerInvariant()}})
  if(-not$ok){throw ('Actual cold child failed: '+$stage.name)};$passed+=$r.passed
 }catch{$failed++;$errors.Add($_.Exception.Message);break}
}
$changed=@($inputs|Where-Object{(Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash.ToLowerInvariant()-cne$_.sha256});if($changed.Count-ne0){$failed++;$errors.Add('Composed child inputs changed.')}
$ok=$failed-eq0-and$rows.Count-eq2
[ordered]@{kind='assisted-cold-actual-scenario-and-reader';status=$(if($ok){'PASS'}else{'FAIL'});passed=$passed;failed=$failed;stages=$rows.ToArray();errors=$errors.ToArray();durationSeconds=$clock.Elapsed.TotalSeconds;inputs=$inputs;changedInputs=$changed;nativeInvoked=$false;archivesRead=0}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('ASSISTED COLD COMPOSED PASS='+$passed+' FAIL='+$failed);if(-not$ok){exit 1};exit 0
