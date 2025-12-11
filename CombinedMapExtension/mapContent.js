// Runs on Google Maps pages to handle auto-copy and label removal
console.log("SimulASLAN Map Toolkit content script active");

handleMapImageDownloadIfPresent();

let lastCopiedCoords = "";
let notificationActive = false;

function showAutoCopyPopup(text) {
  if (notificationActive) return;
  notificationActive = true;

  const popup = document.createElement("div");
  popup.textContent = text;
  Object.assign(popup.style, {
    position: "fixed",
    top: "20px",
    right: "20px",
    backgroundColor: "#4CAF50",
    color: "white",
    padding: "15px 25px",
    borderRadius: "8px",
    boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
    zIndex: "10000",
    fontSize: "16px",
    fontWeight: "bold",
    opacity: "0",
    transition: "opacity 0.3s ease-in-out",
    pointerEvents: "none",
    fontFamily: "Arial, sans-serif",
  });

  document.body.appendChild(popup);
  requestAnimationFrame(() => {
    popup.style.opacity = "1";
  });

  setTimeout(() => {
    popup.style.opacity = "0";
    setTimeout(() => {
      if (document.body.contains(popup)) {
        document.body.removeChild(popup);
      }
      notificationActive = false;
    }, 300);
  }, 3000);
}

function checkAndCopyCoordinates() {
  const currentUrl = window.location.href;

  if (currentUrl.includes("/maps/@")) {
    const match = currentUrl.match(/@([^/]+)/);

    if (match && match[1]) {
      let coords = match[1];

      if (coords.endsWith("m")) {
        coords = coords.slice(0, -1);
      }

      if (coords !== lastCopiedCoords) {
        chrome.storage.local.get(["autoCopyEnabled"], (result) => {
          if (result.autoCopyEnabled === true) {
            navigator.clipboard
              .writeText(coords)
              .then(() => {
                lastCopiedCoords = coords;
                console.log("[SimulASLAN] Coordinates copied:", coords);
                showAutoCopyPopup(`Map Grabbed: ${coords}`);
              })
              .catch((err) => console.error("[SimulASLAN] Failed to copy:", err));
          }
        });
      }
    }
  }
}

// Inject label removal logic
(function injectLabelRemover() {
  const script = document.createElement("script");
  script.src = chrome.runtime.getURL("label-remover.js");
  script.onload = function () {
    this.remove();
  };
  (document.head || document.documentElement).appendChild(script);
})();

setInterval(checkAndCopyCoordinates, 1000);

function handleMapImageDownloadIfPresent() {
  const currentUrl = window.location.href;

  if (!currentUrl.includes("mapimage")) return;

  console.log("[SimulASLAN] mapimage detected, requesting download...");

  chrome.runtime.sendMessage({ action: "force_download", url: currentUrl });

  const overlay = document.createElement("div");
  Object.assign(overlay.style, {
    position: "fixed",
    top: "0",
    left: "0",
    width: "100%",
    height: "100%",
    backgroundColor: "rgba(0, 255, 0, 0.3)",
    zIndex: "9999",
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
  });

  overlay.innerHTML =
    '<h1 style="color:white; text-shadow: 2px 2px 4px #000; font-family: sans-serif;">SimulASLAN: Downloading…</h1>';

  document.body.appendChild(overlay);
}
