# std_06_integrated_test

**Version**: 2026-01-08

## Description
执行标准化的集成测试五步法：环境清理、预检、启动、测试及关闭。

## Steps

### 1. Clean (Step ID: step_1_clean)
- **Action**: `terminal.run`
- **Input**: `sh ./scripts/clean_env.sh`
- **Critical**: True

### 2. Verify (Step ID: step_2_verify)
- **Action**: `terminal.run`
- **Input**: `sh ./scripts/check_config.sh`

### 3. Start (Step ID: step_3_start)
- **Action**: `terminal.run`
- **Input**: `npm run start:mock && npm run start:dev`
- **Wait Condition**: Check if localhost:3000 responds with 200 OK

### 4. Test (Step ID: step_4_test)
- **Action**: `agent.reasoning`
- **Instruction**: 参照 docs/matrix.md 执行全量路径验证。严禁抽样，必须覆盖逻辑分支。

### 5. Shutdown (Step ID: step_5_shutdown)
- **Action**: `terminal.run`
- **Input**: `npm run stop && rm -rf ./test_logs 2>/dev/null || true`
