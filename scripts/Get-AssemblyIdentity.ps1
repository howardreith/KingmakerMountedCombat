[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$AssemblyPath)

$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$path=[IO.Path]::GetFullPath($AssemblyPath)
if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Assembly is missing: $path"}
$name=[Reflection.AssemblyName]::GetAssemblyName($path)
$assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($path)
$target=@($assembly.CustomAttributes|Where-Object AttributeType -eq ([Runtime.Versioning.TargetFrameworkAttribute])|Select-Object -First 1)
$value=[ordered]@{
    name=$name.Name
    version=$name.Version.ToString()
    mvid=$assembly.ManifestModule.ModuleVersionId.ToString()
    targetFramework=if($target.Count-eq1){[string]$target[0].ConstructorArguments[0].Value}else{$null}
    references=@($assembly.GetReferencedAssemblies()|ForEach-Object Name|Sort-Object)
}
[Console]::Out.Write(($value|ConvertTo-Json -Compress -Depth 5))
