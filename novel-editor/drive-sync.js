/* drive-sync.js — optional Google Drive backup for 硯 -Suzuri-
 *
 * Uses Google Identity Services (token client) for auth and the
 * Drive REST API directly via fetch (no gapi client library needed
 * for the calls themselves — gapi/api.js is loaded only as a fallback
 * if it's ever needed, but is not required for this implementation).
 *
 * Scope used: https://www.googleapis.com/auth/drive.file
 * This restricted scope only grants access to files the app itself
 * creates — it does not require access to the user's whole Drive.
 *
 * All project backups are stored as individual JSON files named
 * "suzuri-<projectId>.json" inside a single app folder named
 * "Suzuri Novel Backups", created on first use.
 */

const SuzuriDrive = (() => {
  const SCOPE = "https://www.googleapis.com/auth/drive.file";
  const FOLDER_NAME = "Suzuri Novel Backups";

  let clientId = null;
  let tokenClient = null;
  let accessToken = null;
  let folderId = null;

  function isConfigured() {
    return !!clientId;
  }

  function isSignedIn() {
    return !!accessToken;
  }

  async function configure(id) {
    clientId = id;
    await SuzuriDB.setMeta("driveClientId", id);
  }

  async function loadSavedClientId() {
    clientId = await SuzuriDB.getMeta("driveClientId", null);
    return clientId;
  }

  function ensureGisLoaded() {
    return new Promise((resolve, reject) => {
      if (window.google && window.google.accounts && window.google.accounts.oauth2) {
        resolve();
        return;
      }
      let tries = 0;
      const iv = setInterval(() => {
        tries++;
        if (window.google && window.google.accounts && window.google.accounts.oauth2) {
          clearInterval(iv);
          resolve();
        } else if (tries > 100) {
          clearInterval(iv);
          reject(new Error("Google Identity Services の読み込みに失敗しました"));
        }
      }, 100);
    });
  }

  async function signIn() {
    if (!clientId) throw new Error("先に OAuth クライアント ID を設定してください");
    await ensureGisLoaded();

    return new Promise((resolve, reject) => {
      tokenClient = google.accounts.oauth2.initTokenClient({
        client_id: clientId,
        scope: SCOPE,
        callback: (resp) => {
          if (resp.error) {
            reject(new Error(resp.error));
            return;
          }
          accessToken = resp.access_token;
          resolve(accessToken);
        },
      });
      tokenClient.requestAccessToken({ prompt: "consent" });
    });
  }

  function signOut() {
    if (accessToken && window.google && google.accounts && google.accounts.oauth2) {
      google.accounts.oauth2.revoke(accessToken, () => {});
    }
    accessToken = null;
    folderId = null;
  }

  async function driveFetch(url, options = {}) {
    if (!accessToken) throw new Error("Drive に未接続です");
    const res = await fetch(url, {
      ...options,
      headers: {
        ...(options.headers || {}),
        Authorization: `Bearer ${accessToken}`,
      },
    });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      throw new Error(`Drive API エラー (${res.status}): ${text.slice(0, 200)}`);
    }
    return res;
  }

  async function ensureFolder() {
    if (folderId) return folderId;
    const q = encodeURIComponent(
      `name='${FOLDER_NAME}' and mimeType='application/vnd.google-apps.folder' and trashed=false`
    );
    const res = await driveFetch(
      `https://www.googleapis.com/drive/v3/files?q=${q}&fields=files(id,name)`
    );
    const data = await res.json();
    if (data.files && data.files.length > 0) {
      folderId = data.files[0].id;
      return folderId;
    }
    const createRes = await driveFetch("https://www.googleapis.com/drive/v3/files", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name: FOLDER_NAME,
        mimeType: "application/vnd.google-apps.folder",
      }),
    });
    const created = await createRes.json();
    folderId = created.id;
    return folderId;
  }

  async function findBackupFile(projectId) {
    const fname = `suzuri-${projectId}.json`;
    const folder = await ensureFolder();
    const q = encodeURIComponent(`name='${fname}' and '${folder}' in parents and trashed=false`);
    const res = await driveFetch(
      `https://www.googleapis.com/drive/v3/files?q=${q}&fields=files(id,name,modifiedTime)`
    );
    const data = await res.json();
    return (data.files && data.files[0]) || null;
  }

  async function uploadProject(project) {
    const fname = `suzuri-${project.id}.json`;
    const folder = await ensureFolder();
    const existing = await findBackupFile(project.id);
    const body = JSON.stringify(project, null, 2);
    const metadata = existing
      ? { name: fname }
      : { name: fname, parents: [folder] };

    const boundary = "suzuri-boundary-" + Date.now();
    const multipartBody =
      `--${boundary}\r\n` +
      `Content-Type: application/json; charset=UTF-8\r\n\r\n` +
      `${JSON.stringify(metadata)}\r\n` +
      `--${boundary}\r\n` +
      `Content-Type: application/json\r\n\r\n` +
      `${body}\r\n` +
      `--${boundary}--`;

    const url = existing
      ? `https://www.googleapis.com/upload/drive/v3/files/${existing.id}?uploadType=multipart`
      : `https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart`;

    await driveFetch(url, {
      method: existing ? "PATCH" : "POST",
      headers: { "Content-Type": `multipart/related; boundary=${boundary}` },
      body: multipartBody,
    });

    return true;
  }

  async function downloadProject(projectId) {
    const file = await findBackupFile(projectId);
    if (!file) throw new Error("Drive にバックアップが見つかりません");
    const res = await driveFetch(
      `https://www.googleapis.com/drive/v3/files/${file.id}?alt=media`
    );
    return res.json();
  }

  async function listBackups() {
    const folder = await ensureFolder();
    const q = encodeURIComponent(`'${folder}' in parents and trashed=false`);
    const res = await driveFetch(
      `https://www.googleapis.com/drive/v3/files?q=${q}&fields=files(id,name,modifiedTime)&orderBy=modifiedTime desc`
    );
    const data = await res.json();
    return data.files || [];
  }

  return {
    isConfigured,
    isSignedIn,
    configure,
    loadSavedClientId,
    signIn,
    signOut,
    uploadProject,
    downloadProject,
    listBackups,
  };
})();
