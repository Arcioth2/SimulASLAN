// Runs on mapimage pages to force immediate download
console.log("SimulASLAN Map Toolkit download helper active");

if (window.location.href.includes("mapimage")) {
  const url = window.location.href;
  chrome.runtime.sendMessage({ action: "force_download", url });

  const overlay = document.createElement("div");
  overlay.style.position = "fixed";
  overlay.style.top = "0";
  overlay.style.left = "0";
  overlay.style.width = "100%";
  overlay.style.height = "100%";
  overlay.style.backgroundColor = "rgba(0, 255, 0, 0.3)";
  overlay.style.zIndex = "9999";
  overlay.style.display = "flex";
  overlay.style.justifyContent = "center";
  overlay.style.alignItems = "center";
  overlay.innerHTML =
    '<h1 style="color:white; text-shadow: 2px 2px 4px #000; font-family: sans-serif;">SimulASLAN: Downloading…</h1>';
  document.body.appendChild(overlay);
}
