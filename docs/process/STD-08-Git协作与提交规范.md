# STD-08: Git 协作与提交规范

> **版本**: 1.0
> **适用范围**: 版本控制与团队协作

---

## 1. Git 提交规范

### 1.1 提交消息格式

符合 Conventional Commits 规范：

```text
<type>(<scope>): <subject>

<body>

<footer>
```

### 1.2 Type 类型
- `feat`: 新功能 (feature)
- `fix`: 修复 bug
- `refactor`: 重构 (即不是新增功能，也不是修改bug的代码变动)
- `docs`: 文档更新
- `test`: 增加测试
- `chore`: 构建过程或辅助工具的变动
- `style`: 代码格式修改 (不影响代码运行的变动)
- `perf`: 性能优化

### 1.3 示例
```text
feat(template): add state-driven template system

- Replace FormTemplateUsageType enum with EnumDefinition
- Add TemplateStateBinding for N:M relationship

Closes #123
```

## 2. Git 文件管理规范

### 2.1 必须忽略的文件 (.gitignore)

以下类型文件**严禁**提交：

#### 2.1.1 构建产物
- 编译输出 (`bin/`, `obj/`, `dist/`)
- 包依赖目录 (`node_modules/`, `packages/`)

#### 2.1.2 测试产物
- 缓存文件
- 测试生成的截图、视频、报告
- 临时调试脚本

#### 2.1.3 本地配置
- IDE 本地配置 (`.idea/`, `.vscode/*.local.json`)
- **敏感信息配置文件** (`.env.local`, `appsettings.Development.json` 中包含密钥的部分)
- 操作系统生成文件 (`.DS_Store`, `Thumbs.db`)

#### 2.1.4 日志与临时文件
- 运行日志 (`*.log`)
- 临时文件 (`tmp/`, `temp/`)

### 2.2 应该提交的文件

- ✅ **源代码**: 核心业务逻辑。
- ✅ **测试代码**: 单元测试、集成测试、E2E 测试。
- ✅ **配置模板**: `.env.example`, `appsettings.json` (脱敏)。
- ✅ **文档**: `docs/` 下的所有设计文档和规范。
- ✅ **脚本**: 构建、部署和工具脚本 (`scripts/`)。
- ✅ **数据库迁移**: 迁移脚本。

### 2.3 敏感信息处理

**严禁提交密钥、密码、真实连接字符串。**
- 使用环境变量。
- 使用配置服务(Secret Manager)。

### 2.4 新增忽略规则流程

1. 确认文件不应被版本控制。
2. 添加到 `.gitignore`。
3. 如果文件已被提交，需先从 Git 索引中移除 (`git rm --cached`)。
4. 提交 `.gitignore` 变更。

## 3. 分支管理 (建议)

- `main` / `master`: 主干分支，随时可部署。
- `develop`: 开发分支。
- `feature/*`: 功能分支，从 develop 检出，合并回 develop。
- `hotfix/*`: 修复分支，从 main 检出，合并回 main 和 develop。
- `release/*`: 发布分支。

## 4. 代码审查 (Code Review)

-所有合并请求 (PR/MR) 必须经过至少一人审查。
- 审查重点：
  - 代码逻辑正确性
  - 是否符合 `STD-03-通用编程规范`
  - 测试是否充分
  - 是否包含敏感信息
