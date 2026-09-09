# 网页基准与 Unity 迁移交接

## 当前权威

2026-09-08 策划明确确认：**一切以当前网页端内容为最高准则**。网页工程为 `E:/GameDemo/MVP`，Unity 工程为 `E:/GameProjects/Persona-Card-main`。旧 13 节点、旧三战和旧 Unity 数值文档均不覆盖这次确认。后续任务请先阅读本文件与生成导出中的 `provenance.json`，无需依赖聊天历史猜测规则。

网页实际 `RUN_TEMPLATE_TARGET` 为 17 个核心节点：13 场战斗、N04/N08/N12 三次人格成长、N16 商店。人格成长后还进入商店，完整流程合计四次商店。末尾接原网页报告与人格带出。此前“13 场战斗 + 4 次人格成长”的概括不符合当前源配置，以配置为准。

## 实现入口

- 当前原生场景：`Assets/Scenes/PersonaWebAligned.unity`。
- 当前可运行版本：`Builds/WebAligned/PersonaCards.exe`。
- 原生界面和动效：`Assets/WebAligned/NativeGameView.cs`、`NativeMotion.cs`。
- 规则执行与独立存档：`Assets/WebAligned/NativeRules.cs`。
- 网页导出：`Assets/WebAligned/Resources/WebCore/`；57 个源模块，逐文件 SHA-256 见 `provenance.json`。
- 新回归：`Assets/WebAligned/Tests/NativeRulesTests.cs`；旧 228 项测试仍保留。

Unity Canvas、EventSystem、卡牌与特效均为原生 UGUI；进程内 ClearScript V8 执行原始网页 JavaScript，不使用浏览器或 WebView。网页 `game.js`、PersonaRuntime、RunController、商店、牌型与生成校验算法不改写。`host.txt` 提供界面状态宿主，`bridge.txt` 将界面操作转发给原网页函数并输出展示快照。仅计分动画由 Unity 接管，实际分数、随机抽取、生成候选和胜负仍由原规则计算。

`source.txt` 是工具生成的只读导出，不能手改数值，也不是第二份策划母表。网页内容更新后重新导出、运行迁移测试，再构建。此版本包含 Windows x64 原生 V8 库；Android、iOS、WebGL 未验证，不宣称支持。

## 更新方法

从 Unity 工程根目录运行：

```powershell
node Tools/WebMigration/export-core.cjs E:/GameDemo/MVP Assets/WebAligned/Resources/WebCore
```

导出脚本旁的 `host.js` / `bridge.js` 是维护源；Resources 下同名 txt 是生成结果。美术与录音来自网页 `assets/art`、`assets/audio`，PNG 保持原像素，WebP 转 PNG。不得删除来历不明的原工程资源。

使用 Unity 6000.5.7f1，打开新场景播放，或运行菜单 `Persona Cards > Web Aligned > Build Windows`。命令行构建入口是 `PersonaCards.WebAligned.Editor.MigrationCommands.BuildWindows`。构建只使用新场景；旧 `BattlePrototype` 等场景保留，禁止重新启用旧自动场景重建器覆盖手工内容。

## 存档与规则边界

原网页 localStorage 数据按原键完整保存在 `Application.persistentDataPath/web-aligned-storage-v1.json`，文件采用临时文件替换和 `.bak` 备份。内部活动局版本及恢复是否合法由原网页存档代码校验；不猜测导入旧 Unity 存档，也不自动读取浏览器用户数据。Unity 设置独立保存在 PlayerPrefs `web-aligned-settings-v1`。

永久带出沿用网页 PersonaCollection，只有本地校验后的生成配置参与结算。原生宿主没有生产 API Key，也未配置外部 AI 选择服务；使用网页内建、经本地校验的离线生成回退。后续接真实服务应通过服务端代理，不能把密钥写进客户端。

## 验证材料

- `baseline-editmode.xml`：修改前 228 项旧测试。
- `aligned-editmode.xml`：当前全部测试，查看该文件根节点 `failed` 必须为 0。
- `native-build.log`：成功标记 `NATIVE_WINDOWS_BUILD_PASSED`。
- `native-player.log`：原生运行日志；截图整局验证成功标记 `NATIVE_VISUAL_JOURNEY_PASSED`。
- `Captures/`：真实独立程序截图与状态快照。

`--capture <目录>` 是显式验收入口，禁用真实存档持久化。首战通过正常选牌和结算截图；之后仅为枚举各界面使用测试胜利前置条件，不能将截图巡检当成正常游玩通关或平衡性证明。正常启动不执行这些测试操作。

动效相似度没有客观的自动百分比指标，应以实际牌桌、教程、商店等截图和可操作版本验收，不以“已达到 90%”代替比较。音频来源、授权与作者署名保留在 `Assets/WebAligned/Resources/WebAudio`，运行包旁也提供署名文件。


## 2026-09-08 可编辑界面升级
PersonaWebAligned 场景已保存 18 个真实 UGUI 页面实例，布局源为 Assets/WebAligned/UI 内预制体。入口菜单：Persona Cards > 打开可编辑界面。编辑方法见 EDITABLE_UI.md。NativeMotion 的组件已拆入对应类名脚本，以支持预制体序列化。构建不再重建空场景。旧场景备份位于 BeforeEditable。
242 项回归通过，结果 editable-ui-tests.xml；Windows 构建和真实画面巡检通过，见 editable-build.log、editable-player.log、EditableCaptures。巡检前置胜利仅用于枚举页面，不代表正常通关或动效百分比验收。

