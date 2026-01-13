# STD-06: 集成测试规范

> **版本**: 1.0
> **生效日期**: 2025-12-08
> **状态**: 执行中

---

## 1. 概述

本文档定义了系统集成测试的标准操作流程（SOP）。为确保测试结果的准确性和环境的稳定性，所有开发人员和 AI 助手在进行集成测试时**必须**严格遵守本规范。

## 2. 核心原则（铁律）

集成测试必须遵循以下 **5步铁律**，缺一不可：

1.  **清理环境**：强制关闭相关进程，释放端口。
2.  **静态验证**：在启动应用前，先进行编译、单元测试和资源检查。
3.  **启动环境**：启动全新的测试环境。
4.  **执行测试**：运行自动化或手动测试用例。
5.  **结束清理**：无论测试成功与否，必须关闭进程。

## 3. 详细流程

### 3.1 第一步：清理环境 (Clean)
**目的**: 消除“幽灵进程”和文件锁定，确保测试环境纯净。

**操作**:
- 检查相关服务端口占用。
- 杀掉占用端口的进程。
- 尤其是运行时进程和应用进程。

**伪代码示例**:
```text
FUNCTION CleanupEnvironment():
    TERMINATE_PROCESS("ApiProcess")
    TERMINATE_PROCESS("AppProcess")
    TERMINATE_PROCESS("RuntimeProcess")
    ENSURE_PORTS_AVAILABLE(ApiPort, AppPort, DbPort)
```

### 3.2 第二步：静态验证 (Verify Setup)
**目的**: 在运行应用之前，确保代码能编译、单元测试通过、多语言资源合规。避免在有编译错误的情况下启动应用。

**操作**:
- 运行验证脚本。
- **必须**等待脚本执行完毕且结果为 **PASS**。
- 如果失败，**禁止**进入下一步。

**伪代码示例**:
```text
FUNCTION VerifySetup():
    RUN "verification-script"
    IF RESULT IS FAIL:
        EXIT "Verification failed"
```

### 3.3 第三步：启动测试环境 (Start)
**目的**: 启动后端 API、前端 App 和数据库。

**操作**:
- 使用标准启动脚本。
- 建议使用 Detached 模式（后台运行），通过日志监控状态。
- 等待健康检查通过。

**伪代码示例**:
```text
FUNCTION StartEnvironment():
    RUN "startup-script" WITH MODE="detached"
    WAIT_FOR_HEALTH_CHECK(ApiEndpoint)
    WAIT_FOR_HEALTH_CHECK(AppEndpoint)
    VERIFY_SYSTEM_CONNECTIVITY()
```

### 3.4 第四步：执行测试 (Test)
**目的**: 验证业务功能。

**操作**:
- 执行自动化端到端测试 (E2E)。
- 执行手动功能验证。
- 记录测试结果。

**伪代码示例**:
```text
FUNCTION RunTests():
    EXECUTE "e2e-test-suite"
    // OR
    PERFORM_MANUAL_VERIFICATION()
```

### 3.5 第五步：结束清理 (Shutdown)
**目的**: 释放资源，防止文件锁定影响下一次编译。

**操作**:
- 停止所有服务。
- 确认进程已退出。

**伪代码示例**:
```text
FUNCTION Shutdown():
    RUN "stop-script"
    ENSURE_PROCESS_EXITED("ApiProcess")
    ENSURE_PROCESS_EXITED("AppProcess")
```

## 4. 检查清单

在提交代码或通知用户完成任务前，请确认：

- [ ] 是否已先杀掉旧进程？
- [ ] 验证脚本是否全绿通过？
- [ ] 测试后是否已关闭环境？
- [ ] 测试日志是否已保存？

---
**严格遵守以上规范，杜绝“改完代码不重启”、“编译报错硬启动”等低级错误！**
