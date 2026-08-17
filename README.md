# DS Hub

一个轻量级的 Windows 桌面小工具，把 DeepSeek 常用入口和实用小功能集中在一个小窗口里。

> 非官方项目，与 DeepSeek 官方无关。鲸鱼图标仅用于个人识别用途。

## 功能

- **快捷入口**：DeepSeek 网页版 / Platform / Harness（启动后自动打开浏览器）
- **API 余额**：实时查询 DeepSeek API 余额（每分钟自动刷新，可自设 Token）
- **快速翻译**：中英互译，支持三种方向（自动 / 中→英 / 英→中）
  - 全局热键 `Ctrl+Alt+T`：剪贴板内容即翻即出
  - 翻译历史（最近 100 条，可复制回内容）
- **顶部抽屉**：点击右上角收起图标，窗口收纳为桌面顶部的小鲸鱼条，鼠标放上去即可弹出
- **深色 / 浅色主题**：跟随系统，也可手动切换
- **时间状态行**：显示北京时间与 DeepSeek API 高峰时段倒计时

## 安装

1. 运行 `DS-Hub-Setup-3.11.0.0.exe`（无需管理员权限，按用户安装）
2. 完成后从桌面快捷方式或开始菜单启动

### 系统要求

- Windows 10 / 11（.NET Framework 4.x 已随系统自带）
- 高清屏自动适配（DPI 感知缩放）
- 「启动 Harness」功能需要本机已安装 Node.js（通过 `npx @deepseek-ai/dsh web` 启动）

## 使用

| 操作 | 说明 |
| --- | --- |
| 网页版 / Platform | 打开对应网页 |
| 打开 Harness 界面 | 检测 127.0.0.1:3080，未运行则自动启动并打开浏览器 |
| 重启 Harness | 关闭浏览器与终端进程，重新启动并自动打开浏览器 |
| 关闭 | 停止 Harness 进程树并退出 |
| 设置 | 点击余额区「设置」输入 DeepSeek API Key（仅保存在本机） |
| 充值 | 余额区「充值」按钮，软件内嵌页面直接充值（WebView2，无需跳转浏览器） |
| 价格 | 左上角「价格」按钮，弹出 Flash/Pro 高峰·空闲价目表 |
| Ctrl+Alt+T | 翻译剪贴板内容 |
| 翻译 / 复制 / 清空 / 历史 | 翻译面板操作按钮 |

## 数据存储

- Token 与翻译历史保存在 `%APPDATA%\DS Hub\`，不上传任何数据
- 翻译与余额查询直接调用 DeepSeek 官方 API（`api.deepseek.com`）

## 构建

```
csc.exe /nologo /target:winexe /win32icon:DeepSeek.ico /out:"DS Hub.exe" ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll ^
  App.cs LogoImage.cs
```

安装包使用 [Inno Setup](https://jrsoftware.org/isinfo.php)：

```
ISCC.exe App.iss
```

## 开源许可

MIT License，详见 [LICENSE](LICENSE)。
