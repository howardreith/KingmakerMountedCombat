[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $PSScriptRoot '../runtime'}
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
# Pure in-memory values only. This script never calls the archive reader or
# creates, opens, reads, modifies or enumerates any save/ZIP archive.
# The write names its native save type: Auto keeps the historical level-up flag, Manual clears it.
$write=[pscustomobject]@{name='Autosave';type='Auto';gameId='11111111-1111-4111-8111-111111111111';gameName='Synthetic';area='a'*32}
$header=[pscustomobject]@{Name=$write.name;Type='Auto';GameId=$write.gameId;GameName=$write.gameName;Area=$write.area;CompatibilityVersion=1;IsAutoLevelupSave=$true}
$script:passes=0
Good 'exact in-memory native header scalar contract'{Assert-KmcChunk7ProgressionNativeHeader $header $write}
Good 'exact in-memory native Manual header scalar contract'{$w=Copy-O $write;$w.type='Manual';$h=Copy-O $header;$h.Type='Manual';$h.IsAutoLevelupSave=$false;Assert-KmcChunk7ProgressionNativeHeader $h $w}
Bad 'Manual header claiming the level-up autosave flag'{$w=Copy-O $write;$w.type='Manual';$h=Copy-O $header;$h.Type='Manual';Assert-KmcChunk7ProgressionNativeHeader $h $w}
foreach($key in @('Name','Type','GameId','GameName','Area')){Bad ('array instead of native header string '+$key){$h=Copy-O $header;$h.$key=@($h.$key);Assert-KmcChunk7ProgressionNativeHeader $h $write}}
foreach($key in @('Name','Type','GameId','GameName','Area')){Bad ('foreign native header identity '+$key){$h=Copy-O $header;$h.$key='foreign';Assert-KmcChunk7ProgressionNativeHeader $h $write}}
foreach($value in @('1',1.0,$true,0,2)){Bad ('invalid compatibility token '+[string]$value){$h=Copy-O $header;$h.CompatibilityVersion=$value;Assert-KmcChunk7ProgressionNativeHeader $h $write}}
foreach($value in @('true',1,$false)){Bad ('invalid native autosave flag '+[string]$value){$h=Copy-O $header;$h.IsAutoLevelupSave=$value;Assert-KmcChunk7ProgressionNativeHeader $h $write}}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; pure in-memory header contract only; ZIP/member validation not executed')
