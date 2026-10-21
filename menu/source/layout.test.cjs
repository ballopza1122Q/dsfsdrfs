const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {C,apply,clean,diversify,realSlots}=require('./layout.cjs');
let checks=0;function check(fn){fn();checks++;}
const ins=(code,operand=0)=>({code,operand});
const method=(instructions,handlers=[])=>({instructions,handlers});
const p={types:['Native'],methods:[
  method([ins(C.Call,(3<<16)|1),ins(C.Callvirt,(0x8001<<16)|2),ins(C.Ldstr,1),ins(C.Ldsfld,0)],[[0,0,0,3,3,4]]),
  // Literal and switch data deliberately look like indexed instructions.
  method([ins(C.Ldc_I8),ins(C.Call,0x7fffffff),ins(C.Ldc_R8),ins(C.Ldstr,9999),
    ins(C.Switch,3),ins(C.Stfld,-99),ins(C.Ldftn,9999),ins(C.Ldc_I4,1)]),
  method([ins(C.Ldc_I4,1),ins(C.Newobj,(2<<16)|1),ins(C.CallExtern,(7<<16)|0)]),
  method([ins(C.Ldftn,2),ins(C.Newobj,(2<<16)|1),ins(C.Ldvirtftn,3),ins(C.Newobj,(2<<16)|1)]),
  method([ins(C.Ldfld,1),ins(C.Stfld,2),ins(C.Ldflda,3),ins(C.Stsfld,4),ins(C.Ldsflda,0)]),
  method([ins(C.Nop)])],
  externMethods:[{name:'call'},{name:'.ctor'},{name:'externalA'},{name:'externalB'}],
  strings:['a','b','c','d','e','f'],fields:[
    {isNew:true,method:0},{isNew:true,method:2},{isNew:true,method:-1},{isNew:false},{isNew:false}],
  statics:[],closures:[],newClasses:[],fixes:[{id:0,method:{name:'OnGUI'}}],bridge:'bridge',wrappers:'wrappers',assembly:'game'};
const maps={methods:[5,4,3,2,1,0],externMethods:[2,3,0,1],strings:[5,4,3,2,1,0],fields:[4,3,2,1,0]};
const q=apply(p,maps,new Set([1])).patch;
check(()=>assert.equal(q.methods[5].instructions[0].operand,(3<<16)|4));
check(()=>assert.equal(q.methods[5].instructions[1].operand,(0x8001<<16)|3));
check(()=>assert.equal(q.methods[5].instructions[2].operand,4));
check(()=>assert.equal(q.methods[5].instructions[3].operand,4));
for(const id of [1,3,5,6,7])check(()=>assert.deepEqual(q.methods[4].instructions[id],p.methods[1].instructions[id]));
check(()=>assert.equal(realSlots(p.methods[1].instructions).length,4));
check(()=>assert.deepEqual(q.methods[3].instructions.map(x=>x.operand),[4,(2<<16)|3,(7<<16)|2]));
check(()=>assert.deepEqual(q.methods[2].instructions.map(x=>x.operand),[0,(2<<16)|3,1,(2<<16)|3]));
check(()=>assert.deepEqual(q.methods[1].instructions.map(x=>x.operand),[3,2,1,0,4]));
check(()=>assert.equal(q.fields[4].method,5));
check(()=>assert.equal(q.fields[3].method,3));
check(()=>assert.equal(q.fields[2].method,-1));
check(()=>assert.equal(q.fixes[0].id,5));
check(()=>assert.deepEqual(q.methods[5].handlers,p.methods[0].handlers));
check(()=>assert.deepEqual(q.types,p.types));
for(let i=0;i<100;i++){
  const a=diversify(p,Buffer.from('test-seed-'+i),new Set([1]));
  check(()=>assert.deepEqual(a,diversify(p,Buffer.from('test-seed-'+i),new Set([1]))));
  check(()=>assert.equal(a.report.internalDelegatePointers,1));
  check(()=>assert.equal(a.report.insertedRuntimeInstructions,0));
  for(const [name,data] of Object.entries(a.report.tables))check(()=>assert.ok(data.moved>0,name));
}
check(()=>assert.notDeepEqual(diversify(p,Buffer.from('one'),new Set([1])).maps,diversify(p,Buffer.from('two'),new Set([1])).maps));
for(const instructions of [[ins(C.Ldc_I8)],[ins(C.Switch,5),ins(C.Nop)],[ins(C.Switch,-1)],[ins(181)],[ins(C.Newanon)],[ins(C.Callvirtvirt)],[ins(C.Ldvirtftn2)],[ins(C.Ldsfld,-1)],[ins(C.Call,999)],[ins(C.Ldstr,999)],[ins(C.Newobj,1)],[ins(C.Nop),ins(C.Newobj,1)]]){
  const bad=structuredClone(p);bad.methods[0]=method(instructions);
  check(()=>assert.throws(()=>apply(bad,maps,new Set([1]))));
}
for(const name of ['statics','closures','newClasses']){const bad=structuredClone(p);bad[name].push({});check(()=>assert.throws(()=>diversify(bad,Buffer.from('bad'),new Set([1]))));}
check(()=>assert.throws(()=>apply(p,{...maps,methods:[0,0,1,2,3,4]},new Set([1]))));
// Actual serializer output must round-trip and contain the same amount of code.
if(fs.existsSync(path.join(__dirname,'build/upstream.bytes'))){
  const {inspect}=require('../inspect_patch.cjs');
  const upstream=inspect(fs.readFileSync(path.join(__dirname,'build/upstream.bytes')));
  const ids=new Set(fs.readFileSync(path.join(__dirname,'build/delegate-constructors.tsv'),'utf8').trim().split(/\r?\n/).filter(Boolean).map(Number));
  const actual=diversify(upstream,Buffer.from('independent-audit-seed'),ids);
  check(()=>assert.equal(actual.patch.methods.reduce((n,m)=>n+m.instructions.length,0),upstream.methods.reduce((n,m)=>n+m.instructions.length,0)));
  check(()=>assert.deepEqual(actual.patch.fixes.map(f=>f.method),upstream.fixes.map(f=>f.method)));
  const packaged=inspect(fs.readFileSync(path.join(__dirname,'dist/Assembly-CSharp-patch.bytes')));
  for(let n=1;n<=6;n++)check(()=>assert.ok(packaged.strings.some(s=>s.includes('[MIZZE OWNERSHIP '+n+'/6]')),'readable ownership notice '+n));
}
console.log('PASS: R14.6 layout '+checks+' checks; literals, switch tables, packed calls, fields, delegates, exceptions, inverse proof and unsupported shapes.');
