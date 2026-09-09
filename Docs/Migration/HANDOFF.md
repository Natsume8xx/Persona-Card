# Unity 迁移接续记录

本文件用于新一轮或上下文压缩后恢复项目事实。记录不能替代检查实际代码、目标文件和验证证据；发现不一致时应先核对，不得凭旧聊天宣布完成。

## 入口和权威

- 网页基线：E:/GameDemo/MVP，用户已确认以当前网页内容为最高准则。
- Unity 正式目标：E:/GameProjects/Persona-Card-main。
- 当前隔离工作副本：E:/GameDemo/MVP/tmp/unity-editable；源文件修改先在此验证，再备份同步正式目标。此副本离线包路径修改不得同步 Packages。
- Unity：E:/untiy 3D/6000.5.7f1/Editor/Unity.exe。
- 场景：Assets/Scenes/PersonaWebAligned.unity；运行版：Builds/WebAligned/PersonaCards.exe。
- 正式流程为 17 核心节点：13 战斗、3 成长、1 商店；成长后也有商店，共四次。旧三战/13节点说明不是当前实现基线。

## 继续前必读

1. WEB_BASELINE.md：源模块、导出和存档边界。
2. FEATURES_IMPLEMENTED.md：累计已实现功能和能力边界。
3. REMAINING_WORK.md：未实现、部分完成、待验收项。
4. final-verification.json 与最近交付目录：实际通过的检查和未覆盖内容。
5. 实际源码和目标哈希：防止覆盖用户改动。不能只根据本文件判断文件一致。

## 当前这一轮

- 工作目录：E:/GameDemo/MVP/tmp/unity-shop-growth-polish。
- 本轮：Shop、Forge、PersonaGrowth面板增加暖色渐变；普通按钮深棕黑，选中项暖棕；说明改浅色正文并增加滚动文本行距。
- 成长选槽继续局部更新并保留阅读位置，颜色更新与新主题一致。三个模板追加可编辑组件，未重建场景。
- 验证证据见 ShopGrowthPolish/verification.json；说明见 SHOP_GROWTH_POLISH.md。
- 得分详情已按用户要求移除，后续不得恢复。上一轮Battle/Upgrade质感及输入防护继续保留。
- 规则、数值及存档格式未改；累计功能和剩余项见 FEATURES_IMPLEMENTED.md / REMAINING_WORK.md。

## 下一批明确缺口

- Boss 音效时序；人格能量的所有目标路径/命中视觉与网页逐项比对。
- 成长确认演出和其他剩余操作动效。
- 其他页面实例复用及整体性能；目前成长选槽已局部更新。
- 全部页面视觉与交互差异穷尽审计。
- 远程 AI 服务正式接入；现在是本地校验的离线回退。
- 正常人工 17 节点游玩、真实冷启动存档、极端文本/比例、长时间运行及听感。

## 交付约定

- 每次报告同时写本轮新增、累计已实现、未实现、未验收，并更新上述记录。
- 不声称未经测量的 90% 或完全视觉对齐，不重复把已完成项当待开发。
- 不改变网页数值、随机结果或存档格式来迎合展示；生成导出只通过工具更新。
- 不覆盖来源不明的手工编辑；同步前比较基线哈希，备份旧文件，再核对所有构建文件。
- --capture 不使用正式存档。后续战斗使用测试胜利夹具的流程巡检不是完整人工通关。









## Git交付与本地清理（2026-09-09）

- 本次Git交付使用新分支 codex/unity；main和其他已有分支保持不变，不自动合并。
- Git仅纳入必要工程文件、开发文档、验证摘要、最新测试XML及许可证；备份、截图、日志和构建产物保留本地但忽略。
- 已清理临时验证工程的Artifacts/PlayerDataCache/Bee/BurstCache/ShaderCache与重复Builds/WebAligned；后续需要重新构建临时运行版。
- 正式工程Library/PackageCache被临时离线Packages配置引用，不能整体删除；不得把该离线配置提交到正式仓库。
- 清理后3978个受保护文件与正式构建206文件哈希一致。清理明细仅存于本机 E:/GameDemo/MVP/tmp/local-cleanup-20260909。

