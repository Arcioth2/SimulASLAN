document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.getElementById('pingToggle');
    const logContainer = document.getElementById('log-container');
    const clearBtn = document.getElementById('clearLogs');
  
    // Load Settings
    chrome.storage.local.get(['pingEnabled', 'logs'], (result) => {
      toggle.checked = result.pingEnabled !== false; // Default true
      renderLogs(result.logs || []);
    });
  
    // Handle Toggle
    toggle.addEventListener('change', () => {
      chrome.storage.local.set({ pingEnabled: toggle.checked });
    });
  
    // Handle Clear
    clearBtn.addEventListener('click', () => {
      chrome.storage.local.set({ logs: [] });
      renderLogs([]);
    });
  
    function renderLogs(logs) {
      logContainer.innerHTML = '';
      logs.forEach(log => {
        const div = document.createElement('div');
        div.className = 'log-entry';
        div.textContent = log;
        logContainer.appendChild(div);
      });
    }
  });