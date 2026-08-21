using System;
using System.IO;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 设置存档存取：独立 JSON 文件（不混入战役存档），模式照抄 PrototypeSaveStore——
    /// tmp/bak 原子替换写入、损坏文件 .damaged-时间戳 备份、schemaVersion 校验、异常整体回落（12.6「数据缺失或异常时恢复默认」）。
    /// </summary>
    public sealed class GameSettingsStore
    {
        private const string FileName = "persona-cards-settings-v1.json";

        private readonly string _savePath;

        /// <summary>默认构造：persistentDataPath 下的设置存档。</summary>
        public GameSettingsStore() : this(Path.Combine(Application.persistentDataPath, FileName)) { }

        /// <summary>注入路径构造（EditMode 测试用临时目录，避免污染玩家存档）。</summary>
        public GameSettingsStore(string filePath) => _savePath = filePath;

        /// <summary>当前使用的存档路径（测试可断言）。</summary>
        public string SavePath => _savePath;

        /// <summary>存档文件是否存在。</summary>
        public bool Exists => File.Exists(_savePath);

        /// <summary>保存设置：先写临时文件再原子替换（崩溃时旧档完好）。任何异常吞掉并返回 false（保存失败不阻断游戏）。</summary>
        public bool TrySave(GameSettingsData data)
        {
            if (data == null) return false;
            try
            {
                var directory = Path.GetDirectoryName(_savePath);
                Directory.CreateDirectory(directory);
                var temporary = _savePath + ".tmp";
                var backup = _savePath + ".bak";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(_savePath))
                {
                    File.Replace(temporary, _savePath, backup);
                }
                else
                {
                    File.Move(temporary, _savePath);
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Settings] 设置保存失败：{exception.Message}");
                return false;
            }
        }

        /// <summary>读取设置：文件缺失 / 损坏 / 版本不符均返回 false（调用方回落默认）。损坏文件先备份再放弃。</summary>
        public bool TryLoad(out GameSettingsData data)
        {
            data = null;
            if (!File.Exists(_savePath)) return false;
            try
            {
                data = JsonUtility.FromJson<GameSettingsData>(File.ReadAllText(_savePath));
                if (data == null || data.schemaVersion != 1)
                {
                    // 失败语义统一：返回 false 时 data 必为 null（调用方只信返回值）
                    data = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                var damaged = _savePath + $".damaged-{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Copy(_savePath, damaged, true);
                Debug.LogWarning($"[Settings] 设置存档读取失败，已备份损坏文件：{exception.Message}");
                data = null;
                return false;
            }
        }
    }
}
