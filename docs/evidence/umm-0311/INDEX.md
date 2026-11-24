# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `272238cef56ab9307ec115360df62439efcdf072`, exported 2026-11-24 03:41 UTC.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root (Git Bash or any POSIX shell with sha256sum; the second
command, with Python, works in Windows PowerShell and cmd.exe):

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `pinned-loader-msbuild.json` | `3321c0d8de37980adcbef8f9a4d1bf755fdcbfdd605db9d93559b2748f10df1f` | `cd9c7bf67e80e65a5c2c0178a3668f7111bec75b477ceda77f336bc5f6adc88b` |
| `package-validation.json` | `eb1de8ed5d5553314f6a3286ede5e0e37b2c9074c72e2f2cc63e1126d81dfc60` | `5924df714de016fe9168944be22f178b5a077a7f023208a7f0b8e7b6df9e04f5` |
| `package-manifest.json` | `a68acd40a81b27a71044a655b629f293b0d7124d03b7b2f40c882cb7cc6d8840` | `e1adc6f517fcb8d6fb32a15e85e0cb719f8ca7c7080cd88ec9d31831ed070e49` |
| `api-audit.json` | `e23ca9155226ac001882af428949aad18fa12a6ef69fdff3d08f6fd9b8d7dade` | `fb1d0eab11e08fc29fccbbc534bf27000a83171f46ee0d3343965e34bfa71dab` |
| `loader-profile-inventory.json` | `b7fd62e64b23924a183c857f6a8998d2daaa58082166a11525fdf47ac3a7c9fb` | `50e250f7b159cdcbb7391ee87cf6d149146fba80e7c46c50553bd8235e6e90b8` |
| `onboarding-check-project.json` | `3f91e4988954c4e8fe5851f52152cbcb82e729e5f91b41cb40027e10d17aabc1` | `7ff17c563adf5656547a4379f7f1a82a52702458508a38a11743d46330c5d98a` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
