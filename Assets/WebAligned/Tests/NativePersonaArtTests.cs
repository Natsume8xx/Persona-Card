using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    // AI 人格配图（策划分类规则）：桥接=桥接、偏转=破局、映照=顺势。
    // 生成时按内部分类随机取图写入 template.portrait（随存档持久化，渲染不取随机避免闪变）；
    // 旧存档读档（runController.restoreRun）与永久收藏记录（PersonaCollection.backfillPortraits）自动补图。
    // 8 张基础人格牌立绘为文件级替换（persona-01~08-original-v1.png 覆盖），路径不变，无需断言。
    public class NativePersonaArtTests
    {
        NativeRules rules;
        [SetUp] public void Setup(){rules=new NativeRules(false);}
        [TearDown] public void Teardown(){rules.Dispose();}
        // EvaluateForTest 返回 engine.Evaluate(...).ToString()；JS 侧一律返回字符串，空结果用 '__none__' 占位。
        string Eval(string expression)=>rules.EvaluateForTest(expression)??"";

        [Test] public void ArtPoolsCoverAllThreeDirections()
        {
            var expr="(function(){var p=globalThis.AiPersonaPortraitPool;var errors=[];"+
                "if(!p)return 'missing pool';"+
                "var expect={'AI_DIRECTION_BRIDGE':'ai-bridge','AI_DIRECTION_BREAK':'ai-deflect','AI_DIRECTION_FOLLOW':'ai-mirror'};"+
                "for(var dir in expect){var pool=p.poolFor(dir);if(pool.length!==11)errors.push(dir+' size '+pool.length);"+
                "for(var i=0;i<pool.length;i++)if(pool[i].indexOf('personas-v2/'+expect[dir]+'-')!==0)errors.push(dir+' bad '+pool[i]);"+
                "for(var i=0;i<200;i++){var r=p.randomPortraitFor(dir);if(r.indexOf('assets/art/personas-v2/'+expect[dir]+'-')!==0||r.indexOf('.png')!==r.length-4)errors.push(dir+' random '+r)}};"+
                "if(p.randomPortraitFor('AI_DIRECTION_UNKNOWN')!==null)errors.push('unknown direction returned art');"+
                "return errors.length?errors.join('|'):'OK'})()";
            Assert.AreEqual("OK",Eval(expr));
        }

        // 推进到第一个成长节点：每场战斗打一手并直接胜利结算（与命名测试同一路径）
        JObject EnterGrowthNode()
        {
            rules.Action("start");rules.Action("click","#stage-start");
            for(int guard=0;guard<20;guard++)
            {
                var s=rules.Snapshot();
                switch((string)s["dialog"])
                {
                    case "#persona-growth-dialog":return s;
                    case "#stage-intro-dialog":rules.Action("click","#stage-start");break;
                    case "#native-tutorial":rules.Action("click","#tutorial-skip");break;
                    case "#settlement-dialog":rules.Action("click","#result-primary");break;
                    case "":
                        rules.Action("toggle",index:0);rules.Action("play");
                        rules.EvaluateForTest("score=battleTarget();finish(true);__flush();");
                        break;
                    default:Assert.Fail("Unexpected screen: "+s["dialog"]);break;
                }
            }
            Assert.Fail("未在 20 步内到达成长节点");
            return null;
        }

        [Test] public void GeneratedPersonaGetsRandomArtFromItsDirectionPool()
        {
            EnterGrowthNode();
            var s=rules.Snapshot();
            var portrait=(string)s["growth"]["persona"]["portrait"];
            StringAssert.StartsWith("assets/art/personas-v2/ai-",portrait??"","成长节点快照应带随机配图");
            // N04 方向为桥接或破局，配图前缀必须与方向一致
            var dir=Eval("(function(){var a=runController.getAiPersonaDirectionAssignments();return a&&a['N04']?a['N04']:'__none__'})()");
            Assert.AreNotEqual("__none__",dir);
            var expectedPrefix=dir=="AI_DIRECTION_BRIDGE"?"ai-bridge-":dir=="AI_DIRECTION_BREAK"?"ai-deflect-":"ai-mirror-";
            StringAssert.StartsWith("assets/art/personas-v2/"+expectedPrefix,portrait);
            // 模板内部方向与节点分配一致，且配图已写入模板（而非仅在快照层）
            var meta=Eval("(function(){var st=personaRuntime.getState();var inst=st.personaInstancesById[currentGrowthInstanceId];var t=inst&&personaRuntime.getTemplate(inst.templateId);return t&&t.aiPersonaMeta?t.aiPersonaMeta.internalDirectionId+':'+t.portrait:'__none__'})()");
            Assert.IsTrue(meta.StartsWith(dir+":"),"模板方向与配图应为 "+dir);
            StringAssert.StartsWith("assets/art/personas-v2/"+expectedPrefix,meta.Split(':')[1]);
        }

        [Test] public void RestoreRunBackfillsLegacyAiTemplates()
        {
            rules.Action("start");
            var expr="(function(){var st=JSON.parse(JSON.stringify(runController.serializeState()));"+
                // 旧版无图 AI 人格（顺势），读档应补 ai-mirror 随机图
                "st.dynamicPersonaTemplatesById['AI_LEGACY_FOLLOW']={id:'AI_LEGACY_FOLLOW',name:'AI人格99',mode:'人格',tone:'#725A3A',portrait:null,aiPersonaMeta:{schemaVersion:1,playerFacing:false,internalDirectionId:'AI_DIRECTION_FOLLOW'}};"+
                // 已有配图的不动；无 aiPersonaMeta 的非 AI 模板不动
                "st.dynamicPersonaTemplatesById['AI_WITH_ART']={id:'AI_WITH_ART',name:'AI人格98',mode:'人格',tone:'#725A3A',portrait:'assets/art/keep.png',aiPersonaMeta:{schemaVersion:1,playerFacing:false,internalDirectionId:'AI_DIRECTION_BRIDGE'}};"+
                "st.dynamicPersonaTemplatesById['BASE_NO_META']={id:'BASE_NO_META',name:'无元数据模板',portrait:null};"+
                "runController.restoreRun(st);"+
                "var got=runController.getState().dynamicPersonaTemplatesById;"+
                "var a=got['AI_LEGACY_FOLLOW']&&got['AI_LEGACY_FOLLOW'].portrait||'';var b=got['AI_WITH_ART']&&got['AI_WITH_ART'].portrait||'';var c=got['BASE_NO_META']&&got['BASE_NO_META'].portrait;"+
                "return JSON.stringify({follow:a,kept:b,base:c===null})})()";
            var result=JObject.Parse(Eval(expr));
            StringAssert.StartsWith("assets/art/personas-v2/ai-mirror-",(string)result["follow"]);
            Assert.AreEqual("assets/art/keep.png",(string)result["kept"]);
            Assert.IsTrue((bool)result["base"],"无 aiPersonaMeta 的模板不应被补图");
        }

        [Test] public void CollectionBackfillFillsLegacyRecords()
        {
            var expr="(function(){"+
                "var aiRec={collectionId:'CARRY_legacy_ai',cardId:'CARRY_legacy_ai',templateId:'AI_LEGACY_COL',source:'CARRY_OUT',templateSnapshot:{id:'AI_LEGACY_COL',name:'AI人格97',mode:'人格',portrait:null,aiPersonaMeta:{internalDirectionId:'AI_DIRECTION_BRIDGE'}},acquiredAt:1,originalInstanceId:'persona-9',acquiredRunTemplateId:'RUN_TEMPLATE_TARGET',acquiredAtNodeId:'N04',runsSinceLastUsed:0,version:1};"+
                "var baseRec={collectionId:'CARRY_legacy_base',cardId:'CARRY_legacy_base',templateId:'observer',source:'CARRY_OUT',templateSnapshot:{id:'observer',name:'人格牌01',portrait:null},acquiredAt:1,originalInstanceId:'persona-8',acquiredRunTemplateId:'RUN_TEMPLATE_TARGET',acquiredAtNodeId:'N01',runsSinceLastUsed:0,version:1};"+
                "localStorage.setItem('persona-permanent-collection-v1',JSON.stringify({version:1,records:[aiRec,baseRec]}));"+
                // 用引擎内全局 personaCollection 验证真实路径：模块加载期首读把缓存钉在空集合上
                // （Unity 更晚注入 localStorage），backfillPortraits 必须先失效缓存才能看到注入的收藏记录
                "var coll=personaCollection;"+
                "var first=coll.backfillPortraits();var records=coll.list();"+
                "var ai=records.filter(function(r){return r.collectionId==='CARRY_legacy_ai'})[0];var base=records.filter(function(r){return r.collectionId==='CARRY_legacy_base'})[0];"+
                "var second=coll.backfillPortraits();"+
                "return JSON.stringify({firstChanged:first.changed===true,secondChanged:second.changed===false,aiFound:!!ai,aiArt:ai&&ai.templateSnapshot.portrait||'',baseStillNull:!!base&&base.templateSnapshot.portrait===null,count:records.length})})()";
            var result=JObject.Parse(Eval(expr));
            Assert.IsTrue((bool)result["firstChanged"],"首次补图应发生写入");
            Assert.IsTrue((bool)result["secondChanged"],"补图应幂等，二次调用不再写入");
            Assert.IsTrue((bool)result["aiFound"],"缓存失效后应读到注入的 AI 收藏记录");
            StringAssert.StartsWith("assets/art/personas-v2/ai-bridge-",(string)result["aiArt"]);
            Assert.IsTrue((bool)result["baseStillNull"],"基础人格收藏记录不应被补图");
            Assert.AreEqual(10,(int)result["count"],"list() = 8 张初始基础人格 + 2 条注入收藏");
        }
    }
}
