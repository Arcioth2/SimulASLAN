// This runs ON the google page itself
console.log("SimulASLAN Content Script Active");

// Check if the URL matches our map request
if (window.location.href.includes("mapimage")) {
    
    // The image is usually the only thing on the page, or wrapped in an <img> tag.
    // We can force a download by creating a temporary link.
    
    const url = window.location.href;
    
    // Send a message to background.js to handle the download
    // (Content scripts can't access chrome.downloads directly)
    chrome.runtime.sendMessage({
        action: "force_download",
        url: url
    });

    // Optional: Show a visual indicator on the page
    const div = document.createElement('div');
    div.style.position = 'fixed';
    div.style.top = '0';
    div.style.left = '0';
    div.style.width = '100%';
    div.style.height = '100%';
    div.style.backgroundColor = 'rgba(0, 255, 0, 0.3)';
    div.style.zIndex = '9999';
    div.style.display = 'flex';
    div.style.justifyContent = 'center';
    div.style.alignItems = 'center';
    div.innerHTML = '<h1 style="color:white; text-shadow: 2px 2px 4px #000; font-family: sans-serif;">SimulASLAN: Downloading...</h1>';
    document.body.appendChild(div);
}