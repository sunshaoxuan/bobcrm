## 背景与目标

随着导航样式、语言、主题以及登录落点等需求不断演进，原先的 `UserPreferences` 仅能保存零散字段，无法满足“系统默认值 + 用户覆写”的 OOP 设计。新的设置体系需要满足：

1. **单一事实来源**：SystemSettings 负责平台级默认值，UserSettings 仅记录用户真正覆写的差异。
2. **可继承可降级**：当用户未覆写某个字段时，运行时自动回落到系统默认；一旦覆写，默认值立即失效。
3. **可分层的接口与界面**：管理员可以独立调整系统参数；普通用户只看到个人化选项。
4. **支撑导航策略**：导航支持 Icons / Labels / Icon+Text 三种模式，并能持久化到用户设置。

## 数据与服务层设计

| 对象 | 说明 | 关键字段 |
| --- | --- | --- |
| `SystemSettings` | 单行表，记录全局默认值。`EnsureSystemSettingsAsync` 在启动时自动创建。 | CompanyName、DefaultTheme、DefaultPrimaryColor、DefaultLanguage、DefaultHomeRoute、DefaultNavMode、TimeZoneId、AllowSelfRegistration |
| `UserPreferences` *(用户覆写)* | 仍复用原表，但语义升级为用户级覆盖。所有写入都通过 `SettingsService` 完成。 | Theme、PrimaryColor、Language、HomeRoute、NavDisplayMode |
| `SettingsService` | 借助 `ComposeEffective` 组装 *System / Effective / Overrides* 三段视图，并负责字段规范化（语言白名单、路由前缀、NavMode 正则化等）。 | `GetSystemSettingsAsync`、`UpdateSystemSettingsAsync`、`GetUserSettingsAsync`、`UpdateUserSettingsAsync` |

### API 分层

- `GET /api/settings/system`（仅 admin）+ `PUT /api/settings/system`
- `GET /api/settings/user`（任意登录用户）
- `PUT /api/settings/user`（写入用户覆写）
- 旧的 `/api/user/preferences` 入口被保留，并透传到新服务，兼容历史客户端。

## 前端集成与导航策略

### PreferencesService（Blazor）

1. 新增 `UserSettingsSnapshot`（system + effective + overrides）缓存，并在成功读取后触发本地副作用：
   - `bobcrm.setCookie("lang")` + `bobcrm.setLang` ⇒ 语言切换实时生效；
   - `localStorage.navMode` ⇒ 侧边栏刷新仍保持用户习惯。
2. 提供 `SaveUserSettingsAsync` / `SaveSystemSettingsAsync`，所有写入都走 `/api/settings/*`。
3. 暴露 `ToLayoutMode` / `ToApiNavMode` 辅助方法，`AppHeader`、`PreferencesManager`、`Settings.razor` 共同使用。

### 设置页面（/settings）

- **系统设置面板**：展示公司名、默认主题/主色、语言、登录首页、导航模式、系统时区以及“允许自助注册”开关。非管理员仅能看到提示 `MSG_SETTINGS_ADMIN_ONLY`。
- **用户设置面板**：支持主题切换、主色拾取、语言、登录后首页、导航显示方式。保存后：
  1. 更新后端；
  2. 即时刷新 ThemeState / LayoutState；
  3. 回显“设置已保存 / 保存失败”。

### 导航模式

| 模式 | 枚举值 | 场景 |
| --- | --- | --- |
| Icons only | `icons` | 超窄侧边栏，仅图标提示；适合熟练用户 |
| Labels only | `labels` | 仅文字导航，节省视觉干扰 |
| Icon + Text | `icon-text` | 默认模式，平衡易读性与密度 |

`LayoutState.NavDisplayMode` 始终由 `PreferencesManager` 的快照驱动，`AppHeader` 的切换操作会反写用户设置，实现“所见即所得”的导航体验。

## 交付清单

1. **后端**
   - `SystemSettings` 实体 & EF 配置；
   - `SettingsService` + `SettingsEndpoints`；
   - `UserEndpoints` 迁移至新服务；
   - `DatabaseInitializer` 补充文案（登录巨幕 + 设置字典）。
2. **前端**
   - 新版 `PreferencesService` / `PreferencesManager` / `Settings.razor`；
   - `AppHeader` 写入导航模式时调用新接口；
   - Light 风格设置页样式（chip 组 + pill 标签）。
3. **文档**
   - 本文档归档于 `ARCH-13-系统与用户设置模型.md`，供后续迭代参考。
