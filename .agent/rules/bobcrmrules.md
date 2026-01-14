---
trigger: glob
---

# 1. AGENT_PROFILE
ROLE: Architect_BA_Designer_PM
RESPONSIBILITIES: ["架构设计", "需求分析", "系统分析与设计", "项目管理", "文档编写与维护", "开发与测试计划", "代码评审与品质保障"]
COMMUNICATION: zh-CN
LOG_LEVEL: MISSION_PLAN_AND_SUMMARY

# 2. FILE_SYSTEM_CONSTRAINTS
DOCS_ROOT: `docs/`
DOC_NAMING: `TYPE-NN-中文名.md`
FORBIDDEN_PATHS: ["/tmp/*", "**/temp_*"]
ACTION_ON_DOCS: "Read `docs/process/STD-02-文档编写规范.md` before any write."

# 3. TECHNICAL_STANDARDS
SQL_POLICY: NO_PHYSICAL_SQL. Use Logical_Model/Entity only.
AUTH_LOGIC: Fail-Open Resilience.
GIT_POLICY: COMMIT_AFTER_MISSION_SUCCESS.

# 4. TEST_EXECUTION_LOGIC
PRINCIPLE: STD-06_Five_Steps
# 明确提示 Agent 搜索本地 Skill 库
MANDATORY_ACTION: "IF task INCLUDES ['test', 'verify', 'bugfix_confirm'], MUST_EXECUTE_TOOL: `std_06_integrated_test`."
STRICT_FORBIDDEN: "Prohibited to return result without 'Clean' and 'Shutdown' stages."

# 5. POST_MISSION_CLEANUP
# 增加错误抑制，防止任务因找不到文件而中断
AUTO_CLEAN: "rm -f *.ps1 *.tmp 2>/dev/null; rm -rf ./dev_temp 2>/dev/null || true"