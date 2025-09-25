window.initFieldDragDrop = (canvasSelector, dotnetHelper) => {
    const canvas = document.querySelector(canvasSelector);
    if (!canvas) return;
    if (canvas.dataset.dragInit === 'true') return;
    canvas.dataset.dragInit = 'true';

    let draggedType = null;

    const autoScroll = (e) => {
        const threshold = 60;
        const y = e.clientY;
        if (y < threshold) {
            window.scrollBy(0, -20);
        } else if (window.innerHeight - y < threshold) {
            window.scrollBy(0, 20);
        }
    };
    document.addEventListener('dragover', autoScroll);

    document.querySelectorAll('.draggable-field').forEach(el => {
        if (el.dataset.dragInit === 'true') return;
        el.dataset.dragInit = 'true';
        el.addEventListener('dragstart', e => {
            draggedType = el.getAttribute('data-type');
            e.dataTransfer.setData('text/plain', draggedType);
        });
        el.addEventListener('dragend', () => draggedType = null);
    });

    canvas.addEventListener('dragover', e => {
        e.preventDefault();
    });

    canvas.addEventListener('drop', e => {
        e.preventDefault();
        const type = draggedType;
        draggedType = null;
        if (!type) return;

        const zone = e.target.closest('.row-dropzone');
        if (zone) {
            const insertIndex = parseInt(zone.getAttribute('data-insert'));
            const sec = parseInt(zone.getAttribute('data-section'));
            dotnetHelper.invokeMethodAsync('AddRowFromDrop', type, sec, insertIndex);
            return;
        }

        const rowEl = e.target.closest('.designer-row');
        let rowIndex = -1;
        let secIndex = -1;
        let colIndex = -1;
        if (rowEl) {
            const sectionEl = rowEl.closest('.section-wrapper');
            if (sectionEl) secIndex = parseInt(sectionEl.getAttribute('data-section'));
            rowIndex = parseInt(rowEl.getAttribute('data-row'));
            const colEl = e.target.closest('[data-id]');
            if (colEl) {
                colIndex = parseInt(colEl.getAttribute('data-id'));
            }
        }
        dotnetHelper.invokeMethodAsync('AddFieldFromDrop', type, secIndex, rowIndex, colIndex);
    });
};

// Backwards compatibility
window.initDragDrop = window.initFieldDragDrop;

window.focusFieldLabel = (sectionIndex, rowIndex, fieldIndex) => {
    requestAnimationFrame(() => {
        const selector = `#section-${sectionIndex}-rows .designer-row[data-row='${rowIndex}'] [data-id='${fieldIndex}'] input.form-control-sm`;
        const el = document.querySelector(selector);
        if (el) {
            el.focus();
        }
    });
};

window.focusLastInput = (container) => {
    if (!container) return;
    requestAnimationFrame(() => {
        const inputs = container.querySelectorAll('input');
        if (inputs.length > 0) {
            inputs[inputs.length - 1].focus();
        }
    });
};

window.triggerClick = (el) => {
    if (el) el.click();
};
