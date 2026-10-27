[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Observation','AncestorAdmission','DirectoryIdentity','Namespace','PlatformFiles','Proof','Closure','DynamicInputs','Environment','BootstrapOwnership','CompilerSource','CompilerOutput','AstExecution','OriginalGuardEffects','ProofSelection')][string]$Case,
 [Parameter(Mandatory=$true)][string]$OutputRoot,
 [string]$SourceRoot,
 [string]$GuardSourceRoot,
 [string]$CorrectedProofPath,
 [string]$CorrectedProofSha256,
 [string]$LabRoot='C:\Dev\KingmakerMountedCombatLab'
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('SourceRoot')){$SourceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if([string]::IsNullOrWhiteSpace($SourceRoot)){throw 'An explicit SourceRoot must be nonempty.'}
$SourceRoot=[IO.Path]::GetFullPath($SourceRoot).TrimEnd('\');$LabRoot=[IO.Path]::GetFullPath($LabRoot).TrimEnd('\')
if([string]::IsNullOrEmpty($GuardSourceRoot)){$GuardSourceRoot=$SourceRoot};$GuardSourceRoot=[IO.Path]::GetFullPath($GuardSourceRoot).TrimEnd('\')
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot).TrimEnd('\');$analysis=[IO.Path]::GetFullPath((Join-Path $LabRoot 'analysis-cache')).TrimEnd('\')
foreach($root in @($SourceRoot,$GuardSourceRoot)){if(-not$root.StartsWith($LabRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Test inputs must be within the project lab.'}}
if(-not$OutputRoot.StartsWith($analysis+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Focused test output must be strictly under project analysis-cache.'}
foreach($root in @($OutputRoot,$SourceRoot,$GuardSourceRoot)){$p=$root;while(-not[string]::IsNullOrEmpty($p)){if(Test-Path -LiteralPath $p){if(((Get-Item -LiteralPath $p -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Focused test paths may not contain reparse points.'}};$p=[IO.Path]::GetDirectoryName($p)}}
if(Test-Path -LiteralPath $OutputRoot){throw 'Preserve existing focused test output.'}
if($Case-ceq'ProofSelection'-and([string]::IsNullOrEmpty($CorrectedProofPath)-or$CorrectedProofSha256-cnotmatch'^[0-9a-f]{64}$')){throw 'ProofSelection requires an explicit corrected-proof path and SHA-256.'}
$names=@('Observation','AncestorAdmission','DirectoryIdentity','Namespace','PlatformFiles','Proof','Closure','DynamicInputs','Environment','BootstrapOwnership','CompilerSource','CompilerOutput','AstExecution','OriginalGuardEffects','ProofSelection')
$helpers=@('Invoke-Chunk7ScopedPreflight.ps1','ScopedPreflightProof.ps1','ScopedPreflightClosure.ps1','ScopedPreflightGuards.ps1','ScopedPreflightPlatform.ps1','ScopedPreflightObservation.cs','ScopedPreflightFullAuditReevaluation.ps1')
$needed=@($helpers|ForEach-Object{Join-Path $SourceRoot ('scripts/runtime/'+$_)})+@($names|ForEach-Object{Join-Path $SourceRoot ('tests/ScopedPreflight/Test-ScopedPreflight'+$_+'.ps1')})
foreach($p in $needed){if(-not(Test-Path -LiteralPath $p -PathType Leaf)){throw ('Focused test dependency is missing: '+$p)}}
[void][IO.Directory]::CreateDirectory($OutputRoot);$fixture=Join-Path $OutputRoot 'fixture';$tests=Join-Path $fixture 'tests';$runtime=Join-Path $fixture 'overlays/scripts/runtime';$src=Join-Path $fixture 'src'
foreach($p in @($tests,$runtime,$src)){[void][IO.Directory]::CreateDirectory($p)}
$pins=[Collections.Generic.List[object]]::new()
function Copy-Pinned([string]$From,[string]$To){[IO.File]::Copy($From,$To,$false);$before=(Get-FileHash -LiteralPath $From).Hash.ToLowerInvariant();$after=(Get-FileHash -LiteralPath $To).Hash.ToLowerInvariant();if($before-cne$after){throw 'Private focused test copy changed bytes.'};$pins.Add([pscustomobject]@{source=$From;copy=$To;sha256=$before})}
foreach($name in $helpers){Copy-Pinned (Join-Path $SourceRoot ('scripts/runtime/'+$name)) (Join-Path $runtime $name)}
Copy-Pinned (Join-Path $SourceRoot 'scripts/runtime/ScopedPreflightObservation.cs') (Join-Path $src 'ScopedPreflightObservation.cs')
foreach($name in $names){$leaf='Test-ScopedPreflight'+$name+'.ps1';Copy-Pinned (Join-Path $SourceRoot ('tests/ScopedPreflight/'+$leaf)) (Join-Path $tests $leaf)}
$caseOutput=Join-Path $OutputRoot 'case';$script=Join-Path $tests ('Test-ScopedPreflight'+$Case+'.ps1');$args=@('-OutputRoot',$caseOutput)
switch($Case){
 'ProofSelection'{$args+=@('-CorrectedProofPath',$CorrectedProofPath,'-CorrectedProofSha256',$CorrectedProofSha256,'-LabRoot',$LabRoot)}
 {$_-cin@('Observation','AncestorAdmission','DirectoryIdentity')}{$args+=@('-SourcePath',(Join-Path $src 'ScopedPreflightObservation.cs'))}
 'BootstrapOwnership'{$args+=@('-HomeRoot',$fixture)}
 {$_-cin@('CompilerSource','CompilerOutput')}{$args+=@('-HomeRoot',$fixture,'-SourcePath',(Join-Path $runtime 'ScopedPreflightPlatform.ps1'))}
 'OriginalGuardEffects'{$args+=@('-SourceRoot',$GuardSourceRoot)}
 'AstExecution'{
  $guardFixture=Join-Path $fixture 'repo';$guardRuntime=Join-Path $guardFixture 'scripts/runtime';[void][IO.Directory]::CreateDirectory($guardRuntime)
  foreach($file in Get-ChildItem -LiteralPath (Join-Path $GuardSourceRoot 'scripts/runtime') -File|Where-Object{$_.Extension-cin@('.ps1','.cs')}){Copy-Pinned $file.FullName (Join-Path $guardRuntime $file.Name)}
  $args+=@('-HomeRoot',$fixture,'-FixtureRoot',$guardFixture)
 }
}
$exe=Join-Path $PSHOME 'powershell.exe';$childArgs=@('-NoProfile','-NonInteractive','-File',$script)+$args
$inputs=[ordered]@{schemaVersion=1;case=$Case;sourceRoot=$SourceRoot;guardSourceRoot=$GuardSourceRoot;files=@($pins);executable=$exe;executableSha256=(Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant();arguments=$childArgs;claim='One selected private regression, never inherited audit/source/native qualification.'}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'inputs.json'),($inputs|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$start=[DateTime]::UtcNow;$exit=$null
try{&$exe @childArgs *> (Join-Path $OutputRoot 'test.log');$exit=$LASTEXITCODE}catch{[IO.File]::WriteAllText((Join-Path $OutputRoot 'invocation-failure.txt'),$_.Exception.ToString(),[Text.UTF8Encoding]::new($false));throw}
$receiptPath=Join-Path $caseOutput 'receipt.json';$receipt=if(Test-Path -LiteralPath $receiptPath){[IO.File]::ReadAllText($receiptPath)|ConvertFrom-Json}else{$null}
$status=if($exit-eq0-and$null-ne$receipt-and$receipt.status-ceq'PASS'){'PASS'}else{'FAIL'}
$result=[ordered]@{schemaVersion=1;kind='selected-scoped-preflight-regression';case=$Case;status=$status;exitCode=$exit;seconds=([DateTime]::UtcNow-$start).TotalSeconds;inputsSha256=(Get-FileHash -LiteralPath (Join-Path $OutputRoot 'inputs.json')).Hash.ToLowerInvariant();receiptPath=$receiptPath;receiptSha256=$(if($null-ne$receipt){(Get-FileHash -LiteralPath $receiptPath).Hash.ToLowerInvariant()}else{$null});nativeExecution=$false;actualArchiveOperations=0;qualification=$false}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'result.json'),($result|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Write-Output ('Scoped preflight selected regression '+$Case+': '+$status);if($status-cne'PASS'){throw 'Selected scoped preflight regression failed; raw evidence retained.'}
