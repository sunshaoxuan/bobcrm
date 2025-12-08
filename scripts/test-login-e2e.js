const { chromium } = require('playwright');

(async () => {
    console.log('=== BobCRM 登录集成测试（浏览器自动化）===');
    console.log('时间:', new Date().toLocaleString());
    console.log('');

    const browser = await chromium.launch({ 
        headless: false,  // 显示浏览器窗口，方便观察
        slowMo: 500  // 减慢操作速度，方便观察
    });
    
    const context = await browser.newContext();
    const page = await context.newPage();

    try {
        console.log('[TC-05] 测试浏览器 UI 登录流程...');
        
        // 导航到登录页面
        console.log('  1. 导航到登录页面...');
        await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' });
        console.log('     ✓ 页面加载完成');
        
        // 等待页面元素加载
        await page.waitForTimeout(2000);
        
        // 查找用户名输入框（尝试多种选择器）
        console.log('  2. 查找用户名输入框...');
        const usernameSelectors = [
            'input[type="text"]',
            'input[name="username"]',
            'input[id*="username"]',
            'input[id*="login-username"]',
            '#login-username',
            'input[placeholder*="admin"]',
            'input[placeholder*="用户名"]',
            'input[placeholder*="username"]'
        ];
        
        let usernameInput = null;
        for (const selector of usernameSelectors) {
            try {
                usernameInput = await page.locator(selector).first();
                if (await usernameInput.count() > 0) {
                    console.log(`    ✓ 找到用户名输入框: ${selector}`);
                    break;
                }
            } catch (e) {
                continue;
            }
        }
        
        if (!usernameInput || await usernameInput.count() === 0) {
            // 如果找不到，尝试获取所有输入框
            const allInputs = await page.locator('input').all();
            console.log(`    找到 ${allInputs.length} 个输入框`);
            if (allInputs.length > 0) {
                usernameInput = allInputs[0];
            }
        }
        
        if (!usernameInput || await usernameInput.count() === 0) {
            throw new Error('无法找到用户名输入框');
        }
        
        // 输入用户名
        console.log('  3. 输入用户名: admin');
        await usernameInput.fill('admin');
        await page.waitForTimeout(500);
        
        // 查找密码输入框
        console.log('  4. 查找密码输入框...');
        const passwordSelectors = [
            'input[type="password"]',
            'input[name="password"]',
            'input[id*="password"]',
            'input[id*="login-password"]',
            '#login-password'
        ];
        
        let passwordInput = null;
        for (const selector of passwordSelectors) {
            try {
                passwordInput = await page.locator(selector).first();
                if (await passwordInput.count() > 0) {
                    console.log(`    ✓ 找到密码输入框: ${selector}`);
                    break;
                }
            } catch (e) {
                continue;
            }
        }
        
        if (!passwordInput || await passwordInput.count() === 0) {
            const allInputs = await page.locator('input').all();
            if (allInputs.length > 1) {
                passwordInput = allInputs[1];
            }
        }
        
        if (!passwordInput || await passwordInput.count() === 0) {
            throw new Error('无法找到密码输入框');
        }
        
        // 输入密码
        console.log('  5. 输入密码: Admin@12345');
        await passwordInput.fill('Admin@12345');
        await page.waitForTimeout(500);
        
        // 查找登录按钮
        console.log('  6. 查找登录按钮...');
        const buttonSelectors = [
            'button[type="submit"]',
            'button:has-text("登录")',
            'button:has-text("Login")',
            'button:has-text("ログイン")',
            'button.ant-btn-primary',
            'button[class*="primary"]'
        ];
        
        let loginButton = null;
        for (const selector of buttonSelectors) {
            try {
                loginButton = await page.locator(selector).first();
                if (await loginButton.count() > 0) {
                    console.log(`    ✓ 找到登录按钮: ${selector}`);
                    break;
                }
            } catch (e) {
                continue;
            }
        }
        
        if (!loginButton || await loginButton.count() === 0) {
            // 尝试获取所有按钮
            const allButtons = await page.locator('button').all();
            console.log(`    找到 ${allButtons.length} 个按钮`);
            if (allButtons.length > 0) {
                loginButton = allButtons[allButtons.length - 1]; // 通常登录按钮在最后
            }
        }
        
        if (!loginButton || await loginButton.count() === 0) {
            throw new Error('无法找到登录按钮');
        }
        
        // 点击登录按钮
        console.log('  7. 点击登录按钮...');
        await loginButton.click();
        console.log('    ✓ 已点击登录按钮');
        
        // 等待页面跳转或加载完成（最多等待15秒）
        console.log('  8. 等待登录响应...');
        try {
            // 等待页面跳转或网络请求完成
            await Promise.race([
                page.waitForURL('**/dashboard', { timeout: 15000 }),
                page.waitForURL('**/', { timeout: 15000 }),
                page.waitForLoadState('networkidle', { timeout: 15000 })
            ]);
            
            const currentUrl = page.url();
            console.log(`    ✓ 页面已跳转到: ${currentUrl}`);
            
            // 检查是否成功登录
            // 1. 检查URL是否跳转到首页或仪表板
            const isDashboard = currentUrl.includes('/dashboard') || 
                                currentUrl === 'http://localhost:3000/' || 
                                currentUrl === 'http://localhost:3000';
            
            // 2. 检查localStorage中是否有accessToken
            const accessToken = await page.evaluate(() => {
                return localStorage.getItem('accessToken');
            });
            
            // 3. 检查页面是否还在登录页（如果还在登录页，说明登录失败）
            const isStillOnLoginPage = currentUrl.includes('/login');
            
            console.log(`    当前URL: ${currentUrl}`);
            console.log(`    是否在登录页: ${isStillOnLoginPage}`);
            console.log(`    是否有accessToken: ${accessToken ? '是' : '否'}`);
            
            if (isStillOnLoginPage) {
                // 如果还在登录页，检查是否有错误消息
                const errorElements = await page.locator('[class*="error"], [class*="Error"], .auth-message.error, .ant-message-error').all();
                if (errorElements.length > 0) {
                    for (const elem of errorElements) {
                        const errorText = await elem.textContent();
                        if (errorText && errorText.trim()) {
                            console.log(`    错误消息: ${errorText.trim()}`);
                        }
                    }
                }
                throw new Error('登录失败：页面仍在登录页，未成功跳转');
            }
            
            if (isDashboard && accessToken) {
                console.log('');
                console.log('  ✓ TC-05 通过: 浏览器 UI 登录流程成功');
                console.log('     - 页面成功跳转到首页');
                console.log('     - accessToken 已保存');
                console.log('     - 登录功能正常');
                
                // 等待一下让用户看到结果
                await page.waitForTimeout(3000);
                
                await browser.close();
                process.exit(0);
            } else {
                throw new Error(`登录状态异常：URL=${currentUrl}, hasToken=${!!accessToken}`);
            }
        } catch (error) {
            // 检查是否有错误消息显示
            const errorElements = await page.locator('[class*="error"], [class*="Error"], .auth-message.error').all();
            if (errorElements.length > 0) {
                const errorText = await errorElements[0].textContent();
                console.log(`    ✗ 登录失败: ${errorText}`);
            }
            
            // 截图保存错误信息
            await page.screenshot({ path: 'login-test-error.png', fullPage: true });
            console.log('    错误截图已保存: login-test-error.png');
            
            throw error;
        }
    } catch (error) {
        console.log('');
        console.log('  ✗ TC-05 失败:', error.message);
        
        // 截图保存错误信息
        try {
            await page.screenshot({ path: 'login-test-error.png', fullPage: true });
            console.log('  错误截图已保存: login-test-error.png');
        } catch (e) {
            console.log('  无法保存截图:', e.message);
        }
        
        await browser.close();
        process.exit(1);
    }
})();

