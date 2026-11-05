// The board and side panels are arranged for 16:9. Keep that viewport at every size.
window.JungangMobileLayout = function (config) {
  const mobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
    (/Macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
  const container = document.querySelector('#unity-container');
  const frame = document.querySelector('#unity-fullscreen-container');
  const canvas = document.querySelector('#unity-canvas');
  config.fullscreenElementID = 'unity-container';
  // Own both CSS and render sizes. Unity's fullscreen scaler must not stretch
  // the canvas independently of this 16:9 frame.
  config.matchWebGLToCanvasSize = false;
  if (mobile) {
    const viewport = document.querySelector('meta[name="viewport"]') || document.createElement('meta');
    viewport.name = 'viewport';
    viewport.content = 'width=device-width, initial-scale=1, viewport-fit=cover';
    if (!viewport.parentNode) document.head.appendChild(viewport);
    container.className = 'unity-mobile';
    document.documentElement.classList.add('jungang-mobile');
  }

  const guide = document.createElement('div');
  guide.id = 'mobile-rotate-guide';
  guide.setAttribute('role', 'status');
  guide.innerHTML = '<div><span aria-hidden="true">↻</span><p>휴대폰을 가로로 돌려주세요</p><small>가로 화면에서 게임을 플레이할 수 있습니다.</small></div>';
  if (mobile) container.appendChild(guide);

  function fit() {
    const fullscreen = !!(document.fullscreenElement || document.webkitFullscreenElement);
    document.documentElement.classList.toggle('jungang-fullscreen', fullscreen);
    let width, height;
    if (mobile || fullscreen) {
      const visual = !fullscreen && window.visualViewport && window.visualViewport.scale === 1
        ? window.visualViewport : null;
      container.style.width = (visual ? visual.width : window.innerWidth) + 'px';
      container.style.height = (visual ? visual.height : window.innerHeight) + 'px';
      if (mobile || fullscreen) {
        container.style.left = (visual ? visual.offsetLeft : 0) + 'px';
        container.style.top = (visual ? visual.offsetTop : 0) + 'px';
      }
      const style = getComputedStyle(container);
      width = container.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
      height = container.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    } else {
      container.style.width = '';
      container.style.height = '';
      container.style.left = '';
      container.style.top = '';
      width = Math.min(960, window.innerWidth);
      height = Math.min(540, Math.max(1, window.innerHeight - 38));
    }
    const scale = Math.min(width / 960, height / 540);
    frame.style.width = (960 * scale) + 'px';
    frame.style.height = (540 * scale) + 'px';
    // Exact 16:9 backing dimensions preserve the board layout and input mapping.
    // Use physical display pixels instead of enlarging the old 960x540 image.
    const pixelRatio = Math.max(1, window.devicePixelRatio || 1);
    const maxWidth = mobile ? 2560 : 3840;
    const units = Math.max(1, Math.floor(Math.min(960 * scale * pixelRatio, maxWidth) / 16));
    const renderWidth = units * 16, renderHeight = units * 9;
    if (canvas.width !== renderWidth) canvas.width = renderWidth;
    if (canvas.height !== renderHeight) canvas.height = renderHeight;
  }
  let pending = false;
  function scheduleFit() {
    if (pending) return;
    pending = true;
    requestAnimationFrame(() => { pending = false; fit(); });
  }
  // Only a deliberate button press enters fullscreen; never do so on load/tap.
  window.JungangToggleFullscreen = async function () {
    try {
      if (document.fullscreenElement || document.webkitFullscreenElement) {
        const exit = document.exitFullscreen || document.webkitExitFullscreen;
        if (exit) await exit.call(document);
      } else {
        const enter = container.requestFullscreen || container.webkitRequestFullscreen;
        if (enter) await enter.call(container);
      }
    } catch (_) {
      // Browsers without fullscreen permission continue in the fitted window.
    } finally { scheduleFit(); }
  };
  window.addEventListener('resize', scheduleFit);
  window.addEventListener('orientationchange', scheduleFit);
  document.addEventListener('fullscreenchange', scheduleFit);
  document.addEventListener('webkitfullscreenchange', scheduleFit);
  if (screen.orientation && screen.orientation.addEventListener)
    screen.orientation.addEventListener('change', scheduleFit);
  if (typeof ResizeObserver !== 'undefined')
    new ResizeObserver(scheduleFit).observe(document.documentElement);
  if (window.visualViewport) {
    window.visualViewport.addEventListener('resize', scheduleFit);
    window.visualViewport.addEventListener('scroll', scheduleFit);
  }
  fit();
};
