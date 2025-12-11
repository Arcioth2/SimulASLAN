document.addEventListener('DOMContentLoaded', function() {
    
    // 1. Get the switch element
    const autoCopySwitch = document.getElementById('autoCopySwitch');
    
    // Safety check: if the switch doesn't exist in HTML, stop here to prevent errors
    if (!autoCopySwitch) return;

    // 2. Load the current setting from storage
    chrome.storage.local.get(['autoCopyEnabled'], function(result) {
        // Default to false if undefined, otherwise use stored value
        if (result.autoCopyEnabled) {
            autoCopySwitch.checked = true;
        } else {
            autoCopySwitch.checked = false;
        }
    });

    // 3. Save the setting when the switch is toggled
    autoCopySwitch.addEventListener('change', function() {
        const isEnabled = autoCopySwitch.checked;
        chrome.storage.local.set({ autoCopyEnabled: isEnabled }, function() {
            console.log('Auto Copy setting saved:', isEnabled);
            
            // Optional: Log to the UI logs area (only if it exists)
            const logsDiv = document.getElementById('logs');
            if (logsDiv) {
                const newLog = document.createElement('div');
                newLog.textContent = `> Auto Copy turned ${isEnabled ? 'ON' : 'OFF'}`;
                newLog.style.color = isEnabled ? 'green' : 'red';
                logsDiv.appendChild(newLog);
                logsDiv.scrollTop = logsDiv.scrollHeight;
            }
        });
    });
});