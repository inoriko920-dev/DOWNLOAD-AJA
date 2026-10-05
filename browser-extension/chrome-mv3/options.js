const DEFAULT_SETTINGS = Object.freeze({
  captureBrowserDownloads: false,
  startQueueAfterAdd: true
});

const captureDownloads = document.getElementById("captureDownloads");
const startQueue = document.getElementById("startQueue");
const status = document.getElementById("status");
const saveButton = document.getElementById("save");
const testButton = document.getElementById("test");

initialize().catch((error) => setStatus(`Gagal memuat pengaturan: ${error.message}`, true));

saveButton.addEventListener("click", async () => {
  await chrome.storage.local.set({
    captureBrowserDownloads: captureDownloads.checked,
    startQueueAfterAdd: startQueue.checked
  });
  setStatus("Pengaturan tersimpan.");
});

testButton.addEventListener("click", () => {
  setStatus("Menguji native host...");
  chrome.runtime.sendMessage({ type: "download-aja-ping" }, (response) => {
    if (chrome.runtime.lastError) {
      setStatus(`Tes gagal: ${chrome.runtime.lastError.message}`, true);
      return;
    }

    if (!response) {
      setStatus("Native host tidak merespons. Jalankan installer integrasi browser.", true);
      return;
    }

    const desktopText = response.desktopReachable
      ? "Aplikasi desktop terhubung."
      : "Native host siap; aplikasi desktop belum aktif.";

    setStatus(`${response.message || "Native host merespons."}\n${desktopText}`, !response.accepted);
  });
});

async function initialize() {
  const settings = await chrome.storage.local.get(DEFAULT_SETTINGS);
  captureDownloads.checked = Boolean(settings.captureBrowserDownloads);
  startQueue.checked = settings.startQueueAfterAdd !== false;
}

function setStatus(message, isError = false) {
  status.textContent = message;
  status.style.color = isError ? "#b3261e" : "#137333";
}
