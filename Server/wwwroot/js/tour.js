(function () {
    const highlightClass = 'tour-highlight';
    let currentTargetId = null;

    function clearHighlight() {
        if (!currentTargetId) {
            return;
        }

        const element = document.getElementById(currentTargetId);
        if (element) {
            element.classList.remove(highlightClass);
            element.removeAttribute('data-tour-active');
            if (element.dataset.tourOriginalPosition === 'static') {
                element.style.position = '';
            }
            delete element.dataset.tourOriginalPosition;
        }

        currentTargetId = null;
    }

    function focusTarget(targetId) {
        if (!targetId) {
            clearHighlight();
            return;
        }

        const element = document.getElementById(targetId);
        if (!element) {
            clearHighlight();
            return;
        }

        if (currentTargetId !== targetId) {
            clearHighlight();
            currentTargetId = targetId;
        }

        const computedPosition = window.getComputedStyle(element).position;
        if (computedPosition === 'static') {
            element.dataset.tourOriginalPosition = 'static';
            element.style.position = 'relative';
        } else {
            delete element.dataset.tourOriginalPosition;
        }

        element.classList.add(highlightClass);
        element.setAttribute('data-tour-active', 'true');
        if (typeof element.scrollIntoView === 'function') {
            element.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
        }
    }

    window.tourHelper = {
        highlight: function (targetId) {
            document.body.classList.add('tour-active');
            focusTarget(targetId);
        },
        clear: function () {
            clearHighlight();
            document.body.classList.remove('tour-active');
        },
        shouldStart: function (key) {
            if (!key) {
                return true;
            }

            return localStorage.getItem(key) !== 'completed';
        },
        markCompleted: function (key) {
            if (!key) {
                return;
            }

            localStorage.setItem(key, 'completed');
        }
    };
})();

