const fs=require('fs'),path=require('path'),crypto=require('crypto');
const source=path.resolve(process.argv[2]||path.join(__dirname,'../..'));
const {BALANCE_SCRIPT_FILES,GAME_SUPPORT_SCRIPT_FILES,CLIENT_INTEGRATION_SCRIPT_FILES}=require(path.join(source,'test-load-balance'));
const files=[...BALANCE_SCRIPT_FILES,'card-art-manifest.js','persona/persona-instance.js','persona/persona-condition-evaluator.js','persona/persona-effect-executor.js','persona/legacy-persona-adapter.js','persona/persona-feedback.js','persona/persona-runtime.js','poker-engine.js','stage-limit-runtime.js','shop/shop-runtime.js','deck-sort-runtime.js','run-controller.js',...GAME_SUPPORT_SCRIPT_FILES,...CLIENT_INTEGRATION_SCRIPT_FILES,'persona/persona-collection.js','game.js','save-system.js','tutorial.js'];
const out=path.resolve(process.argv[3]||path.join(__dirname,'payload/Resources/WebCore'));fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(path.join(out,'source.txt'),files.map(f=>'\n// SOURCE: '+f+'\n'+fs.readFileSync(path.join(source,f),'utf8')).join('\n'));
fs.writeFileSync(path.join(out,'provenance.json'),JSON.stringify({source,exportedAt:new Date().toISOString(),files:files.map(f=>({file:f,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(source,f))).digest('hex')}))},null,2));
for(const name of ['host','bridge'])fs.copyFileSync(path.join(__dirname,name+'.js'),path.join(out,name+'.txt'));
console.log('Exported '+files.length+' unchanged source modules.');
