export function setLocal(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

export function getLocal(key) {
  const val = localStorage.getItem(key);
  try {
    return JSON.parse(val);
  } catch {
    return val;
  }
}

export function removeLocal(key) {
  localStorage.removeItem(key);
}

export function setSession(key, value) {
  sessionStorage.setItem(key, JSON.stringify(value));
}

export function getSession(key) {
  const val = sessionStorage.getItem(key);
  try {
    return JSON.parse(val);
  } catch {
    return val;
  }
}

export function removeSession(key) {
  sessionStorage.removeItem(key);
}
