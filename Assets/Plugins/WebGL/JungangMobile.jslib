mergeInto(LibraryManager.library, {
  Jungang_IsMobileBrowser: function () {
    return /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) ||
      (/Macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1) ? 1 : 0;
  }
});
