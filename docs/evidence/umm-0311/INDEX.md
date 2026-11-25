# UMM 0.31.1 baseline evidence

Machine-generated outputs exported 2026-11-25 15:55 UTC for candidate commit `bad0ad0cd4ab8d274352742404c5e6684ab0643a`. Each row names what produced that output (its
source commit, or the reused input it describes); outputs produced at another commit are identified there.
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

| File | Produced by | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- | --- |
| `frozen-product-source-r6.log` | bad0ad0c code, run on the uncommitted batch (37/0; counted in the budget) | `3cc054e7b54026295644e084f3156aed10e39dc4e70783435639281b2745be41` | `24a9934e611d3bb2284561e7f3d49a6bfec6aedde1acce0afa0406a941372f97` |
| `workflow-step-history-probe-r6.txt` | bad0ad0c code, run on the uncommitted batch: Update-KmcWorkflowEntry on a temporary budget root (counted in the budget) | `f1c993a8a5b33e08d821b1d85786675de45234e352b8d83373026361079ac5b9` | `9a953c6e4739d3984f41e709a40a98ccba14780f45d0f9447da8bff9054084cb` |
| `workflow-budget-umm-0311-baseline-r6.json` | read-only copy of the lab budget record umm-0311-baseline before the round-6 gate (allowance 30.62 min unchanged; round-6 direct runs imported) | `a376bd35097b9e80920c91640404cbaf17e96ff5fbbf5f96fb6d5817ea676006` | `7cae5a570c9560ca310cb2dfeaa2f8cdf43260e61713ed6edaa6ec2bfee6c893` |
| `base-game-isolation-r5.log` | d5268152 (round 5; 121/0) | `8ecb6d97c5ca190451534e4b9d7bd2caae9c34d77d7f2bdc40a66fbbc75ba401` (original output, from its first export) | `1810a5cac0af250a3720f8d022cfab7ceada3a806f6c1c62b514f57e91014de5` |
| `chunk7-native-loader-contracts-r5.log` | d5268152 (round 5; 118/0) | `5a01b6ca9f176805c4f783069b5c7f52a746058184856ddea934a8d5bd160999` (original output, from its first export) | `5a01b6ca9f176805c4f783069b5c7f52a746058184856ddea934a8d5bd160999` |
| `workflow-budget-umm-0311-baseline-r5.json` | read-only copy before the round-5 gate (round 5) | `6a72e301078b2baf41e2969360396c75f930c0cc1f3fd929e575e4f74755972a` (original output, from its first export) | `57362eccf5194e5a98bd793f33fe9427710f6aca755e0d6c1b57c16d6454cd9b` |
| `package-admission-reuse-r4.json` | dbe51d82 (round 4; disposable lab clone; 51/51; simulated enclosing workflow entry) | `ac61b3cf9e263cf7f0a8fef5b2d7ef2117e1189b67f63d597ef9d8b74c4ccf8f` (original output, from its first export) | `1fdfb85b127d4364761651b1f72eab59f29ecb990c6c2acbfbe0b01d9fb7d4c2` |
| `frozen-product-source-r4.log` | d63d4cc0 (round 4; 32/32) | `9f2fa2ba4d1169772fc63bd2d8b7ac5bd9d8433900dd9a3d2b3133a61d06b30e` (original output, from its first export) | `aa37bae4591bf4a790257a71355639605220f7e4745b860e423290e45fc3e57c` |
| `chunk7-native-loader-contracts-r4.log` | d63d4cc0 (round 4; 117/117) | `e3b16a2f107e11f9787cfbdf4559d6f017725f53ac4c284357b6597565246e1b` (original output, from its first export) | `e3b16a2f107e11f9787cfbdf4559d6f017725f53ac4c284357b6597565246e1b` |
| `workflow-budget-umm-0311-baseline.json` | read-only copy of the lab budget record before the final round-4 gate (round 4; three entries, 10.5 of 15 min; owner allowance adjustment) | `2f18d425a072a73bfbd722b68675545582ce64239077c123d02137b893128cbf` (original output, from its first export) | `f299796a0a28e07b3cd5a84c5d0f7cc47907e311fc5538f9626365580a9becb8` |
| `pinned-loader-msbuild.json` | 11df053b (round 3) | `fc8dc859da4a6ee0ea32d3f513b532783fa6cf736264d4e9e2eed6fdc014e773` (original output, from its first export) | `443c7ff2c57abf39475edf811eee0e354c078b83a7fb7b30560b9f1f58bca842` |
| `package-admission-reuse.json` | 11df053b (round 3); 45/45 | `1853012e2a098e021c1ce5ba6b43559f11fde240d263004d382f2fbeb9aaa841` (original output, from its first export) | `edb7b52d194fb777756e9b145529bcd3f17b3da18379712771b3a93fbc9acf4c` |
| `package-validation.json` | 11df053b (round 3) | `6e71a771527a66412241dc57f472b60c5cac44ab71653337f421aeabbccb9ea1` (original output, from its first export) | `37e23a78aa0a27586b221574a419ded43dcf29ccaa2594addf4b753e0854be83` |
| `package-manifest.json` | 11df053b (round 3) | `44b7c12686e10bc42f777f4785aa8f3542cf3e7e8ef8cb7d9f8a3170380fd8c1` (original output, from its first export) | `35250fb88cf0bf53dcac0ff2b6479a4eb28e4e9dd411a8524087d775ff8124b4` |
| `observer-package-validation.json` | 11df053b (round 3) | `5d0ae0b5a4687d62fef066de4aeccc6977ae6efe08a0f027cf14ee1d92b3d057` (original output, from its first export) | `2ae93fb98a0765012c65f5b70c64f959be220400e6ba91ba4767e677a33940ed` |
| `observer-package-manifest.json` | 11df053b (round 3) | `8a597d81401cb961a9428cfb394a83635d7f153de5ba4eb87c4109a6c48e1f94` (original output, from its first export) | `743ef081da5615c6652d20d09a1826203f5dc635b10cbcc153c6536d46a4f64f` |
| `chunk7-native-loader-contracts.log` | 11df053b (round 3); 116/116 | `c88bbbc74594096b25d5f6735714f0de33f5f019d4136c97aecef1ae5bb23f76` (original output, from its first export) | `c88bbbc74594096b25d5f6735714f0de33f5f019d4136c97aecef1ae5bb23f76` |
| `frozen-product-source.log` | 11df053b (round 3); 28/28 | `f2efad2ecc1c5484d20b38d6215e2fd35e5a7481023fe742cbe74f17fb073533` (original output, from its first export) | `7d2d05f4bd62edcde9d129fd9a2f15a44b2cc1a3d9c59d9bb8bacb7863f4a07a` |
| `umm-loader-contract-checks.log` | 11df053b (round 3) | `b8ef07a7592cdd528baf4fe21ea4b9aef55d1615f411e2e159ee646f0064ba8b` (original output, from its first export) | `cdb3581887f2308c0742862c45f2388c2611364c60a7662f458e731921ffe58f` |
| `test-harness-summary.json` | 11df053b (run at e4e32899, docs and evidence only after 11df053b), with the f5a9f386 baseline | `48dc2496a3e3fb749b7f71934cfb422445b4a548e1679bd9a192d769022a84fd` (original output, from its first export) | `0a92876b0884195715bdc7e091e501f2ee82483d5a381947e88055b941fd06c5` |
| `api-audit.json` | reused static audit of the unchanged DLL d20474bf, recorded at f5a9f386 | `1e14f1bebceb5e393708dfea190ffbe976dbdf2984b17e04d28f6d59b64ae051` (original output, from its first export) | `4f39becf0603b584cd24e2e89e7b332fdab17bad9302ef8bf90b804a2af5b084` |
| `observer-api-audit.json` | reused static audit of the unchanged observer DLL f9697f91, recorded at f5a9f386 | `947bef18032f59a5ad94bf75960872a7d7a65929279eeba13b9dc44a9ee08c35` (original output, from its first export) | `7afb1eccfba7902df439a2bb7d28422c2ceb8db055f5caff568e0573cbe7dbeb` |
| `loader-profile-inventory.json` | restored unchanged: measured 2026-11-24T03:41:11Z for candidate 272238ce (Git blob a9ef6700 of 215d68fe) | `b7fd62e64b23924a183c857f6a8998d2daaa58082166a11525fdf47ac3a7c9fb` (original output, from its first export) | `50e250f7b159cdcbb7391ee87cf6d149146fba80e7c46c50553bd8235e6e90b8` |
| `onboarding-check-project.json` | controller check-project record from enrollment (reused) | `3f91e4988954c4e8fe5851f52152cbcb82e729e5f91b41cb40027e10d17aabc1` (original output, from its first export) | `7ff17c563adf5656547a4379f7f1a82a52702458508a38a11743d46330c5d98a` |
| `loader-profiles.md` | fleet loader-profile record (reused) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` (original output, from its first export) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | audit tool source (reused) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` (original output, from its first export) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | audit tool project (reused) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` (original output, from its first export) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
