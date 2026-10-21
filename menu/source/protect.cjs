// Build-time obfuscation. This is not a replacement for server-side authorization.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
function tokens(source,visit){
 let out='',i=0;
 while(i<source.length){
  const start=i,c=source[i],next=source[i+1];
  if(c==='/'&&(next==='/'||next==='*')){
   i=next==='/'?source.indexOf('\n',i+2):source.indexOf('*/',i+2);
   if(i<0)i=source.length;else if(next==='*')i+=2;
   out+=source.slice(start,i);continue;
  }
  if((c==='@'&&next==='"')||c==='"'||c==="'"){
   const verbatim=c==='@',quote=verbatim?'"':c;i+=verbatim?2:1;
   while(i<source.length){if(source[i]===quote){if(verbatim&&source[i+1]==='"'){i+=2;continue;}i++;break;}if(!verbatim&&source[i]==='\\')i+=2;else i++;}
   out+=visit(source.slice(start,i),quote==='"'?'string':'char');continue;
  }
  if(/[A-Za-z_]/.test(c)){i++;while(i<source.length&&/[A-Za-z0-9_]/.test(source[i]))i++;out+=visit(source.slice(start,i),'identifier');continue;}
  out+=c;i++;
 }
 return out;
}
function decode(literal){
 if(literal.startsWith('@'))return literal.slice(2,-1).replace(/""/g,'"');
 return literal.slice(1,-1).replace(/\\(?:u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8}|x[0-9a-fA-F]{1,4}|.)/g,s=>{
  const c=s[1],esc={'0':'\0',a:'\x07',b:'\b',f:'\f',n:'\n',r:'\r',t:'\t',v:'\v','\\':'\\','"':'"',"'":"'"};
  if(c==='u'||c==='U'||c==='x')return String.fromCodePoint(parseInt(s.slice(2),16));
  if(!(c in esc))throw Error('Unsupported C# escape '+s);return esc[c];
 });
}
function protect(sources,out){
 const originals=sources.map(p=>[p,fs.readFileSync(p,'utf8')]);
 const salt=crypto.createHash('sha256').update(originals.map(([p,s])=>path.basename(p)+'\n'+s).join('\n')+fs.readFileSync(path.join(path.dirname(sources[0]),'vault-runtime.template.cs'),'utf8')).digest('hex');
 const values=[],ids=new Map();
 function intern(value){if(ids.has(value))return ids.get(value);const id=values.length;ids.set(value,id);values.push(value);return id;}
 const rewritten=originals.map(([p,s])=>[p,tokens(s,(value,kind)=>{
  if(kind!=='string'||!path.basename(p).startsWith('Mizze'))return value;const text=decode(value);
  // Embedded PNG payloads are already binary assets; leave them out of the text vault.
  if(!text.length||text.length>8192||path.basename(p)==='MizzeRightsNotice.cs')return value;
  return 'MzVaultGet('+intern(text)+')';
 })]);
 const vaultFiles=require('./vault-build.cjs').buildVault(values,salt,path.dirname(sources[0]),out);rewritten.push(...vaultFiles);
 const names=new Set();for(const [,s] of rewritten)tokens(s,(v,k)=>{if(k==='identifier'&&/^Mz[A-Za-z0-9_]+$/.test(v))names.add(v);return v;});
 const forward={},reverse={};for(const name of [...names].sort()){
  const mangled='Mz'+crypto.createHash('sha256').update(salt+':'+name).digest('hex').slice(0,14);if(reverse[mangled])throw Error('Symbol collision');forward[name]=mangled;reverse[mangled]=name;
 }
 const folder=path.join(out,'protected-source');fs.mkdirSync(folder,{recursive:true});
 const paths=rewritten.map(([p,s])=>{const dest=path.join(folder,path.basename(p));fs.writeFileSync(dest,tokens(s,(v,k)=>k==='identifier'&&forward[v]?forward[v]:v));return dest;});
 fs.writeFileSync(path.join(out,'symbol-map.json'),JSON.stringify(reverse,null,2)+'\n');
 fs.writeFileSync(path.join(out,'protection-report.json'),JSON.stringify({mode:'ChaCha20/HMAC-SHA256 string vault, shuffled banks and build-specific symbol obfuscation',cipher:'ChaCha20',authentication:'HMAC-SHA256 encrypt-then-MAC',banks:4,embeddedKeysRecoverable:true,decoderCacheOnce:true,strings:values.length,symbols:names.size,assetLiteralsExcluded:true,plainTextRightsNotice:true,bootstrapHooksRenamed:false,wholeFileEncryption:false,serverAuthorizationUnchanged:false,signedLicenseProtocol:9,licenseBackendDeploymentRequired:true,guaranteedTamperProof:false},null,2)+'\n');
 return {sources:paths,reverse};
}
module.exports={protect};
