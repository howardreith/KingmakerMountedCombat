Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureFixtures.ps1')
. (Join-Path $PSScriptRoot 'CurrentRemovalEvidence.ps1')
function Assert-KmcChunk7StockMammothFixtureLaunchArguments($Bound,[string]$Scenario,[bool]$BaseGameOnly,[int]$TimeoutSeconds,[string]$SelectionPath){
 if(-not(Test-KmcChunk7StockMammothFixtureScenario $Scenario)){
  if($Bound.ContainsKey('StockMammothFixtureSelectionPath')){throw 'Stock Mammoth fixture selection path is forbidden under another scenario.'};return
 }
 if(-not$BaseGameOnly-or$TimeoutSeconds-ne1200-or[string]::IsNullOrEmpty($SelectionPath)){
  throw 'Stock Mammoth fixture requires base-only staging, exact selection and bounded outer timeout 1200 seconds.'
 }
 foreach($key in @('SmallFixtureSelectionPath','NativeBootstrapCase','OrdinarySelectionPath','OrdinaryFieldSelectionPath','BootstrapInventorySelectionPath','AssistedColdInventorySelectionPath','ManualSeedSelectionPath','AcquiredManualSelectionPath','AssistedFixtureSelectionPath','OwnedPairsSelectionPath','ManualDerivedSelectionPath','CumulativeAcquisitionSelectionPath','SmallCalibrationSelectionPath','ProgressionSelectionPath','PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','ObserverPackagePath','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas','ObserveWorkingDeserialization')){
  if($Bound.ContainsKey($key)){throw ('Stock Mammoth fixture forbids foreign source/action/area input '+$key)}
 }
}
function Read-KmcChunk7StockMammothFixtureSelectionFile([string]$Path,[string]$RunId){
 $path=Assert-KmcChildPath $Path (Get-KmcLabRoot) 'Stock Mammoth exact selection'
 Assert-KmcNotReparsePoint $path 'Stock Mammoth selection';Assert-KmcNotHardLink $path 'Stock Mammoth selection'
 $file=Get-Item -LiteralPath $path;if($file.PSIsContainer-or$file.Length-lt1-or$file.Length-gt16384){throw 'Stock Mammoth selection is empty or oversized.'}
 $hash=Get-KmcSha256 $path;Assert-KmcManifestJsonMembersUnique $path 'Stock Mammoth selection';$s=Read-KmcJson $path
 Assert-KmcChunk7StockMammothFixtureSelection ([pscustomobject]@{scenario='chunk7-stock-mammoth-fixture-preparation';runId=$RunId;stockMammothFixture=$s})
 $after=Get-Item -LiteralPath $path;if($after.Length-ne$file.Length-or$after.LastWriteTimeUtc.Ticks-ne$file.LastWriteTimeUtc.Ticks-or(Get-KmcSha256 $path)-cne$hash){throw 'Stock Mammoth selection changed during intake.'}
 [pscustomobject]@{path=$path;sha256=$hash;selection=$s}
}
function Get-KmcChunk7StockMammothFixtureSource($Selection,$Fixture,$Candidate){
 Assert-KmcChunk7StockMammothFixtureSelection ([pscustomobject]@{scenario='chunk7-stock-mammoth-fixture-preparation';runId='unassigned-stock-mammoth-fixture';stockMammothFixture=$Selection})
 $f09=ConvertTo-KmcStockMammothSeedSelection $Selection
 $source=Get-KmcChunk7SmallFixtureSource $f09 $Fixture $Candidate
 Assert-KmcChunk7StockMammothFixtureDescriptor ([pscustomobject]@{scenario='chunk7-stock-mammoth-fixture-preparation';runId='unassigned-stock-mammoth-fixture';stockMammothFixture=$Selection;persistenceLoad=$source.descriptor;fixture=$Fixture})
 [pscustomobject]@{path=$source.path;descriptor=$source.descriptor;seedId=$source.seedId;registryPath=$source.registryPath;registrySha256=$source.registrySha256;
  registryEntry=$source.registryEntry;intakePath=$source.intakePath;intakeSha256=$source.intakeSha256;qualification=$source.qualification;inventory=$source.inventory;
  fixtureSetupOnly=$true;producerAncestry=$false;nativeAuthorityGranted=$false}
}
function Copy-KmcChunk7StockMammothFixtureSource($Source,[string]$ProfileRoot,$Lock,$Selection,$Fixture,$Candidate){
 [void](Assert-KmcRuntimeLockOwner $Lock)
 $fresh=Get-KmcChunk7StockMammothFixtureSource $Selection $Fixture $Candidate
 if((ConvertTo-KmcChunk7Canonical $fresh)-cne(ConvertTo-KmcChunk7Canonical $Source)){throw 'Stock Mammoth immutable source changed before staging.'}
 $f09=ConvertTo-KmcStockMammothSeedSelection $Selection
 Copy-KmcChunk7SmallFixtureSource $fresh $ProfileRoot $Lock $f09 $Fixture $Candidate 'chunk7-stock-mammoth-fixture-preparation' 'stock-mammoth-fixture'
}
function Assert-KmcChunk7StockMammothFixtureOutput($Request,$Artifact){
 Assert-KmcChunk7StockMammothFixtureEvidence $Request $Artifact
 $root=Get-KmcAssistedSaveRoot $Request;Assert-KmcDirectoryTreeCloneable $root 'Stock Mammoth settled save root'
 $w=$Artifact.observations.nativeWrite
 Assert-KmcAssistedSettledFile $w.path $w 'Stock Mammoth native Manual'
 Assert-KmcAssistedSettledFile (Join-Path $root $Request.persistenceLoad.fileName) $Request.persistenceLoad 'Stock Mammoth immutable seed copy'
 $actual=@(Get-ChildItem -LiteralPath $root -Force);$names=@($actual|ForEach-Object Name|Sort-Object -CaseSensitive)
 if(@($actual|Where-Object PSIsContainer).Count-ne0-or(ConvertTo-KmcChunk7Canonical $names)-cne(ConvertTo-KmcChunk7Canonical @($Request.persistenceLoad.fileName,$w.fileName|Sort-Object -CaseSensitive))){throw 'Stock Mammoth isolated save root contains an unowned entry.'}
 Add-Type -AssemblyName System.IO.Compression;$stream=$null;$zip=$null
 try{
  $stream=[IO.FileStream]::new($w.path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  if($zip.Entries.Count-gt4096){throw 'Stock Mammoth native archive entry count is unbounded.'}
  $header=Read-KmcChunk7AssistedMetadataEntry $zip 'header.json' 1048576
  $archive=Read-KmcChunk7AssistedMetadataEntry $zip 'kmc-mounted-state' 131072
  foreach($pair in @(@('Name','internalName'),@('GameId','gameId'),@('GameName','gameName'),@('Area','area'),@('Type','type'))){Assert-KmcBootstrapInventoryString $header.($pair[0]) $w.($pair[1])}
  Assert-KmcOrdinaryInteger $header.CompatibilityVersion 1 1;Assert-KmcChunk7Boolean $header.IsAutoLevelupSave $false
  Assert-KmcSmallFixtureEmptyArchive $archive $Request.persistenceLoad
  Assert-KmcAssistedSame $archive $w.archive 'Stock Mammoth actual ZIP metadata versus native readback'
 }finally{if($zip){$zip.Dispose()};if($stream){$stream.Dispose()}}
 $referenceScan=Get-KmcCurrentRemovalReferenceScan $w.path
 if($referenceScan.scannedMembers-lt1-or$referenceScan.referenceHits.Count-ne0){throw 'Stock Mammoth producer archive retains a permanent KMC Horse reference.'}
 Assert-KmcAssistedSettledFile $w.path $w 'Stock Mammoth native Manual after readback'
}
