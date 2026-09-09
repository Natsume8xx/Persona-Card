# 战斗与升级弹窗质感样板

日期：2026-09-09。用户确认先用已有素材优化画面质感，以网页黑金风格为基线。

## 已实现

- Battle、Upgrade 的主要实色面板增加暖色顶光到暗底的渐变，降低平涂感；保留已有画像与装饰按钮。
- 手牌图片添加轻微右下投影，让相邻卡牌更容易分辨。已有选牌抬起和按钮回弹保留，本轮没有新增交互动画。
- 升级目标从灰色块改为深棕黑；普通文字改为浅米色，选中目标突出金边，增加行距。
- NativeSurfaceFinish 是顶点网格效果，不生成纹理/材质，没有逐帧更新。手牌使用 UGUI Shadow，增加少量顶点；尚未做帧率和批次性能测量。
- 显式给 Battle.prefab 和 Upgrade.prefab 添加组件，不重新生成页面结构，不重建场景。

## 继续编辑

在 Battle 或 Upgrade 预制体内选择有 Native Surface Finish 的面板，可调整 Highlight（顶光）、Bottom Shade（暗底）、Light Tint（暖光色），也可禁用组件。手牌 Face 的 Shadow 可调整阴影颜色和位移。已有手工布局覆盖继续通过 NativeAuthoredElement 保留。

## 验证与范围

- 281 项旧回归及 5 项战斗输入回归，共 286 项通过，Windows 构建通过；实际截图与按钮检查结果见 SurfacePolish/verification.json。
- 网页样式参考 shop-simplified.css 中升级弹窗的深色渐变、金边及选中态。本轮不声称逐像素相同或达到某一相似度比例。
- 这是一轮静态质感样板，未替换美术、未实现泛光/模糊背景、未新增成长演出；其他页面尚未推广。
- 规则、数值、随机与存档格式未改动。累计实现与剩余项目分别见 FEATURES_IMPLEMENTED.md、REMAINING_WORK.md。

## 巡检发现并修复的旧问题

固定延时巡检偶尔在发牌未结束时尝试跳过教程，之后教程仍在却执行出牌；原生结算未判断 JSON null，抛出异常并留下 busy 状态。后续成长断言因此失败，并非证实人格被重抽。已让巡检等待发牌完成，战斗命令拦截菜单/弹窗/锁定/无选牌/无行动次数，结算防护空计分对象。新增 5 项输入回归；失败日志保留为 player-journey-before-input-fix.log，不以重跑成功抹去问题记录。
第二次巡检发现出牌后的固定5秒等待也可能早于补牌动画结束，提前重建页面导致动画访问已销毁卡牌。已在该处同样等待 busy 结束；保留 player-journey-before-resolution-wait.log。此修改仅影响 --capture 巡检时序。
