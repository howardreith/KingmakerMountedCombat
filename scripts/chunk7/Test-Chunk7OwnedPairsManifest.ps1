param([string]$RepositoryRoot,[string]$RuntimeDirectory,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))};if(-not$RuntimeDirectory){$RuntimeDirectory=Join-Path $RepositoryRoot 'scripts/runtime'};if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/owned-manifest-'+[Guid]::NewGuid().ToString('N'))};if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior manifest check.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
. (Join-Path $RuntimeDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $RuntimeDirectory 'RuntimeArtifactManifestEvidence.ps1')
$fixtures=(Get-Content -LiteralPath $FixturePath -Raw|ConvertFrom-Json).fixtures;$rows=[Collections.Generic.List[object]]::new()
function CopyValue($V){$V|ConvertTo-Json -Depth 90|ConvertFrom-Json}
function Check([string]$Name,[scriptblock]$Action,[bool]$Refuse=$false){$errorText=$null;$ok=$false;try{&$Action;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse};$rows.Add([pscustomobject]@{name=$Name;pass=$ok;refusalExpected=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Emit($Q,$A,[string]$Tag){$Q.evidenceRoot=Join-Path $OutputDirectory $Tag;[void][IO.Directory]::CreateDirectory($Q.evidenceRoot);$leaf=$A.evidenceKind+'.json';$p=Join-Path $Q.evidenceRoot $leaf;[IO.File]::WriteAllText($p,(ConvertTo-KmcChunk7Canonical $A),[Text.UTF8Encoding]::new($false));$m=[pscustomobject]@{schemaVersion=1;runId=$Q.runId;scenario=$Q.scenario;createdAtUtc='2026-10-13T00:00:00Z';artifacts=@([pscustomobject]@{relativePath=$leaf;kind=$A.evidenceKind;length=[long](Get-Item -LiteralPath $p).Length;sha256=Get-KmcSha256 $p})};[IO.File]::WriteAllText((Join-Path $Q.evidenceRoot 'runtime-artifacts.json'),(ConvertTo-KmcChunk7Canonical $m),[Text.UTF8Encoding]::new($false));return $m}
function Validate($Q,$M,[string]$Kind){Assert-KmcReadOnlyArtifactManifest $Q (Get-KmcSha256 (Join-Path $Q.evidenceRoot 'runtime-artifacts.json')) -GameResult;Assert-KmcKnownRuntimeArtifactsManifested $Q.evidenceRoot $M;$a=Read-KmcChunk7ManifestArtifact $Q $M ($Kind+'.json') $Kind;Assert-KmcChunk7ArtifactEnvelope $Q $a $Kind}
foreach($f in $fixtures){$q=CopyValue $f.request;$a=CopyValue $f.artifact;$m=Emit $q $a $f.name
 Check ('manifest envelope '+$f.name){Validate $q $m $a.evidenceKind}
 Check ('candidate substitution '+$f.name){$changed=CopyValue $q;$changed.dllSha256='0'*64;Validate $changed $m $a.evidenceKind} $true
 Check ('kind substitution '+$f.name){$changed=CopyValue $m;$changed.artifacts[0].kind='chunk7-foreign-evidence';[void](Read-KmcChunk7ManifestArtifact $q $changed ($a.evidenceKind+'.json') $a.evidenceKind)} $true
 Check ('duplicate role '+$f.name){$changed=CopyValue $m;$changed.artifacts+=CopyValue $m.artifacts[0];[void](Read-KmcChunk7ManifestArtifact $q $changed ($a.evidenceKind+'.json') $a.evidenceKind)} $true
}
foreach($spec in @(@('exploration','world'),@('combat-rider-first','combat'),@('persistence-save-multi-exploration-manual','persistence'))){$f=@($fixtures|Where-Object name -CEQ $spec[0])[0];$q=CopyValue $f.request;$a=CopyValue $f.artifact;$a.evidenceKind='chunk7-'+$spec[1]+'-cleanup-pending';$a.status='FAIL';$a.errors=@('exact retained native custody');$a.observations=[pscustomobject]@{cleanupPendingAtHostBoundary=$true}
 if($spec[1]-ceq'combat'){$a.observations=[pscustomobject]@{cleanupPending=[pscustomobject]@{reason='exact retained native custody';serializationBusy=$false;loading=$false;queuedLoads=@();pendingNativeWrites=$false;preparationPending=$true;relationshipTransitionPending=$false;settled=$false}}}
 $m=Emit $q $a ('pending-'+$spec[1]);Check ('pending exact '+$spec[1]){Validate $q $m $a.evidenceKind;Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'}
 Check ('pending cannot PASS '+$spec[1]){Assert-KmcChunk7CleanupArtifacts $q $m 'PASS'} $true
 Check ('pending foreign family '+$spec[1]){$foreign=CopyValue $q;$foreign.ownedPairs.case=if($spec[1]-ceq'world'){'combat-rider-first'}else{'exploration'};Assert-KmcChunk7CleanupArtifacts $foreign $m 'FAIL'} $true
}
$passed=@($rows|Where-Object pass).Count;$failed=$rows.Count-$passed
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{passed=$passed;failed=$failed;nativeInvoked=$false;archiveRead=$false;scope='unchanged manifest byte guards plus selected envelope/pending roles on generated JSON';rows=@($rows)}|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Write-Output ('Owned manifest PASS='+$passed+' FAIL='+$failed);if($failed){exit 1}
