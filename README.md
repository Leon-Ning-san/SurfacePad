# SurfacePad

专为 **Windows Surface 平板触控** 设计的《星露谷物语》**Switch Pro 风格悬浮虚拟手柄**（.NET 8 WPF）。

> 免驱动即开即用，遵循 **Nintendo Switch 逻辑**：右侧 `A` 键确认、**仅右摇杆控制鼠标指针**，自动屏蔽手指触屏导致的鼠标指针乱跳与误触。

---

## 快速开始（免安装）

1. 前往 [**Releases 页面**](https://github.com/Leon-Ning-san/SurfacePad/releases/latest) 下载 `StardewSurfacePad.exe`（Windows x64 单文件独立版，无需安装 .NET 或任何驱动）。
2. 打开《星露谷物语》，在游戏设置中将显示模式设为 **无边框窗口（Windowed Borderless）**。
3. 双击运行 `StardewSurfacePad.exe` 即可直接游玩。

---

## 核心特性

- **零驱动直连**：内置 Win32 `SendInput` 映射引擎，无需安装 ViGEmBus 内核驱动即可在 Surface 上开箱即用；如本机已安装 ViGEmBus，也可在编辑面板中切换为 XInput 模拟手柄模式。
- **Switch 鼠标指针逻辑**：通过底层鼠标钩子（`WH_MOUSE_LL`）拦截手指触屏产生的鼠标移动与点击，**仅右摇杆移动鼠标指针**，防止砍树/浇水/对话时朝向偏移或误点界面。
- **Switch Pro 握持布局与磨砂视觉**：
  - 摇杆与十字键采用深炭灰半透明底色（`#802D2D2D`）配浅灰描边，支持多指同时触控。
  - 右侧钻石四键中 **`A` 键尺寸最大并带任天堂红（`#E60012`）外环**作为视觉锚点。
  - 顶部左右采用贴合屏幕边缘的弧形宽大肩键 `ZL / L` 与 `R / ZR`。
- **不抢焦点 & 自由排版**：窗口设置 `WS_EX_NOACTIVATE`，点击按键不打断游戏；点击顶部中央 **截图键（Capture）** 可拖拽调整每个按键组的位置、透明度与缩放，自动保存至 `%AppData%\StardewSurfacePad\layout.json`。

---

## 默认按键映射

| 虚拟按键 | 区域位置 | 免驱动模式（星露谷默认键鼠） | ViGEm 手柄模式 |
| --- | --- | --- | --- |
| **左摇杆** | 左侧偏上 | `W / A / S / D` 八向移动 | 左摇杆（360° 模拟） |
| **十字键** | 左侧偏下 | `↑ / ↓ / ← / →` 方向键 | D-Pad |
| **右摇杆** | 右侧偏下 | **移动鼠标指针** | 右摇杆 |
| **`A`**（红环最大） | 右侧钻石右 | `X`（确认 / 对话 / 收获 / 开箱） | `A` |
| **`X`** | 右侧钻石上 | `C`（挥工具 / 攻击 / 播种） | `X` |
| **`B`** | 右侧钻石下 | `E`（打开 / 关闭背包） | `B` |
| **`Y`** | 右侧钻石左 | `M`（打开大地图） | `Y` |
| **`L` / `R`** | 顶部左右肩 | 鼠标滚轮上 / 下（切换工具栏） | `LB` / `RB` |
| **`ZL` / `ZR`** | 顶部左右扳机 | `Tab`（切换工具栏排）/ `C`（挥工具） | `LT` / `RT` |
| **`-` / `+`** | 顶部中央两侧 | `F`（任务日志）/ `Esc`（暂停菜单） | `Back` / `Start` |
| **截图键（Capture）** | 顶部正中 | 切换 **布局编辑 / 锁定模式** | 切换 **布局编辑 / 锁定模式** |

---

## 本地编译打包（Windows x64 单文件）

需要 **.NET 8 SDK**：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./ReleaseBuild
```
