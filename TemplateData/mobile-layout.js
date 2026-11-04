// Keep the game's original coordinate space, including fixed-size UI canvases.
window.JungangMobileLayout = function (config) {
  const mobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
    (/Macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
  if (!mobile) return;

  const viewport = document.querySelector('meta[name="viewport"]') || document.createElement('meta');
  viewport.name = 'viewport';
  viewport.content = 'width=device-width, initial-scale=1, viewport-fit=cover';
  if (!viewport.parentNode) document.head.appendChild(viewport);
  const container = document.querySelector('#unity-container');
  const frame = document.querySelector('#unity-fullscreen-container');
  const canvas = document.querySelector('#unity-canvas');
  container.className = 'unity-mobile';
  document.documentElement.classList.add('jungang-mobile');
  canvas.width = 960;
  canvas.height = 600;
  config.matchWebGLToCanvasSize = false;
  config.devicePixelRatio = 1;

  const guide = document.createElement('div');
  guide.id = 'mobile-rotate-guide';
  guide.setAttribute('role', 'status');
  guide.innerHTML = '<div><span aria-hidden="true">↻</span><p>휴대폰을 가로로 돌려주세요</p><small>가로 화면에서 게임을 플레이할 수 있습니다.</small></div>';
  document.body.appendChild(guide);

  function fit() {
    const visual = window.visualViewport;
    container.style.width = (visual ? visual.width : window.innerWidth) + 'px';
    container.style.height = (visual ? visual.height : window.innerHeight) + 'px';
    container.style.left = (visual ? visual.offsetLeft : 0) + 'px';
    container.style.top = (visual ? visual.offsetTop : 0) + 'px';
    const style = getComputedStyle(container);
    const width = container.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    const height = container.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    const scale = Math.min(width / 960, height / 600);
    frame.style.width = (960 * scale) + 'px';
    frame.style.height = (600 * scale) + 'px';
  }
  window.addEventListener('resize', fit);
  window.addEventListener('orientationchange', fit);
  if (window.visualViewport) {
    window.visualViewport.addEventListener('resize', fit);
    window.visualViewport.addEventListener('scroll', fit);
  }
  fit();
};
