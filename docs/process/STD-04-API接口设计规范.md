# STD-04: API 接口设计规范

> **版本**: 1.0
> **适用范围**: 后端 API 开发

---

## 1. RESTful 原则

- **GET**: 查询资源
- **POST**: 创建资源
- **PUT**: 完整更新资源
- **PATCH**: 部分更新资源
- **DELETE**: 删除资源

## 2. URL 命名规范

使用复数名词表示资源，层级清晰。

```text
GET    /api/templates                    # 获取模板列表
GET    /api/templates/{id}               # 获取单个模板
POST   /api/templates                    # 创建模板
PUT    /api/templates/{id}               # 更新模板
DELETE /api/templates/{id}               # 删除模板
POST   /api/templates/{id}/regenerate    # 对资源的特定操作 (动词)
```

## 3. 响应格式规范

API 应返回统一的响应结构，便于前端处理。

### 3.1 成功响应
```json
{
  "data": {
    "id": "123",
    "name": "Example"
  },
  "success": true
}
```
或直接返回数据对象（取决于项目约定），但需保持一致。

### 3.2 错误响应
```json
{
  "error": {
    "code": "INVALID_INPUT",
    "message": "The field 'name' is required.",
    "details": [
      { "field": "name", "error": "Required" }
    ]
  },
  "success": false
}
```

### 3.3 HTTP 状态码使用
- **200 OK**: 成功
- **201 Created**: 创建成功
- **204 No Content**: 删除成功/无返回内容
- **400 Bad Request**: 客户端请求无效（参数错误）
- **401 Unauthorized**: 未登录
- **403 Forbidden**: 无权限
- **404 Not Found**: 资源不存在
- **500 Internal Server Error**: 服务器内部错误

## 4. 版本控制

建议在 URL 或 Header 中包含 API 版本号。
- URL: `/api/v1/templates`
- Header: `Accept-Version: v1`

## 5. 分页与过滤

对于列表接口，必须支持分页和排序。

**请求参数示例**:
- `page`: 页码 (默认 1)
- `pageSize`: 每页数量 (默认 20)
- `sortBy`: 排序字段
- `sortOrder`:asc/desc

**响应示例**:
```json
{
  "items": [...],
  "total": 100,
  "page": 1,
  "pageSize": 20
}
```
