using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PersonaCards.WebAligned.Tests
{
    // AI 人格命名（规格 V1.1）页面侧验证：
    // 冻结 JS 的 name client / displayNameOf 与共享向量 PersonaNameValidationCases.json 一致，
    // headless（allowNetwork=false）下成长节点走 NETWORK_ERROR → FALLBACK 写入，
    // AI_NAMED 显示名贯通成长卡/详情/装备位/战斗事件，战斗事件 source 首段与桥接人格名保持同源
    //（NativeGameView.cs 的事件匹配依赖该不变量）。
    public class NativePersonaNameTests
    {
        NativeRules rules;
        JObject vectors;
        static string VectorsPath=>Path.Combine(Application.dataPath,"WebAligned","Tests","PersonaNameValidationCases.json");
        [SetUp] public void Setup(){rules=new NativeRules(false);vectors=JObject.Parse(File.ReadAllText(VectorsPath));}
        [TearDown] public void Teardown(){rules.Dispose();}
        // EvaluateForTest 返回 engine.Evaluate(...).ToString()；JS 侧一律返回字符串，空结果用 '__none__' 占位。
        string Eval(string expression)=>rules.EvaluateForTest(expression)??"";

        [Test] public void PageSideConstantsAndValidationMatchSharedVectors()
        {
            var expr="(function(){var v="+JsonConvert.SerializeObject(vectors)+";var c=globalThis.AiPersonaNameClient;var errors=[];"+
                "if(!c)return 'missing client';"+
                "if(c.NAME_STYLE_VERSION!==v.styleVersion)errors.push('styleVersion');"+
                "if(c.DEFAULT_TIMEOUT_MS!==3000)errors.push('timeout');"+
                "var full=c.fullReservedNames();if(full.length!==60)errors.push('reserved count '+full.length);"+
                "for(var i=0;i<v.baseReservedNames.length;i++)if(full.indexOf(v.baseReservedNames[i])<0)errors.push('base '+v.baseReservedNames[i]);"+
                "for(var i=0;i<v.bossBaseNames.length;i++)if(full.indexOf(v.bossBaseNames[i])<0)errors.push('boss '+v.bossBaseNames[i]);"+
                "for(var i=0;i<v.bossReservedInvalidNames.length;i++)if(full.indexOf(v.bossReservedInvalidNames[i])<0)errors.push('bossvariant '+v.bossReservedInvalidNames[i]);"+
                "for(var i=0;i<v.formatValidNames.length;i++){var n=v.formatValidNames[i];if(!c.validateDisplayName(n).ok)errors.push('valid rejected '+n);if(!c.validateDisplayName(n,{reservedNames:full}).ok)errors.push('valid reserved '+n)}"+
                "for(var i=0;i<v.formatInvalidNames.length;i++)if(c.validateDisplayName(v.formatInvalidNames[i]).ok)errors.push('invalid passed '+JSON.stringify(v.formatInvalidNames[i]));"+
                "for(var i=0;i<v.reservedInvalidNames.length;i++)if(c.validateDisplayName(v.reservedInvalidNames[i],{reservedNames:full}).ok)errors.push('reserved passed '+v.reservedInvalidNames[i]);"+
                "for(var i=0;i<v.usedNamesInvalid.length;i++)if(c.validateDisplayName(v.usedNamesInvalid[i],{usedNames:v.usedNamesInvalid}).ok)errors.push('used passed '+v.usedNamesInvalid[i]);"+
                "for(var i=0;i<v.semanticInvalidNames.length;i++)if(!c.validateDisplayName(v.semanticInvalidNames[i]).ok)errors.push('semantic blocked '+v.semanticInvalidNames[i]);"+
                "var req=v.validNameRequests[0];var good={schemaVersion:1,requestId:req.requestId,selectedCandidateId:req.selectedCandidateId,styleVersion:v.styleVersion,displayName:'留白守望者'};"+
                "var checked=c.validateResponse(req,good);if(!checked.ok||checked.displayName!=='留白守望者')errors.push('good response rejected');"+
                "if(c.validateResponse(req,Object.assign({},good,{extra:1})).ok)errors.push('extra field passed');"+
                "if(c.validateResponse(req,Object.assign({},good,{requestId:'x'})).ok)errors.push('requestId mismatch passed');"+
                "if(c.validateResponse(req,Object.assign({},good,{selectedCandidateId:'y'})).ok)errors.push('candidate mismatch passed');"+
                "if(c.validateResponse(req,Object.assign({},good,{styleVersion:'PERSONA_NAME_STYLE_V0_9'})).ok)errors.push('style mismatch passed');"+
                "if(c.validateResponse(req,Object.assign({},good,{displayName:'克制的赌徒'})).ok)errors.push('reserved displayName passed');"+
                "if(c.validateResponse(req,Object.assign({},good,{displayName:'AB'})).ok)errors.push('invalid displayName passed');"+
                "return errors.length?errors.join('|'):'OK'})()";
            Assert.AreEqual("OK",Eval(expr));
        }

        [Test] public void DisplayNameOfFallsBackToInternalName()
        {
            var expr="(function(){var d=globalThis.GamePersonaPresentation.displayNameOf;var errors=[];"+
                "if(d({name:'AI人格3'})!=='AI人格3')errors.push('plain');"+
                "if(d({name:'AI人格3',aiPersonaMeta:{naming:{status:'FALLBACK',displayName:null}}})!=='AI人格3')errors.push('fallback');"+
                "if(d({name:'AI人格3',aiPersonaMeta:{naming:{status:'AI_NAMED',displayName:'留白守望者'}}})!=='留白守望者')errors.push('named');"+
                "if(d({name:'AI人格3',aiPersonaMeta:{naming:{status:'AI_NAMED',displayName:''}}})!=='AI人格3')errors.push('empty');"+
                "if(d({name:'AI人格3',aiPersonaMeta:{naming:{status:'AI_NAMED',displayName:123}}})!=='AI人格3')errors.push('nonstring');"+
                "if(d(null)!=='未命名人格')errors.push('null');"+
                "return errors.length?errors.join('|'):'OK'})()";
            Assert.AreEqual("OK",Eval(expr));
        }

        // 推进到第一个成长节点：每场战斗打一手并直接胜利结算
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
        string GrowthNamingExpr()=>"(function(){var st=personaRuntime.getState();var inst=st.personaInstancesById[currentGrowthInstanceId];if(!inst)return '__none__';var t=personaRuntime.getTemplate(inst.templateId);var n=t&&t.aiPersonaMeta&&t.aiPersonaMeta.naming;return n?JSON.stringify(n):'__none__'})()";
        // 命名链路是异步的（fetch reject → FALLBACK 写入），轮询并泵微任务等待收敛
        JObject WaitForNaming()
        {
            for(int i=0;i<60;i++)
            {
                var payload=Eval(GrowthNamingExpr());
                if(payload!="__none__"&&!string.IsNullOrEmpty(payload))return JObject.Parse(payload);
                rules.Snapshot();
            }
            return null;
        }

        [Test] public void GrowthNodeWritesFallbackNamingInHeadless()
        {
            EnterGrowthNode();
            var naming=WaitForNaming();
            Assert.IsNotNull(naming,"headless 成长节点应写入 FALLBACK 命名");
            Assert.AreEqual("FALLBACK",(string)naming["status"]);
            Assert.AreEqual(JTokenType.Null,naming["displayName"].Type);
            Assert.AreEqual(1,(int)naming["schemaVersion"]);
            Assert.AreEqual((string)vectors["styleVersion"],(string)naming["styleVersion"]);
            var s=rules.Snapshot();
            var internalName=(string)s["growth"]["persona"]["template"]["name"];
            StringAssert.IsMatch("^AI人格\\d+$",internalName);
            // 命名不得改动 template.name（aiPersonaSequenceNumber 依赖内部名格式），FALLBACK 回落内部名
            Assert.AreEqual(internalName,(string)s["growth"]["persona"]["name"]);
            StringAssert.Contains(internalName,(string)s["text"]["#persona-growth-card"]);
        }

        [Test] public void AiNamedDisplayNameFlowsThroughGrowthDetailEquipAndBattle()
        {
            EnterGrowthNode();
            var naming=WaitForNaming();
            Assert.IsNotNull(naming,"注入前应先收敛 FALLBACK，避免命名写入竞态");
            var internalName=(string)rules.Snapshot()["growth"]["persona"]["template"]["name"];
            var injected=Eval("(function(){var st=personaRuntime.getState();var inst=st.personaInstancesById[currentGrowthInstanceId];var t=personaRuntime.getTemplate(inst.templateId);var meta=JSON.parse(JSON.stringify(t.aiPersonaMeta||{}));meta.naming={schemaVersion:1,styleVersion:'PERSONA_NAME_STYLE_V1_1',status:'AI_NAMED',displayName:'留白守望者'};var upd=JSON.parse(JSON.stringify(t));upd.aiPersonaMeta=meta;upd.conditions=[];personaRuntime.registerTemplate(upd);return 'OK'})()");
            Assert.AreEqual("OK",injected);
            // 成长卡（growth-slot 动作触发重渲染）与快照桥接名
            var s=rules.Action("growth-slot",index:0);
            StringAssert.Contains("留白守望者",(string)s["text"]["#persona-growth-card"]);
            Assert.AreEqual("留白守望者",(string)s["growth"]["persona"]["name"]);
            // 详情弹窗
            var id=(string)s["growth"]["persona"]["id"];
            s=rules.Action("persona-detail",id);
            Assert.AreEqual("留白守望者",(string)s["personaDetail"]["name"]);
            rules.Action("click","#persona-detail-close");
            // 确认成长：装备至槽位 0（目标局随后进入商店）
            rules.Action("click","#persona-growth-confirm");
            s=rules.Snapshot();
            Assert.AreEqual("留白守望者",(string)s["personas"][0]["name"]);
            if((string)s["dialog"]=="#shop-dialog"){rules.Action("click","#shop-leave");s=rules.Snapshot();}
            // 后续节点若为战斗：该人格触发时事件显示名正确
            if((string)s["dialog"]=="#stage-intro-dialog"||(string)s["dialog"]=="")
            {
                if((string)s["dialog"]=="#stage-intro-dialog")rules.Action("click","#stage-start");
                rules.Action("toggle",index:0);
                var played=rules.Action("play");
                var personas=(JArray)played["lastScore"]["breakdown"]["personas"];
                var named=personas.FirstOrDefault(p=>(string)p["displayName"]=="留白守望者");
                Assert.IsNotNull(named,"AI 生成人格触发时 breakdown 应带显示名");
                Assert.AreEqual(internalName,(string)named["name"]);
            }
        }

        [Test] public void BattleEventSourcesStayConsistentWithBridgePersonaNames()
        {
            // 节制（PER_004）：本回合没有使用弃牌——第一手不弃牌必触发
            rules.Action("loadout");rules.Action("loadout-place","restraint",0);rules.Action("click","#start-loadout-confirm");rules.Action("click","#stage-start");
            rules.Action("toggle",index:0);
            var played=rules.Action("play");
            var personas=(JArray)played["lastScore"]["breakdown"]["personas"];
            Assert.Greater(personas.Count,0,"节制应在本回合触发");
            var first=(JObject)personas[0];
            Assert.AreEqual("人格牌04",(string)first["name"]);
            Assert.AreEqual("人格牌04",(string)first["displayName"]);
            var snap=rules.Snapshot();
            var restraint=snap["personas"].FirstOrDefault(p=>(string)p["instance"]["templateId"]=="restraint");
            Assert.IsNotNull(restraint);
            Assert.AreEqual("人格牌04",(string)restraint["name"]);
            StringAssert.Contains("人格牌04",(string)snap["scoreDetails"]);
            // 人格事件 source 首段必须落在 breakdown 人格名集合内（NativeGameView.cs 的事件匹配不变量）
            // 只过滤 phase=='人格牌' 的事件：牌型基础事件的 source 是牌型名（如「高牌」），不在断言范围
            var names=personas.Select(p=>(string)p["displayName"]).Concat(personas.Select(p=>(string)p["name"])).Where(n=>!string.IsNullOrEmpty(n)).ToHashSet();
            foreach(var ev in played["lastScore"].SelectTokens("$..[?(@.phase=='人格牌')]"))
            {
                var src=(string)ev["source"];
                if(string.IsNullOrEmpty(src))continue;
                var head=src.Split('·')[0].Trim();
                Assert.IsTrue(names.Contains(head),$"事件 source「{src}」首段不在人格名集合内");
            }
        }
    }
}
