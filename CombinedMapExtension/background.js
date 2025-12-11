// Handles tile stitching, auto-downloads, and shared utility setup

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.action === "stitch_tiles") {
    handleStitching(message.tiles, sendResponse);
    return true;
  }

  if (message.action === "force_download") {
    triggerDownload(message.url, sender, sendResponse);
    return true;
  }
});

function triggerDownload(url, sender, sendResponse) {
  chrome.downloads.download(
    {
      url,
      filename: "mapimage.jpg",
      conflictAction: "overwrite",
      saveAs: false,
    },
    (downloadId) => {
      if (chrome.runtime.lastError) {
        console.error(chrome.runtime.lastError);
        sendResponse?.({ success: false, error: chrome.runtime.lastError.message });
        return;
      }

      console.log("Download started with ID:", downloadId);

      // Auto-close the tab that initiated the request
      setTimeout(() => {
        if (sender?.tab?.id) {
          chrome.tabs.remove(sender.tab.id);
        }
      }, 3000);

      sendResponse?.({ success: true });
    }
  );
}

// Force filename for any download triggered by this extension
chrome.downloads.onDeterminingFilename.addListener((item, suggest) => {
  if (item.byExtensionId === chrome.runtime.id || item.url.includes("mapimage")) {
    suggest({ filename: "mapimage.jpg", conflictAction: "overwrite" });
  }
});

async function handleStitching(tiles, sendResponse) {
  try {
    await setupOffscreenDocument("offscreen.html");

    const response = await chrome.runtime.sendMessage({
      action: "stitch_in_offscreen",
      tiles,
    });

    if (response.error) throw new Error(response.error);

    chrome.downloads.download(
      {
        url: response.dataUrl,
        filename: "mapimage.jpg",
        saveAs: false,
      },
      async (downloadId) => {
        if (chrome.runtime.lastError) {
          sendResponse({ success: false, error: chrome.runtime.lastError.message });
        } else {
          console.log("Stitched image download started:", downloadId);
          sendResponse({ success: true });
        }

        await closeOffscreenDocument();
      }
    );
  } catch (err) {
    console.error(err);
    sendResponse({ success: false, error: err.message });
  }
}

let creating;
async function setupOffscreenDocument(path) {
  const existingContexts = await chrome.runtime.getContexts({
    contextTypes: ["OFFSCREEN_DOCUMENT"],
  });
  if (existingContexts.length > 0) return;

  if (creating) {
    await creating;
    return;
  }

  creating = chrome.offscreen.createDocument({
    url: path,
    reasons: ["BLOBS"],
    justification: "Stitching map tiles",
  });

  await creating;
  creating = null;
}

async function closeOffscreenDocument() {
  const existingContexts = await chrome.runtime.getContexts({
    contextTypes: ["OFFSCREEN_DOCUMENT"],
  });

  if (existingContexts.length) {
    await chrome.offscreen.closeDocument();
  }
}
