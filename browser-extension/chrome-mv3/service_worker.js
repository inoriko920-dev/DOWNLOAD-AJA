const HOST_NAME = "com.downloadaja.native_host";
const PROTOCOL_VERSION = 1;

const DEFAULT_SETTINGS = Object.freeze({
  captureBrowserDownloads: false,
  startQueueAfterAdd: true
});

chrome.runtime.onInstalled.addListener(async () => {
  const current = await chrome.storage.local.get(DEFAULT_SETTINGS);
  await chrome.storage.local.set({
    captureBrowserDownloads: Boolean(current.captureBrowserDownloads),
    startQueueAfterAdd: current.startQueueAfterAdd !== false
  });

  await chrome.contextMenus.removeAll();
  chrome.contextMenus.create({
    id: "download-aja-link",
    title: "Unduh link dengan DOWNLOAD-AJA",
    contexts: ["link"]
  });
  chrome.contextMenus.create({
    id: "download-aja-media",
    title: "Unduh media dengan DOWNLOAD-AJA",
    contexts: ["video", "audio", "image"]
  });
  chrome.contextMenus.create({
    id: "download-aja-page",
    title: "Kirim halaman ke DOWNLOAD-AJA",
    contexts: ["page"]
  });
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  const url = info.linkUrl || info.srcUrl || info.pageUrl || tab?.url;
  await sendUrlToDownloadAja(url, "context-menu");
});

chrome.action.onClicked.addListener(async (tab) => {
  await sendUrlToDownloadAja(tab?.url, "toolbar");
});

chrome.downloads.onCreated.addListener(async (item) => {
  const settings = await chrome.storage.local.get(DEFAULT_SETTINGS);
  if (!settings.captureBrowserDownloads) {
    return;
  }

  const url = item.finalUrl || item.url;
  if (!isHttpUrl(url)) {
    return;
  }

  const response = await sendUrlToDownloadAja(url, "browser-download", {
    silentSuccess: true,
    silentFailure: true
  });

  if (!response?.accepted) {
    // Graceful fallback: Chrome keeps downloading when DOWNLOAD-AJA rejects or
    // cannot be reached. Never cancel the browser download before acknowledgement.
    return;
  }

  try {
    await chrome.downloads.cancel(item.id);
    await chrome.downloads.erase({ id: item.id });
  } catch (error) {
    console.warn("DOWNLOAD-AJA: browser download could not be cancelled", error);
  }
});

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message?.type === "download-aja-ping") {
    sendNativeMessage({
      version: PROTOCOL_VERSION,
      command: "ping",
      url: null,
      startQueueAfterAdd: false
    }).then(sendResponse);
    return true;
  }

  return false;
});

async function sendUrlToDownloadAja(url, source, options = {}) {
  if (!isHttpUrl(url)) {
    if (!options.silentFailure) {
      await notify(
        "DOWNLOAD-AJA",
        "URL ini bukan HTTP/HTTPS sehingga tidak dikirim ke aplikasi."
      );
    }
    return null;
  }

  const settings = await chrome.storage.local.get(DEFAULT_SETTINGS);
  const response = await sendNativeMessage({
    version: PROTOCOL_VERSION,
    command: "add-url",
    url,
    startQueueAfterAdd: settings.startQueueAfterAdd !== false,
    source
  });

  if (!response) {
    if (!options.silentFailure) {
      await notify(
        "DOWNLOAD-AJA tidak tersedia",
        "Native host tidak dapat dihubungi. Jalankan installer integrasi browser DOWNLOAD-AJA."
      );
    }
    return null;
  }

  if (response.version !== PROTOCOL_VERSION) {
    if (!options.silentFailure) {
      await notify(
        "Versi integrasi tidak cocok",
        `Extension memakai protocol ${PROTOCOL_VERSION}, host membalas ${response.version}.`
      );
    }
    return { ...response, accepted: false };
  }

  if (response.accepted) {
    if (!options.silentSuccess) {
      await notify("DOWNLOAD-AJA", response.message || "URL diterima.");
    }
  } else if (!options.silentFailure) {
    await notify("DOWNLOAD-AJA menolak URL", response.message || "Permintaan ditolak.");
  }

  return response;
}

function sendNativeMessage(payload) {
  return new Promise((resolve) => {
    chrome.runtime.sendNativeMessage(HOST_NAME, payload, (response) => {
      if (chrome.runtime.lastError) {
        console.warn("DOWNLOAD-AJA native messaging error:", chrome.runtime.lastError.message);
        resolve(null);
        return;
      }

      resolve(response || null);
    });
  });
}

function isHttpUrl(value) {
  if (typeof value !== "string" || value.length === 0) {
    return false;
  }

  try {
    const parsed = new URL(value);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

async function notify(title, message) {
  try {
    await chrome.notifications.create({
      type: "basic",
      iconUrl: "icon128.png",
      title,
      message
    });
  } catch (error) {
    // Icons are added during packaging. During unpacked development Chrome may
    // reject the notification; logging is preferable to breaking the handoff.
    console.info(`${title}: ${message}`, error);
  }
}
