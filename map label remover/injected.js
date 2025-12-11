(function () {
  function findMapInstance() {
    if (!window.google || !google.maps) {
      return null;
    }

    const keys = Object.keys(window);
    for (const key of keys) {
      try {
        const value = window[key];
        if (value instanceof google.maps.Map) {
          console.log("[Maps Label Remover] Found map instance on window key:", key);
          return value;
        }
      } catch (e) {
        // Ignore access errors
      }
    }

    return null;
  }

  function applyNoLabels(map) {
    if (!map || typeof map.setOptions !== "function") {
      return false;
    }

    // This is your original snippet logic:
    map.setOptions({
      styles: [
        {
          featureType: "all",
          stylers: [{ visibility: "off" }]
        }
      ]
    });

    console.log("[Maps Label Remover] Applied no-labels style.");
    return true;
  }

  function tryApply() {
    const map = findMapInstance();
    if (!map) {
      return false;
    }
    return applyNoLabels(map);
  }

  // Repeatedly try for a while, since Maps loads async
  let attempts = 0;
  const maxAttempts = 60; // ~30s if interval is 500ms

  const intervalId = setInterval(function () {
    attempts += 1;
    const success = tryApply();

    if (success || attempts >= maxAttempts) {
      clearInterval(intervalId);
      if (!success) {
        console.log("[Maps Label Remover] Gave up: could not find a google.maps.Map instance.");
      }
    }
  }, 500);
})();
