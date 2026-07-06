# Floating HUD

Floating HUD 是一个 Windows 桌面悬浮 HUD，用来把命令行输出渲染成屏幕上的悬浮文字。

程序会按配置定时执行一条命令，把命令的标准输出解析为 JSON，然后用这个 JSON 更新 HUD 的文本和样式。

## 运行要求

- Windows
- 开发时需要 .NET SDK 10.0 或更高版本
- 运行 framework-dependent 发布包时需要目标机器已安装对应版本的 .NET Desktop Runtime
- 运行 self-contained 发布包时不需要目标机器预装 .NET 运行时

## 项目结构

```text
floating-hud/
  run-source.cmd
  publish-release.cmd
  src/
    floating-hud.csproj
    *.cs
    *.xaml
```

## 从源码运行

在项目根目录运行：

```cmd
run-source.cmd
```

等价于：

```cmd
dotnet run --project src\floating-hud.csproj -c Release
```

可以传入第一个参数作为设置目录：

```cmd
run-source.cmd C:\path\to\floating-hud-data
```

如果不传设置目录，Floating HUD 默认把设置和刷新错误日志保存到：

```text
%AppData%\floating-hud
```

## 发布

在项目根目录运行：

```cmd
publish-release.cmd
```

脚本会生成四个 Windows x64 发布包：

```text
artifacts/win-x64-framework-dependent-singlefile/
artifacts/win-x64-framework-dependent/
artifacts/win-x64-self-contained-singlefile/
artifacts/win-x64-self-contained/
```

`win-x64-framework-dependent-singlefile` 是不包含 .NET 运行时的单文件版。  
`win-x64-framework-dependent` 是不包含 .NET 运行时的普通文件夹版。  
`win-x64-self-contained-singlefile` 是包含 .NET 运行时的单文件版。  
`win-x64-self-contained` 是包含 .NET 运行时的普通文件夹版。

framework-dependent 包的目标机器需要提前安装对应版本的 .NET Desktop Runtime；self-contained 包不需要。

## 基本操作

右键 HUD 文本可以打开菜单：

- `锁定`：切换 HUD 是否允许拖动和滚轮缩放
- `配置`：编辑命令行、刷新周期、字号、锚点和交互颜色
- `打开配置目录`：用资源管理器打开设置和刷新错误日志所在目录
- `清空错误日志`：删除刷新错误日志
- `关闭`：退出程序

未锁定时：

- 按住鼠标左键拖动 HUD 文本可以移动位置
- 在 HUD 文本上滚动鼠标滚轮可以调整字号

## 命令执行方式

Floating HUD 会通过下面的方式执行配置里的命令：

```cmd
cmd.exe /d /s /c <配置的命令行>
```

因此可以使用普通 `cmd.exe` 语法，包括 `echo` 这类内建命令、重定向、管道、`&&`、批处理脚本等。

命令需要把 HUD 数据写到标准输出。标准错误不会直接显示，只会在执行或解析失败时写入错误日志。

如果上一次命令还在运行时又触发了刷新，正在运行的命令会被终止，然后排队执行新的刷新。

## 标准输出格式

命令的标准输出在去掉首尾空白后，必须是一个完整、合法的 JSON 对象。

最小合法输出：

```json
{}
```

典型输出：

```json
{
  "text": "Floating HUD",
  "fontColor": "#FFFFFFFF",
  "strokeThickness": 0.1,
  "strokeColor": "#000000FF"
}
```

示例命令：

```cmd
echo {"text": "Floating HUD", "fontColor": "#FFFFFFFF", "strokeThickness": 0.1, "strokeColor": "#000000FF"}
```

也可以用脚本或其他 shell 输出 JSON：

```cmd
powershell.exe -NoProfile -Command "$o = [ordered]@{ text = (Get-Date).ToString(); tooltipText = 'Updated by PowerShell' }; $o | ConvertTo-Json -Compress"
```

```cmd
python.exe -c "import json, time; print(json.dumps({'text': time.ctime()}))"
```

## JSON 字段

所有字段都是可选字段。未输出的字段会保留上一次的值。对于 `text`，如果当前还没有成功设置过文本，就显示配置里的默认值。

未知字段会被忽略。

| 字段 | 类型 | 行为 |
| --- | --- | --- |
| `text` | 字符串 | HUD 主文本。最长保留 256 个字符，超出部分会被截断。 |
| `tooltipText` | 字符串 | 鼠标悬停时显示的提示文本。最长保留 1024 个字符，超出部分会被截断。 |
| `fontName` | 字符串 | 字体名称。提供时不能为空或纯空白。 |
| `isBold` | 布尔值 | `true` 使用粗体，`false` 使用普通字重。 |
| `isItalic` | 布尔值 | `true` 使用斜体，`false` 使用普通字体样式。 |
| `fontColor` | 字符串 | 文本填充颜色，格式为 RGB/RGBA 十六进制颜色。 |
| `strokeThickness` | 数字 | 描边粗细比例，相对于逻辑字号。必须是有限且非负的数字。超过 `0.25` 会被限制为 `0.25`。 |
| `strokeColor` | 字符串 | 描边颜色，格式为 RGB/RGBA 十六进制颜色。 |

## 颜色格式

颜色支持 RGB 或 RGBA 十六进制格式：

```text
#RRGGBB
RRGGBB
#RRGGBBAA
RRGGBBAA
```

RGB 格式会使用完全不透明。RGBA 格式的最后一个字节是透明度。

示例：

```text
#FFFFFFFF  白色，完全不透明
#000000FF  黑色，完全不透明
#FF000080  红色，约 50% 不透明
```

## 校验和错误处理

如果命令输出无法解析，或某个字段非法，Floating HUD 会尽量保留之前的 HUD 内容。

常见错误包括：

- 标准输出为空
- 标准输出不是合法 JSON
- JSON 根节点不是对象
- 字段类型不正确，例如写成 `"isBold": "true"` 而不是 `"isBold": true`
- 颜色字符串非法
- `strokeThickness` 为负数、`NaN` 或无穷大

如果一部分字段合法、一部分字段非法，合法字段仍然会被应用，错误会被记录到日志。

如果标准输出合法但命令退出码不是 `0`，HUD 会应用这次输出，同时显示警告外框并记录日志。

刷新错误日志默认写入：

```text
%AppData%\floating-hud\errors
```

如果启动时传入了自定义设置目录，则错误日志会写入该目录下的 `errors` 子目录。

## 配置项

配置窗口包含：

- `默认值`：命令还没有产生有效 `text` 时显示的兜底文本
- `命令行`：刷新时执行的命令
- `刷新周期`：命令重复执行的间隔秒数；`0` 表示只在启动时执行一次
- `字号`：HUD 的逻辑字号
- `锚点`：保存和恢复位置时使用的相对锚点
- `悬停背景`：鼠标悬停在 HUD 上时显示的背景色
- `锁定外框`：HUD 锁定且鼠标悬停时显示的外框颜色
- `错误外框`：最近一次刷新出现执行错误或输出校验错误时显示的外框颜色
- `警告外框`：标准输出合法但命令退出码非零时显示的外框颜色
