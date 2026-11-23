# StardewSurfacePad

专为 **Windows Surface 平板触控** 设计的《星露谷物语》**Switch Pro 风格虚拟悬浮手柄**（.NET 8 WPF）。

## 核心特性

1. **免驱动即开即用（零安装成本）**：
   - 内置 Win32 `SendInput` 键鼠直连映射引擎，**无需安装 ViGEmBus 或任何内核驱动**，双击 `StardewSurfacePad.exe` 即可控制《星露谷物语》。
   - 若系统已安装 ViGEmBus，也支持在编辑面板中切换为 XInput 虚拟手柄模式。
2. **还原 Switch 逻辑：仅右摇杆控制鼠标指针**：
   - 通过底层鼠标钩子（`WH_MOUSE_LL`）抑制 Windows 触屏转鼠标跳转，配合空白区域防误触吸收，避免手指操作摇杆/按键时让鼠标指针乱跑或误点游戏界面。
   - **只有右摇杆**负责平滑移动鼠标指针。
3. **Nintendo Switch Pro 物理键位与视觉风格**：
   - 右侧钻石四键：上 `X`（挥工具 `C`）、左 `Y`（大地图 `M`）、右 `A`（最大尺寸 + 任天堂红 `#E60012` 外环，确认/对话/收获 `X`）、下 `B`（打开/关闭背包 `E`）。
   - 左右弧形宽大肩键：左上 `ZL`（`Tab` 切工具排）/ `L`（滚轮上切栏）、右上 `R`（滚轮下切栏）/ `ZR`（`C` 挥工具/攻击）。
   - 顶部系统键：`-`（任务日志 `F`）、`Capture` 截图键（切换布局编辑/锁定模式）、`+`（暂停菜单 `Esc`）。
   - 摇杆与十字键采用 `#802D2D2D` 深炭灰半透明磨砂底色与浅灰描边，支持多指同时触控。
4. **Surface 悬浮窗优化**：
   - `WS_EX_NOACTIVATE` + `WM_MOUSEACTIVATE` 不抢占游戏焦点（建议将《星露谷物语》设为**无边框窗口**）。
   - 布局编辑模式支持拖拽各按键组、调节透明度与缩放比例，自动保存至 `%AppData%\StardewSurfacePad\layout.json`。

## 直接下载使用

前往本仓库的 [Releases](../../releases) 页面下载打包好的单文件 `StardewSurfacePad.exe`（Windows x64 自包含版本，无需提前安装 .NET 运行时）。

## 本地编译打包（Windows x64 单文件）

需要 **.NET 8 SDK**：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./ReleaseBuild
```
