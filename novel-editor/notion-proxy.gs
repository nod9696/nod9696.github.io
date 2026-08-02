/**
 * suzuri-notion-proxy — Google Apps Script Web App
 *
 * Forwards requests from 硯 -Suzuri- to the Notion API, since Notion's API
 * does not send CORS headers and cannot be called directly from a browser.
 *
 * SETUP
 * 1. Go to https://script.google.com/ → New project
 * 2. Delete the default code and paste this whole file in
 * 3. Deploy → New deployment → type "Web app"
 *      - Execute as: Me
 *      - Who has access: Anyone
 * 4. Copy the resulting URL (ends in /exec) — this is your "Proxy URL"
 *      in Suzuri's settings screen.
 *
 * This script does not store your Notion token — it only forwards
 * whatever token the app sends with each request, straight through to
 * Notion, and relays the response back.
 */

function doPost(e) {
  var result;
  try {
    var payload = JSON.parse(e.postData.contents);
    var url = "https://api.notion.com/v1/" + payload.path;

    var options = {
      method: (payload.method || "get").toLowerCase(),
      headers: {
        Authorization: "Bearer " + payload.token,
        "Notion-Version": "2022-06-28",
      },
      muteHttpExceptions: true,
    };

    if (payload.body) {
      options.contentType = "application/json";
      options.payload = JSON.stringify(payload.body);
    }

    var response = UrlFetchApp.fetch(url, options);
    result = response.getContentText();
  } catch (err) {
    result = JSON.stringify({ error: String(err) });
  }

  return ContentService.createTextOutput(result).setMimeType(ContentService.MimeType.JSON);
}

// simple health check if you open the /exec URL directly in a browser
function doGet(e) {
  return ContentService.createTextOutput(
    JSON.stringify({ ok: true, message: "suzuri-notion-proxy is running" })
  ).setMimeType(ContentService.MimeType.JSON);
}
