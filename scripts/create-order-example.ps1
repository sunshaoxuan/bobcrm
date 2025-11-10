#!/usr/bin/env pwsh
# ============================================================================
# 订单管理示例自动创建脚本
# 自动创建Order/OrderLine/OrderLineAttribute三层结构的完整示例
# ============================================================================

param(
    [string]$ApiBaseUrl = "http://localhost:5200",
    [string]$Token = ""
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " BobCRM 订单管理示例自动创建工具" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 检查API可用性
Write-Host "[1/7] 检查API连接..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ApiBaseUrl/api/health" -Method Get -TimeoutSec 5
    Write-Host "  ✓ API连接成功" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 无法连接到API: $ApiBaseUrl" -ForegroundColor Red
    Write-Host "    请确保API服务正在运行" -ForegroundColor Red
    exit 1
}

# 准备请求头
$headers = @{
    "Content-Type" = "application/json"
}

if ($Token) {
    $headers["Authorization"] = "Bearer $Token"
}

# 创建Order实体
Write-Host ""
Write-Host "[2/7] 创建Order实体定义..." -ForegroundColor Yellow

$orderEntity = @{
    namespace = "BobCrm.Domain.Orders"
    entityName = "Order"
    displayNameKey = "ENTITY_ORDER"
    descriptionKey = "ENTITY_ORDER_DESC"
    structureType = "MasterDetailGrandchild"
    icon = "shopping-cart"
    category = "订单管理"
    interfaces = @(
        @{ interfaceType = "Base"; isEnabled = $true },
        @{ interfaceType = "Archive"; isEnabled = $true },
        @{ interfaceType = "Audit"; isEnabled = $true }
    )
    fields = @(
        @{
            propertyName = "OrderNo"
            displayNameKey = "FIELD_ORDER_NO"
            dataType = "String"
            length = 50
            isRequired = $true
            sortOrder = 1
        },
        @{
            propertyName = "CustomerName"
            displayNameKey = "FIELD_CUSTOMER_NAME"
            dataType = "String"
            length = 100
            isRequired = $true
            sortOrder = 2
        },
        @{
            propertyName = "OrderDate"
            displayNameKey = "FIELD_ORDER_DATE"
            dataType = "DateTime"
            isRequired = $true
            defaultValue = "NOW"
            sortOrder = 3
        },
        @{
            propertyName = "TotalAmount"
            displayNameKey = "FIELD_TOTAL_AMOUNT"
            dataType = "Decimal"
            precision = 18
            scale = 2
            isRequired = $true
            defaultValue = "0"
            sortOrder = 4
        },
        @{
            propertyName = "Status"
            displayNameKey = "FIELD_STATUS"
            dataType = "String"
            length = 20
            isRequired = $true
            defaultValue = "Draft"
            sortOrder = 5
        },
        @{
            propertyName = "Notes"
            displayNameKey = "FIELD_NOTES"
            dataType = "Text"
            isRequired = $false
            sortOrder = 6
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-definitions" -Method Post -Headers $headers -Body $orderEntity
    $orderId = $response.id
    Write-Host "  ✓ Order实体创建成功 (ID: $orderId)" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 创建Order实体失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 创建OrderLine实体
Write-Host ""
Write-Host "[3/7] 创建OrderLine实体定义..." -ForegroundColor Yellow

$orderLineEntity = @{
    namespace = "BobCrm.Domain.Orders"
    entityName = "OrderLine"
    displayNameKey = "ENTITY_ORDER_LINE"
    descriptionKey = "ENTITY_ORDER_LINE_DESC"
    structureType = "MasterDetail"
    icon = "unordered-list"
    category = "订单管理"
    interfaces = @(
        @{ interfaceType = "Base"; isEnabled = $true },
        @{ interfaceType = "Audit"; isEnabled = $true }
    )
    fields = @(
        @{
            propertyName = "OrderId"
            displayNameKey = "FIELD_ORDER_ID"
            dataType = "Integer"
            isRequired = $true
            sortOrder = 1
        },
        @{
            propertyName = "ProductCode"
            displayNameKey = "FIELD_PRODUCT_CODE"
            dataType = "String"
            length = 50
            isRequired = $true
            sortOrder = 2
        },
        @{
            propertyName = "ProductName"
            displayNameKey = "FIELD_PRODUCT_NAME"
            dataType = "String"
            length = 200
            isRequired = $true
            sortOrder = 3
        },
        @{
            propertyName = "Quantity"
            displayNameKey = "FIELD_QUANTITY"
            dataType = "Integer"
            isRequired = $true
            defaultValue = "1"
            sortOrder = 4
        },
        @{
            propertyName = "UnitPrice"
            displayNameKey = "FIELD_UNIT_PRICE"
            dataType = "Decimal"
            precision = 18
            scale = 2
            isRequired = $true
            sortOrder = 5
        },
        @{
            propertyName = "Subtotal"
            displayNameKey = "FIELD_SUBTOTAL"
            dataType = "Decimal"
            precision = 18
            scale = 2
            isRequired = $true
            defaultValue = "0"
            sortOrder = 6
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-definitions" -Method Post -Headers $headers -Body $orderLineEntity
    $orderLineId = $response.id
    Write-Host "  ✓ OrderLine实体创建成功 (ID: $orderLineId)" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 创建OrderLine实体失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 创建OrderLineAttribute实体
Write-Host ""
Write-Host "[4/7] 创建OrderLineAttribute实体定义..." -ForegroundColor Yellow

$orderLineAttrEntity = @{
    namespace = "BobCrm.Domain.Orders"
    entityName = "OrderLineAttribute"
    displayNameKey = "ENTITY_ORDER_LINE_ATTRIBUTE"
    descriptionKey = "ENTITY_ORDER_LINE_ATTRIBUTE_DESC"
    structureType = "Single"
    icon = "tags"
    category = "订单管理"
    interfaces = @(
        @{ interfaceType = "Base"; isEnabled = $true }
    )
    fields = @(
        @{
            propertyName = "OrderLineId"
            displayNameKey = "FIELD_ORDER_LINE_ID"
            dataType = "Integer"
            isRequired = $true
            sortOrder = 1
        },
        @{
            propertyName = "AttributeKey"
            displayNameKey = "FIELD_ATTRIBUTE_KEY"
            dataType = "String"
            length = 50
            isRequired = $true
            sortOrder = 2
        },
        @{
            propertyName = "AttributeValue"
            displayNameKey = "FIELD_ATTRIBUTE_VALUE"
            dataType = "String"
            length = 200
            isRequired = $true
            sortOrder = 3
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-definitions" -Method Post -Headers $headers -Body $orderLineAttrEntity
    $orderLineAttrId = $response.id
    Write-Host "  ✓ OrderLineAttribute实体创建成功 (ID: $orderLineAttrId)" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 创建OrderLineAttribute实体失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 配置Order -> OrderLine关系
Write-Host ""
Write-Host "[5/7] 配置Order->OrderLine主子表关系..." -ForegroundColor Yellow

$masterDetailConfig1 = @{
    structureType = "MasterDetailGrandchild"
    children = @(
        @{
            childEntityId = $orderLineId
            foreignKeyField = "OrderId"
            collectionProperty = "OrderLines"
            cascadeDeleteBehavior = "Cascade"
            autoCascadeSave = $true
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-advanced/$orderId/configure-master-detail" -Method Post -Headers $headers -Body $masterDetailConfig1
    Write-Host "  ✓ Order->OrderLine关系配置成功" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 配置Order->OrderLine关系失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 配置OrderLine -> OrderLineAttribute关系
Write-Host ""
Write-Host "[6/7] 配置OrderLine->OrderLineAttribute主子表关系..." -ForegroundColor Yellow

$masterDetailConfig2 = @{
    structureType = "MasterDetail"
    children = @(
        @{
            childEntityId = $orderLineAttrId
            foreignKeyField = "OrderLineId"
            collectionProperty = "Attributes"
            cascadeDeleteBehavior = "Cascade"
            autoCascadeSave = $true
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-advanced/$orderLineId/configure-master-detail" -Method Post -Headers $headers -Body $masterDetailConfig2
    Write-Host "  ✓ OrderLine->OrderLineAttribute关系配置成功" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 配置OrderLine->OrderLineAttribute关系失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 生成AggVO代码
Write-Host ""
Write-Host "[7/7] 生成AggVO代码..." -ForegroundColor Yellow

try {
    # 生成Order的AggVO
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-advanced/$orderId/generate-aggvo" -Method Post -Headers $headers
    Write-Host "  ✓ OrderAggVO代码生成成功" -ForegroundColor Green

    # 生成OrderLine的AggVO
    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/entity-advanced/$orderLineId/generate-aggvo" -Method Post -Headers $headers
    Write-Host "  ✓ OrderLineAggVO代码生成成功" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ 生成AggVO代码失败: $($_.Exception.Message)" -ForegroundColor Red
    # 不退出，因为代码生成失败不影响实体定义
}

# 完成
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host " ✓ 订单管理示例创建完成！" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "实体ID信息：" -ForegroundColor Cyan
Write-Host "  Order ID:              $orderId" -ForegroundColor White
Write-Host "  OrderLine ID:          $orderLineId" -ForegroundColor White
Write-Host "  OrderLineAttribute ID: $orderLineAttrId" -ForegroundColor White
Write-Host ""
Write-Host "后续步骤：" -ForegroundColor Cyan
Write-Host "  1. 发布实体：POST /api/entity-definitions/{id}/publish" -ForegroundColor White
Write-Host "  2. 查看文档：docs/examples/ORDER_MANAGEMENT_EXAMPLE.md" -ForegroundColor White
Write-Host "  3. 访问前端：http://localhost:3000/entity-definitions" -ForegroundColor White
Write-Host ""
Write-Host "测试API：" -ForegroundColor Cyan
Write-Host "  查看主子表配置：" -ForegroundColor White
Write-Host "    GET $ApiBaseUrl/api/entity-advanced/$orderId/children" -ForegroundColor Gray
Write-Host ""
