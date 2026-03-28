/**
 * post-to-note.js
 * 最新のブログ記事(blog_indie_games_*.md)をnote.comに自動投稿する
 * 使い方: node post-to-note.js [--dry-run]
 *
 * --dry-run: 実際には投稿せずに内容確認のみ行う
 */

const { chromium } = require('playwright');
const { marked } = require('marked');
const fs = require('fs');
const path = require('path');
require('dotenv').config({ path: path.join(__dirname, '.env') });

const SESSION_FILE = path.join(__dirname, 'session.json');
const BLOG_DIR = process.env.BLOG_DIR || 'f:/Claude';
const NOTE_TAGS = (process.env.NOTE_TAGS || 'インディーゲーム,ゲームレビュー,Steam').split(',').map(t => t.trim());
const HEADLESS = process.env.HEADLESS !== 'false';
const DRY_RUN = process.argv.includes('--dry-run');
const LOG_FILE = path.join(__dirname, 'post.log');

function log(msg) {
  const timestamp = new Date().toLocaleString('ja-JP');
  const line = `[${timestamp}] ${msg}`;
  console.log(line);
  fs.appendFileSync(LOG_FILE, line + '\n');
}

// 今週のブログファイルを取得（過去7日以内）
function getThisWeeksBlogFile() {
  const files = fs.readdirSync(BLOG_DIR)
    .filter(f => f.match(/^blog_indie_games_\d{4}_\d{2}_\d{2}\.md$/))
    .sort()
    .reverse();

  if (files.length === 0) throw new Error('ブログファイルが見つかりません');

  const latest = files[0];
  // ファイル名から日付を取得 (blog_indie_games_YYYY_MM_DD.md)
  const match = latest.match(/(\d{4})_(\d{2})_(\d{2})/);
  if (match) {
    const fileDate = new Date(`${match[1]}-${match[2]}-${match[3]}`);
    const daysAgo = (Date.now() - fileDate.getTime()) / (1000 * 60 * 60 * 24);
    if (daysAgo > 7) {
      throw new Error(`最新ファイル(${latest})が7日以上前のものです。/indie-blog を実行して今週の記事を生成してください。`);
    }
  }

  return path.join(BLOG_DIR, latest);
}

// Markdownファイルをパースしてタイトルと本文を取得
function parseBlogFile(filePath) {
  const raw = fs.readFileSync(filePath, 'utf-8');
  const lines = raw.split('\n');

  // H1をタイトルとして抽出
  const titleLine = lines.find(l => l.startsWith('# '));
  const title = titleLine ? titleLine.replace(/^#\s+/, '').trim() : 'インディーゲームTOP10';

  // 参照ソースセクション（最後の ---以降）を除いた本文
  const lastSeparator = raw.lastIndexOf('\n---\n');
  const bodyRaw = lastSeparator > 0 ? raw.slice(0, lastSeparator) : raw;

  // タイトル行を除いたMarkdown本文
  const bodyMarkdown = bodyRaw.replace(/^#[^\n]*\n/, '').trim();

  // note.com用: ★を絵文字的にそのまま使い、Markdownをプレーンテキストに寄せた形で渡す
  // note.comエディタはmarkdown記法の一部を解釈するため、見出し(##)等はそのまま残す
  return { title, bodyMarkdown, raw };
}

// note.comのエディタに本文を入力する
async function typeContent(page, bodyMarkdown) {
  // エディタのbody領域を探してクリック
  const editorSelectors = [
    '.ProseMirror',
    '[contenteditable="true"][role="textbox"]',
    '.editor-styles-wrapper [contenteditable]',
    '[data-testid="editor-body"]',
    '.o-notebook-note'
  ];

  let editor = null;
  for (const sel of editorSelectors) {
    const el = page.locator(sel).last();
    if (await el.count() > 0) {
      editor = el;
      break;
    }
  }

  if (!editor) throw new Error('エディタが見つかりませんでした。selectors を確認してください。');

  await editor.click();
  await page.waitForTimeout(500);

  // クリップボード経由でHTMLとして貼り付け
  // note.comはHTMLのペーストを受け付けるため、markedでHTML変換してから貼り付け
  const html = marked(bodyMarkdown);
  await page.evaluate((htmlContent) => {
    const dt = new DataTransfer();
    dt.setData('text/html', htmlContent);
    dt.setData('text/plain', document.createElement('div').innerHTML = htmlContent);
    const el = document.activeElement;
    el.dispatchEvent(new ClipboardEvent('paste', {
      clipboardData: dt,
      bubbles: true,
      cancelable: true
    }));
  }, html);

  // フォールバック: クリップボードAPIが効かない場合はキータイプ
  const editorText = await editor.textContent();
  if (!editorText || editorText.trim() === '') {
    log('クリップボードペーストが効きませんでした。プレーンテキストで入力します...');
    await editor.click();
    // Markdown記法を保持したままプレーンテキストとして入力
    await page.keyboard.insertText(bodyMarkdown);
  }
}

// タグを追加する
async function addTags(page, tags) {
  const tagInputSelectors = [
    '[placeholder*="タグ"]',
    '[data-testid="tag-input"]',
    'input[name*="tag"]',
    '.p-tag-input input'
  ];

  for (const sel of tagInputSelectors) {
    const el = page.locator(sel).first();
    if (await el.count() > 0) {
      for (const tag of tags) {
        await el.click();
        await el.fill(tag);
        await page.keyboard.press('Enter');
        await page.waitForTimeout(300);
      }
      log(`タグを追加: ${tags.join(', ')}`);
      return;
    }
  }
  log('タグ入力欄が見つかりませんでした（スキップ）');
}

async function run() {
  log('=== note.com 自動投稿開始 ===');

  if (DRY_RUN) log('[DRY RUN モード] 実際の投稿は行いません');

  // セッションファイルの確認
  if (!fs.existsSync(SESSION_FILE)) {
    throw new Error('session.json が見つかりません。先に node setup-session.js を実行してください。');
  }

  // ブログファイルの取得・パース
  const blogFile = getThisWeeksBlogFile();
  log(`投稿対象ファイル: ${blogFile}`);
  const { title, bodyMarkdown } = parseBlogFile(blogFile);
  log(`タイトル: ${title}`);
  log(`本文: ${bodyMarkdown.length} 文字`);

  if (DRY_RUN) {
    log('DRY RUN: 投稿内容の確認完了。実際の投稿はスキップします。');
    return;
  }

  const browser = await chromium.launch({ headless: HEADLESS });
  const context = await browser.newContext({ storageState: SESSION_FILE });
  const page = await context.newPage();

  try {
    // 新規記事ページへ
    log('note.com 新規記事ページを開いています...');
    await page.goto('https://note.com/notes/new', { waitUntil: 'networkidle' });

    // ログイン確認
    if (page.url().includes('login')) {
      throw new Error('セッションが切れています。node setup-session.js を再実行してください。');
    }

    // タイトル入力
    const titleSelectors = [
      '[placeholder*="タイトル"]',
      '[data-placeholder*="タイトル"]',
      'h1[contenteditable]',
      '[data-testid="title"]',
      '.editor-title'
    ];

    let titleInput = null;
    for (const sel of titleSelectors) {
      const el = page.locator(sel).first();
      if (await el.count() > 0) {
        titleInput = el;
        break;
      }
    }

    if (titleInput) {
      await titleInput.click();
      await titleInput.fill(title);
      log('タイトルを入力しました');
    } else {
      log('警告: タイトル入力欄が見つかりませんでした');
    }

    await page.keyboard.press('Tab');
    await page.waitForTimeout(500);

    // 本文入力
    await typeContent(page, bodyMarkdown);
    log('本文を入力しました');

    await page.waitForTimeout(1000);

    // 公開ボタンをクリック
    const publishBtnSelectors = [
      'button:has-text("公開ボタン")',
      'button:has-text("投稿")',
      '[data-testid="publish-button"]',
      '.p-article-editor__submit button',
      'header button:last-child'
    ];

    let publishBtn = null;
    for (const sel of publishBtnSelectors) {
      const el = page.locator(sel).first();
      if (await el.count() > 0 && await el.isVisible()) {
        publishBtn = el;
        break;
      }
    }

    if (!publishBtn) throw new Error('公開ボタンが見つかりませんでした');

    await publishBtn.click();
    log('公開ボタンをクリックしました');
    await page.waitForTimeout(1500);

    // 公開設定モーダルでタグを追加
    await addTags(page, NOTE_TAGS);

    // 「公開する」確認ボタン
    const confirmSelectors = [
      'button:has-text("公開する")',
      'button:has-text("投稿する")',
      '[data-testid="confirm-publish"]',
      '.p-publish-modal__submit'
    ];

    let confirmBtn = null;
    for (const sel of confirmSelectors) {
      const el = page.locator(sel).first();
      if (await el.count() > 0 && await el.isVisible()) {
        confirmBtn = el;
        break;
      }
    }

    if (!confirmBtn) throw new Error('「公開する」ボタンが見つかりませんでした');

    await confirmBtn.click();
    log('「公開する」をクリックしました');

    // 投稿完了を待つ
    await page.waitForURL(/note\.com\/.+\/n\/.+/, { timeout: 15000 });
    const publishedUrl = page.url();
    log(`✅ 投稿完了: ${publishedUrl}`);

    // セッションを更新保存
    await context.storageState({ path: SESSION_FILE });

  } catch (err) {
    log(`❌ エラー: ${err.message}`);
    // スクリーンショットを保存（デバッグ用）
    const screenshotPath = path.join(__dirname, `error_${Date.now()}.png`);
    await page.screenshot({ path: screenshotPath });
    log(`スクリーンショット保存: ${screenshotPath}`);
    throw err;
  } finally {
    await browser.close();
  }
}

run().catch(err => {
  log(`致命的エラー: ${err.message}`);
  process.exit(1);
});
