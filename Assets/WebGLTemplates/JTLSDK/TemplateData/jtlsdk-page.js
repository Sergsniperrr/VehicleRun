(function () {
  "use strict";

  var body = document.body;
  var data = body.dataset;
  var canvas = document.getElementById("unity-canvas");
  var inputBlock = document.getElementById("jtlsdk-input-block");
  var loader = document.getElementById("jtlsdk-loader");
  var progressFill = document.getElementById("jtlsdk-progress-fill");
  var isMobile = /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
  var page = window.JTLSDK_PAGE || (window.JTLSDK_PAGE = {});

  page.platform = data.platform;

  function resolvePixelRatio(setting) {
    var native = window.devicePixelRatio || 1;

    if (!setting || setting === "auto") {
      return native;
    }

    if (setting.indexOf("max:") === 0) {
      return Math.min(native, parseFloat(setting.substring(4)) || native);
    }

    return parseFloat(setting) || native;
  }

  function parseAspect(value) {
    if (!value || value === "free") {
      return 0;
    }

    var parts = String(value).split(/[\/:x×]/i);

    if (parts.length === 1) {
      var single = parseFloat(parts[0]);
      return single > 0 ? single : 0;
    }

    var width = parseFloat(parts[0]);
    var height = parseFloat(parts[1]);
    return width > 0 && height > 0 ? width / height : 0;
  }

  function fillsViewport(aspect, width, height) {
    if (aspect <= 0) {
      return true;
    }

    if (aspect < 1) {
      return width <= height;
    }

    if (aspect > 1) {
      return width >= height;
    }

    return width === height;
  }

  function fitCanvas() {
    var aspect = isMobile && data.aspectMobile === "free" ? 0 : parseAspect(data.aspect);
    var width = window.innerWidth;
    var height = window.innerHeight;

    if (fillsViewport(aspect, width, height)) {
      canvas.style.width = "100%";
      canvas.style.height = "100%";
      return;
    }

    if (width / height > aspect) {
      width = Math.round(height * aspect);
    } else {
      height = Math.round(width / aspect);
    }

    canvas.style.width = width + "px";
    canvas.style.height = height + "px";
  }

  function showBanner(message, type) {
    if (type === "error") {
      console.error(message);
    } else {
      console.warn(message);
    }
  }

  function sendFirstFrameReady() {
    if (page.firstFrameReadySent || typeof ytgame === "undefined" || !ytgame.game || !ytgame.game.firstFrameReady) {
      return;
    }

    try {
      ytgame.game.firstFrameReady();
      page.firstFrameReadySent = true;
    } catch (error) {
      page.firstFrameReadySent = false;
    }
  }

  function preloadPlatform() {
    if (data.platform === "yandex" && typeof YaGames !== "undefined") {
      page.ysdk = YaGames.init();
    }

    if (data.platform === "youtube") {
      sendFirstFrameReady();
    }
  }

  page.blockInput = function (blocked) {
    if (inputBlock === null) {
      return;
    }

    inputBlock.classList.toggle("jtlsdk-input-block--on", blocked);

    if (blocked && typeof canvas.blur === "function") {
      canvas.blur();
    }
  };

  function hideLoader() {
    loader.classList.add("jtlsdk-loader--hidden");
    window.setTimeout(function () {
      loader.style.display = "none";
    }, 300);
  }

  var config = {
    dataUrl: data.data,
    frameworkUrl: data.framework,
    codeUrl: data.code,
    memoryUrl: data.memory,
    symbolsUrl: data.symbols,
    streamingAssetsUrl: "StreamingAssets",
    companyName: data.company,
    productName: data.product,
    productVersion: data.version,
    devicePixelRatio: resolvePixelRatio(isMobile ? data.dprMobile : data.dprDesktop),
    showBanner: showBanner
  };

  window.addEventListener("resize", fitCanvas);
  window.addEventListener("orientationchange", fitCanvas);
  fitCanvas();
  preloadPlatform();

  var script = document.createElement("script");
  script.src = data.loader;
  script.onload = function () {
    createUnityInstance(canvas, config, function (progress) {
      progressFill.style.width = Math.round(progress * 100) + "%";
    }).then(function (instance) {
      page.unityInstance = instance;
      hideLoader();
    }).catch(function (message) {
      console.error(message);
    });
  };
  document.body.appendChild(script);
})();
