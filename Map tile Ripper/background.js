// 1. Force Filename Listener
// This watches for any download started by THIS extension and forces the name.
chrome.downloads.onDeterminingFilename.addListener((item, suggest) => {
    // Only affect downloads triggered by this extension
    if (item.byExtensionId === chrome.runtime.id) {
        suggest({ 
            filename: "mapimage.jpg", 
            conflictAction: "overwrite" 
        });
    }
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === "stitch_tiles") {
        handleStitching(message.tiles, sendResponse);
        return true;
    }
});

async function handleStitching(tiles, sendResponse) {
    try {
        await setupOffscreenDocument('offscreen.html');

        const response = await chrome.runtime.sendMessage({
            action: "stitch_in_offscreen",
            tiles: tiles
        });

        if (response.error) throw new Error(response.error);

        // Download using the Data URL
        // The listener at the top of this file will ensure the name is correct.
        chrome.downloads.download({
            url: response.dataUrl,
            filename: "mapimage.jpg", // Fallback
            saveAs: false
        }, (downloadId) => {
            if (chrome.runtime.lastError) {
                sendResponse({ success: false, error: chrome.runtime.lastError.message });
            } else {
                sendResponse({ success: true });
            }
        });

    } catch (err) {
        console.error(err);
        sendResponse({ success: false, error: err.message });
    }
}

let creating;
async function setupOffscreenDocument(path) {
    const existingContexts = await chrome.runtime.getContexts({
        contextTypes: ['OFFSCREEN_DOCUMENT']
    });
    if (existingContexts.length > 0) return;

    if (creating) {
        await creating;
    } else {
        creating = chrome.offscreen.createDocument({
            url: path,
            reasons: ['BLOBS'],
            justification: 'Stitching map tiles',
        });
        await creating;
        creating = null;
    }
}