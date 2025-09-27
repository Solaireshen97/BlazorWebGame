# BlazorWebGame React 客户端

这是一个最小的 React 应用程序，用于测试与 BlazorWebGame 服务器的连接，帮助评估是否将前端从 Blazor WebAssembly 重构到 React。

## 功能特性

- ✅ HTTP API 连接测试
- ✅ SignalR 实时通信测试 
- ✅ 战斗系统 API 测试
- ✅ 服务器地址配置
- ✅ 连接状态显示
- ✅ 基本的 UI 界面

## 技术栈

- React 18 + TypeScript
- Axios (HTTP 请求)
- @microsoft/signalr (实时通信)
- Webpack 5 (构建工具)

## 快速开始

### 1. 启动 BlazorWebGame 服务器

首先确保 BlazorWebGame 服务器正在运行：

```bash
cd ../src/BlazorWebGame.Server
dotnet run
```

服务器默认运行在：
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5239`

### 2. 启动 React 客户端

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev
```

React 应用将在 `http://localhost:3000` 启动。

### 3. 测试连接

1. 打开浏览器访问 `http://localhost:3000`
2. 检查服务器配置是否正确
3. 点击"测试 HTTP 连接"
4. 点击"连接 SignalR"
5. 尝试战斗系统测试

## 可用脚本

```bash
# 开发模式启动
npm run dev

# 构建生产版本
npm run build

# 开发服务器（后台）
npm run start
```

## API 测试功能

### 连接测试
- HTTP API 连接状态检测
- SignalR 实时连接测试
- 服务器响应时间显示

### 战斗系统测试
- 开始战斗 (`POST /api/battle/start`)
- 查询战斗状态 (`GET /api/battle/state/{id}`)
- 停止战斗 (`POST /api/battle/stop/{id}`)
- SignalR 战斗更新接收

### 支持的服务器地址
- `https://localhost:7000` (默认)
- `http://localhost:5239`
- `https://localhost:7051`
- `http://localhost:5190`

## 文件结构

```
react-client/
├── public/
│   └── index.html          # HTML 模板
├── src/
│   ├── components/         # React 组件
│   │   ├── ConnectionStatus.tsx
│   │   ├── ServerConfig.tsx
│   │   └── BattleTest.tsx
│   ├── services/          # API 服务
│   │   ├── ApiService.ts
│   │   └── SignalRService.ts
│   ├── types.ts           # TypeScript 类型定义
│   ├── App.tsx            # 主应用组件
│   └── index.tsx          # 应用入口
├── package.json
├── tsconfig.json
├── webpack.config.js
└── README.md
```

## 开发注意事项

### CORS 配置
确保 BlazorWebGame 服务器的 CORS 配置允许来自 `http://localhost:3000` 的请求。

在 `appsettings.json` 中添加：
```json
{
  "Security": {
    "Cors": {
      "AllowedOrigins": [
        "https://localhost:7051",
        "http://localhost:5190",
        "http://localhost:3000"
      ]
    }
  }
}
```

### HTTPS 证书
如果使用 HTTPS 连接，浏览器可能显示证书警告。在开发环境中，可以：
1. 接受自签名证书
2. 或使用 HTTP 端口进行测试

### 调试信息
所有 API 请求和 SignalR 事件都会在浏览器控制台中记录日志。

## 评估建议

这个 React 客户端可以帮助您：

1. **测试 API 兼容性** - 验证现有的 API 是否可以被 React 客户端使用
2. **评估实时通信** - 测试 SignalR 在 React 中的集成难度
3. **比较开发体验** - 对比 React 与 Blazor 的开发体验
4. **性能对比** - 比较应用启动速度和运行性能
5. **生态系统评估** - 评估 React 生态系统的优势

### 优势：
- 更大的开发者社区和生态系统
- 更丰富的第三方组件库
- 更好的性能（无需 WebAssembly 加载）
- 更灵活的部署选项

### 考虑因素：
- 需要重写现有的 Blazor 组件
- 类型安全需要手动维护 TypeScript 类型
- SignalR 集成相对简单，但需要额外配置

## 故障排除

### 连接失败
1. 确保服务器正在运行
2. 检查服务器地址和端口
3. 验证 CORS 配置
4. 检查防火墙设置

### SignalR 连接问题
1. 检查 WebSocket 支持
2. 验证服务器的 SignalR Hub 配置
3. 查看浏览器网络选项卡的错误信息

### 构建错误
1. 确保 Node.js 版本 >= 18
2. 删除 `node_modules` 并重新安装
3. 检查 TypeScript 配置