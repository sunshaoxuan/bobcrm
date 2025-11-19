# GAP-01 架构与功能差距记录

> 创建日期：2025-11-19
> 状态：已确认 (Confirmed)
> 关联决策：优先推进低代码/无代码功能 (Phase 3)，暂缓后端微服务化重构。

## 1. 后端架构 (Backend Architecture)

### 现状
- **模式**：模块化单体 (Modular Monolith)。
- **结构**：分层（Core, Infrastructure, Services）存在于同一项目 `BobCrm.Api` 的不同文件夹中。
- **调用**：层间通过进程内方法调用 (In-process method calls)。

### 目标 (User Vision)
- **模式**：洋葱架构，完全微服务化。
- **结构**：各层独立部署，物理隔离。
- **调用**：网络化调用 (Networked calls)。

### 决议 (Resolution)
- **暂缓 (Deferred)**。
- **理由**：全微服务化可能带来显著的性能开销（序列化/反序列化、网络IO、类初始化），需先进行详细的效率分析评估后再决定是否实施。
- **当前行动**：保持现有模块化单体结构，但确保逻辑分层清晰，避免层间耦合，为未来可能的拆分留出余地。

## 2. 前端设计器 (Form Designer)

### 现状
- **绑定方式**：扁平字段选择 (Flat list of fields)。
- **属性配置**：基础属性编辑。

### 目标 (User Vision)
- **绑定方式**：对象式深层绑定 (Deep path binding, e.g., `customer.address.city`)。
- **属性配置**：高级属性配置，支持设计态与运行态的不同渲染逻辑。

### 决议 (Resolution)
- **次级任务 (Secondary Priority)**。
- **当前行动**：在完成 Phase 3 (低代码/无代码闭环) 后，作为 Phase 4 的重点任务进行优化。

## 3. 低代码/无代码平台 (Low-Code/No-Code Platform)

### 现状
- **实体**：动态实体系统已建立。
- **模板**：FormTemplate 基础已具备。
- **闭环**：系统级实体（组织、角色、用户、客户）尚未完全接入“实体-模板-功能-角色-用户”的完整链路。

### 目标 (User Vision)
- **闭环**：实现完整的应用闭环。
- **范围**：覆盖组织、角色、用户、客户等核心实体。

### 决议 (Resolution)
- **首要任务 (Top Priority)**。
- **当前行动**：立即执行 Phase 3，重构 Customer 及其他系统实体模块，使其完全由元数据和模板驱动。
