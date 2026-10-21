// Validate generated extern descriptors against the supplied declaration dump.
const fs=require('node:fs'),path=require('node:path');const {inspect}=require('../inspect_patch.cjs');
const p=inspect(fs.readFileSync(path.join(__dirname,'dist/Assembly-CSharp-patch.bytes'))),root=process.argv[2];
const cp=require('node:child_process'),build=path.join(__dirname,'build'),probe=path.join(build,'BindingFieldTypes.exe');
const compiled=cp.spawnSync(path.join(process.env.WINDIR||'C:/Windows','Microsoft.NET/Framework64/v4.0.30319/csc.exe'),['/nologo','/target:exe','/out:'+probe,'/reference:'+path.join(build,'Mono.Cecil.dll'),path.join(__dirname,'BindingFieldTypes.cs')],{encoding:'utf8'});
if(compiled.status!==0)throw Error('Cannot inspect declaration types: '+compiled.stdout+compiled.stderr);
const names=[...new Set(p.fields.filter(f=>!f.isNew).map(f=>f.owner.match(/, ([^,]+), Version=/)[1]))];
const probed=cp.spawnSync(probe,names.map(n=>path.join(build,n+'.dll')),{encoding:'utf8'});
if(probed.status!==0)throw Error('Field metadata probe failed: '+probed.stderr);
const fieldTypes=new Map(probed.stdout.trim().split(/\r?\n/).filter(Boolean).map(line=>{const [asm,owner,name,type]=line.split('\t');return [asm+'|'+owner+'|'+name,type];}));
const aliases={'System.Single':'float','System.Double':'double','System.Boolean':'bool','System.Int16':'short','System.Int32':'int','System.Int64':'long','System.Byte':'byte','System.String':'string','System.Char':'char','System.Object':'object','System.Type':'Type'};
function bare(t){return t.split(',')[0].replace(/\+/g,'.');}
function short(t){t=bare(t);if(t.endsWith('&'))return short(t.slice(0,-1))+'&';if(t.endsWith('[]'))return short(t.slice(0,-2))+'[]';return aliases[t]||t.slice(t.lastIndexOf('.')+1);}
const files=new Map(),evidence=[],errors=[];
for(const e of p.externMethods){
 const asm=e.owner.match(/, ([^,]+), Version=/)[1];if(!files.has(asm))files.set(asm,fs.readFileSync(path.join(root,asm+'.dll.cs'),'utf8'));
 const text=files.get(asm),owner=bare(e.owner).replace(/`1.*$/,'<T>'),ns=owner.slice(0,owner.lastIndexOf('.')),name=owner.slice(owner.lastIndexOf('.')+1);
 const chunks=text.split('// Namespace: ');let chunk=chunks.find(c=>c.startsWith(ns+'\n')&&new RegExp('(?:class|struct|interface|enum) '+name.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')+'(?: :| //|\\s*\\n)').test(c));
 // The declaration dump flattens nested classes. Validate this one against
 // the supplied metadata relationship, then use its unique flattened chunk.
 if(!chunk&&owner==='UnityEngine.Camera.CameraCallback'){
  const proof=JSON.parse(fs.readFileSync(path.join(__dirname,'render-evidence.json')));
  if(proof.fullName!=='UnityEngine.Camera+CameraCallback'||proof.CameraByValTypeIndex!==proof.CallbackDeclaringTypeIndex)throw Error('Invalid camera callback metadata evidence');
  const nested=chunks.filter(c=>c.startsWith('\n')&&/public sealed class CameraCallback : MulticastDelegate/.test(c));
  if(nested.length===1&&chunks.some(c=>c.startsWith('UnityEngine\n')&&/public sealed class Camera :/.test(c)&&/public static CameraCallback onPreCull;/.test(c)))chunk=nested[0];
 }
 if(!chunk){errors.push('Missing owner '+e.owner);continue;}
 const candidates=chunk.split('\n').map(l=>l.trim()).filter(l=>l.includes(' '+e.name+'(')&&l.includes('('));
 const hit=candidates.find(l=>{const ps=l.slice(l.indexOf('(')+1,l.lastIndexOf(')')).trim();const a=ps?ps.split(',').map(q=>q.trim().replace(/\s+\w+(?:\s*=.*)?$/,'').replace(/^(?:ref|out) /,'').replace(/\?$/,'')):[];return a.length===e.parameters.length&&a.every((q,i)=>q===short(e.parameters[i]).replace(/&$/,'')||(q==='object'&&bare(e.parameters[i])==='UnityEngine.Object'));});
 if(!hit){errors.push('Missing signature '+bare(e.owner)+'.'+e.name+'('+e.parameters.map(short).join(',')+') candidates '+candidates.join('; '));continue;}
 evidence.push({assembly:asm,owner:bare(e.owner),method:e.name,parameters:e.parameters,declaration:hit});
}
for(const f of p.fields.filter(f=>!f.isNew)){
 const asm=f.owner.match(/, ([^,]+), Version=/)[1];if(!files.has(asm))files.set(asm,fs.readFileSync(path.join(root,asm+'.dll.cs'),'utf8'));const text=files.get(asm),owner=bare(f.owner),ns=owner.slice(0,owner.lastIndexOf('.')),name=owner.slice(owner.lastIndexOf('.')+1);
 const chunk=text.split('// Namespace: ').find(c=>c.startsWith(ns+'\n')&&new RegExp('(?:class|struct) '+name+'(?: :| //)').test(c));const line=chunk&&chunk.split('\n').find(l=>l.includes(' '+f.name+';'));
 if(!line)errors.push('Missing field '+owner+'.'+f.name);else {
  const type=fieldTypes.get(asm+'|'+owner+'|'+f.name);
  const declaration=line.trim().split('//')[0].replace(/\s*;\s*$/,'');
  const actual=declaration.slice(0,declaration.lastIndexOf(f.name)).trim().replace(/^(?:(?:public|private|protected|internal|static|readonly|const)\s+)+/,'');
  if(!type||short(type)!==actual)errors.push('Field type mismatch '+owner+'.'+f.name+': compiled '+type+', native '+actual);
  else evidence.push({assembly:asm,owner,field:f.name,fieldType:type,nativeFieldType:actual,declaration:line.trim()});
 }
}
if(errors.length){console.error(errors.join('\n'));process.exit(1);}fs.writeFileSync(path.join(__dirname,'bindings-evidence.json'),JSON.stringify(evidence,null,2)+'\n');console.log('PASS: '+evidence.length+' extern/field declarations match TH iOS dump.');
