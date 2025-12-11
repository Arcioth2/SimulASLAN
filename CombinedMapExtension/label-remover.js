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
          console.log("[SimulASLAN] Found map instance on window key:", key);
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

    map.setOptions({
      styles: [
        {
          featureType: "all",
          stylers: [{ visibility: "off" }],
        },
      ],
    });

    console.log("[SimulASLAN] Applied no-labels style.");
    return true;
  }

  function tryApply() {
    const map = findMapInstance();
    if (!map) {
      return false;
    }
    return applyNoLabels(map);
  }

  let attempts = 0;
  const maxAttempts = 60;

  const intervalId = setInterval(function () {
    attempts += 1;
    const success = tryApply();

    if (success || attempts >= maxAttempts) {
      clearInterval(intervalId);
      if (!success) {
        console.log("[SimulASLAN] Gave up: could not find a google.maps.Map instance.");
      }
    }
  }, 500);
})();
