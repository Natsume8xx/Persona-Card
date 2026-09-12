# AI 人格配图资源对照表

## 一、资源位置

- 源文件：`Resource/美术资源/人格牌汇总/` 下三个分类文件夹（偏转 / 桥接 / 映照），每类 11 张、共 33 张（其中 6 张无名图），尺寸 977×1610 PNG。
- 工程内位置：`Assets/WebAligned/Resources/WebArt/personas-v2/`，全部文件与源文件 md5 一致（byte 级复制）。
  - 新增 33 张随机池文件：`ai-bridge-01~11.png`、`ai-deflect-01~11.png`、`ai-mirror-01~11.png`（编号与源文件名数字前缀一致）。
  - 8 张基础人格占位图被正式立绘覆盖：`persona-01~08-original-v1.png`（路径不变，读取代码零改动）。

## 二、策划分类 → 游戏内方向

| 策划分类 | 游戏内方向 | 池文件前缀 |
|---|---|---|
| 偏转 | 破局 `AI_DIRECTION_BREAK` | `ai-deflect-*` |
| 桥接 | 桥接 `AI_DIRECTION_BRIDGE` | `ai-bridge-*` |
| 映照 | 顺势 `AI_DIRECTION_FOLLOW` | `ai-mirror-*` |

## 三、随机规则

AI 人格生成时按上述方向**随机取一张**写入 `template.portrait`，随存档持久化；渲染时只读不随机（避免闪变）。旧存档读档与旧收藏记录会自动按同一规则补图（见第六节）。

## 四、33 张 AI 随机池图对照表

### 偏转 → 破局（ai-deflect-*）

| 工程文件 | 源文件 |
|---|---|
| ai-deflect-01.png | 1 花色漫游者.png |
| ai-deflect-02.png | 2 同色共鸣者.png |
| ai-deflect-03.png | 3 满手承诺者.png |
| ai-deflect-04.png | 4.png（无名） |
| ai-deflect-05.png | 5 沉默的协作者.png |
| ai-deflect-06.png | 6.png（无名） |
| ai-deflect-07.png | 7侧径探访者.png |
| ai-deflect-08.png | 8边界回望者.png |
| ai-deflect-09.png | 9点数锻造师.png |
| ai-deflect-10.png | 10回合倾听者.png |
| ai-deflect-11.png | 11谨慎的先行者.png |

### 桥接 → 桥接（ai-bridge-*）

| 工程文件 | 源文件 |
|---|---|
| ai-bridge-01.png | 1 隐境寻路者.png |
| ai-bridge-02.png | 2 断舍离者.png |
| ai-bridge-03.png | 3.png（无名） |
| ai-bridge-04.png | 4.png（无名） |
| ai-bridge-05.png | 5暗门记录员.png |
| ai-bridge-06.png | 6迷途提灯者.png |
| ai-bridge-07.png | 7未竟寻路人.png |
| ai-bridge-08.png | 8末牌预见者.png |
| ai-bridge-09.png | 9层级整理师.png |
| ai-bridge-10.png | 10 余数保管员.png |
| ai-bridge-11.png | 11秩序拼接者.png |

### 映照 → 顺势（ai-mirror-*）

| 工程文件 | 源文件 |
|---|---|
| ai-mirror-01.png | 1 终局观察者.png |
| ai-mirror-02.png | 2 克制的赌徒.png |
| ai-mirror-03.png | 3 结构收藏家.png |
| ai-mirror-04.png | 4.png（无名） |
| ai-mirror-05.png | 5.png（无名） |
| ai-mirror-06.png | 6雾线测绘者.png |
| ai-mirror-07.png | 7远景校准者.png |
| ai-mirror-08.png | 8余路收藏家.png |
| ai-mirror-09.png | 9牌序守望者.png |
| ai-mirror-10.png | 10清醒的梦想家.png |
| ai-mirror-11.png | 11守序的变通者.png |

## 五、8 张基础人格立绘替换表（文件级覆盖）

| 工程文件 | 内部 id | 游戏内显示名 | 源文件 |
|---|---|---|---|
| persona-01-original-v1.png | observer | 人格牌01 | 映照/1 终局观察者.png |
| persona-02-original-v1.png | wanderer | 人格牌02 | 偏转/1 花色漫游者.png |
| persona-03-original-v1.png | pathfinder | 人格牌03 | 桥接/1 隐境寻路者.png |
| persona-04-original-v1.png | restraint | 人格牌04 | 映照/2 克制的赌徒.png |
| persona-05-original-v1.png | collector | 人格牌05 | 映照/3 结构收藏家.png |
| persona-06-original-v1.png | resonance | 人格牌06 | 偏转/2 同色共鸣者.png |
| persona-07-original-v1.png | commitment | 人格牌07 | 偏转/3 满手承诺者.png |
| persona-08-original-v1.png | purger | 人格牌08 | 桥接/2 断舍离者.png |

## 六、配图机制（三个写入入口，一个渲染出口）

1. **新生成**：AI 人格工厂（template-factory 模块）在 fromCandidate 时按 `aiPersonaMeta.internalDirectionId` 从对应池随机取图写入 `template.portrait`。
2. **旧局读档**：`runController.restoreRun` 对无图且带方向的 AI 模板补图（下次保存持久化）。
3. **旧收藏记录**：`PersonaCollection.backfillPortraits` 在宿主注入存档时（NativeRestoreStorage）失效缓存重读存储、补图并写回，幂等（二次调用不再写入）。
4. **渲染出口**：商城 / 图鉴 / 详情均直接读 `template.portrait`，不做随机（同一人格每次查看图不变）。

说明：附带修复了一个旧收藏记录的既有问题——收藏模块在加载期首读把缓存钉在空集合上，导致 Unity 下旧收藏记录在图鉴不可见（甚至会被之后的 carryOut 覆盖）。补图入口统一做了缓存失效，图鉴现能正常显示历史收藏的 AI 人格。

## 七、验收记录（相对最新一次改动）

- EditMode：新增 4 个配图测试（图池覆盖三方向 / 生成随机分配与方向一致 / 读档补图 / 收藏补图幂等），全量回归通过。
- Play 冒烟：控制台零错误；图鉴 9 张（8 基础 + 1 历史 AI 收藏），AI 收藏卡显示 `ai-mirror-10.png` 立绘（顺势方向），贴图 977×1610 加载成功；补图结果已写入本地存档。
- 场景文件零改动；8 张基础立绘替换后路径不变。
