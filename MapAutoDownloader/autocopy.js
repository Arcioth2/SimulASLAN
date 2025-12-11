// autocopy.js
// Handles automatically copying coordinates when URL changes on Google Maps

let lastCopiedCoords = '';
let notificationActive = false;

// Function to show the Green Popup (Notification)
function showAutoCopyPopup(text) {
    if (notificationActive) return;
    notificationActive = true;

    const popup = document.createElement('div');
    popup.textContent = text;
    
    // Styling to match the "Green Map Grabbed" style
    Object.assign(popup.style, {
        position: 'fixed',
        top: '20px',
        right: '20px',
        backgroundColor: '#4CAF50', // Green
        color: 'white',
        padding: '15px 25px',
        borderRadius: '8px',
        boxShadow: '0 4px 6px rgba(0,0,0,0.1)',
        zIndex: '10000', // High z-index to sit on top of maps
        fontSize: '16px',
        fontWeight: 'bold',
        opacity: '0',
        transition: 'opacity 0.3s ease-in-out',
        pointerEvents: 'none',
        fontFamily: 'Arial, sans-serif'
    });

    document.body.appendChild(popup);

    // Fade in
    requestAnimationFrame(() => {
        popup.style.opacity = '1';
    });

    // Fade out and remove after 3 seconds
    setTimeout(() => {
        popup.style.opacity = '0';
        setTimeout(() => {
            if (document.body.contains(popup)) {
                document.body.removeChild(popup);
            }
            notificationActive = false;
        }, 300);
    }, 3000);
}

// Function to Extract and Copy Coordinates
function checkAndCopyCoordinates() {
    // URL format example: https://www.google.com/maps/@52.2129919,5.2793703,1110810m/...
    const currentUrl = window.location.href;
    
    if (currentUrl.includes('/maps/@')) {
        // Extract the part after '@' until the next slash
        const match = currentUrl.match(/@([^/]+)/);
        
        if (match && match[1]) {
            let coords = match[1]; 

            // FIX: Remove the 'm' at the end if it exists (e.g. 1110810m -> 1110810)
            if (coords.endsWith('m')) {
                coords = coords.slice(0, -1);
            }

            // Prevent infinite copying of the same location
            if (coords !== lastCopiedCoords) {
                
                // Check Chrome Storage for the "Auto Copy" setting
                chrome.storage.local.get(['autoCopyEnabled'], function(result) {
                    // Only proceed if setting is enabled (default off or strictly true)
                    if (result.autoCopyEnabled === true) {
                        
                        // Copy to clipboard
                        navigator.clipboard.writeText(coords).then(() => {
                            lastCopiedCoords = coords; 
                            console.log('[SimulASLAN] Coordinates copied:', coords);
                            showAutoCopyPopup(`Map Grabbed: ${coords}`);
                        }).catch(err => {
                            console.error('[SimulASLAN] Failed to copy:', err);
                        });
                    }
                });
            }
        }
    }
}

// Monitor URL Changes every second
setInterval(checkAndCopyCoordinates, 1000);