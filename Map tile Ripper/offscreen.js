chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
    if (msg.action === "stitch_in_offscreen") {
        createMapData(msg.tiles)
            .then(dataUrl => sendResponse({ dataUrl }))
            .catch(err => sendResponse({ error: err.message }));
        return true;
    }
});

async function createMapData(tiles) {
    const rightMost = tiles.reduce((a, b) => (a.x > b.x) ? a : b);
    const bottomMost = tiles.reduce((a, b) => (a.y > b.y) ? a : b);
    const width = rightMost.x + rightMost.w;
    const height = bottomMost.y + bottomMost.h;

    const canvas = new OffscreenCanvas(width, height);
    const ctx = canvas.getContext('2d');

    const drawPromises = tiles.map(async (tile) => {
        try {
            const response = await fetch(tile.url);
            const blob = await response.blob();
            const bitmap = await createImageBitmap(blob);
            ctx.drawImage(bitmap, tile.x, tile.y, tile.w, tile.h);
        } catch (e) {
            console.warn("Skipped tile:", tile.url);
        }
    });

    await Promise.all(drawPromises);

    // Convert directly to Base64 String (Data URL)
    // This avoids the Blob UUID naming issue
    const blob = await canvas.convertToBlob({ type: 'image/jpeg', quality: 0.95 });
    
    return new Promise((resolve) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.readAsDataURL(blob);
    });
}