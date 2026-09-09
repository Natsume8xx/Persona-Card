using System;
using System.IO;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    // The frozen JS export is the authority. This class grants no CLR/network access.
    public sealed class NativeRules : IDisposable
    {
        readonly V8ScriptEngine engine;
        readonly string savePath;
        string lastStorage;
        public NativeRules(bool persistence = true)
        {
            savePath = persistence ? Path.Combine(Application.persistentDataPath, "web-aligned-storage-v1.json") : null;
            HostSettings.AuxiliarySearchPath = Path.Combine(Application.dataPath, "WebAligned/Plugins/x86_64") + ";" + Path.Combine(Application.dataPath, "Plugins/x86_64");
            engine = new V8ScriptEngine();
            foreach (var name in new[] { "host", "source", "bridge" })
                engine.Execute(Resources.Load<TextAsset>("WebCore/" + name).text);
            if (savePath != null && File.Exists(savePath))
            {
                try { engine.Invoke("NativeRestoreStorage", File.ReadAllText(savePath)); }
                catch (Exception e) { Debug.LogWarning("存档未载入，原文件已保留：" + e.Message); }
            }
        }
        public JObject Snapshot() => JObject.Parse((string)engine.Invoke("NativeSnapshot"));
        public JArray DrainAudioCues() => JArray.Parse((string)engine.Invoke("NativeDrainAudioCues"));
        public JObject Action(string type, string id = null, int index = 0)
        {
            DrainAudioCues(); // Discard startup or previous presentation events; never replay on snapshot/restore.
            engine.Invoke("NativeAction", new JObject { ["type"] = type, ["id"] = id, ["index"] = index }.ToString());
            for (var i = 0; i < 80; i++) { engine.Invoke("__flush"); }
            var state = Snapshot();
            if (state["errors"] is JArray errors && errors.Count > 0) throw new InvalidOperationException(errors.ToString());
            if (savePath != null)
            {
                var storage = state["storage"].ToString();
                if (storage != lastStorage)
                {
                    var temp = savePath + ".tmp";
                    File.WriteAllText(temp, storage);
                    if (File.Exists(savePath)) File.Replace(temp, savePath, savePath + ".bak");
                    else File.Move(temp, savePath);
                    lastStorage = storage;
                }
            }
            return state;
        }
        public string EvaluateForTest(string expression) => engine.Evaluate(expression).ToString();
        public void Dispose() => engine.Dispose();
    }
}

