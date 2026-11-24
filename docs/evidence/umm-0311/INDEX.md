# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `f5a9f3865a4e79432c85d67a17b3aaaf0c7e1b3f`, exported 2026-11-24 04:21 UTC.
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
| `pinned-loader-msbuild.json` | `dc9086d781e227f56240e7d74cc8a7bb2e752dfd25e05b7194c68792f905756d` | `1828e85b672d2d8cd0eada7005605fe924a981b4132a68fde45cb7eb853d061c` |
| `package-admission-reuse.json` | `abe6b9299ce344bc9033f1c8d91ed30eece6ff44b135ec78d46e79b1eeb9d46b` | `6d1acb034be902fac6fff093e337db3b7eef1b257702af79fb273b61a892c5d4` |
| `package-validation.json` | `3c5ad8f0f8d73f5ceb1c88893022b93d8d6c6447bb27145c2d08d142aa0b35f9` | `9fc3fa7312a176e6ed1627f11aadca84b48502849daa09b7812aa7ab3927faea` |
| `package-manifest.json` | `4ebdfcbdc0a5b2f534b340b8c3cdf79dde012e558423d856b424a53c7782a2ea` | `0ad45926bb95ed00c78931339b66547b8be6f3b2d2ae73cc6f1af6cde5167c2e` |
| `observer-package-validation.json` | `86d08eed658f845ac50489dd8f26dfb4129a5daba39e7b4b685405a24be248c1` | `ea55733472d4a2d19271ac960d54b247ee8c02c972f3131bab58d8920e9808bf` |
| `observer-package-manifest.json` | `6dcb07b4b34963b5848cae0794a1c9444f6d0f1721c133e0de106ea01d007c43` | `e7471e8fcc5712cabd5589995751b3745dff8fb05dbbd7c42d2495c72c4cc046` |
| `api-audit.json` | `1e14f1bebceb5e393708dfea190ffbe976dbdf2984b17e04d28f6d59b64ae051` | `4f39becf0603b584cd24e2e89e7b332fdab17bad9302ef8bf90b804a2af5b084` |
| `observer-api-audit.json` | `947bef18032f59a5ad94bf75960872a7d7a65929279eeba13b9dc44a9ee08c35` | `7afb1eccfba7902df439a2bb7d28422c2ceb8db055f5caff568e0573cbe7dbeb` |
| `test-harness-summary.json` | `301866d496d3bc9b8af36f6cea4c7c239b02969d7e4632bcfe221376821d1f54` | `7a95cb63e5e2f05b32552a14ea0bf58d2597f79012c7aedd58c9ea0dd1ca043a` |
| `onboarding-check-project.json` | `3f91e4988954c4e8fe5851f52152cbcb82e729e5f91b41cb40027e10d17aabc1` | `7ff17c563adf5656547a4379f7f1a82a52702458508a38a11743d46330c5d98a` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
