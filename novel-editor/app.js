/* app.js — 硯 -Suzuri- application logic */

(() => {
  "use strict";

  // ---------- state ----------
  let project = null;        // current project object (full, nested)
  let activeChapterId = null;
  let activeSceneId = null;
  let viewMode = "write";    // "write" | "plot"
  let saveTimer = null;
  let saveDebounceMs = 600;

  // ---------- element refs ----------
  const el = {
    sidebar: document.getElementById("sidebar"),
    btnToggleSidebar: document.getElementById("btnToggleSidebar"),
    projectSelect: document.getElementById("projectSelect"),
    btnNewProject: document.getElementById("btnNewProject"),
    btnRenameProject: document.getElementById("btnRenameProject"),
    btnAddChapter: document.getElementById("btnAddChapter"),
    tree: document.getElementById("tree"),
    totalCharCount: document.getElementById("totalCharCount"),
    goalRow: document.getElementById("goalRow"),
    goalCharCount: document.getElementById("goalCharCount"),
    goalBarTrack: document.getElementById("goalBarTrack"),
    goalBarFill: document.getElementById("goalBarFill"),

    breadcrumb: document.getElementById("sceneBreadcrumb"),
    saveStatus: document.getElementById("saveStatus"),
    btnFocus: document.getElementById("btnFocus"),
    btnPlot: document.getElementById("btnPlot"),
    btnSettings: document.getElementById("btnSettings"),

    writeView: document.getElementById("writeView"),
    plotView: document.getElementById("plotView"),
    loglineInput: document.getElementById("loglineInput"),
    synopsisInput: document.getElementById("synopsisInput"),
    beatList: document.getElementById("beatList"),

    sceneTitle: document.getElementById("sceneTitle"),
    sceneBeatSelect: document.getElementById("sceneBeatSelect"),
    sceneBeatAllocation: document.getElementById("sceneBeatAllocation"),
    sceneSummary: document.getElementById("sceneSummary"),
    editor: document.getElementById("editor"),
    tickRail: document.getElementById("tickRail"),
    tickRailFill: document.getElementById("tickRailFill"),
    tickMarks: document.getElementById("tickMarks"),

    sceneCharCount: document.getElementById("sceneCharCount"),
    sceneManuscriptPages: document.getElementById("sceneManuscriptPages"),
    lastSavedAt: document.getElementById("lastSavedAt"),
    focusHint: document.getElementById("focusHint"),

    settingsModal: document.getElementById("settingsModal"),
    btnCloseSettings: document.getElementById("btnCloseSettings"),
    goalInput: document.getElementById("goalInput"),
    driveClientId: document.getElementById("driveClientId"),
    btnSaveClientId: document.getElementById("btnSaveClientId"),
    driveAuthRow: document.getElementById("driveAuthRow"),
    btnDriveSignIn: document.getElementById("btnDriveSignIn"),
    driveAuthStatus: document.getElementById("driveAuthStatus"),
    driveActionsRow: document.getElementById("driveActionsRow"),
    btnDriveUpload: document.getElementById("btnDriveUpload"),
    btnDriveDownload: document.getElementById("btnDriveDownload"),
    driveSyncStatus: document.getElementById("driveSyncStatus"),

    notionToken: document.getElementById("notionToken"),
    notionParentPageId: document.getElementById("notionParentPageId"),
    notionProxyUrl: document.getElementById("notionProxyUrl"),
    btnSaveNotionConfig: document.getElementById("btnSaveNotionConfig"),
    btnNotionTest: document.getElementById("btnNotionTest"),
    notionAuthStatus: document.getElementById("notionAuthStatus"),
    btnNotionUpload: document.getElementById("btnNotionUpload"),
    btnNotionDownload: document.getElementById("btnNotionDownload"),
    notionSyncStatus: document.getElementById("notionSyncStatus"),

    btnExportJson: document.getElementById("btnExportJson"),
    btnImportJson: document.getElementById("btnImportJson"),
    importFileInput: document.getElementById("importFileInput"),

    promptModal: document.getElementById("promptModal"),
    promptLabel: document.getElementById("promptLabel"),
    promptInput: document.getElementById("promptInput"),
    promptCancel: document.getElementById("promptCancel"),
    promptOk: document.getElementById("promptOk"),

    app: document.getElementById("app"),
  };

  // ---------- utils ----------
  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  function fmtTime(ts) {
    if (!ts) return "未保存";
    const d = new Date(ts);
    const pad = (n) => String(n).padStart(2, "0");
    return `${pad(d.getHours())}:${pad(d.getMinutes())} に保存`;
  }

  function charLen(str) {
    return [...(str || "")].length;
  }

  // simple promise-based prompt dialog (replaces window.prompt)
  function showPrompt(label, defaultValue = "") {
    return new Promise((resolve) => {
      el.promptLabel.textContent = label;
      el.promptInput.value = defaultValue;
      el.promptModal.hidden = false;
      el.promptInput.focus();
      el.promptInput.select();

      function cleanup(result) {
        el.promptModal.hidden = true;
        el.promptOk.removeEventListener("click", onOk);
        el.promptCancel.removeEventListener("click", onCancel);
        el.promptInput.removeEventListener("keydown", onKey);
        resolve(result);
      }
      function onOk() {
        const v = el.promptInput.value.trim();
        cleanup(v || null);
      }
      function onCancel() {
        cleanup(null);
      }
      function onKey(e) {
        if (e.key === "Enter") onOk();
        if (e.key === "Escape") onCancel();
      }
      el.promptOk.addEventListener("click", onOk);
      el.promptCancel.addEventListener("click", onCancel);
      el.promptInput.addEventListener("keydown", onKey);
    });
  }

  // ---------- lightweight context menu ----------
  let menuEl = null;
  function closeMenu() {
    if (menuEl) {
      menuEl.remove();
      menuEl = null;
      document.removeEventListener("click", closeMenu, true);
    }
  }
  function openMenu(x, y, items) {
    closeMenu();
    menuEl = document.createElement("div");
    menuEl.className = "ctx-menu";
    Object.assign(menuEl.style, {
      position: "fixed",
      left: x + "px",
      top: y + "px",
      background: "#F3EFE4",
      border: "1px solid #C9BFA8",
      borderRadius: "6px",
      boxShadow: "0 8px 24px rgba(0,0,0,0.25)",
      padding: "4px",
      zIndex: 100,
      minWidth: "140px",
      fontFamily: '"Noto Serif JP", serif',
    });
    items.forEach((item) => {
      const btn = document.createElement("button");
      btn.textContent = item.label;
      Object.assign(btn.style, {
        display: "block",
        width: "100%",
        textAlign: "left",
        padding: "7px 10px",
        background: "transparent",
        border: "none",
        cursor: "pointer",
        fontSize: "13px",
        color: item.danger ? "#B33A3A" : "#23201A",
        borderRadius: "4px",
      });
      btn.addEventListener("mouseenter", () => (btn.style.background = "rgba(0,0,0,0.06)"));
      btn.addEventListener("mouseleave", () => (btn.style.background = "transparent"));
      btn.addEventListener("click", (e) => {
        e.stopPropagation();
        closeMenu();
        item.onClick();
      });
      menuEl.appendChild(btn);
    });
    document.body.appendChild(menuEl);
    setTimeout(() => document.addEventListener("click", closeMenu, true), 0);
  }

  // ---------- project loading ----------
  async function refreshProjectSelect() {
    const projects = await SuzuriDB.listProjects();
    el.projectSelect.innerHTML = "";
    projects.forEach((p) => {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = p.name;
      el.projectSelect.appendChild(opt);
    });
    return projects;
  }

  async function loadProject(id) {
    project = await SuzuriDB.getProject(id);
    if (!project) return;
    await SuzuriDB.setMeta("activeProjectId", project.id);
    el.projectSelect.value = project.id;

    // pick first scene as active
    const firstChapter = project.chapters[0];
    activeChapterId = firstChapter ? firstChapter.id : null;
    activeSceneId = firstChapter && firstChapter.scenes[0] ? firstChapter.scenes[0].id : null;

    renderTree();
    populateBeatSelect();
    loadSceneIntoEditor();
    updateTotals();
    el.goalInput.value = project.goal || "";
    renderPlotView();
    updateBreadcrumb();
  }

  async function initProjects() {
    let projects = await refreshProjectSelect();
    if (projects.length === 0) {
      const np = SuzuriDB.createEmptyProject("無題の作品");
      await SuzuriDB.putProject(np);
      projects = await refreshProjectSelect();
    }
    const savedActiveId = await SuzuriDB.getMeta("activeProjectId", null);
    const activeId = projects.find((p) => p.id === savedActiveId) ? savedActiveId : projects[0].id;
    await loadProject(activeId);
  }

  // ---------- tree rendering ----------
  function findScene(sceneId) {
    for (const ch of project.chapters) {
      const sc = ch.scenes.find((s) => s.id === sceneId);
      if (sc) return { chapter: ch, scene: sc };
    }
    return null;
  }

  // sum of characters across every scene in the project, and per-beat totals
  // keyed by beatKey — powers both the write-view allocation readout and the
  // plot-view allocation bars.
  function computeBeatStats() {
    const byKey = {};
    project.beats.forEach((b) => (byKey[b.key] = { charTotal: 0, scenes: [] }));
    let totalChars = 0;
    project.chapters.forEach((ch) => {
      ch.scenes.forEach((sc) => {
        const n = charLen(sc.content);
        totalChars += n;
        if (sc.beatKey && byKey[sc.beatKey]) {
          byKey[sc.beatKey].charTotal += n;
          byKey[sc.beatKey].scenes.push({ sceneId: sc.id, chapterId: ch.id, title: sc.title || "無題のシーン", charTotal: n });
        }
      });
    });
    return { byKey, totalChars };
  }

  function populateBeatSelect() {
    el.sceneBeatSelect.innerHTML = '<option value="">未設定</option>';
    project.beats.forEach((beat, i) => {
      const opt = document.createElement("option");
      opt.value = beat.key;
      opt.textContent = `${String(i + 1).padStart(2, "0")}. ${beat.title}`;
      el.sceneBeatSelect.appendChild(opt);
    });
  }

  function updateSceneBeatAllocation() {
    const found = activeSceneId ? findScene(activeSceneId) : null;
    if (!found || !found.scene.beatKey) {
      el.sceneBeatAllocation.textContent = "";
      el.sceneBeatAllocation.classList.remove("over");
      return;
    }
    const beat = project.beats.find((b) => b.key === found.scene.beatKey);
    const stats = computeBeatStats();
    const beatStat = stats.byKey[found.scene.beatKey];
    const actualPct = stats.totalChars > 0 ? (beatStat.charTotal / stats.totalChars) * 100 : 0;
    const over = actualPct > beat.targetShare * 1.15;
    el.sceneBeatAllocation.textContent = `このビート計 ${beatStat.charTotal.toLocaleString()}字（全体の${actualPct.toFixed(1)}% ／ 目安${beat.targetShare}%）`;
    el.sceneBeatAllocation.classList.toggle("over", over);
  }

  function renderTree() {
    el.tree.innerHTML = "";
    project.chapters
      .slice()
      .sort((a, b) => a.order - b.order)
      .forEach((chapter) => {
        const node = document.createElement("div");
        node.className = "chapter-node" + (chapter.collapsed ? " collapsed" : "");

        const row = document.createElement("div");
        row.className = "chapter-row";
        row.innerHTML = `
          <span class="chapter-caret">
            <svg viewBox="0 0 24 24" width="10" height="10"><path d="M6 9l6 6 6-6" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round"/></svg>
          </span>
          <span class="node-label"></span>
          <span class="node-count"></span>
          <button class="node-menu-btn" title="操作">⋯</button>
        `;
        row.querySelector(".node-label").textContent = chapter.title;
        const chapterCharTotal = chapter.scenes.reduce((s, sc) => s + charLen(sc.content), 0);
        row.querySelector(".node-count").textContent = chapterCharTotal.toLocaleString();

        row.addEventListener("click", (e) => {
          if (e.target.closest(".node-menu-btn")) return;
          chapter.collapsed = !chapter.collapsed;
          SuzuriDB.putProject(project);
          renderTree();
        });
        row.querySelector(".node-menu-btn").addEventListener("click", (e) => {
          e.stopPropagation();
          const r = e.target.getBoundingClientRect();
          openMenu(r.left, r.bottom + 4, [
            { label: "＋ シーンを追加", onClick: () => addScene(chapter.id) },
            { label: "名前を変更", onClick: () => renameChapter(chapter.id) },
            { label: "上に移動", onClick: () => moveChapter(chapter.id, -1) },
            { label: "下に移動", onClick: () => moveChapter(chapter.id, 1) },
            { label: "章を削除", danger: true, onClick: () => deleteChapter(chapter.id) },
          ]);
        });

        node.appendChild(row);

        const sceneList = document.createElement("div");
        sceneList.className = "scene-list";
        chapter.scenes
          .slice()
          .sort((a, b) => a.order - b.order)
          .forEach((scene) => {
            const srow = document.createElement("div");
            srow.className = "scene-row" + (scene.id === activeSceneId ? " active" : "");
            srow.innerHTML = `
              <span class="node-label"></span>
              <span class="beat-badge" hidden></span>
              <span class="node-count"></span>
              <button class="node-menu-btn" title="操作">⋯</button>
            `;
            srow.querySelector(".node-label").textContent = scene.title || "（無題のシーン）";
            srow.querySelector(".node-count").textContent = charLen(scene.content).toLocaleString();
            if (scene.beatKey) {
              const beatIdx = project.beats.findIndex((b) => b.key === scene.beatKey);
              if (beatIdx >= 0) {
                const badge = srow.querySelector(".beat-badge");
                badge.hidden = false;
                badge.textContent = String(beatIdx + 1).padStart(2, "0");
                badge.title = project.beats[beatIdx].title;
              }
            }

            srow.addEventListener("click", (e) => {
              if (e.target.closest(".node-menu-btn")) return;
              switchScene(chapter.id, scene.id);
            });
            srow.querySelector(".node-menu-btn").addEventListener("click", (e) => {
              e.stopPropagation();
              const r = e.target.getBoundingClientRect();
              openMenu(r.left, r.bottom + 4, [
                { label: "名前を変更", onClick: () => renameScene(chapter.id, scene.id) },
                { label: "上に移動", onClick: () => moveScene(chapter.id, scene.id, -1) },
                { label: "下に移動", onClick: () => moveScene(chapter.id, scene.id, 1) },
                { label: "シーンを削除", danger: true, onClick: () => deleteScene(chapter.id, scene.id) },
              ]);
            });

            const wrap = document.createElement("div");
            wrap.className = "scene-row-wrap";
            wrap.appendChild(srow);
            if (scene.summary && scene.summary.trim()) {
              const preview = document.createElement("div");
              preview.className = "scene-summary-preview";
              preview.textContent = scene.summary.trim();
              preview.title = scene.summary.trim();
              wrap.appendChild(preview);
            }
            sceneList.appendChild(wrap);
          });

        const addRow = document.createElement("div");
        addRow.className = "add-scene-row";
        addRow.textContent = "＋ シーンを追加";
        addRow.addEventListener("click", () => addScene(chapter.id));
        sceneList.appendChild(addRow);

        node.appendChild(sceneList);
        el.tree.appendChild(node);
      });
  }

  // ---------- scene switching / editing ----------
  function switchScene(chapterId, sceneId) {
    flushSave();
    activeChapterId = chapterId;
    activeSceneId = sceneId;
    renderTree();
    loadSceneIntoEditor();
  }

  function loadSceneIntoEditor() {
    const found = activeSceneId ? findScene(activeSceneId) : null;
    if (!found) {
      el.sceneTitle.value = "";
      el.sceneSummary.value = "";
      el.editor.value = "";
      el.editor.disabled = true;
      el.sceneTitle.disabled = true;
      el.sceneBeatSelect.disabled = true;
      el.sceneSummary.disabled = true;
      el.sceneBeatAllocation.textContent = "";
      el.breadcrumb.textContent = "シーンがありません — ＋章で追加してください";
      updateCounts("");
      return;
    }
    el.editor.disabled = false;
    el.sceneTitle.disabled = false;
    el.sceneBeatSelect.disabled = false;
    el.sceneSummary.disabled = false;
    const { chapter, scene } = found;
    el.sceneTitle.value = scene.title;
    el.sceneBeatSelect.value = scene.beatKey || "";
    el.sceneSummary.value = scene.summary || "";
    el.editor.value = scene.content;
    updateBreadcrumb();
    el.lastSavedAt.textContent = fmtTime(scene.updatedAt);
    updateCounts(scene.content);
    setSaveStatus("idle");
  }

  function updateCounts(content) {
    const n = charLen(content);
    el.sceneCharCount.textContent = `${n.toLocaleString()} 字`;
    el.sceneManuscriptPages.textContent = `${(n / 400).toFixed(1)} 枚（400字換算）`;
    renderTickRail(n);
    updateSceneBeatAllocation();
  }

  function updateTotals() {
    const total = project.chapters.reduce(
      (sum, ch) => sum + ch.scenes.reduce((s, sc) => s + charLen(sc.content), 0),
      0
    );
    el.totalCharCount.textContent = total.toLocaleString();
    if (project.goal && project.goal > 0) {
      el.goalRow.hidden = false;
      el.goalBarTrack.hidden = false;
      el.goalCharCount.textContent = project.goal.toLocaleString();
      const pct = Math.min(100, (total / project.goal) * 100);
      el.goalBarFill.style.width = pct + "%";
    } else {
      el.goalRow.hidden = true;
      el.goalBarTrack.hidden = true;
    }
  }

  // manuscript-paper tick rail: signature visual element.
  // Represents the current scene's length in 400-character units ("枚").
  function renderTickRail(charCount) {
    const unit = 400;
    const target = Math.max(2000, Math.ceil((charCount + unit) / 2000) * 2000);
    const numTicks = target / unit;
    el.tickMarks.innerHTML = "";
    for (let i = 1; i <= numTicks; i++) {
      const pct = (i / numTicks) * 100;
      const mark = document.createElement("div");
      mark.className = "tick-mark";
      mark.style.bottom = pct + "%";
      el.tickMarks.appendChild(mark);
      if (i % 5 === 0 || i === numTicks) {
        const label = document.createElement("div");
        label.className = "tick-mark-label";
        label.style.bottom = pct + "%";
        label.textContent = i;
        el.tickMarks.appendChild(label);
      }
    }
    const fillPct = Math.min(100, (charCount / target) * 100);
    el.tickRailFill.style.height = fillPct + "%";
  }

  function updateBreadcrumb() {
    if (!project) return;
    if (viewMode === "plot") {
      el.breadcrumb.textContent = `${project.name} ／ ログライン・プロット・ビートシート`;
      return;
    }
    const found = activeSceneId ? findScene(activeSceneId) : null;
    if (found) {
      el.breadcrumb.textContent = `${project.name} ／ ${found.chapter.title} ／ ${found.scene.title || "無題のシーン"}`;
    } else {
      el.breadcrumb.textContent = project.name;
    }
  }

  function setViewMode(mode) {
    viewMode = mode;
    el.app.classList.toggle("view-plot", mode === "plot");
    if (mode === "plot" && el.app.classList.contains("focus-mode")) {
      toggleFocusMode(false);
    }
    if (mode === "plot") renderPlotView();
    updateBreadcrumb();
  }

  function renderPlotView() {
    if (!project) return;
    el.loglineInput.value = project.logline || "";
    el.synopsisInput.value = project.synopsis || "";

    const stats = computeBeatStats();

    el.beatList.innerHTML = "";
    project.beats.forEach((beat, i) => {
      const beatStat = stats.byKey[beat.key];
      const actualPct = stats.totalChars > 0 ? (beatStat.charTotal / stats.totalChars) * 100 : 0;
      const over = actualPct > beat.targetShare * 1.15;
      const barPct = Math.min(100, actualPct);

      const item = document.createElement("div");
      item.className = "beat-item";
      item.innerHTML = `
        <div class="beat-header">
          <span class="beat-index"></span>
          <span class="beat-title"></span>
          <span class="beat-percent"></span>
        </div>
        <p class="beat-guide"></p>
        <div class="beat-allocation">
          <div class="beat-allocation-track">
            <div class="beat-allocation-fill"></div>
            <div class="beat-allocation-target-marker"></div>
          </div>
          <div class="beat-allocation-numbers">
            <span class="beat-allocation-actual"></span>
            <span class="beat-allocation-target"></span>
          </div>
        </div>
        <div class="beat-scene-chips"></div>
        <textarea class="beat-textarea" rows="2" placeholder="このビートで起こることを書く……"></textarea>
      `;
      item.querySelector(".beat-index").textContent = String(i + 1).padStart(2, "0");
      item.querySelector(".beat-title").textContent = beat.title;
      item.querySelector(".beat-percent").textContent = beat.percent;
      item.querySelector(".beat-guide").textContent = beat.guide;

      const fill = item.querySelector(".beat-allocation-fill");
      fill.style.width = barPct + "%";
      fill.classList.toggle("over", over);
      item.querySelector(".beat-allocation-target-marker").style.left = Math.min(100, beat.targetShare) + "%";
      const actualSpan = item.querySelector(".beat-allocation-actual");
      actualSpan.textContent = `実際 ${actualPct.toFixed(1)}%（${beatStat.charTotal.toLocaleString()}字）`;
      actualSpan.classList.toggle("over", over);
      item.querySelector(".beat-allocation-target").textContent = `目安 ${beat.targetShare}%`;

      const chipsWrap = item.querySelector(".beat-scene-chips");
      if (beatStat.scenes.length === 0) {
        const empty = document.createElement("span");
        empty.className = "beat-scene-chip-empty";
        empty.textContent = "紐づくシーンなし";
        chipsWrap.appendChild(empty);
      } else {
        beatStat.scenes.forEach((s) => {
          const chip = document.createElement("button");
          chip.type = "button";
          chip.className = "beat-scene-chip";
          chip.textContent = `${s.title}（${s.charTotal.toLocaleString()}字）`;
          chip.addEventListener("click", () => {
            setViewMode("write");
            switchScene(s.chapterId, s.sceneId);
          });
          chipsWrap.appendChild(chip);
        });
      }

      const ta = item.querySelector(".beat-textarea");
      ta.value = beat.content || "";
      ta.addEventListener("input", () => {
        beat.content = ta.value;
        setSaveStatus("saving");
        scheduleSave();
      });
      el.beatList.appendChild(item);
    });
  }

  function onLoglineInput() {
    if (!project) return;
    project.logline = el.loglineInput.value;
    setSaveStatus("saving");
    scheduleSave();
  }

  function onSynopsisInput() {
    if (!project) return;
    project.synopsis = el.synopsisInput.value;
    setSaveStatus("saving");
    scheduleSave();
  }
  function setSaveStatus(state) {
    el.saveStatus.dataset.state = state;
    const map = { idle: "—", saving: "保存中…", saved: "保存済み", error: "保存エラー" };
    el.saveStatus.textContent = map[state] || "—";
  }

  const scheduleSave = debounce(() => flushSave(), saveDebounceMs);

  function onSceneBeatChange() {
    const found = activeSceneId ? findScene(activeSceneId) : null;
    if (!found) return;
    found.scene.beatKey = el.sceneBeatSelect.value || null;
    setSaveStatus("saving");
    scheduleSave();
    updateSceneBeatAllocation();
    renderTree();
  }

  function onEditorInput() {
    const found = activeSceneId ? findScene(activeSceneId) : null;
    if (!found) return;
    found.scene.title = el.sceneTitle.value;
    found.scene.summary = el.sceneSummary.value;
    found.scene.content = el.editor.value;
    found.scene.updatedAt = Date.now();
    updateCounts(found.scene.content);
    setSaveStatus("saving");
    scheduleSave();
  }

  async function flushSave() {
    if (!project) return;
    try {
      await SuzuriDB.putProject(project);
      setSaveStatus("saved");
      el.lastSavedAt.textContent = fmtTime(Date.now());
      updateTotals();
      renderTreeCountsOnly();
    } catch (err) {
      console.error(err);
      setSaveStatus("error");
    }
  }

  function renderTreeCountsOnly() {
    // cheap refresh of counts + breadcrumb without losing scroll/collapse state
    updateBreadcrumb();
    renderTree();
  }

  // ---------- structural edits ----------
  async function addChapter() {
    const title = await showPrompt("新しい章のタイトル", `第${project.chapters.length + 1}章`);
    if (!title) return;
    const now = Date.now();
    project.chapters.push({
      id: SuzuriDB.uid("chap"),
      title,
      order: project.chapters.length,
      collapsed: false,
      scenes: [{ id: SuzuriDB.uid("scene"), title: "シーン1", content: "", order: 0, updatedAt: now, beatKey: null, summary: "" }],
    });
    await SuzuriDB.putProject(project);
    renderTree();
    updateTotals();
  }

  async function renameChapter(chapterId) {
    const ch = project.chapters.find((c) => c.id === chapterId);
    const title = await showPrompt("章のタイトル", ch.title);
    if (!title) return;
    ch.title = title;
    await SuzuriDB.putProject(project);
    renderTree();
    if (chapterId === activeChapterId) loadSceneIntoEditor();
  }

  async function moveChapter(chapterId, dir) {
    const sorted = project.chapters.slice().sort((a, b) => a.order - b.order);
    const idx = sorted.findIndex((c) => c.id === chapterId);
    const swapIdx = idx + dir;
    if (swapIdx < 0 || swapIdx >= sorted.length) return;
    const a = sorted[idx], b = sorted[swapIdx];
    [a.order, b.order] = [b.order, a.order];
    await SuzuriDB.putProject(project);
    renderTree();
  }

  async function deleteChapter(chapterId) {
    if (project.chapters.length <= 1) {
      alert("最後の章は削除できません。");
      return;
    }
    const ch = project.chapters.find((c) => c.id === chapterId);
    if (!confirm(`「${ch.title}」を削除しますか？中のシーンも全て削除されます。`)) return;
    project.chapters = project.chapters.filter((c) => c.id !== chapterId);
    if (activeChapterId === chapterId) {
      const fc = project.chapters[0];
      activeChapterId = fc.id;
      activeSceneId = fc.scenes[0] ? fc.scenes[0].id : null;
    }
    await SuzuriDB.putProject(project);
    renderTree();
    loadSceneIntoEditor();
    updateTotals();
  }

  async function addScene(chapterId) {
    const ch = project.chapters.find((c) => c.id === chapterId);
    const title = await showPrompt("新しいシーン名", `シーン${ch.scenes.length + 1}`);
    if (!title) return;
    const now = Date.now();
    const scene = { id: SuzuriDB.uid("scene"), title, content: "", order: ch.scenes.length, updatedAt: now, beatKey: null, summary: "" };
    ch.scenes.push(scene);
    ch.collapsed = false;
    await SuzuriDB.putProject(project);
    switchScene(chapterId, scene.id);
    updateTotals();
  }

  async function renameScene(chapterId, sceneId) {
    const found = findScene(sceneId);
    const title = await showPrompt("シーン名", found.scene.title);
    if (!title) return;
    found.scene.title = title;
    await SuzuriDB.putProject(project);
    renderTree();
    if (sceneId === activeSceneId) loadSceneIntoEditor();
  }

  async function moveScene(chapterId, sceneId, dir) {
    const ch = project.chapters.find((c) => c.id === chapterId);
    const sorted = ch.scenes.slice().sort((a, b) => a.order - b.order);
    const idx = sorted.findIndex((s) => s.id === sceneId);
    const swapIdx = idx + dir;
    if (swapIdx < 0 || swapIdx >= sorted.length) return;
    const a = sorted[idx], b = sorted[swapIdx];
    [a.order, b.order] = [b.order, a.order];
    await SuzuriDB.putProject(project);
    renderTree();
  }

  async function deleteScene(chapterId, sceneId) {
    const ch = project.chapters.find((c) => c.id === chapterId);
    if (ch.scenes.length <= 1) {
      alert("章内の最後のシーンは削除できません。");
      return;
    }
    const scene = ch.scenes.find((s) => s.id === sceneId);
    if (!confirm(`「${scene.title}」を削除しますか？`)) return;
    ch.scenes = ch.scenes.filter((s) => s.id !== sceneId);
    if (activeSceneId === sceneId) {
      activeSceneId = ch.scenes[0].id;
      activeChapterId = ch.id;
    }
    await SuzuriDB.putProject(project);
    renderTree();
    loadSceneIntoEditor();
    updateTotals();
  }

  // ---------- project-level actions ----------
  async function newProject() {
    const name = await showPrompt("新しい作品のタイトル", "無題の作品");
    if (!name) return;
    const np = SuzuriDB.createEmptyProject(name);
    await SuzuriDB.putProject(np);
    await refreshProjectSelect();
    await loadProject(np.id);
  }

  async function renameProject() {
    const name = await showPrompt("作品のタイトル", project.name);
    if (!name) return;
    project.name = name;
    await SuzuriDB.putProject(project);
    await refreshProjectSelect();
    loadSceneIntoEditor();
  }

  // ---------- focus mode ----------
  function toggleFocusMode(forceOn) {
    const isOn = el.app.classList.contains("focus-mode");
    const next = forceOn !== undefined ? forceOn : !isOn;
    el.app.classList.toggle("focus-mode", next);
    el.focusHint.hidden = !next;
    if (next) el.editor.focus();
  }

  // ---------- settings modal ----------
  async function openSettings() {
    el.goalInput.value = project.goal || "";
    const savedId = await SuzuriDrive.loadSavedClientId();
    el.driveClientId.value = savedId || "";
    refreshDriveUi();

    await SuzuriNotion.loadSaved();
    el.notionToken.value = (await SuzuriDB.getMeta("notionToken", "")) || "";
    el.notionParentPageId.value = (await SuzuriDB.getMeta("notionParentPageId", "")) || "";
    el.notionProxyUrl.value = (await SuzuriDB.getMeta("notionProxyUrl", "")) || "";
    refreshNotionUi();

    el.settingsModal.hidden = false;
  }
  function closeSettings() {
    el.settingsModal.hidden = true;
  }

  function refreshDriveUi() {
    const configured = SuzuriDrive.isConfigured();
    const signedIn = SuzuriDrive.isSignedIn();
    el.btnDriveSignIn.disabled = !configured;
    el.driveAuthStatus.textContent = !configured
      ? "クライアント ID 未設定"
      : signedIn
      ? "接続済み"
      : "未接続";
    el.driveActionsRow.hidden = !signedIn;
  }

  function refreshNotionUi() {
    const configured = SuzuriNotion.isConfigured();
    el.btnNotionTest.disabled = !configured;
    el.notionAuthStatus.textContent = configured ? "設定済み（下の接続確認でテスト）" : "未設定";
  }

  // ---------- init ----------
  function registerServiceWorker() {
    if ("serviceWorker" in navigator) {
      window.addEventListener("load", () => {
        navigator.serviceWorker.register("service-worker.js").catch((e) => console.warn("SW failed", e));
      });
    }
  }

  function bindEvents() {
    el.btnToggleSidebar.addEventListener("click", () => {
      el.sidebar.classList.toggle("collapsed");
    });

    el.projectSelect.addEventListener("change", (e) => loadProject(e.target.value));
    el.btnNewProject.addEventListener("click", newProject);
    el.btnRenameProject.addEventListener("click", renameProject);
    el.btnAddChapter.addEventListener("click", addChapter);

    el.sceneTitle.addEventListener("input", onEditorInput);
    el.sceneSummary.addEventListener("input", onEditorInput);
    el.sceneBeatSelect.addEventListener("change", onSceneBeatChange);
    el.editor.addEventListener("input", onEditorInput);

    el.btnFocus.addEventListener("click", () => toggleFocusMode());
    el.btnPlot.addEventListener("click", () => setViewMode(viewMode === "plot" ? "write" : "plot"));
    el.loglineInput.addEventListener("input", onLoglineInput);
    el.synopsisInput.addEventListener("input", onSynopsisInput);
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && el.app.classList.contains("focus-mode")) {
        toggleFocusMode(false);
      }
      if ((e.metaKey || e.ctrlKey) && e.key === ".") {
        e.preventDefault();
        toggleFocusMode();
      }
    });

    el.btnSettings.addEventListener("click", openSettings);
    el.btnCloseSettings.addEventListener("click", closeSettings);
    el.settingsModal.addEventListener("click", (e) => {
      if (e.target === el.settingsModal) closeSettings();
    });

    el.goalInput.addEventListener("change", async () => {
      project.goal = parseInt(el.goalInput.value, 10) || 0;
      await SuzuriDB.putProject(project);
      updateTotals();
    });

    el.btnSaveClientId.addEventListener("click", async () => {
      const id = el.driveClientId.value.trim();
      if (!id) return;
      await SuzuriDrive.configure(id);
      refreshDriveUi();
      el.driveSyncStatus.textContent = "クライアント ID を保存しました。";
    });

    el.btnDriveSignIn.addEventListener("click", async () => {
      try {
        el.driveSyncStatus.textContent = "接続しています…";
        await SuzuriDrive.signIn();
        refreshDriveUi();
        el.driveSyncStatus.textContent = "接続しました。";
      } catch (err) {
        console.error(err);
        el.driveSyncStatus.textContent = "接続に失敗しました: " + err.message;
      }
    });

    el.btnDriveUpload.addEventListener("click", async () => {
      try {
        el.driveSyncStatus.textContent = "Drive に保存しています…";
        await SuzuriDrive.uploadProject(project);
        el.driveSyncStatus.textContent = "Drive に保存しました。";
      } catch (err) {
        console.error(err);
        el.driveSyncStatus.textContent = "保存に失敗しました: " + err.message;
      }
    });

    el.btnDriveDownload.addEventListener("click", async () => {
      if (!confirm("Drive のバックアップで、この端末の作品データを上書きします。よろしいですか？")) return;
      try {
        el.driveSyncStatus.textContent = "Drive から取得しています…";
        const remote = await SuzuriDrive.downloadProject(project.id);
        await SuzuriDB.putProject(remote);
        await loadProject(remote.id);
        el.driveSyncStatus.textContent = "復元しました。";
      } catch (err) {
        console.error(err);
        el.driveSyncStatus.textContent = "復元に失敗しました: " + err.message;
      }
    });

    el.btnSaveNotionConfig.addEventListener("click", async () => {
      const token = el.notionToken.value.trim();
      const parentPageId = el.notionParentPageId.value.trim();
      const proxyUrl = el.notionProxyUrl.value.trim();
      if (!token || !parentPageId || !proxyUrl) {
        el.notionSyncStatus.textContent = "3項目すべて入力してください。";
        return;
      }
      await SuzuriNotion.configure({ token, proxyUrl, parentPageId });
      refreshNotionUi();
      el.notionSyncStatus.textContent = "設定を保存しました。「接続確認」で動作を確かめてください。";
    });

    el.btnNotionTest.addEventListener("click", async () => {
      try {
        el.notionSyncStatus.textContent = "確認しています…";
        await SuzuriNotion.testConnection();
        el.notionSyncStatus.textContent = "接続OK。親ページにアクセスできています。";
      } catch (err) {
        console.error(err);
        el.notionSyncStatus.textContent = "接続に失敗しました: " + err.message;
      }
    });

    el.btnNotionUpload.addEventListener("click", async () => {
      try {
        el.notionSyncStatus.textContent = "Notion に保存しています…";
        await SuzuriNotion.uploadProject(project);
        el.notionSyncStatus.textContent = "Notion に保存しました。";
      } catch (err) {
        console.error(err);
        el.notionSyncStatus.textContent = "保存に失敗しました: " + err.message;
      }
    });

    el.btnNotionDownload.addEventListener("click", async () => {
      if (!confirm("Notion のバックアップで、この端末の作品データを上書きします。よろしいですか？")) return;
      try {
        el.notionSyncStatus.textContent = "Notion から取得しています…";
        const remote = await SuzuriNotion.downloadProject(project.id);
        await SuzuriDB.putProject(remote);
        await loadProject(remote.id);
        el.notionSyncStatus.textContent = "復元しました。";
      } catch (err) {
        console.error(err);
        el.notionSyncStatus.textContent = "復元に失敗しました: " + err.message;
      }
    });

    el.btnExportJson.addEventListener("click", () => {
      const blob = new Blob([JSON.stringify(project, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${project.name}.json`;
      a.click();
      URL.revokeObjectURL(url);
    });

    el.btnImportJson.addEventListener("click", () => el.importFileInput.click());
    el.importFileInput.addEventListener("change", async (e) => {
      const file = e.target.files[0];
      if (!file) return;
      try {
        const text = await file.text();
        const data = JSON.parse(text);
        if (!data.id || !data.chapters) throw new Error("不正なファイル形式です");
        data.id = SuzuriDB.uid("proj"); // import as a new project to avoid clobbering
        data.name = data.name + "（インポート）";
        await SuzuriDB.putProject(data);
        await refreshProjectSelect();
        await loadProject(data.id);
        el.driveSyncStatus.textContent = "インポートしました。";
      } catch (err) {
        alert("インポートに失敗しました: " + err.message);
      }
      el.importFileInput.value = "";
    });

    window.addEventListener("beforeunload", () => {
      // best-effort synchronous-ish flush; IndexedDB writes already happen on debounce
    });

    window.addEventListener("resize", () => {
      const found = activeSceneId ? findScene(activeSceneId) : null;
      if (found) renderTickRail(charLen(found.scene.content));
    });
  }

  async function init() {
    bindEvents();
    registerServiceWorker();
    if (window.matchMedia("(max-width: 760px)").matches) {
      el.sidebar.classList.add("collapsed");
    }
    await SuzuriDrive.loadSavedClientId();
    await initProjects();
  }

  document.addEventListener("DOMContentLoaded", init);
})();
