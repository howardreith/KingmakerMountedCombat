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
