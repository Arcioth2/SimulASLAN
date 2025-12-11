document.getElementById('stitchBtn').addEventListener('click', async () => {
    const btn = document.getElementById('stitchBtn');
    const status = document.getElementById('status');
    
    // UI Loading State
    btn.disabled = true;
    btn.innerText = "Scanning Map...";
    status.innerText = "";
    status.className = "";

    try {
        const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

        // Execute the scanner in the active tab
        const results = await chrome.scripting.executeScript({
            target: { tabId: tab.id },
            func: scanForGoogleTiles
        });

        const tileData = results[0].result;

        if (!tileData || tileData.length === 0) {
            throw new Error("No visible tiles found. Is the map loaded?");
        }

        status.innerText = `Found ${tileData.length} tiles. Stitching...`;

        // Send to background for processing
        chrome.runtime.sendMessage({ 
            action: "stitch_tiles", 
            tiles: tileData 
        }, (response) => {
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
                status.innerText = "Stitching Failed: " + (response.error || "Unknown");
                status.className = "error";
                btn.innerText = "RETRY";
                btn.disabled = false;
            }
        });

    } catch (err) {
        status.innerText = err.message;
        status.className = "error";
        resetBtn();
    }

    function resetBtn() {
        btn.disabled = false;
        btn.innerText = "STITCH & SAVE";
    }
});

// This function runs INSIDE the web page
function scanForGoogleTiles() {
    // 1. Find all images that look like tiles (Google Maps or standard tile servers)
    // Checks for 'maps.googleapis' OR standard '/vt' tile patterns
    const imgs = Array.from(document.querySelectorAll('img')).filter(img => 
        img.src && (img.src.includes('maps.googleapis.com/maps/vt') || img.src.includes('/vt?'))
    );

    if (imgs.length === 0) return [];

    // 2. Get screen positions
    const rects = imgs.map(img => {
        const r = img.getBoundingClientRect();
        return { 
            src: img.src, 
            x: r.left + window.scrollX, 
            y: r.top + window.scrollY,
            w: r.width,
            h: r.height
        };
    });

    // 3. Normalize coordinates (0,0 based on top-left visible tile)
    const minX = Math.min(...rects.map(r => r.x));
    const minY = Math.min(...rects.map(r => r.y));

    // 4. Return clean data
    return rects.map(r => ({
        url: r.src,
        x: Math.round(r.x - minX),
        y: Math.round(r.y - minY),
        w: r.w,
        h: r.h
    }));
}