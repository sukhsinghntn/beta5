window.initImageResize = (el, dotnetHelper) => {
    if (!el || el.dataset.resizeInit === 'true') return;
    el.dataset.resizeInit = 'true';
    const img = el.querySelector('img');
    if (!img) return;
    const handle = document.createElement('span');
    handle.className = 'resize-handle';
    el.appendChild(handle);
    let startX, startY, startW, startH;
    const onMouseMove = e => {
        const newW = Math.max(20, startW + (e.clientX - startX));
        const newH = Math.max(20, startH + (e.clientY - startY));
        img.style.width = newW + 'px';
        img.style.height = newH + 'px';
    };
    const onMouseUp = () => {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        if (dotnetHelper) {
            dotnetHelper.invokeMethodAsync('OnImageResized', img.offsetWidth, img.offsetHeight);
        }
    };
    handle.addEventListener('mousedown', e => {
        e.preventDefault();
        e.stopPropagation();
        startX = e.clientX;
        startY = e.clientY;
        startW = img.offsetWidth;
        startH = img.offsetHeight;
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
    handle.addEventListener('click', e => e.stopPropagation());
};
