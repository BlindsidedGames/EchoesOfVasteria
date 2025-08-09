const pages = [
  { title: 'Wiki Home', url: 'index.html' },
  { title: 'Green Slime', url: 'green.html' },
];

function setTheme(theme) {
  document.documentElement.classList.remove('light-mode', 'dark-mode');
  if (theme) {
    document.documentElement.classList.add(theme + '-mode');
  }
  localStorage.setItem('theme', theme);
}

function initTheme() {
  const saved = localStorage.getItem('theme');
  if (saved) {
    setTheme(saved);
  }
}

document.addEventListener('DOMContentLoaded', () => {
  // Let wiki control its own theme; default to dark if unset
  initTheme();
  const lightBtn = document.getElementById('light-btn');
  const darkBtn = document.getElementById('dark-btn');

  if (lightBtn) {
    lightBtn.addEventListener('click', () => setTheme('light'));
  }
  if (darkBtn) {
    darkBtn.addEventListener('click', () => setTheme('dark'));
  }
  // search removed
});
