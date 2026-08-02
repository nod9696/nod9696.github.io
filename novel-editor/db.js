/* db.js — minimal IndexedDB wrapper for 硯 -Suzuri-
 *
 * Data shape (one record per project, nested chapters/scenes):
 * {
 *   id, name, goal, createdAt, updatedAt,
 *   logline, synopsis,
 *   beats: [ { key, title, percent, guide, content } ],   // Save the Cat beat sheet
 *   chapters: [
 *     { id, title, order, collapsed,
 *       scenes: [ { id, title, content, order, updatedAt } ]
 *     }
 *   ]
 * }
 *
 * Also a "meta" store for small key/value app settings
 * (active project id, drive client id, drive folder id, etc).
 */

// Save the Cat!（ブレイク・スナイダー）の15ビート構成のテンプレート。
// guide は要点のみ短くまとめた独自の説明文（引用ではない）。
// percent は原著が示す「物語内でその出来事が起こる位置（ページ位置の目安）」。
// targetShare は本アプリ独自の指標で、各ビートに書く分量の目安を
// 15項目の合計が100になるよう正規化したもの（配分バーの比較に使用）。
const STC_BEATS_TEMPLATE = [
  { key: "opening_image", title: "オープニング・イメージ", percent: "1%", targetShare: 1, guide: "物語開始前の主人公を象徴する、変化前のスナップショット。" },
  { key: "theme_stated", title: "テーマの提示", percent: "5%", targetShare: 1, guide: "主人公以外の誰かが、物語全体で証明されるテーマをさりげなく口にする。" },
  { key: "setup", title: "セットアップ", percent: "1〜10%", targetShare: 8, guide: "主人公の日常・欠けているもの・直すべき人間関係や弱点を見せる。" },
  { key: "catalyst", title: "きっかけ", percent: "12%", targetShare: 1, guide: "日常を揺るがす出来事が起き、後戻りできなくなる。" },
  { key: "debate", title: "悩み", percent: "12〜25%", targetShare: 12, guide: "行くべきか、行かざるべきか。主人公が迷い、ためらう期間。" },
  { key: "break_into_two", title: "第二幕突入", percent: "25%", targetShare: 1, guide: "主人公が能動的な選択をして、非日常の世界へ踏み出す。" },
  { key: "b_story", title: "Bストーリー", percent: "30%", targetShare: 1, guide: "テーマを別角度から映す、もう一つの関係性（恋愛・友情など）が始まる。" },
  { key: "fun_and_games", title: "お楽しみ", percent: "30〜50%", targetShare: 23, guide: "作品の見せ場。予告編に使われるような場面が続くパート。" },
  { key: "midpoint", title: "ミッドポイント", percent: "50%", targetShare: 1, guide: "偽りの勝利、または偽りの敗北。賭け金が明確に上がる転換点。" },
  { key: "bad_guys_close_in", title: "迫りくる悪役", percent: "55〜75%", targetShare: 18, guide: "外部の敵に加え、内部の疑念やチームの亀裂も主人公を追い詰める。" },
  { key: "all_is_lost", title: "すべてを失う", percent: "75%", targetShare: 1, guide: "最も大切なものを失う、どん底の瞬間。" },
  { key: "dark_night", title: "魂の暗夜", percent: "75〜80%", targetShare: 9, guide: "どん底で絶望を味わい、そこから立ち上がる糸口を見つける。" },
  { key: "break_into_three", title: "第三幕突入", percent: "80%", targetShare: 1, guide: "Aストーリーの答えとBストーリーの学びが合流し、解決への行動が始まる。" },
  { key: "finale", title: "フィナーレ", percent: "80〜99%", targetShare: 21, guide: "学びを実践し、世界と自分自身を作り変えて決着させる。" },
  { key: "final_image", title: "ファイナル・イメージ", percent: "99〜100%", targetShare: 1, guide: "オープニング・イメージと対になる、変化後のスナップショット。" },
];

function freshBeats() {
  return STC_BEATS_TEMPLATE.map((b) => ({ ...b, content: "" }));
}

// backfills missing fields on projects created before this feature existed,
// and preserves any beat content already written under a matching key.
function ensureProjectShape(project) {
  if (!project) return project;
  if (typeof project.logline !== "string") project.logline = "";
  if (typeof project.synopsis !== "string") project.synopsis = "";
  if (!Array.isArray(project.beats) || project.beats.length !== STC_BEATS_TEMPLATE.length) {
    const existingByKey = {};
    (project.beats || []).forEach((b) => {
      if (b && b.key) existingByKey[b.key] = b.content || "";
    });
    project.beats = STC_BEATS_TEMPLATE.map((b) => ({ ...b, content: existingByKey[b.key] || "" }));
  } else {
    // backfill targetShare on beats saved before this field existed
    project.beats.forEach((b, i) => {
      if (typeof b.targetShare !== "number") b.targetShare = STC_BEATS_TEMPLATE[i].targetShare;
    });
  }
  (project.chapters || []).forEach((ch) => {
    (ch.scenes || []).forEach((sc) => {
      if (!("beatKey" in sc)) sc.beatKey = null;
      if (typeof sc.summary !== "string") sc.summary = "";
    });
  });
  return project;
}

const SuzuriDB = (() => {
  const DB_NAME = "suzuri-novel-db";
  const DB_VERSION = 1;
  let dbPromise = null;

  function open() {
    if (dbPromise) return dbPromise;
    dbPromise = new Promise((resolve, reject) => {
      const req = indexedDB.open(DB_NAME, DB_VERSION);
      req.onupgradeneeded = (e) => {
        const db = e.target.result;
        if (!db.objectStoreNames.contains("projects")) {
          db.createObjectStore("projects", { keyPath: "id" });
        }
        if (!db.objectStoreNames.contains("meta")) {
          db.createObjectStore("meta", { keyPath: "key" });
        }
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
    return dbPromise;
  }

  function tx(storeName, mode) {
    return open().then((db) => db.transaction(storeName, mode).objectStore(storeName));
  }

  function uid(prefix) {
    return `${prefix}_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`;
  }

  // ---- projects ----
  async function listProjects() {
    const store = await tx("projects", "readonly");
    return new Promise((resolve, reject) => {
      const req = store.getAll();
      req.onsuccess = () => resolve(req.result.sort((a, b) => a.createdAt - b.createdAt));
      req.onerror = () => reject(req.error);
    });
  }

  async function getProject(id) {
    const store = await tx("projects", "readonly");
    return new Promise((resolve, reject) => {
      const req = store.get(id);
      req.onsuccess = () => resolve(req.result ? ensureProjectShape(req.result) : null);
      req.onerror = () => reject(req.error);
    });
  }

  async function putProject(project) {
    project.updatedAt = Date.now();
    ensureProjectShape(project);
    const store = await tx("projects", "readwrite");
    return new Promise((resolve, reject) => {
      const req = store.put(project);
      req.onsuccess = () => resolve(project);
      req.onerror = () => reject(req.error);
    });
  }

  async function deleteProject(id) {
    const store = await tx("projects", "readwrite");
    return new Promise((resolve, reject) => {
      const req = store.delete(id);
      req.onsuccess = () => resolve();
      req.onerror = () => reject(req.error);
    });
  }

  function createEmptyProject(name) {
    const now = Date.now();
    return {
      id: uid("proj"),
      name: name || "無題の作品",
      goal: 0,
      createdAt: now,
      updatedAt: now,
      logline: "",
      synopsis: "",
      beats: freshBeats(),
      chapters: [
        {
          id: uid("chap"),
          title: "第一章",
          order: 0,
          collapsed: false,
          scenes: [
            { id: uid("scene"), title: "シーン1", content: "", order: 0, updatedAt: now, beatKey: null, summary: "" },
          ],
        },
      ],
    };
  }

  // ---- meta ----
  async function getMeta(key, fallback = null) {
    const store = await tx("meta", "readonly");
    return new Promise((resolve, reject) => {
      const req = store.get(key);
      req.onsuccess = () => resolve(req.result ? req.result.value : fallback);
      req.onerror = () => reject(req.error);
    });
  }

  async function setMeta(key, value) {
    const store = await tx("meta", "readwrite");
    return new Promise((resolve, reject) => {
      const req = store.put({ key, value });
      req.onsuccess = () => resolve();
      req.onerror = () => reject(req.error);
    });
  }

  return {
    uid,
    listProjects,
    getProject,
    putProject,
    deleteProject,
    createEmptyProject,
    getMeta,
    setMeta,
    STC_BEATS_TEMPLATE,
    ensureProjectShape,
  };
})();

