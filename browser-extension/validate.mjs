import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "chrome-mv3");
const manifest = JSON.parse(readFileSync(resolve(root, "manifest.json"), "utf8"));
const nativeHostTemplate = JSON.parse(
  readFileSync(resolve(import.meta.dirname, "native-host", "com.downloadaja.native_host.json.template"), "utf8")
);

assert(manifest.manifest_version === 3, "manifest_version must be 3");
assert(manifest.background?.service_worker === "service_worker.js", "MV3 service worker missing");
assert(manifest.permissions?.includes("nativeMessaging"), "nativeMessaging permission missing");
assert(manifest.permissions?.includes("contextMenus"), "contextMenus permission missing");
assert(manifest.permissions?.includes("downloads"), "downloads permission missing");
assert(typeof manifest.key === "string" && manifest.key.length > 0, "deterministic extension key missing");

const extensionId = chromeExtensionId(Buffer.from(manifest.key, "base64"));
const expectedOrigin = `chrome-extension://${extensionId}/`;
assert(
  nativeHostTemplate.allowed_origins?.includes(expectedOrigin),
  `native host allowed_origins must contain ${expectedOrigin}`
);
assert(nativeHostTemplate.name === "com.downloadaja.native_host", "native host name mismatch");
assert(nativeHostTemplate.type === "stdio", "native host must use stdio");

console.log(`Chrome MV3 validation PASS — deterministic extension ID: ${extensionId}`);

function chromeExtensionId(publicKeyDer) {
  const bytes = createHash("sha256").update(publicKeyDer).digest().subarray(0, 16);
  let id = "";
  for (const value of bytes) {
    id += String.fromCharCode(97 + (value >> 4));
    id += String.fromCharCode(97 + (value & 0x0f));
  }
  return id;
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}
