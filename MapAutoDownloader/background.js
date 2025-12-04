// Listen for messages from content.js
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === "force_download") {
    
    console.log("Received force download request for: " + request.url);
    
    // Trigger the download manually
    chrome.downloads.download({
      url: request.url,
      filename: "mapimage.jpg",
      conflictAction: "overwrite",
      saveAs: false
    }, (downloadId) => {
      if (chrome.runtime.lastError) {
        console.error(chrome.runtime.lastError);
      } else {
        console.log("Download started with ID: " + downloadId);
        
        // Auto-close tab after 3 seconds
        setTimeout(() => {
            if (sender.tab && sender.tab.id) {
                chrome.tabs.remove(sender.tab.id);
            }
        }, 3000);
      }
    });
  }
});

// Keep the filename logic just in case
chrome.downloads.onDeterminingFilename.addListener((item, suggest) => {
  if (item.url.includes("mapimage")) {
    suggest({ filename: "mapimage.jpg", conflictAction: "overwrite" });
  }
});