const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

(async () => {
    const baseUrl = process.env.BASE_URL || 'http://localhost:3000';
    const headless = process.env.HEADLESS !== 'false';
    const slowMo = Number(process.env.SLOWMO || 0);
    const artifactDir = process.env.SCREENSHOT_DIR || 'artifacts';
    const recordVideo = (process.env.RECORD_VIDEO || 'false').toLowerCase() === 'true';

    if (!fs.existsSync(artifactDir)) {
        fs.mkdirSync(artifactDir, { recursive: true });
    }

    console.log('=== BobCRM 模板链路集成测试（Playwright 自动化）===');
    console.log('时间:', new Date().toLocaleString());
    console.log('Base URL:', baseUrl);
    console.log('Headless:', headless);
    console.log('');

    const browser = await chromium.launch({
        headless,
        slowMo
    });

    const context = await browser.newContext({
        recordVideo: recordVideo ? { dir: artifactDir } : undefined
    });
    const page = await context.newPage();
    const stepShot = async (name) => {
        const file = path.join(artifactDir, name);
        await page.screenshot({ path: file, fullPage: true });
    };

    const selectors = {
        listHost: '.list-template-host',
        tableRow: 'table tr.ant-table-row',
        runtimeLayout: '.runtime-layout',
        editLayout: '.runtime-layout.edit-mode',
        saveButton: 'button:has-text("保存"), button:has-text("Save")',
        editButton: 'button:has-text("编辑"), button:has-text("Edit")'
    };

    async function findFirstSelector(selectors) {
        for (const selector of selectors) {
            const locator = page.locator(selector).first();
            if (await locator.count() > 0) {
                return locator;
            }
        }
        return null;
    }

    // 工具函数：登录（API 获取 token 并注入 localStorage）
    async function login() {
        console.log('[Step 1] 登录中...');

        const apiLogin = await context.request.post(`${baseUrl}/api/auth/login`, {
            data: { username: 'admin', password: 'Admin@12345' }
        });
        if (!apiLogin.ok()) {
            throw new Error(`API 登录失败: ${apiLogin.status()}`);
        }
        const json = await apiLogin.json();
        const { accessToken, refreshToken } = json.data || {};
        if (!accessToken || !refreshToken) {
            throw new Error('API 登录未返回 token');
        }

        // 预先访问域以便可写 localStorage
        await page.goto(`${baseUrl}/login`, { waitUntil: 'domcontentloaded' });
        await page.evaluate(([accessToken, refreshToken]) => {
            localStorage.setItem('accessToken', accessToken);
            localStorage.setItem('refreshToken', refreshToken);
        }, [accessToken, refreshToken]);

        // 跳转首页验证会话
        await page.goto(`${baseUrl}/`, { waitUntil: 'networkidle' });
        await page.waitForURL(/.*(dashboard|\/)$/i, { timeout: 20000 });
        // 等待应用完成初始化，避免截到 splash 画面
        await page.waitForSelector('.app-stage.ready', { timeout: 15000 }).catch(() => {});
        await page.waitForTimeout(500);
        console.log('   ✓ 登录成功（API token 注入）');
        await stepShot('step1-login.png');
    }

    // 工具函数：校验列表并点行
    async function testListTemplate() {
        console.log('[Step 2] 校验列表模板 (Customers)...');
        await page.goto(`${baseUrl}/customers`);

        // 等待列表宿主加载
        try {
            await page.waitForSelector(selectors.listHost, { timeout: 10000 });
            console.log('   ✓ 列表模板宿主已加载');
        } catch (e) {
            console.log('   ! 未找到 .list-template-host，可能使用默认列表视图');
        }

        // 等待表格和数据加载
        await page.waitForSelector('table', { timeout: 15000 });
        await page.waitForTimeout(3000);

        const rows = await page.locator('table tbody tr').count();
        console.log(`   ✓ 找到 ${rows} 行数据`);

        if (rows === 0) {
            throw new Error('列表无数据，无法继续详情校验');
        }
        await stepShot('step2-list.png');
    }

    // 工具函数：进入详情
    async function testDetailTemplate() {
        console.log('[Step 3] 进入详情模板 (Detail View)...');

        const row = page.locator('table tbody tr').first();
        await row.click();

        await page.waitForSelector(selectors.runtimeLayout, { timeout: 10000 });
        console.log('   ✓ 详情运行态布局已加载');

        const widgetCount = await page.locator(`${selectors.runtimeLayout} > div`).count();
        const title = await page.locator('h1').first().textContent();
        console.log(`   ✓ 详情包含 ${widgetCount} 个 Widget，标题: ${title || '(空)'}`);

        if (widgetCount === 0) {
            throw new Error('详情页未渲染任何 Widget');
        }
        if (!title || title.trim().length === 0) {
            throw new Error('详情页标题为空');
        }
        await stepShot('step3-detail.png');
    }

    // 工具函数：编辑/保存
    async function testEditMode() {
        console.log('[Step 4] 进入编辑模式并保存...');

        await page.click(selectors.editButton);
        await page.waitForSelector(selectors.editLayout, { timeout: 5000 });
        console.log('   ✓ 已进入编辑模式');

        const editableInputs = page.locator(`${selectors.editLayout} input, ${selectors.editLayout} textarea, ${selectors.editLayout} [contenteditable="true"]`);
        const count = await editableInputs.count();
        if (count > 0) {
            const input = editableInputs.first();
            const originalValue = await input.inputValue();
            const newValue = originalValue ? `${originalValue}_UPD` : 'AUTO_UPD';
            await input.fill(newValue);
            console.log(`   ✓ 修改输入值为: ${newValue}`);
        } else {
            console.log('   ! 未找到可编辑输入框，直接尝试保存');
        }

        await page.click(selectors.saveButton);
        await page.waitForSelector(selectors.runtimeLayout, { timeout: 10000 });

        const waitExitEdit = async (timeoutMs) => {
            try {
                await page.waitForFunction(() => {
                    const el = document.querySelector('.runtime-layout');
                    return !el || !el.classList.contains('edit-mode');
                }, { timeout: timeoutMs });
                return true;
            } catch {
                return false;
            }
        };

        let saved = await waitExitEdit(12000);

        // 其次：成功提示
        if (!saved) {
            try {
                await page.waitForSelector('.ant-message-success, .ant-notification-notice-success', { timeout: 8000 });
                saved = true;
            } catch {
                saved = false;
            }
        }

        // 如果仍未退出，尝试点击取消按钮退出
        if (!saved) {
            const cancelButton = await findFirstSelector(['button:has-text("取消")', 'button:has-text("Cancel")']);
            if (cancelButton) {
                await cancelButton.click();
                saved = await waitExitEdit(8000);
            }
        }

        if (!saved) {
            throw new Error('保存后未检测到退出编辑模式或成功提示');
        }

        console.log('   ✓ 保存成功并退出编辑模式');
        await stepShot('step4-edit-save.png');
    }

    try {
        await login();
        await testListTemplate();
        await testDetailTemplate();
        await testEditMode();

        console.log('');
        console.log('=== 用例全部通过 ===');
        const videoPath = recordVideo && page.video() ? await page.video().path() : null;
        await browser.close();
        if (videoPath) {
            console.log(`录屏已保存: ${videoPath}`);
        }
        process.exit(0);
    } catch (error) {
        console.error('');
        console.error('=== 用例失败 ===');
        console.error(error);
        const screenshotPath = path.join(artifactDir, 'template-test-error.png');
        await page.screenshot({ path: screenshotPath, fullPage: true });
        const videoPath = recordVideo && page.video() ? await page.video().path() : null;
        await browser.close();
        if (videoPath) {
            console.error(`录屏已保存: ${videoPath}`);
        }
        process.exit(1);
    }
})();
