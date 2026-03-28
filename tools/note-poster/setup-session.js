/**
 * setup-session.js
 * 初回のみ実行: Googleアカウントでnote.comにログインし、セッションを保存する
 * 使い方: node setup-session.js
 */

const { chromium } = require('playwright');
const path = require('path');

const SESSION_FILE = path.join(__dirname, 'session.json');

(async () => {
  console.log('=== note.com セッションセットアップ ===');
  console.log('ブラウザが開きます。Googleアカウントでnote.comにログインしてください。');
  console.log('ログイン完了後、このスクリプトが自動的にセッションを保存します。\n');

  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // note.comのログインページへ
  await page.goto('https://note.com/login');
  await page.waitForLoadState('domcontentloaded');

  // Googleログインボタンをクリック
  try {
    const googleBtn = page.getByRole('button', { name: /Google/i })
      .or(page.locator('a[href*="google"]').first())
      .or(page.locator('button:has-text("Google")').first());
    await googleBtn.waitFor({ timeout: 10000 });
    await googleBtn.click();
    console.log('Googleログインボタンをクリックしました。ブラウザでGoogleアカウントを選択してください。');
  } catch {
    console.log('Googleボタンが見つかりませんでした。手動でGoogleログインを進めてください。');
  }

  // ログイン完了を待つ（note.comのホームページに遷移するまで）
  console.log('\nログイン完了を待機中...');
  await page.waitForURL('https://note.com/**', { timeout: 120000 });

  // ログイン後のURLがダッシュボードなら成功
  const currentUrl = page.url();
  if (currentUrl.includes('note.com') && !currentUrl.includes('login')) {
    console.log(`\nログイン成功: ${currentUrl}`);
    await context.storageState({ path: SESSION_FILE });
    console.log(`セッションを保存しました: ${SESSION_FILE}`);
    console.log('\n次回からは自動でログインできます。');
  } else {
    console.error('ログインに失敗した可能性があります。もう一度お試しください。');
  }

  await browser.close();
})();
