mergeInto(LibraryManager.library, {
  $JungangClipboardState: {
    canvas: null, visible: false, focused: false, code: "", box: null,
    down: false, result: 0, rawAt: -10000, serial: 0,
    hit: function(e) {
      var s=JungangClipboardState;
      if(!s.visible||!s.code||!s.box||!s.canvas||!s.canvas.isConnected)return false;
      var r=s.canvas.getBoundingClientRect(),b=s.box;
      if(r.width<=0||r.height<=0||b.sw<=0||b.sh<=0)return false;
      var x=(e.clientX-r.left)*b.sw/r.width;
      var y=b.sh-(e.clientY-r.top)*b.sh/r.height;
      return x>=b.x&&x<=b.x+b.w&&y>=b.y&&y<=b.y+b.h;
    },
    legacy: function(text) {
      var previous=document.activeElement,field=document.createElement("textarea"),ok=false;
      field.value=text;field.readOnly=true;
      field.style.cssText="position:fixed;left:-10000px;top:0;width:1px;height:1px;opacity:0;";
      document.body.appendChild(field);
      try{field.focus();field.select();field.setSelectionRange(0,text.length);ok=document.execCommand("copy");}
      catch(e){ok=false;}
      finally{field.remove();if(previous&&previous.isConnected&&previous.focus)previous.focus();}
      return ok;
    },
    copy: function() {
      var s=JungangClipboardState;
      if(!s.visible||!s.code)return;
      var text=s.code,request=++s.serial;s.result=0;
      function failed() {
        if(request!==s.serial||!s.visible||text!==s.code)return;
        if(s.legacy(text)){s.result=1;return;}
        s.result=-1;
        window.prompt("자동 복사가 차단되었습니다. 아래 초대코드를 복사하세요.",text);
      }
      try {
        if(window.isSecureContext&&navigator.clipboard&&navigator.clipboard.writeText) {
          navigator.clipboard.writeText(text).then(function(){if(request===s.serial&&s.visible&&text===s.code)s.result=1;},failed);
        } else failed();
      }catch(e){failed();}
    },
    install: function() {
      var s=JungangClipboardState;
      if(s.canvas)return;
      s.canvas=Module.canvas;
      // Run in the browser's real input event, before Unity's later frame callback.
      document.addEventListener("pointerdown",function(e){s.down=e.target===s.canvas&&(e.button===0||e.pointerType==="touch")&&s.hit(e);},true);
      document.addEventListener("pointerup",function(e){var copy=s.down&&s.hit(e);s.down=false;if(copy){s.rawAt=performance.now();s.copy();}},true);
      document.addEventListener("pointercancel",function(){s.down=false;},true);
      document.addEventListener("keydown",function(e){if(s.visible&&s.focused&&!e.repeat&&(e.key==="Enter"||e.key===" ")){s.rawAt=performance.now();s.copy();}},true);
    }
  },
  Jungang_ClipboardUpdate__deps: ["$JungangClipboardState","$UTF8ToString"],
  Jungang_ClipboardUpdate: function(text,x,y,w,h,sw,sh,visible,focused) {
    var s=JungangClipboardState;s.install();
    var code=UTF8ToString(text);
    if(s.code!==code){s.serial++;s.result=0;s.down=false;}
    s.code=code;s.box={x:x,y:y,w:w,h:h,sw:sw,sh:sh};s.visible=!!visible;s.focused=!!focused;
  },
  Jungang_ClipboardCopyFallback__deps: ["$JungangClipboardState"],
  Jungang_ClipboardCopyFallback: function() {
    var s=JungangClipboardState;
    if(performance.now()-s.rawAt>=2000)s.copy();
  },
  Jungang_ClipboardReadResult__deps: ["$JungangClipboardState"],
  Jungang_ClipboardReadResult: function() {
    var s=JungangClipboardState,result=s.result;s.result=0;return result;
  },
  Jungang_ClipboardHide__deps: ["$JungangClipboardState"],
  Jungang_ClipboardHide: function() {
    var s=JungangClipboardState;s.visible=false;s.down=false;s.result=0;s.serial++;
  }
});
