// Convert the pinned upstream serializer's container to the supplied game variant.
// Instructions and helper methods are entirely compiled from this directory's C#.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {inspect}=require('../inspect_patch.cjs');
const dir=__dirname,upstream=fs.readFileSync(path.join(dir,'build/upstream.bytes')),original=inspect(upstream),parts=[];
const layout=require('./layout.cjs');
// Check against the pinned VM, rather than silently trusting a different opcode ABI.
const enumSource=fs.readFileSync(path.join(dir,'../runtime-test/Instruction.cs'),'utf8');
for(const [name,value] of Object.entries(layout.C))if(!new RegExp('\\b'+name+'\\s*=\\s*'+value+'\\s*[,}]').test(enumSource))throw Error('Opcode ABI changed: '+name);
const delegateIds=new Set(fs.readFileSync(path.join(dir,'build/delegate-constructors.tsv'),'utf8').trim().split(/\r?\n/).filter(Boolean).map(Number));
const seed=crypto.createHash('sha256').update('Mizze-layout-R14.6\0').update(upstream).digest();
const diversified=layout.diversify(original,seed,delegateIds),p=diversified.patch;
function i(n){const b=Buffer.alloc(4);b.writeInt32LE(n);parts.push(b);}function bit(v){parts.push(Buffer.from([v?1:0]));}
function s(v){const b=Buffer.from(v);let n=b.length;while(n>=128){parts.push(Buffer.from([(n&127)|128]));n>>>=7;}parts.push(Buffer.from([n]),b);}
function list(a,f){i(a.length);a.forEach(f);}function t(v){const n=p.types.indexOf(v);if(n<0)throw Error('Unknown type '+v);i(n);}
function method(m){bit(m.generic);t(m.owner);s(m.name);if(m.generic)list(m.args,t);list(m.parameters,(v,index)=>{if(m.generic)bit(m.parameterIsGeneric[index]);if(m.generic&&m.parameterIsGeneric[index])s(v);else t(v);});}
if(p.closures.length||p.statics.length||p.newClasses.length)throw Error('Unexpected closure/static/class metadata');
const magic=Buffer.alloc(8);magic.writeBigUInt64LE(0x66a319b20db4a2cbn);parts.push(magic);s(p.bridge);list(p.types,s);
list(p.methods,m=>{list(m.instructions,x=>{i(x.code);i(x.operand);});list(m.handlers,h=>h.forEach(i));});
list(p.externMethods,method);list(p.strings,s);list(p.fields,f=>{bit(f.isNew);t(f.owner);s(f.name);if(f.isNew){t(f.type);i(f.method);}});
i(0);i(0);s(p.wrappers);s(p.assembly);bit(true);list(p.fixes,f=>{method(f.method);i(f.id);});i(0);
const bytes=Buffer.concat(parts),result=inspect(bytes),out=path.join(dir,'dist');fs.mkdirSync(out,{recursive:true});
require('node:assert/strict').deepEqual(layout.clean(result),layout.clean(p),'Serialized layout changed semantics');
if(bytes.length!==upstream.length+1)throw Error('Unexpected bytecode size change');
fs.writeFileSync(path.join(out,'Assembly-CSharp-patch.bytes'),bytes);fs.writeFileSync(path.join(out,'SHA256SUMS.txt'),result.sha256+'  Assembly-CSharp-patch.bytes\n');
const methodRows=fs.readFileSync(path.join(dir,'build/methods.tsv'),'utf8').trim().split(/\r?\n/).filter(Boolean);
if(methodRows.length!==p.methods.length)throw Error('Incomplete private method map');
const seen=new Set();
const remappedRows=methodRows.map(line=>{const row=line.split('\t'),old=Number(row[0]);if(row.length!==3||!Number.isInteger(old)||old<0||old>=p.methods.length||seen.has(old))throw Error('Invalid private method map');seen.add(old);row[0]=String(diversified.maps.methods[old]);return row;}).sort((a,b)=>Number(a[0])-Number(b[0]));
fs.writeFileSync(path.join(out,'methods.tsv'),remappedRows.map(row=>row.join('\t')).join('\n')+'\n');
fs.writeFileSync(path.join(dir,'build/layout-map.json'),JSON.stringify(diversified.maps));
fs.writeFileSync(path.join(out,'layout-report.json'),JSON.stringify({...diversified.report,patchSha256:result.sha256},null,2)+'\n');
fs.writeFileSync(path.join(out,'manifest.json'),JSON.stringify({size:result.size,sha256:result.sha256,magic:result.magic,methods:result.methods.length,externMethods:result.externMethods.length,newFields:result.fields.filter(f=>f.isNew).length,fixes:result.fixes,source:'menu/source/*.cs',target:'Free Fire TH iOS 1.132.1',deviceVerified:false},null,2)+'\n');
console.log('Packaged Mizze: '+result.size+' bytes; SHA256 '+result.sha256);

for(const name of ['symbol-map.json','protection-report.json'])fs.copyFileSync(path.join(dir,'build',name),path.join(out,name));
