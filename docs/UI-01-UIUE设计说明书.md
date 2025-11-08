# BobCRM UI / UE 设计说明书（Design Language Playbook）

> 基于《Calm Neumorphic Glass CRM》设计语言与参考视觉稿（柔和玻璃、浅蓝灰、漂浮卡片）。本说明书定义一套可以生长出任何界面元素的通用设计语言，而非页面局部样式对照表。所有 UI 决策都需在“设计目标 → 视觉语法 → 交互语法 → 模式组合”链路下推导。

---

## 1. 设计意图（Design Intent）

1. **Calm Productivity**：高密度信息也保持低噪声。通过模糊玻璃层、柔和阴影和柔蓝色调营造“静水深流”的工作氛围。  
2. **Soft Neumorphic Glass**：所有可视元素由柔软圆角、玻璃质感和内外双阴影构成，强调“浮在浅色雾面背景之上”的轻盈感。  
3. **Composability**：任何页面由“容器（Container）+内容（Content）+状态层（State）”三层生成。设计师需要先确定信息结构，再套用容器类型与状态语法。  
4. **System before Screen**：先建立视觉与交互系统，再推导出导航、列表、表单、图表等场景。未被列举的元素也能用本系统组合而成。

---

## 2. 品牌支柱（Brand Pillars）

| 支柱 | 说明 | 视觉/交互表现 |
| --- | --- | --- |
| **冷静可信** | 面向企业的 CRM，需要权威且理性 | 低饱和灰蓝、连贯栅格、对齐严谨 |
| **轻盈柔和** | 参考样稿的柔性玻璃与软拟物 | blur 16–20px、双阴影、高光描边、圆角 12/16 |
| **高效密度** | 信息量大但不压迫 | 二列信息排列、12 列栅格、8pt 间距体系 |
| **反馈明确** | 所有操作有即时反馈 | hover 阴影过渡、active 内凹、focus ring、一致化 toast |

---

## 3. 视觉系统（Visual System）

### 3.1 色彩与语义
- **背景层**：`bg/base #F3F6FA` → `bg/surface rgba(255,255,255,0.70)` （blur16）→ `bg/elevated rgba(255,255,255,0.82)`（blur20）。页面越靠上层，亮度越高、模糊越强。  
- **品牌主色**：`primary/600 #739FD6`（主行动、数据主线）、`primary/500 #8CB4E6`（hover）与 `primary/100 #E8F1FD`（轻底）。  
- **中性色**：`neutral/900 #1F2937` 至 `neutral/300 #CBD5E1` 覆盖正文、次要文字、描边。  
- **语义色**：Success/Warning/Danger/Info 统一 100/600 组合，严禁自由增加色值。语义层仅用于状态片、徽章和图表强调区。  
- **深色模式映射**：背景转为 `#0F1520`，中性色反转（`neutral/900 → #E5E7EB`），阴影减弱、描边改为 6% 白，语义色饱和度下降 10–15%。

### 3.2 字体与层次
- 字体族：Inter / SF Pro / Noto Sans JP（等宽 JetBrains Mono）。  
- 字号级联：`display 24/32` → `title 20/28` → `subtitle 16/24` → `body 14/22` → `meta 12/18`。  
- 权重：600（标题/数字）、500（导航/标签）、400（正文/meta）。  
- 字距：0.2px，确保细腻冷静。文本对齐始终与栅格对齐。

### 3.3 图形元素
- **图标**：线性 1.5px，端点圆润，默认 `neutral/500`，hover/active 转 `primary/600`。  
- **插画**：线性或低饱和渐变的扁平插画，避免厚重色块。  
- **数据可视**：主线 `primary/600` + 20% 透明填充，对比线使用 `#6CB6A8`、`#D28C6B`、`#9A8CD4`。

### 3.4 表面与层级
- **radius**：`soft 12`, `card 16`, `pill 999`。  
- **Elevation**：  
  - `elev/flat`：无阴影 + `border/soft rgba(10,20,40,0.08)`  
  - `elev/raised`：`0 6px 18px rgba(15,23,42,0.08)` + `-2 -2 6 rgba(255,255,255,0.6)` 内高光  
  - `elev/pressed`：`inset 2 2 6 rgba(15,23,42,0.10)` + `inset -2 -2 6 rgba(255,255,255,0.55)`  
- **层级规则**：层级从基础内容 → 粘性工具条 → Popover → Modal → Toast，每上升一层背景透明度增加、模糊+4px、阴影半径+6px。

### 3.5 Light / Dark 主题切换规范
- **统一 Token 架构**：`theme-calm-light` 与 `theme-calm-dark` 共用同一 CSS 变量命名（如 `--color-primary-600`、`--shadow-raised`），通过覆盖值实现切换；组件禁止写死颜色。  
- **背景映射**：  
  - Light：`bg/base #F3F6FA` → `bg/surface rgba(255,255,255,0.70)`（blur16）→ `bg/elevated rgba(255,255,255,0.82)`（blur20）。  
  - Dark：`bg/base #0F1520` → `bg/surface rgba(16,20,30,0.55)`（blur20）→ `bg/elevated rgba(18,24,38,0.72)`（blur24）；描边为 `rgba(255,255,255,0.06)`，高光 `rgba(255,255,255,0.18)`。  
- **文字与图标**：中性色在暗色模式中反转（`neutral/900 → #E5E7EB`，`neutral/500 → #9AA4B2`）；图标默认 `neutral/500`，hover→`primary/500`，active→`primary/600`。  
- **阴影与高光**：暗色模式外阴影减弱为 `0 8px 24px rgba(0,0,0,0.35)`，并加入柔和外光 `0 0 12px rgba(115,159,214,0.25)` 维持漂浮感。  
- **语义色**：保持色相，饱和度降低 10–15%；语义背景 100 层在暗色模式下透明度 +5% 以满足对比度。  
- **切换动效**：在主题切换时对 `color / background-color / box-shadow / backdrop-filter` 应用 200 ms 过渡，禁止 layout shift；Skeleton/插画需要 light/dark 双份。  
- **交互状态**：focus-ring 一律 `rgba(115,159,214,0.35)`；hover/active 亮度变化幅度在两套主题中保持对称，避免操控感不一致。  
- **可达性**：每次提交需对 light/dark 进行对比度与 Tab-序验证，确保第 8 节可达性规则在两套主题下都成立。

---

## 4. 空间与布局系统（Spatial System）

1. **栅格**：桌面 12 列（槽宽 24px），移动 4 列。内容区宽度可自由流式，但不得脱离栅格。  
2. **间距**：8pt 系列（4/8/12/16/24/32/48）。容器内外所有间隙必须使用该序列；通过 `density` 控制±20%（compact / comfortable / spacious）。  
3. **容器分类**：  
   - **Base Layer**：页面背景面板，承载整体布局。  
   - **Section Container**：对齐栅格的内容块，用于模块/面板。  
   - **Card Container**：浮在 section 之上的信息卡（metric、timeline、list 等）。  
   - **Dock Container**：导航、批量操作条、工具条等粘性容器。  
   - **Micro Container**：按钮、过滤标签等微交互元素。  
4. **视觉顺序**：任何容器内部遵循 `标题 → 主信息 → 次信息 → 操作`。  
5. **响应式**：  
   - `lg` 以上可同时显示侧栏/右栏；`md` 收敛为单侧栏；`sm` 折叠为抽屉。  
   - 表格 ↓ `sm` 时转卡片列表；工具条转为上下堆叠。  
6. **信息密度语法**：当同时存在标签/数据/动作时，优先采用左右两列；最小 padding 16px；交互控件最小 32 × 32。

---

## 5. 交互语言（Interaction Language）

| 状态 | 规则 |
| --- | --- |
| Hover | 透明度 +4% 或 elev/flat→raised；图标转 primary/600，文字颜色保持。 |
| Active/Pressed | 使用 `elev/pressed` 内凹态；背景亮度 -4~6%。 |
| Focus | 统一 `focus-ring 0 0 0 3px rgba(115,159,214,0.25)`；禁止额外位移。 |
| Disabled | 降低对比度至 40–60%，阴影弱化，占位尺寸不变。 |
| Feedback | Toast 停留 3–5 秒；语义色 600/100 组合；Inline 提示位于控件下方，使用 meta 字号。 |
| Motion | 120/180/240ms，`cubic-bezier(0.2,0.8,0.2,1)` 入场，`ease-out` 悬停；动画变化 ≤10px，优先 opacity 与 translate。 |
| Keyboard | Tab 顺序与视觉顺序一致；Focus 可见；快捷键在 Tooltip 中注明。 |

---

## 6. 组合模式（Pattern System）

> 模式描述“如何组合容器与交互结构”，设计师可基于业务语义自行替换内容。

1. **Data Workspace Pattern**：摘要区（高优信息+主操作）、细节区（模板渲染）、时间线/协作区。适合客户、公司、商机等对象。  
2. **Flow & Form Pattern**：逐步或单屏表单。以 Section Container 划分步骤；每段包含标题、说明、字段组、操作行。错误与校验只改变状态层，不破坏布局。  
3. **Collection Pattern**：列表、表格、卡片集合。工具条（筛选、搜索、分段）+ 内容列表 + 批量操作条。密度可切换 compact/comfortable。  
4. **Control Panel Pattern**：仪表盘、看板。通过 Card Container 组合指标卡、任务卡、图表卡；卡片统一 16px 内边距，支持拖拽或重新排序。  
5. **Inline Edit Pattern**：浏览态 = 纯内容；编辑态 = focus ring + glass 底 + 轻描边；禁止弹窗全屏编辑。  
6. **Filter & Segmentation Pattern**：软标签（pill）、下拉、抽屉。选中态 `primary/100` 背景 + 20% 透明描边；条件多时折叠为 drawer。  
7. **Bulk Action Pattern**：当有选中元素时，顶部/底部浮出 bulk bar（bg/elevated + blur20）。左侧显示数量，右侧主行动+次行动。  
8. **Feedback & Status Pattern**：Toast（右上堆叠）、Inline 状态条（语义底+neutral 文案）、空状态（插画+一句文案+主按钮）、错误边界（danger/100 区块+重试）。

---

## 7. 组件族规范（Component Families）

### 7.1 动作组件（Buttons & Controls）
- **Filled / Soft / Ghost / Link** 四态共享：高度 36–40px、`radius/pill`、图标间距 8px、loading spinner 1.5px。  
- Segmented control、toggle、tabs 等皆遵循“软玻璃底 + 选中内凹”逻辑：未选中 elev/flat，选中 elev/pressed + 主色描边。  
- Icon-only 控件保持 40px 触达面积，hover 背景 `rgba(115,159,214,0.06)`。

### 7.2 表单组件（Fields）
- 容器：玻璃底 + soft 边 + `radius/soft`；占位符 `neutral/500`；输入文本 `neutral/900`。  
- Focus：focus ring + 轻阴影；错误态边框 `danger/600` 60% 透明度 + meta 辅助文案。  
- 组合字段（日期区间、Tag 输入等）保持 8px 分隔线；内部图标距边 12px；辅助按钮触达 32px。

### 7.3 信息容器（Cards & Panels）
- Header（标题+操作）、Content（可双列）和 State 层（徽章/警示）。  
- Metric/Tally卡：主数字使用 title 尺寸 + 600 字重；趋势信息以 meta 字号放在右上。  
- Timeline/Activity 卡：主列 + meta 信息 + 操作。节点使用 primary/600 实心点。  
- 无论卡片类型，状态层只能改变局部背景（语义/100），禁止整卡换色。

### 7.4 标签与徽章（Chips & Badges）
- 标签（Tag/Filter）= `soft` 背景 + 20% 透明描边；可包含图标/数字。  
- 状态徽章 = 语义色 100 背景 + 600 文案 + `radius/pill`。  
- 计数器（Counter）= `primary/500` 文本 + 1px 玻璃描边。

### 7.5 数据集合（Tables, Lists, Boards）
- 表头：glass 背景 + 粘顶 + 字号 subtitle；排序/过滤图标 aligned 右侧。  
- 行：舒适 44px、紧凑 36px；hover 背景 `rgba(10,20,40,0.03)`；选中行加左 3px 主色指示线。  
- 卡片列表/看板列：列容器 bg/surface + radius/card；卡片 spacing 16；拖拽阴影 `elev/raised`。  
- 空状态：线性插画 + body 文案 + 主按钮，背景保持 bg/surface。

### 7.6 导航与结构（Navigation）
- Top App Bar（64px）= Logo/名称 + 主导航 + Utility 区（语言、通知、头像）。  
- Side Rail（72px icon-only）或 Side Nav（240px）。未选项 = ghost，当前 = primary/100 背景 + 左 3px 指示线。  
- Breadcrumb/Context Pill 采用 soft 样式，区分所在层级。  
- 右侧信息栏/Context Panel 以 `bg/elevated` 浮在主内容之上，可容纳 Todo、评论、元数据等。

### 7.7 叠加层（Overlays）
- Drawer/Modal 使用 `bg/elevated` + blur20；头部固定，主体可滚动；底部行动条固定。  
- Popover/Dropdown 同样遵循玻璃底 + soft 阴影；箭头半径 6。  
- Toast 位于右上，堆叠不超过 3 条，自动关闭；hover 暂停计时。

### 7.8 数据可视化（Charts）
- 网格线 `rgba(20,40,70,0.08)`、坐标文字 `neutral/500`。  
- 面积图填充 20% 透明度；柱图圆角 4px；点图使用实心圆 + 外光。  
- Tooltip 采用玻璃底 + 阴影，列出指标、数值、同比/环比。

---

## 8. 可达性与多语言

- 文字对比：正文 ≥4.5:1，大字 ≥3:1。  
- 触达面积：桌面 ≥32×32，移动 ≥40×40。  
- 键盘：可聚焦元素必须显示 focus ring；热键需在 Tooltip 标注。  
- 动态提示：颜色与图标并用，避免仅依赖颜色表达。  
- 本地化：所有文案由资源表提供；日期/货币/姓名顺序可切换；数字/时间格式遵循用户区域设置。  
- Skeleton：任何数据加载 >400ms 要显示骨架（玻璃矩形 + 轻阴影）。

---

## 9. 实现守则（Implementation Guardrails）

1. **Token First**：颜色、阴影、圆角、间距、字重全部来源于设计 Token。禁止新建色值或自定义阴影。  
2. **Layer Discipline**：同一容器不可混用玻璃与实底阴影；升降层必须遵循 Elevation 规则。  
3. **Component Overrides**：在 Ant Design Blazor 等组件库上统一覆写变量实现 glass + soft-neumorphism，而非逐组件写样式。  
4. **State Coverage**：所有可交互元素必须实现 hover/active/focus/disabled 四态。  
5. **Responsive Proof**：每个页面至少验证 `sm/md/lg` 三个断点。  
6. **Documentation**：交付 Figma Frame + Token JSON + 交互说明，引用本说明书作为唯一视觉依据。  
7. **Dark Mode Parity**：新组件必须同时提供暗色映射，遵守第 3.1 节规则。

---

## 10. Token JSON（摘录）

```json
{
  "color": {
    "bg": {
      "base": "#F3F6FA",
      "surface": "rgba(255,255,255,0.70)",
      "elevated": "rgba(255,255,255,0.82)"
    },
    "primary": {
      "100": "#E8F1FD",
      "500": "#8CB4E6",
      "600": "#739FD6"
    },
    "neutral": {
      "900": "#1F2937",
      "700": "#4B5563",
      "500": "#94A3B8",
      "300": "#CBD5E1"
    },
    "semantic": {
      "success": { "100": "#E6F6F1", "600": "#1FA971" },
      "warning": { "100": "#FFF4E5", "600": "#C97A11" },
      "danger":  { "100": "#FDECEE", "600": "#C2414B" },
      "info":    { "100": "#E9F0FE", "600": "#4D7BD6" }
    }
  },
  "radius": { "soft": 12, "card": 16, "pill": 999 },
  "shadow": {
    "raised": "0 6px 18px rgba(15,23,42,0.08), -2px -2px 6px rgba(255,255,255,0.6) inset",
    "pressed": "inset 2px 2px 6px rgba(15,23,42,0.10), inset -2px -2px 6px rgba(255,255,255,0.55)"
  },
  "focusRing": "0 0 0 3px rgba(115,159,214,0.25)",
  "spacing": [4,8,12,16,24,32,48],
  "typography": {
    "fontFamily": "Inter, 'SF Pro', 'Noto Sans JP'",
    "scale": {
      "display": {"size": 24, "lineHeight": 32, "weight": 600},
      "title":   {"size": 20, "lineHeight": 28, "weight": 600},
      "subtitle":{"size": 16, "lineHeight": 24, "weight": 500},
      "body":    {"size": 14, "lineHeight": 22, "weight": 400},
      "meta":    {"size": 12, "lineHeight": 18, "weight": 400}
    }
  }
}
```

> **实践方式**：设计与前端共用 Token JSON 生成样式变量（CSS 变量或 Theme 对象），再依据本说明书的语法组合出新组件/页面。
