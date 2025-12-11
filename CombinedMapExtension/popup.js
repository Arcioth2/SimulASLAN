document.addEventListener("DOMContentLoaded", () => {
  setupStitcher();
  setupAutoCopyToggle();
});

function setupStitcher() {
  const btn = document.getElementById("stitchBtn");
  const status = document.getElementById("status");

  btn.addEventListener("click", async () => {
    btn.disabled = true;
    btn.innerText = "Scanning Map...";
    status.innerText = "";
    status.className = "";

    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: scanForGoogleTiles,
      });

      const tileData = results[0].result;

      if (!tileData || tileData.length === 0) {
        throw new Error("No visible tiles found. Is the map loaded?");
      }

      status.innerText = `Found ${tileData.length} tiles. Stitching...`;

      chrome.runtime.sendMessage(
        {
          action: "stitch_tiles",
          tiles: tileData,
        },
        (response) => {
          if (chrome.runtime.lastError) {
            status.innerText = "Error: " + chrome.runtime.lastError.message;
            status.className = "error";
            resetBtn();
          } else if (response && response.success) {
            status.innerText = "Success! Saved mapimage.jpg";
            status.className = "success";
            btn.innerText = "DONE";
            setTimeout(resetBtn, 3000);
          } else {
            status.innerText = "Stitching Failed: " + (response?.error || "Unknown");
            status.className = "error";
            btn.innerText = "RETRY";
            btn.disabled = false;
          }
        }
      );
    } catch (err) {
      status.innerText = err.message;
      status.className = "error";
      resetBtn();
    }
  });

  function resetBtn() {
    btn.disabled = false;
    btn.innerText = "STITCH & SAVE";
  }
}

function setupAutoCopyToggle() {
  const autoCopySwitch = document.getElementById("autoCopySwitch");
  if (!autoCopySwitch) return;

  chrome.storage.local.get(["autoCopyEnabled"], (result) => {
    autoCopySwitch.checked = result.autoCopyEnabled === true;
  });

  autoCopySwitch.addEventListener("change", () => {
    const isEnabled = autoCopySwitch.checked;
    chrome.storage.local.set({ autoCopyEnabled: isEnabled });
  });
}

function scanForGoogleTiles() {
  const imgs = Array.from(document.querySelectorAll("img")).filter(
    (img) => img.src && (img.src.includes("maps.googleapis.com/maps/vt") || img.src.includes("/vt?"))
  );

  if (imgs.length === 0) return [];

  const rects = imgs.map((img) => {
    const r = img.getBoundingClientRect();
    return {
      src: img.src,
      x: r.left + window.scrollX,
      y: r.top + window.scrollY,
      w: r.width,
      h: r.height,
    };
  });

  const minX = Math.min(...rects.map((r) => r.x));
  const minY = Math.min(...rects.map((r) => r.y));

  return rects.map((r) => ({
    url: r.src,
    x: Math.round(r.x - minX),
    y: Math.round(r.y - minY),
    w: r.w,
    h: r.h,
  }));
}
