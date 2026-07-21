(() => {
  "use strict";

  const payload = window.THUMBHASH_DEMO;
  if (!payload?.images?.length) {
    document.getElementById("gallery").textContent = "演示数据尚未生成。";
    return;
  }

  const gallery = document.getElementById("gallery");
  const delayInput = document.getElementById("delay");
  const delayValue = document.getElementById("delay-value");
  const replayButton = document.getElementById("replay");
  const toast = document.getElementById("toast");
  const cards = [];
  let generation = 0;
  let toastTimer = 0;

  const formatBytes = (bytes) => {
    if (bytes < 1024) return `${bytes} B`;
    return `${(bytes / 1024).toFixed(bytes > 1024 * 100 ? 0 : 1)} KB`;
  };

  const showToast = (message) => {
    toast.textContent = message;
    toast.classList.add("visible");
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => toast.classList.remove("visible"), 1800);
  };

  const copyText = async (text) => {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      const textarea = document.createElement("textarea");
      textarea.value = text;
      textarea.style.position = "fixed";
      textarea.style.opacity = "0";
      document.body.append(textarea);
      textarea.select();
      document.execCommand("copy");
      textarea.remove();
    }
  };

  payload.images.forEach((image, index) => {
    const card = document.createElement("figure");
    card.className = `image-card ${image.layout}`;
    card.dataset.transparent = image.source.endsWith(".png") ? "true" : "false";
    card.innerHTML = `
      <div class="media-shell" style="--ratio: ${image.width} / ${image.height}; --average: ${image.averageColor}" tabindex="0" role="button" aria-label="按住对比 ${image.title} 的 ThumbHash 占位图">
        <img class="thumb-image" src="${image.placeholder}" alt="" aria-hidden="true">
        <img class="full-image" alt="${image.title}" draggable="false">
        <span class="load-state">ThumbHash</span>
      </div>
      <figcaption class="card-caption">
        <div>
          <h3><a href="${image.sourcePage}" target="_blank" rel="noreferrer">${image.title} ↗</a></h3>
          <p>${image.kind} · ${image.width}×${image.height} · ${formatBytes(image.fileBytes)} → ${image.hashBytes} B hash</p>
        </div>
        <button class="hash-button" type="button" aria-label="复制 ${image.title} 的 ThumbHash">复制 Hash</button>
      </figcaption>`;

    const shell = card.querySelector(".media-shell");
    const fullImage = card.querySelector(".full-image");
    const loadState = card.querySelector(".load-state");
    const hashButton = card.querySelector(".hash-button");

    const startPeek = () => card.classList.add("is-peeking");
    const stopPeek = () => card.classList.remove("is-peeking");
    shell.addEventListener("pointerdown", startPeek);
    shell.addEventListener("pointerup", stopPeek);
    shell.addEventListener("pointercancel", stopPeek);
    shell.addEventListener("pointerleave", stopPeek);
    shell.addEventListener("keydown", (event) => {
      if (event.code === "Space" || event.code === "Enter") {
        event.preventDefault();
        startPeek();
      }
    });
    shell.addEventListener("keyup", stopPeek);
    shell.addEventListener("blur", stopPeek);
    hashButton.addEventListener("click", async () => {
      await copyText(image.thumbHash);
      showToast(`${image.title} · ThumbHash 已复制`);
    });

    gallery.append(card);
    cards.push({ card, fullImage, loadState, image, index });
  });

  const replay = () => {
    generation += 1;
    const currentGeneration = generation;
    const baseDelay = Number(delayInput.value);

    cards.forEach(({ card, fullImage, loadState, image, index }) => {
      card.classList.remove("is-loaded", "is-peeking");
      loadState.textContent = "ThumbHash";
      fullImage.removeAttribute("src");

      window.setTimeout(() => {
        if (currentGeneration !== generation) return;

        const reveal = () => {
          const stagger = Math.min(index * 90, 540);
          window.setTimeout(() => {
            if (currentGeneration !== generation) return;
            card.classList.add("is-loaded");
            loadState.textContent = "原图就绪";
          }, baseDelay + stagger);
        };

        fullImage.addEventListener("load", reveal, { once: true });
        fullImage.src = `${image.source}?demo=${currentGeneration}`;
        if (fullImage.complete) reveal();
      }, Math.min(index * 55, 330));
    });
  };

  delayInput.addEventListener("input", () => {
    delayValue.textContent = `${delayInput.value} ms`;
  });
  delayInput.addEventListener("change", replay);
  replayButton.addEventListener("click", replay);

  replay();
})();
