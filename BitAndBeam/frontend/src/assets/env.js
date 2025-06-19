// This file will be replaced with actual values at runtime
window.__env = {};

// Add environment variables
window.__env.API_URL = "${API_URL}";

// Make env object immutable
Object.freeze(window.__env);
