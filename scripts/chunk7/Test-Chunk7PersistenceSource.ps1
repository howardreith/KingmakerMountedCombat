$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
$passed=0
$script:sourceLab=Join-Path $chunk7AnalysisRoot ('source-fixtures-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($script:sourceLab)
function Get-KmcLabRoot { return $script:sourceLab }
function Write-TestJson([string]$Path,$Value) {[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 90 -Compress),[Text.UTF8Encoding]::new($false))}
function Write-TestZip([string]$Path,$Header,$Archive,[string]$Mutation='') {
    Add-Type -AssemblyName System.IO.Compression
    $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $zip=$null
    try {
        $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true)
        $entries=@([pscustomobject]@{name='header.json';text=($Header|ConvertTo-Json -Depth 90 -Compress)},
          [pscustomobject]@{name='kmc-mounted-state';text=($Archive|ConvertTo-Json -Depth 90 -Compress)})
        if($Mutation-ceq'duplicate-header'){$entries+=Copy-Test $entries[0]}
        if($Mutation-ceq'duplicate-metadata'){$entries+=Copy-Test $entries[1]}
        if($Mutation-ceq'duplicate-header-key'){$entries[0].text=$entries[0].text.Replace('"Type":"Manual"','"Type":"Manual","Type":"Manual"')}
        if($Mutation-ceq'oversized-metadata'){$entries[1].text+=' '*(128KB)}
        foreach($entry in $entries){$output=$null;try{$output=[IO.StreamWriter]::new($zip.CreateEntry($entry.name).Open(),[Text.UTF8Encoding]::new($false));$output.Write($entry.text)}finally{if($output){$output.Dispose()}}}
    } finally {if($zip){$zip.Dispose()};$stream.Dispose()}
}
function Pin-TestReceipts($Context) {
    $artifactPath=Join-Path $Context.evidence 'chunk7-persistence-evidence.json';Write-TestJson $artifactPath $Context.artifact
    $Context.manifest.artifacts[0].length=[long](Get-Item -LiteralPath $artifactPath).Length
    $Context.manifest.artifacts[0].sha256=Get-KmcSha256 $artifactPath
    $manifestPath=Join-Path $Context.evidence 'runtime-artifacts.json';Write-TestJson $manifestPath $Context.manifest
    $Context.game.evidenceManifestSha256=Get-KmcSha256 $manifestPath
    $gamePath=Join-Path $Context.evidence 'runtime-game-result.json';Write-TestJson $gamePath $Context.game
    $Context.result.evidenceManifestSha256=$Context.game.evidenceManifestSha256;$Context.result.gameResultSha256=Get-KmcSha256 $gamePath
    Write-TestJson (Join-Path $Context.evidence 'runtime-result.json') $Context.result
    Write-TestJson (Join-Path $Context.evidence 'runtime-request.json') $Context.request
    Write-TestJson (Join-Path $Context.evidence 'base-game-isolation-result.json') $Context.isolation
    Write-TestJson (Join-Path $Context.profile 'owner.json') $Context.owner
}
function New-TestSource([string]$Name,[string]$Mutation='') {
    $run='synthetic-c7-source-'+$Name
    $profile=Join-Path $script:sourceLab ('runtime-staging/persistence-'+$run)
    $evidence=Join-Path $script:sourceLab ('runtime-evidence/'+$run)
    [void][IO.Directory]::CreateDirectory((Join-Path $profile 'Saved Games'))
    [void][IO.Directory]::CreateDirectory($evidence)
    $q=Copy-Test $request;$q.runId=$run;$q|Add-Member NoteProperty evidenceRoot $evidence
    $a=Copy-Test $good;$a.runId=$run
    $path=Join-Path $profile ('Saved Games/'+(Get-KmcChunk7PersistenceLeaf $q.persistenceCase))
    $header=[pscustomobject]@{Name='KMC_C7_WORLD';Type='Manual';CompatibilityVersion=1;GameId=$q.fixture.working.gameId;GameName=$q.fixture.working.gameName;Area=$q.fixture.working.area}
    $saved=Copy-Test $archive
    if($Mutation-ceq'foreign-header'){$header.GameId='99999999-9999-4999-8999-999999999999'}
    if($Mutation-ceq'wrong-native-category'){$header.Type='Quick'}
    if($Mutation-ceq'dropped-archive-pair'){$saved.Pairs=@($saved.Pairs[0])}
    Write-TestZip $path $header $saved $Mutation
    $sha=Get-KmcSha256 $path
    $a.observations.nativeWrite.sha256=$sha;$a.observations.nativeWrite.length=[long](Get-Item -LiteralPath $path).Length
    $a.observations.nativeWrite|Add-Member NoteProperty path $path
    $manifest=[pscustomobject]@{schemaVersion=1;runId=$run;scenario=$q.scenario;createdAtUtc='2026-10-12T00:00:00Z';artifacts=@([pscustomobject]@{relativePath='chunk7-persistence-evidence.json';kind='chunk7-persistence-evidence';length=1L;sha256='0'*64})}
    $owner=[pscustomobject]@{runId=$run;scenario=$q.scenario;persistenceCase=$q.persistenceCase;transactionToken='a'*64}
    $game=[ordered]@{status='PASS';transactionToken=$owner.transactionToken;evidenceManifestSha256='0'*64;assertionFailCount=0;subscenarioFailCount=0}
    $result=[ordered]@{status='PASS';transactionToken=$owner.transactionToken;modsRestored=$true;workingRestored=$true;baselineImmutable=$true;saveProtectionPassed=$true;saveWriteAllowlistPassed=$true;errors=@();restoredSaveInventoryDigest='b'*64;evidenceManifestSha256='0'*64;gameResultSha256='0'*64}
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$game[$field]=$q.$field;$result[$field]=$q.$field}
    $isolation=[ordered]@{runId=$run;stagingMode='base-game-only-kmc'}
    foreach($field in @('stageVerified','dependenciesUnchanged','processExited','modsRestored','saveProtectionPassed','gameReportedPass','nativeInventoryObserved','nativeCompatibilityQualified')){$isolation[$field]=$true}
    $context=[pscustomobject]@{path=$path;profile=$profile;evidence=$evidence;sha=$sha;request=$q;artifact=$a;manifest=$manifest;owner=$owner;game=[pscustomobject]$game;result=[pscustomobject]$result;isolation=[pscustomobject]$isolation}
    Pin-TestReceipts $context
    return $context
}
function Read-TestSource($Context) {
    Get-KmcChunk7PersistenceSource -SourceRunId $Context.request.runId -ExpectedSha256 $Context.sha -Fixture $Context.request.fixture -Case $Context.request.persistenceCase -ExpectedDllSha256 $request.dllSha256 -ExpectedCommit $request.commit
}
Check 'isolated source positive with actual ZIP and exact immutable restoration receipts' {
    $c=New-TestSource 'positive';$before=@(Get-ChildItem -LiteralPath $c.profile,$c.evidence -File -Recurse|Sort-Object FullName|ForEach-Object{$_.FullName+'|'+$_.Length+'|'+$_.LastWriteTimeUtc.Ticks+'|'+(Get-KmcSha256 $_.FullName)})
    $found=Read-TestSource $c
    if($found.path-cne$c.path-or$found.descriptor.sha256-cne$c.sha-or$found.descriptor.internalName-cne'KMC_C7_WORLD'-or$found.descriptor.fileName-cne'Manual_300_KMC_C7_WORLD.zks'){throw 'Exact returned source descriptor differs.'}
    $after=@(Get-ChildItem -LiteralPath $c.profile,$c.evidence -File -Recurse|Sort-Object FullName|ForEach-Object{$_.FullName+'|'+$_.Length+'|'+$_.LastWriteTimeUtc.Ticks+'|'+(Get-KmcSha256 $_.FullName)})
    if(($before-join'|')-cne($after-join'|')){throw 'Read-only intake changed a source byte or timestamp.'}
}
foreach($mutation in @('wrong-archive-pin','wrong-candidate','wrong-commit','wrong-case','wrong-token','incomplete-restoration','missing-native-inventory','missing-compatibility','native-fail','changed-manifest','changed-game-result','duplicate-receipt-key')) {
    Reject ('source '+$mutation) {
        $c=New-TestSource $mutation
        switch($mutation) {
          'wrong-archive-pin' {$c.sha='c'*64}
          'wrong-candidate' {$c.request.dllSha256='c'*64;Pin-TestReceipts $c}
          'wrong-commit' {$c.request.commit='c'*40;Pin-TestReceipts $c}
          'wrong-case' {$c.owner.persistenceCase='multi-exploration-quick';Pin-TestReceipts $c}
          'wrong-token' {$c.owner.transactionToken='c'*64;Pin-TestReceipts $c}
          'incomplete-restoration' {$c.result.workingRestored=$false;Pin-TestReceipts $c}
          'missing-native-inventory' {$c.isolation.nativeInventoryObserved=$false;Pin-TestReceipts $c}
          'missing-compatibility' {$c.isolation.nativeCompatibilityQualified=$false;Pin-TestReceipts $c}
          'native-fail' {$c.game.assertionFailCount=1;Pin-TestReceipts $c}
          'changed-manifest' {[IO.File]::AppendAllText((Join-Path $c.evidence 'runtime-artifacts.json'),' ')}
          'changed-game-result' {[IO.File]::AppendAllText((Join-Path $c.evidence 'runtime-game-result.json'),' ')}
          'duplicate-receipt-key' {$p=Join-Path $c.evidence 'runtime-request.json';$s=[IO.File]::ReadAllText($p);[IO.File]::WriteAllText($p,$s.Replace('"schemaVersion":2','"schemaVersion":2,"schemaVersion":2'))}
        }
        Read-TestSource $c
    }
}
foreach($mutation in @('foreign-header','wrong-native-category','duplicate-header','duplicate-header-key','duplicate-metadata','oversized-metadata','dropped-archive-pair')) {
    Reject ('source fully repinned '+$mutation) {$c=New-TestSource $mutation $mutation;Read-TestSource $c}
}
Check 'cold source compares preserved graph and requires a fresh process' {
    $c=New-TestSource 'cold-positive';$q=Copy-Test $coldRequest;$q.persistenceLoad.sha256=$c.sha;$q.persistenceLoad.length=[long](Get-Item -LiteralPath $c.path).Length
    $a=Copy-Test $cold
    Assert-KmcChunk7PersistenceColdSource $c.request.runId $q $a
}
foreach($mutation in @('reused-process','wrong-restored-graph','wrong-candidate','wrong-case')) {
    Reject ('cold source '+$mutation) {
        $c=New-TestSource ('cold-'+$mutation);$q=Copy-Test $coldRequest;$q.persistenceLoad.sha256=$c.sha;$q.persistenceLoad.length=[long](Get-Item -LiteralPath $c.path).Length
        $a=Copy-Test $cold
        switch($mutation){'reused-process' {$a.observations.processId=123};'wrong-restored-graph' {$a.observations.loadedArchive.Pairs[0].Generation=99};'wrong-candidate' {$q.dllSha256='c'*64};'wrong-case' {$q.persistenceCase='multi-exploration-quick'}}
        Assert-KmcChunk7PersistenceColdSource $c.request.runId $q $a
    }
}
Write-Host ('CHUNK7 PERSISTENCE SOURCE PASS='+$passed+' FAIL=0; synthetic filesystem/ZIP/receipt validators only. Preserved fixture root: '+$script:sourceLab)
