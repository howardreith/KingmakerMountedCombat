Set-StrictMode -Version Latest
# F12 upgrade/rollback rehearsal (7E X5, owner-selected Option A). Three runs of the
# request-free normal-player control, each a temporary guarded install in the isolated
# base-game tree with exactly KMC and the observer:
#   u1-previous-install  the released preview.211 package loads the reference-free owner
#                        seed (load only) and writes its ordinary KMC folder state;
#   u2-upgrade           the candidate's package files replace preview.211's over that
#                        exact folder state, as a player upgrades, and drive the controls;
#   u3-rollback          preview.211 over the same folder state loads the pre-upgrade seed.
# Mounted save-format upgrade is carried by the schema-1/2 migration rows, not here.
function Get-KmcChunk7UpgradeRehearsalSteps {@('u1-previous-install','u2-upgrade','u3-rollback')}
function Test-KmcChunk7UpgradePreviousStep([AllowNull()][string]$Step){$Step-cin@('u1-previous-install','u3-rollback')}

# The only previous package this mode admits: the released preview.211 "-b" package.
function Get-KmcChunk7PreviousPackagePins {
 [pscustomobject][ordered]@{leaf='KingmakerMountedCombat-0.1.0-chunk6d-preview.211-chunk6-closeout211-b-diagnostic.zip'
  packageSha256='e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039';manifestSha256='ca54c37cc5de332bad4514d04b585ccf5b31977a57f44c6f0e19288a769d8ec3'
  dllSha256='11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea';dllMvid='0aa68ba0-f293-480e-8ecb-d7bdaeca73d0'
  infoSha256='a52a335b21f356bcc02de29a158f2bc839ac0e09bd17e5d3461382040366c0bd';version='0.1.0-chunk6d-preview.211';commit='a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6'}
}
# Known defects of the released preview.211 that it genuinely logs in this isolated
# base-game-only tree. Its Horse registration admits only the seven-option Ranger
# AllFeatures list that Call of the Wild writes; the base game has twelve, so the old
# binary logs this exception at start (fixed in Chunk 7 by 81c191af). The u1 session-log
# expectation of the two steps that load it (u1, u3) describes exactly what the old binary
# does here; nothing else is admitted.
function Get-KmcChunk7PreviousPackageKnownDefects {
 @([pscustomobject][ordered]@{id='preview211-base-game-ranger-allfeatures-horse-registration'
  line='[KingmakerMountedCombat] [Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger companion AllFeatures surface no longer has seven stock options.'
  expectedOccurrences=1;sourceFixCommit='81c191afec2fc3210b8777323447c0b4edb4ddb1';reference='docs/CHUNK7-RANGER-BOOTSTRAP-REPAIR.md'})
}
# Only the steps that stage exactly the pinned preview.211 package (u1 first install, u3
# rollback) admit them. u2 runs the candidate and keeps the strict quiet-start rule; any other
# step or package admits none.
function Get-KmcChunk7UpgradeAdmittedLogDefects($UpgradeRehearsal){
 if($null-eq$UpgradeRehearsal){return @()}
 $pins=Get-KmcChunk7PreviousPackagePins;$s=$UpgradeRehearsal.stagedKmc
 if([string]$UpgradeRehearsal.step-cnotin@('u1-previous-install','u3-rollback')-or$null-eq$s-or[string]$s.role-cne'previous'-or[string]$s.version-cne$pins.version-or[string]$s.commit-cne$pins.commit-or
  [string]$s.packageSha256-cne$pins.packageSha256-or[string]$s.manifestSha256-cne$pins.manifestSha256-or[string]$s.dllSha256-cne$pins.dllSha256-or[string]$s.dllMvid-cne$pins.dllMvid){return @()}
 Get-KmcChunk7PreviousPackageKnownDefects
}
function Get-KmcChunk7ZipMemberHashes([string]$Path){
 Add-Type -AssemblyName System.IO.Compression
 $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);$zip=$null
 try{
  $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  if($zip.Entries.Count-gt16){throw 'Package member count is not bounded.'}
  $hashes=[ordered]@{}
  foreach($entry in $zip.Entries){
   # Packages written by Compress-Archive on Windows PowerShell name members with '\'.
   $name=$entry.FullName.Replace('\','/')
   if($entry.Length-gt64MB-or$hashes.Contains($name)){throw 'Package members are oversized or ambiguous.'}
   $inner=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
   try{$hashes[$name]=([BitConverter]::ToString($sha.ComputeHash($inner))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose();$inner.Dispose()}
  }
  $hashes
 }finally{if($zip){$zip.Dispose()};$stream.Dispose()}
}
function Get-KmcChunk7PreviousPackage {
 $pins=Get-KmcChunk7PreviousPackagePins;$artifacts=[IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'artifacts'))
 $package=Join-Path $artifacts $pins.leaf;$manifestPath=$package+'.manifest.json'
 foreach($pair in @(@($package,'previous package'),@($manifestPath,'previous package manifest'))){
  if(-not(Test-Path -LiteralPath $pair[0] -PathType Leaf)){throw ($pair[1]+' is missing.')}
  Assert-KmcNotReparsePoint $pair[0] $pair[1];Assert-KmcNotHardLink $pair[0] $pair[1]
 }
 if((Get-KmcSha256 $package)-cne$pins.packageSha256-or(Get-KmcSha256 $manifestPath)-cne$pins.manifestSha256){throw 'The previous package or its manifest is not the released preview.211 identity.'}
 $m=Read-KmcJson $manifestPath
 if([string]$m.version-cne$pins.version-or[string]$m.commit-cne$pins.commit-or[string]$m.packageSha256-cne$pins.packageSha256-or
  [string]$m.dllSha256-cne$pins.dllSha256-or[string]$m.dllMvid-cne$pins.dllMvid){throw 'The previous package manifest does not bind the released preview.211 identity.'}
 $members=Get-KmcChunk7ZipMemberHashes $package
 if((@($members.Keys)|Sort-Object)-join'|'-cne'KingmakerMountedCombat/Info.json|KingmakerMountedCombat/KingmakerMountedCombat.dll'-or
  $members['KingmakerMountedCombat/KingmakerMountedCombat.dll']-cne$pins.dllSha256-or$members['KingmakerMountedCombat/Info.json']-cne$pins.infoSha256){
  throw 'The previous package members are not exactly the released Info.json and DLL.'
 }
 if((Get-KmcSha256 $package)-cne$pins.packageSha256){throw 'The previous package changed during qualification.'}
 [pscustomobject][ordered]@{path=$package;manifestPath=$manifestPath;version=$pins.version;commit=$pins.commit;packageSha256=$pins.packageSha256
  manifestSha256=$pins.manifestSha256;dllSha256=$pins.dllSha256;dllMvid=$pins.dllMvid}
}

# u1 and u3 load the reference-free owner seed through its reviewed registry gate.
function Get-KmcChunk7UpgradeSeedSource($Fixture){
 $selection=[pscustomobject][ordered]@{case='owner-seed-readonly-inventory';baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0
  seedId='KMC_C7_NO_MODS_OWNER_SEED';seedSha256='3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f'}
 Get-KmcChunk7OwnerSeedSource $selection $Fixture $null
}

# A passed u1 is proved from its own records, each hash-bound to the next: the outer
# runtime result binds the game record, the game record binds the observer result, and
# the observer saw exactly preview.211 loaded. Four hashes (result, game record, observer
# result, state) are pinned into the u2/u3 request; reading that request re-derives the
# whole proof and the carried files, never just the hashes.
function Get-KmcChunk7UpgradeCarryRecordPaths([string]$SourceRunId){
 $lab=Get-KmcLabRoot;$evidence=Join-Path $lab ('runtime-evidence/'+$SourceRunId)
 [pscustomobject][ordered]@{request=(Join-Path $evidence 'runtime-request.json');result=(Join-Path $evidence 'runtime-result.json');game=(Join-Path $evidence 'runtime-game-result.json')
  observer=(Join-Path $evidence 'observer-result.json');state=(Join-Path $lab ('runtime-state/transactions/'+$SourceRunId+'.json'))}
}
# Hashes and parses the same bytes, so a record cannot change between the two.
function Read-KmcChunk7UpgradeRecord([string]$Path){
 if(-not(Test-Path -LiteralPath $Path -PathType Leaf)){throw ('Upgrade carry source record is missing: '+$Path)}
 Assert-KmcNotReparsePoint $Path 'upgrade carry source record';Assert-KmcNotHardLink $Path 'upgrade carry source record'
 $bytes=[IO.File]::ReadAllBytes($Path)
 if($bytes.Length-lt2-or$bytes.Length-gt16MB){throw ('Upgrade carry source record is empty or oversized: '+$Path)}
 $sha=[Security.Cryptography.SHA256]::Create();try{$hash=([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes);if($text.Length-gt0-and$text[0]-eq[char]0xFEFF){$text=$text.Substring(1)}
 [pscustomobject]@{sha256=$hash;value=($text|ConvertFrom-Json)}
}
# One retained normal-player run, read from the lab by run id and bound to a pinned result
# SHA-256: result -> game record -> observer result, and the UMM receipt hash the result records.
# Statuses must agree across the three records. Row consumers (42, 55-56) build on this.
function Read-KmcChunk7NormalPlayerRun([string]$RunId,[string]$ResultSha256){
 if($RunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$'){throw 'Normal-player run id is invalid.'}
 if($ResultSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'Normal-player run pin lacks a result SHA-256.'}
 $paths=Get-KmcChunk7UpgradeCarryRecordPaths $RunId
 $records=[ordered]@{};foreach($name in @('request','result','game','observer')){$records[$name]=Read-KmcChunk7UpgradeRecord $paths.$name}
 $request=$records.request.value;$result=$records.result.value;$game=$records.game.value;$observer=$records.observer.value
 if($records.result.sha256-cne$ResultSha256){throw ('Normal-player run result does not match the SHA-256 its pin records: '+$RunId)}
 if([string]$request.runId-cne$RunId-or[string]$result.runId-cne$RunId-or[string]$game.runId-cne$RunId-or[string]$observer.runId-cne$RunId-or
  -not(Test-KmcChunk7NormalPlayerScenario $request.scenario)-or-not(Test-KmcChunk7NormalPlayerScenario $result.scenario)){throw ('Normal-player run records do not all name this normal-player run: '+$RunId)}
 if([string]$result.gameResultSha256-cne$records.game.sha256-or[string]$game.evidenceKind-cne'kmc-normal-player-observer'-or[string]$game.observerResultSha256-cne$records.observer.sha256){
  throw ('Normal-player run records are not hash-bound result to game record to observer result: '+$RunId)}
 # An outer FAIL may follow a game and observer PASS (the runner's own UMM receipt can refuse
 # after the game record is written, as in b199 u1), but an outer PASS needs both to PASS.
 if([string]$result.status-cnotin@('PASS','FAIL')-or([string]$result.status-ceq'PASS'-and([string]$game.status-cne'PASS'-or[string]$observer.status-cne'PASS'))){throw ('Normal-player run records disagree on the verdict: '+$RunId)}
 $receiptRequest=[pscustomobject]@{runId=$RunId;evidenceRoot=(Split-Path -Parent $paths.result);normalPlayer=$request.normalPlayer}
 Assert-KmcChunk7NormalPlayerUmmReceiptHash $receiptRequest $result
 if([string]$result.status-ceq'PASS'){Assert-KmcChunk7NormalPlayerUmmReceipt $receiptRequest $result}
 $u=if($null-ne$request.PSObject.Properties['normalPlayer']-and$null-ne$request.normalPlayer.PSObject.Properties['upgradeRehearsal']){$request.normalPlayer.upgradeRehearsal}else{$null}
 $gu=if($null-ne$game.PSObject.Properties['upgradeRehearsal']){$game.upgradeRehearsal}else{$null}
 [pscustomobject]@{runId=$RunId;paths=$paths;request=$request;result=$result;game=$game;observer=$observer;rehearsal=$u;gameRehearsal=$gu
  resultSha256=$records.result.sha256;gameResultSha256=$records.game.sha256;observerResultSha256=$records.observer.sha256
  ummReceiptSha256=(Get-KmcChunk7NormalPlayerRecordedUmmReceiptSha256 $result).value}
}
# The full external reader on the run's own retained records, after confirming the request
# names exactly the evidence folder the records were read from. Tests may inject a reader.
function Invoke-KmcChunk7NormalPlayerRunReader($Run,[scriptblock]$RunReader=$null){
 $root=Split-Path -Parent $Run.paths.result
 if(-not[string]::Equals([IO.Path]::GetFullPath([string]$Run.request.evidenceRoot).TrimEnd('\','/'),[IO.Path]::GetFullPath($root).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)){
  throw ('Normal-player run request does not name the evidence folder its records were read from: '+$Run.runId)}
 if($null-ne$RunReader){&$RunReader $Run.paths.result $Run.paths.request}
 else{$null=& (Join-Path $PSScriptRoot 'Test-RuntimeResult.ps1') -ResultPath $Run.paths.result -RequestPath $Run.paths.request 6>$null}
}

# Matrix rows 55-56 consume one complete rehearsal. Each run proves only its own predecessor
# u1, so completeness is checked here: exactly u1, u2, u3 in that order, one rehearsal id,
# distinct runs, each pinned by its result SHA-256 and passed, all on one candidate, run in
# time order, and u2 and u3 each binding the same u1 records (all five pins). Every step's
# request section is then re-derived (carried files included) and the full external reader
# re-reads each run.
function Assert-KmcChunk7UpgradeRehearsalComplete([string]$RehearsalId,$Steps,[scriptblock]$RunReader=$null){
 if($RehearsalId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$'){throw 'Upgrade rehearsal id is invalid.'}
 $order=@(Get-KmcChunk7UpgradeRehearsalSteps);$pins=@(@($Steps)|Where-Object{$null-ne$_})
 if($pins.Count-ne$order.Count){throw 'An upgrade rehearsal is complete only with exactly u1, u2 and u3.'}
 for($i=0;$i-lt$order.Count;$i++){
  Assert-KmcExactProperties $pins[$i] @('step','runId','resultSha256') 'upgrade rehearsal step pin'
  if([string]$pins[$i].step-cne$order[$i]){throw 'An upgrade rehearsal is complete only with u1, u2 and u3 in that order.'}
 }
 if(@($pins|ForEach-Object{[string]$_.runId}|Sort-Object -Unique).Count-ne$order.Count){throw 'An upgrade rehearsal needs three distinct runs.'}
 $runs=@(for($i=0;$i-lt$order.Count;$i++){Read-KmcChunk7NormalPlayerRun ([string]$pins[$i].runId) ([string]$pins[$i].resultSha256)})
 for($i=0;$i-lt$order.Count;$i++){$r=$runs[$i]
  if($null-eq$r.rehearsal-or$null-eq$r.gameRehearsal-or[string]$r.rehearsal.step-cne$order[$i]-or[string]$r.gameRehearsal.step-cne$order[$i]-or
   [string]$r.rehearsal.rehearsalId-cne$RehearsalId-or[string]$r.gameRehearsal.rehearsalId-cne$RehearsalId){throw ('An upgrade rehearsal mixes steps or rehearsal ids: '+$r.runId)}
  if([string]$r.result.status-cne'PASS'){throw ('An upgrade rehearsal step did not pass: '+$order[$i])}
  if($null-eq$r.ummReceiptSha256){throw ('An upgrade rehearsal step does not record its UMM receipt SHA-256: '+$order[$i])}
 }
 foreach($field in @('commit','productVersion','dllSha256','dllMvid')){
  if(@($runs|ForEach-Object{[string]$_.result.$field}|Sort-Object -Unique).Count-ne1){throw ('An upgrade rehearsal spans more than one candidate: '+$field)}
 }
 for($i=1;$i-lt$order.Count;$i++){
  $before=[DateTimeOffset]::MinValue;$after=[DateTimeOffset]::MinValue
  if(-not[DateTimeOffset]::TryParse([string]$runs[$i-1].result.completedAtUtc,[ref]$before)-or-not[DateTimeOffset]::TryParse([string]$runs[$i].result.startedAtUtc,[ref]$after)-or$after-lt$before){
   throw 'An upgrade rehearsal is complete only with u1, u2 and u3 run in that order.'}
 }
 # The u1 proof again from u1's own records (state included); its pins must be exactly what
 # u2 and u3 each declared as their carry source.
 $u1=(Read-KmcChunk7UpgradeU1 $runs[0].runId $RehearsalId).pins
 if([string]$u1.resultSha256-cne$runs[0].resultSha256){throw 'The u1 proof does not read the pinned u1 result.'}
 foreach($r in @($runs[1],$runs[2])){
  $c=$r.rehearsal.carrySource
  if($null-eq$c-or[string]$r.rehearsal.carrySourceRunId-cne[string]$u1.runId){throw ('An upgrade rehearsal step does not bind the same u1: '+$r.runId)}
  foreach($field in @('runId','resultSha256','gameResultSha256','observerResultSha256','stateSha256')){
   if([string]$c.$field-cne[string]$u1.$field){throw ('An upgrade rehearsal step does not bind the same u1: '+$r.runId+' '+$field)}
  }
 }
 foreach($r in $runs){Assert-KmcChunk7UpgradeRehearsalSection $r.request}
 foreach($r in $runs){Invoke-KmcChunk7NormalPlayerRunReader $r $RunReader}
 [pscustomobject][ordered]@{contract='f12-upgrade-rehearsal-complete-v1';rows=@(55,56);rehearsalId=$RehearsalId
  candidate=[pscustomobject][ordered]@{commit=[string]$runs[0].result.commit;productVersion=[string]$runs[0].result.productVersion;dllSha256=[string]$runs[0].result.dllSha256;dllMvid=[string]$runs[0].result.dllMvid}
  steps=@(foreach($r in $runs){[pscustomobject][ordered]@{step=[string]$r.rehearsal.step;runId=$r.runId;resultSha256=$r.resultSha256;gameResultSha256=$r.gameResultSha256
   observerResultSha256=$r.observerResultSha256;ummReceiptSha256=$r.ummReceiptSha256;stagedRole=[string]$r.rehearsal.stagedKmc.role}})}
}
function Read-KmcChunk7UpgradeU1([string]$SourceRunId,[string]$RehearsalId){
 if($SourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$'){throw 'Upgrade carry source run id is invalid.'}
 if($RehearsalId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$'){throw 'Upgrade rehearsal id is invalid.'}
 $pins=Get-KmcChunk7PreviousPackagePins;$paths=Get-KmcChunk7UpgradeCarryRecordPaths $SourceRunId
 $records=[ordered]@{};foreach($name in @('request','result','game','observer','state')){$records[$name]=Read-KmcChunk7UpgradeRecord $paths.$name}
 $request=$records.request.value;$result=$records.result.value;$game=$records.game.value;$observer=$records.observer.value;$state=$records.state.value
 $u=if($null-ne$request.PSObject.Properties['normalPlayer']-and$null-ne$request.normalPlayer.PSObject.Properties['upgradeRehearsal']){$request.normalPlayer.upgradeRehearsal}else{$null}
 if([string]$request.runId-cne$SourceRunId-or-not(Test-KmcChunk7NormalPlayerScenario $request.scenario)-or$null-eq$u-or[string]$u.step-cne'u1-previous-install'-or[string]$u.rehearsalId-cne$RehearsalId-or
  [string]$u.stagedKmc.packageSha256-cne$pins.packageSha256-or[string]$result.runId-cne$SourceRunId-or[string]$result.status-cne'PASS'){
  throw 'Upgrade carry source is not a passed u1 previous-package install of the released preview.211 package.'
 }
 # u1's own UMM receipt: its SHA-256 recorded in u1's outer result, and its admission re-judged.
 if($null-eq$result.PSObject.Properties['normalPlayerUmmReceiptSha256']){throw 'Upgrade carry source u1 result does not record its UMM session receipt SHA-256.'}
 $receiptRequest=[pscustomobject]@{runId=$SourceRunId;evidenceRoot=(Split-Path -Parent $paths.result);normalPlayer=$request.normalPlayer}
 Assert-KmcChunk7NormalPlayerUmmReceiptHash $receiptRequest $result;Assert-KmcChunk7NormalPlayerUmmReceipt $receiptRequest $result
 $gu=if($null-ne$game.PSObject.Properties['upgradeRehearsal']){$game.upgradeRehearsal}else{$null}
 if([string]$result.gameResultSha256-cne$records.game.sha256-or[string]$game.evidenceKind-cne'kmc-normal-player-observer'-or[string]$game.status-cne'PASS'-or[string]$game.runId-cne$SourceRunId-or
  $null-eq$gu-or[string]$gu.rehearsalId-cne$RehearsalId-or[string]$gu.step-cne'u1-previous-install'-or$null-eq$gu.loadedKmc-or
  [string]$gu.loadedKmc.dllSha256-cne$pins.dllSha256-or[string]$gu.loadedKmc.dllMvid-cne$pins.dllMvid-or[string]$game.observerResultSha256-cne$records.observer.sha256){
  throw 'Upgrade carry source game record does not bind a passed u1 that loaded preview.211.'
 }
 $loaded=if($null-ne$observer.PSObject.Properties['observations']-and$null-ne$observer.observations.PSObject.Properties['presenceLoaded']){$observer.observations.presenceLoaded}else{$null}
 if([string]$observer.status-cne'PASS'-or[string]$observer.runId-cne$SourceRunId-or$null-eq$loaded-or[string]$loaded.kmcDllSha256-cne$pins.dllSha256-or[string]$loaded.kmcMvid-cne$pins.dllMvid){
  throw 'Upgrade carry source observer did not see exactly preview.211 loaded.'
 }
 if([int]$state.schemaVersion-ne4-or[string]$state.runId-cne$SourceRunId-or[string]$state.phase-cne'restored'-or[string]$state.stagingMode-cne'base-game-only-kmc-observer'-or
  [string]$state.packageSha256-cne$pins.packageSha256-or$state.runtimeWritesClassified-ne$true-or@($state.runtimeUnexpectedWrites).Count-ne0){
  throw 'Upgrade carry source transaction is not the restored, fully classified u1 staging of the previous package.'
 }
 [pscustomobject]@{pins=[pscustomobject][ordered]@{runId=$SourceRunId;resultSha256=$records.result.sha256;gameResultSha256=$records.game.sha256;observerResultSha256=$records.observer.sha256;stateSha256=$records.state.sha256}
  state=$state}
}
function Get-KmcChunk7UpgradeCarrySource([string]$SourceRunId,[string]$RehearsalId){(Read-KmcChunk7UpgradeU1 $SourceRunId $RehearsalId).pins}
# Offline re-derivation of a pinned u1: the full proof again, the same four hashes, and the
# same carried files (name, length, bytes) that u1's classified writes yield.
function Assert-KmcChunk7UpgradeCarrySource($Pins,[string]$RehearsalId,$CarriedFiles){
 Assert-KmcExactProperties $Pins @('runId','resultSha256','gameResultSha256','observerResultSha256','stateSha256') 'upgrade carry source pins'
 $derived=Get-KmcChunk7UpgradeCarrySource ([string]$Pins.runId) $RehearsalId
 foreach($field in @('runId','resultSha256','gameResultSha256','observerResultSha256','stateSha256')){
  if([string]$Pins.$field-cne[string]$derived.$field){throw ('The pinned u1 record no longer matches: '+$field)}
 }
 # Assigned first: the carry function returns its list as one object.
 $written=Get-KmcChunk7UpgradeCarry ([string]$Pins.runId) $RehearsalId
 $actual=@($written|ForEach-Object{[string]$_.name+'|'+[long]$_.length+'|'+[string]$_.sha256}|Sort-Object)
 $declared=@(@($CarriedFiles)|ForEach-Object{[string]$_.name+'|'+[long]$_.length+'|'+[string]$_.sha256}|Sort-Object)
 if(($actual-join';')-cne($declared-join';')){throw 'The carried files are not exactly the files the pinned u1 wrote.'}
}

# u2 and u3 carry u1's ordinary KMC folder state: only the files the restored u1
# transaction classified as expected runtime writes in the KMC folder (UMM's assembly
# cache), each re-hashed from u1's retained staged-after tree. Both must name a passed
# u1 of the same rehearsal id. u3 deliberately lays u1's folder state (not u2's):
# rollback restores the previous install as the player had it.
function Get-KmcChunk7UpgradeCarry([string]$SourceRunId,[string]$RehearsalId){
 # The state is the exact parsed bytes of the proved u1 record set.
 $state=(Read-KmcChunk7UpgradeU1 $SourceRunId $RehearsalId).state
 $lab=Get-KmcLabRoot;$pins=Get-KmcChunk7PreviousPackagePins
 $stagedAfter=Assert-KmcChildPath ([string]$state.stagedAfter) (Join-Path $lab 'runtime-staging') 'u1 staged-after tree'
 $carry=@()
 foreach($write in @($state.runtimeExpectedWrites)){
  if([string]$write.path-cnotmatch'^KingmakerMountedCombat/'){continue}
  if([string]$write.change-cne'added'-or[string]$write.kind-cne'file'-or[string]$write.path-cnotmatch'^KingmakerMountedCombat/KingmakerMountedCombat\.dll\.[0-9]{1,9}\.cache$'){
   throw ('Upgrade carry source holds a KMC-folder write outside the closed UMM cache shape: '+[string]$write.path)
  }
  $file=Assert-KmcChildPath (Join-Path $stagedAfter ([string]$write.path)) (Join-Path $lab 'runtime-staging') 'carried u1 file'
  # UMM's cache is a File.Copy of the loaded DLL: the carried bytes are exactly preview.211's.
  if([string]$write.runtimeSha256-cne$pins.dllSha256){throw 'A carried u1 cache is not a byte copy of the previous package DLL.'}
  if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw 'A carried u1 file is missing from its retained staged-after tree.'}
  Assert-KmcNotReparsePoint $file 'carried u1 file';Assert-KmcNotHardLink $file 'carried u1 file'
  $item=Get-Item -LiteralPath $file
  if($item.Length-gt64MB-or(Get-KmcSha256 $file)-cne[string]$write.runtimeSha256){throw 'A carried u1 file differs from its classified runtime write.'}
  $carry+=[pscustomobject][ordered]@{name=$item.Name;sourcePath=$file;length=[long]$item.Length;sha256=[string]$write.runtimeSha256}
 }
 if($carry.Count-lt1-or$carry.Count-gt4){throw 'Upgrade carry source holds no, or an unbounded set of, KMC-folder runtime files.'}
 ,$carry
}

# The request records which package was staged, why, what was carried in, and the
# exact u1 records it was carried from.
function New-KmcChunk7UpgradeRehearsalSection([string]$Step,[string]$RehearsalId,$Previous,$Manifest,$CarrySource,$Carry){
 if($Step-cnotin@(Get-KmcChunk7UpgradeRehearsalSteps)){throw 'Unknown upgrade rehearsal step.'}
 $staged=if(Test-KmcChunk7UpgradePreviousStep $Step){
  [ordered]@{role='previous';version=$Previous.version;commit=$Previous.commit;packageSha256=$Previous.packageSha256;manifestSha256=$Previous.manifestSha256;dllSha256=$Previous.dllSha256;dllMvid=$Previous.dllMvid}
 }else{
  # The candidate's manifestSha256 is deliberately null: the candidate package and its manifest
  # are already pinned by the qualification-suite snapshot, which the runner checks before staging
  # (Assert-KmcQualificationSuiteContinuity, QualificationSuiteContinuity.ps1:395 compares
  # record.package.manifestSha256 with the manifest beside the package). The previous package
  # is not the suite's package; it is pinned by Get-KmcChunk7PreviousPackagePins, so only it
  # carries its manifest hash here.
  [ordered]@{role='candidate';version=[string]$Manifest.version;commit=[string]$Manifest.commit;packageSha256=[string]$Manifest.packageSha256;manifestSha256=$null;dllSha256=[string]$Manifest.dllSha256;dllMvid=[string]$Manifest.dllMvid}
 }
 $first=$Step-ceq'u1-previous-install'
 [ordered]@{contract='f12-upgrade-rehearsal-v1';rehearsalId=$RehearsalId;step=$Step;stagedKmc=$staged
  carrySourceRunId=$(if($first){$null}else{[string]$CarrySource.runId})
  carrySource=$(if($first){$null}else{[ordered]@{runId=[string]$CarrySource.runId;resultSha256=[string]$CarrySource.resultSha256;gameResultSha256=[string]$CarrySource.gameResultSha256
   observerResultSha256=[string]$CarrySource.observerResultSha256;stateSha256=[string]$CarrySource.stateSha256}})
  carriedFiles=@(foreach($c in @($Carry|Where-Object{$null-ne$_})){
   # Each element is one carried file record; a nested array is a shape defect, never flattened.
   if($c-is[Array]-or$null-eq$c.PSObject.Properties['sourcePath']-or$c.name-isnot[string]){throw 'Carried files are not a flat list of file records.'}
   [ordered]@{name=$c.name;length=[long]$c.length;sha256=[string]$c.sha256}})}
}
function Assert-KmcChunk7UpgradeRehearsalSection($Request){
 $u=$Request.normalPlayer.upgradeRehearsal;$pins=Get-KmcChunk7PreviousPackagePins
 Assert-KmcExactProperties $u @('contract','rehearsalId','step','stagedKmc','carrySourceRunId','carrySource','carriedFiles') 'upgrade rehearsal section'
 if([string]$u.rehearsalId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$'){throw 'Upgrade rehearsal id is invalid.'}
 Assert-KmcExactProperties $u.stagedKmc @('role','version','commit','packageSha256','manifestSha256','dllSha256','dllMvid') 'upgrade rehearsal staged KMC'
 if([string]$u.contract-cne'f12-upgrade-rehearsal-v1'-or[string]$u.step-cnotin@(Get-KmcChunk7UpgradeRehearsalSteps)){throw 'Upgrade rehearsal contract or step differs.'}
 $previous=Test-KmcChunk7UpgradePreviousStep ([string]$u.step);$s=$u.stagedKmc
 if($previous){
  if([string]$s.role-cne'previous'-or[string]$s.version-cne$pins.version-or[string]$s.commit-cne$pins.commit-or[string]$s.packageSha256-cne$pins.packageSha256-or
   [string]$s.manifestSha256-cne$pins.manifestSha256-or[string]$s.dllSha256-cne$pins.dllSha256-or[string]$s.dllMvid-cne$pins.dllMvid){throw 'A previous-package step does not stage exactly the released preview.211 package.'}
 }elseif([string]$s.role-cne'candidate'-or$null-ne$s.manifestSha256-or[string]$s.dllSha256-cne[string]$Request.dllSha256-or[string]$s.dllMvid-cne[string]$Request.dllMvid-or
  [string]$s.version-cne[string]$Request.productVersion-or[string]$s.commit-cne[string]$Request.commit-or[string]$s.packageSha256-cnotmatch'^[0-9a-f]{64}$'){throw 'The upgrade step does not stage exactly the run candidate.'}
 $files=@($u.carriedFiles)
 if([string]$u.step-ceq'u1-previous-install'){if($null-ne$u.carrySourceRunId-or$null-ne$u.carrySource-or$files.Count-ne0){throw 'The first install carries nothing.'}}
 else{
  if([string]$u.carrySourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$'-or[string]$u.carrySourceRunId-ceq[string]$Request.runId-or$files.Count-lt1-or$files.Count-gt4-or
   $null-eq$u.carrySource-or[string]$u.carrySource.runId-cne[string]$u.carrySourceRunId){throw 'Upgrade and rollback carry an exact bounded u1 folder state.'}
  foreach($f in $files){Assert-KmcExactProperties $f @('name','length','sha256') 'carried file'
   if([string]$f.name-cnotmatch'^KingmakerMountedCombat\.dll\.[0-9]{1,9}\.cache$'-or[string]$f.sha256-cne$pins.dllSha256-or[long]$f.length-lt1-or[long]$f.length-gt64MB){throw 'A carried file is outside the closed UMM cache shape.'}}
  if(@($files|ForEach-Object{[string]$_.name}|Sort-Object -Unique).Count-ne$files.Count){throw 'Carried file names repeat.'}
  Assert-KmcChunk7UpgradeCarrySource $u.carrySource ([string]$u.rehearsalId) $files
 }
}
