// Client-side encryption increases static-analysis work; embedded keys are recoverable.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const hash=(...parts)=>crypto.createHash('sha256').update(parts.join(':')).digest();
function buildVault(values,salt,dir,out){
    const encryption=hash(salt,'vault-encryption-v2'),mac=hash(salt,'vault-authentication-v2');
    const combined=Buffer.concat([encryption,mac]),shares=[];
    for(let part=0;part<3;part++)shares.push(Buffer.concat([hash(salt,'share',part,0),hash(salt,'share',part,1)]));
    const final=Buffer.from(combined);for(const share of shares)for(let i=0;i<64;i++)final[i]^=share[i];shares.push(final);
    const nonces=new Set();
    const encoded=values.map((value,id)=>{
        const nonce=hash(salt,'nonce',id).subarray(0,12);assertUnique(nonce);
        const iv=Buffer.alloc(16);iv.writeUInt32LE(1);nonce.copy(iv,4);
        const cipher=crypto.createCipheriv('chacha20',encryption,iv);
        const header=Buffer.alloc(17);header[0]=1;header.writeUInt32LE(id,1);nonce.copy(header,5);
        const ciphertext=Buffer.concat([cipher.update(Buffer.from(value,'utf8')),cipher.final()]);
        const authenticated=Buffer.concat([header,ciphertext]);
        const tag=crypto.createHmac('sha256',mac).update(authenticated).digest();
        return Buffer.concat([authenticated,tag]).toString('base64');
    });
    function assertUnique(nonce){const n=nonce.toString('hex');if(nonces.has(n))throw Error('Vault nonce collision');nonces.add(n);}
    const order=encoded.map((_,id)=>id).sort((a,b)=>Buffer.compare(hash(salt,'slot',a),hash(salt,'slot',b)));
    let bootstrap=`namespace COW.GamePlay { public partial class SceneEditBoxSelectTool { public static void MzVaultInit(){ MzVaultEncoded=new string[${values.length}];MzVaultCache=new string[${values.length}];MzVaultKeys=new byte[64];`;
    const bankOrder=[0,1,2,3].sort((a,b)=>Buffer.compare(hash(salt,'bank',a),hash(salt,'bank',b)));
    for(const i of bankOrder)bootstrap+=`MzVaultBank${i}();`;
    bootstrap+='}\n}}\n';
    const files=[[path.join(dir,'MizzeVault.generated.cs'),fs.readFileSync(path.join(dir,'vault-runtime.template.cs'),'utf8')],
        [path.join(dir,'MizzeVaultBootstrap.generated.cs'),bootstrap]];
    for(let bank=0;bank<4;bank++){
        let source=`namespace COW.GamePlay { public partial class SceneEditBoxSelectTool { public static void MzVaultBank${bank}(){\n`;
        for(let i=0;i<64;i++)source+=`MzVaultKeys[${i}]=(byte)(MzVaultKeys[${i}]^${shares[bank][i]});`;
        source+='\n';
        for(let slot=bank;slot<order.length;slot+=4){const id=order[slot];source+=`MzVaultEncoded[${id}]=${JSON.stringify(encoded[id])};\n`;}
        source+='}\n}}\n';files.push([path.join(dir,`MizzeVaultBank${bank}.generated.cs`),source]);
    }
    fs.writeFileSync(path.join(out,'vault-test-vectors.json'),JSON.stringify(values));
    return files;
}
module.exports={buildVault};
