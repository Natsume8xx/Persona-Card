# 人格牌音频素材

2026-09-07：替换整个振荡器、噪声和程序化旋律模块。生产运行仅播放本目录成品文件，失败时静音，不生成替代音。

## 来源与授权

- Kenney Casino Audio 1.1：https://kenney.nl/assets/casino-audio ，CC0；原授权见 licenses/kenney-casino.txt。
- Kenney Interface Sounds 1.0：https://kenney.nl/assets/interface-sounds ，CC0；原授权见 licenses/kenney-interface.txt。
- Kevin MacLeod / Incompetech，Darkest Child（USUAN1100783）与 Darkest Child var A（USUAN1100784）。作者现行曲目页标注 CC BY 4.0；原始元数据见 licenses/incompetech-tracks.json，玩家可见署名见 credits.html，设置页已提供入口。
- 音乐原下载地址：https://incompetech.com/music/royalty-free/mp3-royaltyfree/Darkest%20Child.mp3 与 https://incompetech.com/music/royalty-free/mp3-royaltyfree/Darkest%20Child%20var%20A.mp3 。
- 文件未剪辑、未变调、未重新生成；运行时只进行混音、淡入淡出、循环和多版本轮换。发布时必须一并保留署名页和许可文件。

## 声音方向及接入

暗色镜厅牌桌：菜单使用较舒缓的竖琴、钢片琴和低音弦乐配乐；战斗切换为同主题的较快版本，避免风格割裂。纸牌滑动用于选牌，落牌用于出牌，推牌用于弃牌，筹码用于逐牌计分、结算及购买；人格链以玻璃音、翻牌和筹码碰撞区分蓄力、移动、落点。

audio-effects.js 的 cues 是播放映射权威。按素材轮换而非生成随机音色；同音节流、单素材最多三声部、总音效最多十声部。音乐最高为主音量的 38%，强反馈时短暂压低配乐。切屏约 1.1 秒交叉淡化；完整歌曲结尾柔化后循环，并非重新作曲的无缝循环。

首次指针或键盘操作解锁；设置变化不从头播放；后台暂停，返回后从原进度继续。主音量、音乐、操作音效独立遵循原设置存储。出牌/弃牌在游戏动作被接纳后触发，成功购买后播放筹码声，避免按键与 DOM 监听各播放一次。

## 验证

运行 `node minimal-bgm-tests.js` 检查素材签名及播放器行为，包含手势解锁、切屏、静音、设置不中断、后台暂停、声部限制和缺失素材容错。音频仅改变表现，不修改战斗数值或存档结构。

2026-09-07 验证结果：音频专项通过；本地浏览器实测两首 MP3、点击、发牌、出牌、筹码和人格连锁音效均 readyState=4；菜单轨在切入战斗后暂停，战斗轨继续播放；关闭音乐立即暂停。此验证确认加载及播放行为，不代替真人耳机试听。

全套 68 个测试文件：59 通过、9 失败。将本次 game.js/index.html 音频改动在测试读取时反向还原后，9 项仍以相同原因失败：battle-layout-polish、battle-persona-card-hierarchy、scoring-card-feedback、shop-ui 是旧缓存版本断言；boss、developer-mode、forge、intervention-probability、score 是头像相关测试桩缺少 style.setProperty/removeProperty。未扩大音频范围修改这些已有问题。
