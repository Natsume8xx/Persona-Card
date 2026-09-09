# 人格牌 · Unity 网页对齐版

本分支保存原生 Unity 迁移与优化版本；原有场景和开发内容继续保留。

## 打开与运行

1. 使用 Unity **6000.5.7f1** 打开仓库根目录，首次打开等待包解析与资源导入。
2. 打开 **Assets/Scenes/PersonaWebAligned.unity**，点击 Play。
3. Windows x64 构建入口：**Persona Cards > Web Aligned > Build Windows**，输出 **Builds/WebAligned/PersonaCards.exe**。

现有版本在 Unity 原生 UGUI 内运行，使用随仓库附带的 ClearScript/V8 Windows 插件执行已导出的网页规则。正常运行和构建不需要本机原网页目录；只有重新导出规则或美术时才需要网页源码路径。其他平台尚未验证。

当前流程为 **17 个核心节点：13 场战斗、3 次人格成长、1 个独立商店节点**；成长后也会进入商店，共四次商店。独立的本手得分详情按钮/弹窗已按最新要求移除。

## 开发记录与验证

- [累计已实现功能](Docs/Migration/FEATURES_IMPLEMENTED.md)
- [剩余开发与验收](Docs/Migration/REMAINING_WORK.md)
- [接续记录](Docs/Migration/HANDOFF.md)
- [网页基线与导出方式](Docs/Migration/WEB_BASELINE.md)
- [可编辑界面说明](Docs/Migration/EDITABLE_UI.md)
- [最近验证摘要](Docs/Migration/final-verification.json)
- [286项回归结果](Docs/Migration/ShopGrowthPolish/tests.xml)
- [第三方许可证](Docs/Migration/Licenses)

最近交付通过286项EditMode回归、Windows构建和自动界面/流程检查。自动流程后续战斗使用测试胜利夹具，不能当作人工正常通关；完整人工通关、冷启动存档、听感和整体性能仍待验收。

缓存、构建输出、本地备份、批量截图和运行日志不纳入版本控制；开发文档中的历史截图路径可能仅在原开发机存在。必要原始素材和 `.meta` 必须随工程提交。游戏存档位于 Unity persistentDataPath，不在仓库中；远程AI尚未正式接入，当前使用本地校验的离线回退。

## 原项目说明

# <<人格牌>>游戏demo开发
## 简介
希望能顺利完成！
