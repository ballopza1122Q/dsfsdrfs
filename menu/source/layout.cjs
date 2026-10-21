// Build-time diversification for this pinned IFix ABI. No runtime decoder or
// instruction insertion. This raises the cost of reusing fixed indices; it does
// not conceal the VM format or make local authorization unpatchable.
const crypto=require('node:crypto'),assert=require('node:assert/strict');
const C={Call:151,Callvirt:9,CallExtern:175,Newobj:105,Ldftn:69,Ldvirtftn:94,
  Ldstr:16,Ldfld:38,Stfld:4,Ldflda:87,Ldsfld:144,Stsfld:154,Ldsflda:60,
  Ldc_I4:180,Ldc_I8:51,Ldc_R8:103,Switch:102,Nop:89,
  Newanon:26,Callvirtvirt:172,Ldvirtftn2:79};
const tables=['methods','externMethods','strings','fields'];
function permutation(n,seed,label){
  const order=Array.from({length:n},(_,i)=>i);let counter=0;
  function next(bound){
    const limit=Math.floor(0x100000000/bound)*bound;
    for(;;){const v=crypto.createHmac('sha256',seed).update(label+':'+counter++).digest().readUInt32LE();if(v<limit)return v%bound;}
  }
  for(let i=n-1;i>0;i--){const j=next(i+1);[order[i],order[j]]=[order[j],order[i]];}
  if(n>1&&order.every((id,i)=>id===i))order.push(order.shift());
  const forward=Array(n);order.forEach((old,id)=>forward[old]=id);return forward;
}
function inverse(map){const r=Array(map.length);map.forEach((id,old)=>r[id]=old);return r;}
function clean(p){
  const q=structuredClone(p);for(const key of ['sha256','size','magic','fixTableFlag'])delete q[key];
  for(const m of q.methods){delete m.start;for(const x of m.instructions)delete x.offset;}
  return q;
}
function realSlots(instructions){
  const result=[];
  for(let i=0;i<instructions.length;i++){
    const x=instructions[i];if(!Number.isInteger(x.code)||x.code<0||x.code>180)throw Error('Unsupported opcode at '+i);
    result.push(i);let skip=0;
    if(x.code===C.Ldc_I8||x.code===C.Ldc_R8)skip=1;
    if(x.code===C.Switch){if(!Number.isInteger(x.operand)||x.operand<0)throw Error('Invalid switch count');skip=Math.ceil(x.operand/2);}
    if(skip>instructions.length-1-i)throw Error('Truncated inline data');i+=skip;
  }
  return result;
}
function apply(p,maps,delegateConstructors){
  if(p.statics.length||p.closures.length||p.newClasses.length)throw Error('Unsupported static/closure/class metadata');
  const q=structuredClone(p),stats={instructions:0,references:0,internalDelegatePointers:0,inlineDataSlots:0};
  function ref(table,id){const map=maps[table];if(!Number.isInteger(id)||id<0||id>=map.length)throw Error('Invalid '+table+' reference '+id);stats.references++;return map[id];}
  for(const name of tables){
    assert.equal(maps[name].length,p[name].length);assert.equal(new Set(maps[name]).size,p[name].length);
    for(const id of maps[name])assert.ok(Number.isInteger(id)&&id>=0&&id<p[name].length);
  }
  const fieldCodes=new Set([C.Ldfld,C.Stfld,C.Ldflda,C.Ldsfld,C.Stsfld,C.Ldsflda]);
  for(const m of q.methods){
    const slots=realSlots(m.instructions);stats.inlineDataSlots+=m.instructions.length-slots.length;
    for(let index=0;index<slots.length;index++){
      const x=m.instructions[slots[index]],original=x.operand;stats.instructions++;
      if([C.Newanon,C.Callvirtvirt,C.Ldvirtftn2].includes(x.code))throw Error('Unsupported anonymous method reference');
      if(x.code===C.Newobj&&delegateConstructors.has(original&0xffff)){
        const previous=index>0?m.instructions[slots[index-1]]:null;
        if(!previous)throw Error('Missing delegate pointer');
        if(previous.code===C.Ldc_I4){previous.operand=ref('methods',previous.operand);stats.internalDelegatePointers++;}
        else if(previous.code!==C.Ldftn&&previous.code!==C.Ldvirtftn)throw Error('Unsupported delegate pointer shape');
      }
      if(x.code===C.Call||x.code===C.Callvirt)x.operand=(original&0xffff0000)|ref('methods',original&0xffff);
      else if(x.code===C.Newobj||x.code===C.CallExtern)x.operand=(original&0xffff0000)|ref('externMethods',original&0xffff);
      else if(x.code===C.Ldftn||x.code===C.Ldvirtftn)x.operand=ref('externMethods',original);
      else if(x.code===C.Ldstr)x.operand=ref('strings',original);
      else if(fieldCodes.has(x.code))x.operand=ref('fields',original);
    }
  }
  for(const f of q.fields)if(f.isNew&&f.method>=0)f.method=ref('methods',f.method);
  for(const f of q.fixes)f.id=ref('methods',f.id);
  for(const name of tables){const reordered=Array(q[name].length);q[name].forEach((value,old)=>reordered[maps[name][old]]=value);q[name]=reordered;}
  return {patch:q,stats};
}
function diversify(p,seed,delegateConstructors=new Set()){
  for(const table of ['methods','externMethods'])if(p[table].length>65536)throw Error('Packed reference exceeds 16 bits');
  for(const id of delegateConstructors){assert.ok(id>=0&&id<p.externMethods.length);assert.equal(p.externMethods[id].name,'.ctor');}
  const maps=Object.fromEntries(tables.map(name=>[name,permutation(p[name].length,seed,name)]));
  const {patch,stats}=apply(p,maps,delegateConstructors);
  const backwards=Object.fromEntries(tables.map(name=>[name,inverse(maps[name])]));
  const restored=apply(patch,backwards,new Set([...delegateConstructors].map(id=>maps.externMethods[id]))).patch;
  assert.deepEqual(clean(restored),clean(p),'Layout inverse failed to recover original semantics');
  const report={schema:1,version:'R14.6',algorithm:'deterministic-table-permutation-v1',
    tables:Object.fromEntries(tables.map(name=>[name,{entries:maps[name].length,moved:maps[name].filter((id,i)=>id!==i).length}])),
    ...stats,delegateConstructors:delegateConstructors.size,inverseRoundtripVerified:true,
    insertedRuntimeInstructions:0,typesAndNativeDescriptorsUnchanged:true,clientStillReversible:true};
  return {patch,maps,report};
}
module.exports={C,clean,realSlots,apply,diversify};
