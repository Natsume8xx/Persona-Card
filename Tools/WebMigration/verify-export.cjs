const fs=require('fs'),path=require('path'),crypto=require('crypto');
const out=path.resolve(process.argv[2]||'Assets/WebAligned/Resources/WebCore');
const provenance=JSON.parse(fs.readFileSync(path.join(out,'provenance.json'),'utf8'));
const source=path.resolve(process.argv[3]||provenance.source);
const text=provenance.files.map(({file,sha256})=>{
  const bytes=fs.readFileSync(path.join(source,file));
  if(crypto.createHash('sha256').update(bytes).digest('hex')!==sha256)throw Error('Web baseline changed: '+file);
  return '\n// SOURCE: '+file+'\n'+bytes.toString('utf8');
}).join('\n');
if(fs.readFileSync(path.join(out,'source.txt'),'utf8')!==text)throw Error('Generated source differs from web baseline');
for(const name of ['host','bridge'])if(fs.readFileSync(path.join(__dirname,name+'.js'),'utf8')!==fs.readFileSync(path.join(out,name+'.txt'),'utf8'))throw Error('Stale adapter export: '+name);
console.log(`Verified ${provenance.files.length} unchanged web modules and both adapter exports.`);
