[CmdletBinding()]
param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$RuntimeDirectory){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/AssistedAcquisitionConnectionFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/assisted-wiring-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Fresh output required.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RuntimeDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $RuntimeDirectory 'Chunk7CumulativeAcquisitionFixtures.ps1')
# Private reader shadows are not independent repositories; only root discovery is an external test boundary.
$connectionRoot=[IO.Path]::GetFullPath($RepositoryRoot)
function Get-KmcRepositoryRoot {return $connectionRoot}
function Get-KmcLabRoot {return [IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($connectionRoot))}
$script:pass=0;$script:fail=0
function Good([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;Write-Host ('PASS '+$Name)}catch{$script:fail++;Write-Host ('FAIL '+$Name+': '+$_+' '+$_.ScriptStackTrace)}}
function Bad([string]$Name,[scriptblock]$Body){$caught=$false;try{&$Body}catch{$caught=$true};if($caught){$script:pass++;Write-Host ('PASS refuses '+$Name)}else{$script:fail++;Write-Host ('FAIL accepted '+$Name)}}
function New-Request {$f=[IO.File]::ReadAllText($FixturePath)|ConvertFrom-Json;$r=$f.request;foreach($p in @($r.PSObject.Properties)){if($null-eq$p.Value){$r.PSObject.Properties.Remove($p.Name)}};$r.PSObject.Properties.Remove('saveAccessAllowed');$r.productVersion=(Read-KmcJson (Join-Path $RepositoryRoot 'version.json')).productVersion;$r.evidenceRoot=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$r.runId);$r}
$validatorPath=Join-Path $RuntimeDirectory 'Test-RuntimeRequest.ps1';$body=[IO.File]::ReadAllText($validatorPath);$import='. (Join-Path $PSScriptRoot '+"'RuntimeHarness.Common.ps1')";if($body.Split(@($import),[StringSplitOptions]::None).Count-ne2){throw 'Actual validator import differs.'};$validator=[scriptblock]::Create($body.Replace($import,'# Common already loaded; actual validator follows.'))
function Validate($R){$p=Join-Path $OutputDirectory 'request.json';[IO.File]::WriteAllText($p,(ConvertTo-KmcChunk7Canonical $R),[Text.UTF8Encoding]::new($false));&$validator -RequestPath $p|Out-Null}
Good 'actual full request accepts exact assisted cumulative route' {Validate (New-Request)}
$changes=[ordered]@{
 'missing cold hash'={param($r)$r.cumulativeAcquisition.PSObject.Properties.Remove('assistedColdArtifactSha256')}
 'foreign mode'={param($r)$r.cumulativeAcquisition.sourceMode='generic'}
 'old Manual leaf'={param($r)$r.persistenceLoad.fileName='Manual_2_KMC_C7_ACQUIRED_FIELD.zks'}
 'old Manual name'={param($r)$r.persistenceLoad.internalName='KMC_C7_ACQUIRED_FIELD'}
 'cold same run'={param($r)$r.cumulativeAcquisition.assistedColdRunId=$r.runId}
 'cold same producer'={param($r)$r.cumulativeAcquisition.assistedColdRunId=$r.cumulativeAcquisition.sourceRunId}
 'cold singleton'={param($r)$r.cumulativeAcquisition.assistedColdRunId=@($r.cumulativeAcquisition.assistedColdRunId)}
 'two writes'={param($r)$r.cumulativeAcquisition.expectedNativeWriteCount=2}
 'no native load'={param($r)$r.cumulativeAcquisition.expectedNativeLoadCount=0}
 'prior not main'={param($r)$r.cumulativeAcquisition.priorOwnerId=$r.cumulativeAcquisition.existingActorId}
 'unclosed selection'={param($r)$r.cumulativeAcquisition|Add-Member arbitrary $true}
 'borrowed family'={param($r)$r.scenario='chunk7-manual-derived-acquisition'}
}
foreach($c in $changes.GetEnumerator()){Bad $c.Key {$r=New-Request;&$c.Value $r;Validate $r}}
$script:assistedCalls=0;$script:legacyCalls=0
function Get-KmcAssistedCumulativeSource($Selection,$Fixture,$Candidate){$script:assistedCalls++;[pscustomobject]@{kind='assisted';selection=$Selection}}
function Assert-KmcDirectoryTreeCloneable($Path,$Name){$script:legacyCalls++;throw 'Legacy external source boundary reached.'}
Good 'actual qualifier routes only exact new mode to assisted complete-source gate' {$r=New-Request;$v=Get-KmcChunk7CumulativeSource $r.cumulativeAcquisition $r.fixture $r;if($v.kind-cne'assisted'-or$script:assistedCalls-ne1-or$script:legacyCalls-ne0){throw 'Source dispatch changed.'}}
Good 'actual old mode still reaches original qualified directory boundary' {$f=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/CumulativeExistingFixture.json'))|ConvertFrom-Json;$caught=$false;try{Get-KmcChunk7CumulativeSource $f.request.cumulativeAcquisition $f.request.fixture $f.request|Out-Null}catch{if($_.Exception.Message-cne'Legacy external source boundary reached.'){throw};$caught=$true};if(-not$caught-or$script:legacyCalls-ne1){throw 'Legacy source path missing.'}}
$runnerPath=Join-Path $RuntimeDirectory 'Invoke-KingmakerRuntimeScenario.ps1';$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($runnerPath,[ref]$tokens,[ref]$errors);if($errors){throw 'Runner parse failed.'}
$nodes=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Clauses[0].Item1.Extent.Text-ceq'Test-KmcAssistedCumulativeMode $request.cumulativeAcquisition'-and$n.Extent.Text.Contains('Copy-KmcAssistedCumulativeSourceArtifacts')},$true));if($nodes.Count-ne1){throw 'Exact locked-copy branch missing.'}
$copy=[scriptblock]::Create($nodes[0].Extent.Text);$script:copyRows=@()
function Copy-KmcAssistedCumulativeSourceArtifacts($Source,$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){$script:copyRows+=,'assisted';if(-not[object]::ReferenceEquals($Lock,$script:expectedLock)){throw 'Lock changed.'}}
function Copy-KmcChunk7CumulativeSourceArtifact($Source,$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){$script:copyRows+=,'legacy';if(-not[object]::ReferenceEquals($Lock,$script:expectedLock)){throw 'Lock changed.'}}
$lock=[pscustomobject]@{owned=$true};$script:expectedLock=$lock;$cumulativeAcquisitionSource=[pscustomobject]@{};$profileRoot='generated';$fixturePayload=[pscustomobject]@{};$manifest=[pscustomobject]@{};$request=New-Request
Good 'actual runner dual-copy branch retains lock and exact selection' {&$copy;if($script:copyRows.Count-ne1-or$script:copyRows[0]-cne'assisted'){throw 'Wrong new copy route.'}}
Good 'actual runner old single-copy branch unchanged' {$request.cumulativeAcquisition.PSObject.Properties.Remove('sourceMode');&$copy;if($script:copyRows.Count-ne2-or$script:copyRows[1]-cne'legacy'){throw 'Wrong old copy route.'}}
[ordered]@{pass=$script:pass;fail=$script:fail;nativeInvoked=$false;archiveReads=0;boundary='Actual full request validator and qualifier/runner dispatch. Completed source qualifiers and locked-copy implementations are separately tested external boundaries here.'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail);if($script:fail){throw 'Assisted wiring checks failed.'}
