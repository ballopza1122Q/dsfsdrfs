# Mizze bytes build

Edit CSharp files under menu/source, then push to main.
GitHub > Actions > Build Mizze bytes > latest successful run > Artifacts > Mizze-bytes.
The artifact contains Assembly-CSharp-patch.bytes and SHA256SUMS.txt.
GitHub login is required to download Actions artifacts.
This workflow builds bytes only. UnityFS/gzip/fileinfo packaging and release
signing remain separate steps before using a modified patch in the proxy.
No signing private keys, Firebase Admin credentials or service account files are uploaded.
Private signing credentials belong on the operator VPS, never in this repository.
The workflow does not deploy Firebase Rules or change the VPS.
The source retains its currently configured API endpoint; uploading it does not
change server addresses. Local compilation is not an iPhone/game verification.
If the repository is public, its source code is visible to everyone.