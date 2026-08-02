/* notion-sync.js — optional Notion backup for 硯 -Suzuri-
 *
 * The Notion API does not send CORS headers, so it cannot be called
 * directly from a browser. This module instead calls a small Google
 * Apps Script Web App (deployed by the user — see README) that forwards
 * requests to api.notion.com and returns the JSON response.
 *
 * Requires an "internal integration" secret from
 * https://www.notion.so/my-integrations, and a parent page in Notion
 * that has been shared with that integration. Each project is backed
 * up as a child page named "suzuri-<projectId>" under that parent,
 * with the full project JSON stored as a series of code blocks
 * (Notion limits each text block to ~2000 characters, so long
 * manuscripts are split into multiple blocks and re-joined on read).
 */

const SuzuriNotion = (() => {
  const NOTION_VERSION = "2022-06-28";
  const CHUNK_SIZE = 1900; // stay safely under Notion's 2000-char rich_text limit
  const APPEND_BATCH = 80; // stay under Notion's 100-blocks-per-call limit

  let token = null;
  let proxyUrl = null;
  let parentPageId = null;

  function isConfigured() {
    return !!(token && proxyUrl && parentPageId);
  }

  async function loadSaved() {
    token = await SuzuriDB.getMeta("notionToken", null);
    proxyUrl = await SuzuriDB.getMeta("notionProxyUrl", null);
    parentPageId = await SuzuriDB.getMeta("notionParentPageId", null);
    return isConfigured();
  }

  async function configure({ token: t, proxyUrl: p, parentPageId: pid }) {
    token = t;
    proxyUrl = p;
    parentPageId = normalizePageId(pid);
    await SuzuriDB.setMeta("notionToken", token);
    await SuzuriDB.setMeta("notionProxyUrl", proxyUrl);
    await SuzuriDB.setMeta("notionParentPageId", parentPageId);
  }

  // accepts either a raw 32-char id or a full Notion URL and extracts the id
  function normalizePageId(input) {
    if (!input) return input;
    const hex = input.replace(/[^a-zA-Z0-9]/g, "");
    const last32 = hex.slice(-32);
    if (last32.length === 32) {
      return `${last32.slice(0, 8)}-${last32.slice(8, 12)}-${last32.slice(12, 16)}-${last32.slice(16, 20)}-${last32.slice(20)}`;
    }
    return input.trim();
  }

  // calls the GAS proxy using a "simple request" (text/plain body) so the
  // browser never sends a CORS preflight — Apps Script Web Apps handle
  // simple POSTs fine but are unreliable with the OPTIONS preflight.
  async function callProxy(method, path, body) {
    if (!isConfigured()) throw new Error("Notion連携が未設定です");
    const res = await fetch(proxyUrl, {
      method: "POST",
      headers: { "Content-Type": "text/plain;charset=utf-8" },
      body: JSON.stringify({ token, method, path, body: body || null }),
    });
    if (!res.ok) {
      throw new Error(`プロキシエラー (${res.status})`);
    }
    const data = await res.json();
    if (data.error) throw new Error(`Notion APIエラー: ${data.error}`);
    if (data.object === "error") throw new Error(`Notion APIエラー: ${data.message || data.code}`);
    return data;
  }

  async function listAllChildren(blockId) {
    let results = [];
    let cursor = null;
    do {
      const qs = cursor ? `?start_cursor=${cursor}&page_size=100` : `?page_size=100`;
      const res = await callProxy("get", `blocks/${blockId}/children${qs}`);
      results = results.concat(res.results || []);
      cursor = res.has_more ? res.next_cursor : null;
    } while (cursor);
    return results;
  }

  async function findBackupPage(projectId) {
    const targetTitle = `suzuri-${projectId}`;
    const children = await listAllChildren(parentPageId);
    return (
      children.find(
        (b) => b.type === "child_page" && b.child_page && b.child_page.title === targetTitle
      ) || null
    );
  }

  function chunkString(str, size) {
    const chunks = [];
    for (let i = 0; i < str.length; i += size) chunks.push(str.slice(i, i + size));
    if (chunks.length === 0) chunks.push("");
    return chunks;
  }

  function codeBlock(text) {
    return {
      object: "block",
      type: "code",
      code: {
        language: "json",
        rich_text: [{ type: "text", text: { content: text } }],
      },
    };
  }

  async function appendChunksInBatches(pageId, chunks) {
    for (let i = 0; i < chunks.length; i += APPEND_BATCH) {
      const batch = chunks.slice(i, i + APPEND_BATCH).map(codeBlock);
      await callProxy("patch", `blocks/${pageId}/children`, { children: batch });
    }
  }

  async function archiveAllChildren(pageId) {
    const children = await listAllChildren(pageId);
    for (const block of children) {
      await callProxy("patch", `blocks/${block.id}`, { archived: true });
    }
  }

  async function uploadProject(project) {
    const title = `suzuri-${project.id}`;
    const json = JSON.stringify(project);
    const chunks = chunkString(json, CHUNK_SIZE);

    const existing = await findBackupPage(project.id);

    if (existing) {
      await archiveAllChildren(existing.id);
      await appendChunksInBatches(existing.id, chunks);
      // keep the visible page title in sync with the project's display name
      await callProxy("patch", `pages/${existing.id}`, {
        properties: { title: [{ text: { content: `${title} — ${project.name}` } }] },
      });
    } else {
      const firstBatch = chunks.slice(0, 90).map(codeBlock);
      const created = await callProxy("post", "pages", {
        parent: { page_id: parentPageId },
        properties: { title: [{ text: { content: `${title} — ${project.name}` } }] },
        children: firstBatch,
      });
      if (chunks.length > 90) {
        await appendChunksInBatches(created.id, chunks.slice(90));
      }
    }
    return true;
  }

  async function downloadProject(projectId) {
    const page = await findBackupPage(projectId);
    if (!page) throw new Error("Notionにバックアップが見つかりません");
    const children = await listAllChildren(page.id);
    const text = children
      .filter((b) => b.type === "code")
      .map((b) => (b.code.rich_text || []).map((rt) => rt.plain_text || "").join(""))
      .join("");
    return JSON.parse(text);
  }

  async function testConnection() {
    // a lightweight call that both confirms the token/proxy work and that
    // the parent page has actually been shared with the integration
    await callProxy("get", `blocks/${parentPageId}`);
    return true;
  }

  return {
    isConfigured,
    loadSaved,
    configure,
    uploadProject,
    downloadProject,
    testConnection,
  };
})();
