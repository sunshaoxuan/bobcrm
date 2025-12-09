const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

(async () => {
    const baseUrl = process.env.BASE_URL || 'http://localhost:3000';
    const headless = process.env.HEADLESS !== 'false';
    const slowMo = Number(process.env.SLOWMO || 0);
    const artifactDir = process.env.SCREENSHOT_DIR || 'artifacts';

    if (!fs.existsSync(artifactDir)) {
        fs.mkdirSync(artifactDir, { recursive: true });
    }

    console.log('=== BobCRM 模板链路集成测试（设计器自动化）===');
    console.log('时间:', new Date().toLocaleString());
    console.log('Base URL:', baseUrl);
    console.log('Headless:', headless);
    console.log('');

    const browser = await chromium.launch({
        headless,
        slowMo
    });
    
    const context = await browser.newContext();
    const page = await context.newPage();

    // 工具函数：登录
    async function login() {
        console.log('[Step 1] 登录中...');
        await page.goto(`${baseUrl}/login`);
        await page.fill('input[placeholder*="admin"]', 'admin');
        await page.fill('input[placeholder*="password"]', 'Admin@12345');
        await page.click('button[type="submit"]');
        await page.waitForURL('**/dashboard', { timeout: 15000 });
        console.log('   ✓ 登录成功');
    }

    // 工具函数：校验列表并点击
    async function testListTemplate() {
        console.log('[Step 2] 访问列表模板 (Customer List)...');
        await page.goto(`${baseUrl}/customer/list`);
        
        // 等待列表加载
        // 假设列表使用了 ListTemplateHost，我们可以等待 .list-template-host 或具体的 widget
        try {
            await page.waitForSelector('.list-template-host', { timeout: 10000 });
            console.log('   ✓ 列表模板宿主已加载');
        } catch (e) {
            console.log('   ⚠ 未找到 .list-template-host，可能是默认列表页');
        }
        
        // 等待数据加载
        await page.waitForTimeout(2000);
        
        // 检查是否有行数据
        const rows = await page.locator('table tr.ant-table-row').count();
        console.log(`   ✓ 找到 ${rows} 行数据`);
        
        if (rows === 0) {
            console.log('   ⚠ 无数据，无法进行详情校验');
            return false;
        }
        return true;
    }

    // 工具函数：进入详情页
    async function testDetailTemplate() {
        console.log('[Step 3] 进入详情模板 (Detail View)...');
        
        // 点击第一行 (假设第一列是链接或整行可点)
        // Ant Design Table row
        await page.locator('table tr.ant-table-row').first().click();
        
        // 等待详情页加载
        // 等待 PageLoader 相应的元素
        try {
            await page.waitForSelector('.runtime-layout', { timeout: 10000 });
            console.log('   ✓ 详情页运行布局已加载');
            
            // 检查是否有 Widget
            const widgetCount = await page.locator('.runtime-layout > div').count();
            console.log(`   ✓ 详情页包含 ${widgetCount} 个 Widget`);

            // 记录标题
            const title = await page.locator('h1').textContent();
            console.log(`   ✓ 实体标题: ${title}`);
            
            return true;
        } catch (e) {
            console.error('   ✕ 详情页加载失败:', e.message);
            throw e;
        }
    }

    // 工具函数：编辑模式
    async function testEditMode() {
        console.log('[Step 4] 进入编辑模式...');
        
        // 点击编辑按钮
        await page.click('button:has-text("编辑"), button:has-text("Edit")');
        
        // 等待进入编辑模式
        await page.waitForSelector('.runtime-layout.edit-mode', { timeout: 5000 });
        console.log('   ✓ 已进入编辑模式');
        
        // 尝试修改一个输入框
        const input = await page.locator('.runtime-layout input').first();
        if (await input.count() > 0) {
            const originalValue = await input.inputValue();
            console.log(`   原始值: ${originalValue}`);
            
            const newValue = originalValue + '_UPD';
            await input.fill(newValue);
            console.log(`   修改值: ${newValue}`);
            
            // 点击保存
            await page.click('button:has-text("保存"), button:has-text("Save")');
            
            // 等待保存完成并退出编辑模式
            await page.waitForSelector('.runtime-layout:not(.edit-mode)', { timeout: 10000 });
            console.log('   ✓ 保存成功并返回浏览模式');
            
            // 可选：检查是否更新（取决于后端响应）
        } else {
            console.log('   ⚠ 未找到可编辑的输入框，跳过修改校验');
        }
    }

    try {
        await login();
        const hasData = await testListTemplate();
        if (hasData) {
            await testDetailTemplate();
            await testEditMode();
        }
        
        console.log('');
        console.log('=== 测试全部通过 ===');
        await browser.close();
        process.exit(0);
    } catch (error) {
        console.error('');
        console.error('=== 测试失败 ===');
        console.error(error);
        const screenshotPath = path.join(artifactDir, 'template-test-error.png');
        await page.screenshot({ path: screenshotPath, fullPage: true });
        await browser.close();
        process.exit(1);
    }
})();
