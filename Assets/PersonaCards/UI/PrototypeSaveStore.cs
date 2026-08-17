using System;
using System.IO;
using UnityEngine;

namespace PersonaCards.UI
{
    public sealed class PrototypeSaveStore
    {
        private const string FileName = "persona-cards-save-v1.json";

        public string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public bool Exists => File.Exists(SavePath);

        public void Save(PrototypeSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var directory = Path.GetDirectoryName(SavePath);
            Directory.CreateDirectory(directory);
            var temporary = SavePath + ".tmp";
            var backup = SavePath + ".bak";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath))
            {
                File.Replace(temporary, SavePath, backup);
            }
            else
            {
                File.Move(temporary, SavePath);
            }
        }

        public bool TryLoad(out PrototypeSaveData data)
        {
            data = null;
            if (!File.Exists(SavePath)) return false;
            try
            {
                data = JsonUtility.FromJson<PrototypeSaveData>(File.ReadAllText(SavePath));
                return data != null && data.schemaVersion >= 1 && data.schemaVersion <= 3;
            }
            catch (Exception exception)
            {
                var damaged = SavePath + $".damaged-{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Copy(SavePath, damaged, true);
                Debug.LogWarning($"存档读取失败，已备份损坏文件：{exception.Message}");
                data = null;
                return false;
            }
        }
    }
}
