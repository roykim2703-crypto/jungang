// The board and side panels are arranged for 16:9. Keep that viewport at every size.
window.JungangMobileLayout = function (config) {
  const mobile = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
    (/Macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1);
  const container = document.querySelector('#unity-container');
  const frame = document.querySelector('#unity-fullscreen-container');
  const canvas = document.querySelector('#unity-canvas');
  config.fullscreenElementID = 'unity-container';
  if (mobile) {
    const viewport = document.querySelector('meta[name="viewport"]') || document.createElement('meta');
    viewport.name = 'viewport';
    viewport.content = 'width=device-width, initial-scale=1, viewport-fit=cover';
    if (!viewport.parentNode) document.head.appendChild(viewport);
    container.className = 'unity-mobile';
    document.documentElement.classList.add('jungang-mobile');
    canvas.width = 960;
    canvas.height = 540;
    config.matchWebGLToCanvasSize = false;
    config.devicePixelRatio = 1;
  }

  const guide = document.createElement('div');
  guide.id = 'mobile-rotate-guide';
  guide.setAttribute('role', 'status');
  guide.innerHTML = '<div><span aria-hidden="true">↻</span><p>휴대폰을 가로로 돌려주세요</p><small>가로 화면에서 게임을 플레이할 수 있습니다.</small></div>';
  if (mobile) container.appendChild(guide);

  function fit() {
    const fullscreen = document.fullscreenElement === container;
    let width, height;
    if (mobile || fullscreen) {
      const visual = window.visualViewport;
      container.style.width = (visual ? visual.width : window.innerWidth) + 'px';
      container.style.height = (visual ? visual.height : window.innerHeight) + 'px';
      if (mobile) {
        container.style.left = (visual ? visual.offsetLeft : 0) + 'px';
        container.style.top = (visual ? visual.offsetTop : 0) + 'px';
      }
      const style = getComputedStyle(container);
      width = container.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
      height = container.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
    } else {
      container.style.width = '';
      container.style.height = '';
      width = Math.min(960, window.innerWidth);
      height = Math.min(540, Math.max(1, window.innerHeight - 38));
    }
    const scale = Math.min(width / 960, height / 540);
    frame.style.width = (960 * scale) + 'px';
    frame.style.height = (540 * scale) + 'px';
  }
  window.addEventListener('resize', fit);
  window.addEventListener('orientationchange', fit);
  document.addEventListener('fullscreenchange', fit);
  if (window.visualViewport) {
    window.visualViewport.addEventListener('resize', fit);
    window.visualViewport.addEventListener('scroll', fit);
  }
  fit();
};
