// The board and side panels are arranged for 16:9. Keep that viewport at every size.
window.JungangMobileLayout = function (config) {
  const mobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
    (/Macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
  const container = document.querySelector('#unity-container');
  const frame = document.querySelector('#unity-fullscreen-container');
  const canvas = document.querySelector('#unity-canvas');
  document.documentElement.classList.add('jungang-fill');
  const fullscreenButton = document.querySelector('#unity-fullscreen-button');
  // The template footer is hidden; retain a small explicit toggle for re-entry.
  if (fullscreenButton) {
    container.appendChild(fullscreenButton);
    fullscreenButton.setAttribute('role', 'button');
    fullscreenButton.setAttribute('aria-label', '전체화면 전환');
    fullscreenButton.title = '전체화면 전환';
    fullscreenButton.tabIndex = 0;
  }
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
    const visual = !fullscreen && window.visualViewport && window.visualViewport.scale === 1
      ? window.visualViewport : null;
    container.style.width = (visual ? visual.width : window.innerWidth) + 'px';
    container.style.height = (visual ? visual.height : window.innerHeight) + 'px';
    container.style.left = (visual ? visual.offsetLeft : 0) + 'px';
    container.style.top = (visual ? visual.offsetTop : 0) + 'px';
    const style = getComputedStyle(container);
    const width = container.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
    const height = container.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
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
  let autoFullscreenAttempted = false;
  const gestureEvent = window.PointerEvent ? 'pointerup' : 'touchend';
  function stopAutoFullscreen() {
    autoFullscreenAttempted = true;
    container.removeEventListener(gestureEvent, autoFullscreen);
  }
  async function enterFullscreen() {
    if (document.fullscreenElement || document.webkitFullscreenElement) return;
    const enter = container.requestFullscreen || container.webkitRequestFullscreen;
    if (enter) await enter.call(container);
  }
  function autoFullscreen(event) {
    if (event.type === 'pointerup' && (!event.isPrimary || event.button !== 0)) return;
    if (autoFullscreenAttempted || !event.isTrusted ||
        (event.target.closest && event.target.closest('#unity-fullscreen-button'))) return;
    stopAutoFullscreen();
    // Let Unity consume the initiating tap before changing the canvas size.
    // Transient user activation remains available across these two frames.
    requestAnimationFrame(() => requestAnimationFrame(() => {
      enterFullscreen().catch(() => {}).finally(scheduleFit);
    }));
  }
  // On load the page already fills the viewport. Native fullscreen is requested
  // once on the first real interaction, as required by browser activation rules.
  container.addEventListener(gestureEvent, autoFullscreen, { passive: true });
  window.JungangToggleFullscreen = async function () {
    stopAutoFullscreen();
    try {
      if (document.fullscreenElement || document.webkitFullscreenElement) {
        const exit = document.exitFullscreen || document.webkitExitFullscreen;
        if (exit) await exit.call(document);
      } else {
        await enterFullscreen();
      }
    } catch (_) {
      // Browsers without fullscreen permission continue in the fitted window.
    } finally { scheduleFit(); }
  };
  if (fullscreenButton) {
    fullscreenButton.onclick = () => window.JungangToggleFullscreen();
    fullscreenButton.onkeydown = event => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault(); window.JungangToggleFullscreen();
      }
    };
  }
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
