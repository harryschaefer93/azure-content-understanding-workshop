// CU Test Harness — JS interop helpers
window.harnessInterop = {
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },

    // ── PDF.js Document Viewer ──

    _pdfDoc: null,
    _pdfScale: 1.5,

    /**
     * Render a PDF page to a canvas element.
     * @param {string} canvasId - ID of the <canvas> element
     * @param {Uint8Array} pdfData - PDF file bytes
     * @param {number} pageNum - 1-based page number
     * @param {number} rotation - rotation in degrees (0, 90, 180, 270)
     * @returns {{ width, height, pageCount, scale }} rendered dimensions + total pages
     */
    renderPdfPage: async function (canvasId, pdfData, pageNum, rotation) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return null;

        // Set worker source (CDN-hosted)
        if (typeof pdfjsLib !== 'undefined') {
            pdfjsLib.GlobalWorkerOptions.workerSrc =
                'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
        } else {
            console.error('PDF.js not loaded');
            return null;
        }

        try {
            // Load document (cache for page navigation)
            if (!this._pdfDoc) {
                this._pdfDoc = await pdfjsLib.getDocument({ data: pdfData }).promise;
            }

            const page = await this._pdfDoc.getPage(pageNum);
            const viewport = page.getViewport({ scale: this._pdfScale, rotation: rotation || 0 });

            canvas.width = viewport.width;
            canvas.height = viewport.height;

            const ctx = canvas.getContext('2d');
            await page.render({ canvasContext: ctx, viewport: viewport }).promise;

            return {
                width: viewport.width,
                height: viewport.height,
                pageCount: this._pdfDoc.numPages,
                scale: this._pdfScale
            };
        } catch (err) {
            console.error('PDF render error:', err);
            return null;
        }
    },

    /** Reset cached PDF document (call when switching files). */
    resetPdfCache: function () {
        if (this._pdfDoc) {
            this._pdfDoc.destroy();
            this._pdfDoc = null;
        }
    },

    /**
     * Draw bounding boxes on an overlay div positioned over the canvas/image.
     * Polygon coordinates are in inches (Azure DI/CU format).
     * @param {string} overlayId - ID of the overlay <div>
     * @param {Array} boxes - [{ fieldName, value, polygon: [x1,y1,...], pageNumber }]
     * @param {number} scale - PDF.js scale factor (used with 72 DPI to convert inches→px)
     * @param {number} canvasWidth - rendered canvas width
     * @param {number} canvasHeight - rendered canvas height
     * @param {string|null} highlightedField - field name to highlight, or null
     * @param {number} rotation - rotation applied to the page (0, 90, 180, 270)
     * @param {object|null} dotnetRef - .NET object reference for click callbacks
     */
    drawBoundingBoxes: function (overlayId, boxes, scale, canvasWidth, canvasHeight, highlightedField, rotation, dotnetRef) {
        const overlay = document.getElementById(overlayId);
        if (!overlay) return;
        overlay.innerHTML = '';
        overlay.style.width = canvasWidth + 'px';
        overlay.style.height = canvasHeight + 'px';

        const pxPerInch = 72 * scale;
        const rot = rotation || 0;

        boxes.forEach(function (box) {
            if (!box.polygon || box.polygon.length < 4) return;

            const xs = [], ys = [];
            for (let i = 0; i < box.polygon.length; i += 2) {
                var xInch = box.polygon[i];
                var yInch = box.polygon[i + 1];
                var xPx, yPx;

                // Transform coordinates based on rotation
                if (rot === 90) {
                    xPx = yInch * pxPerInch;
                    yPx = canvasHeight - xInch * pxPerInch;
                } else if (rot === 180) {
                    xPx = canvasWidth - xInch * pxPerInch;
                    yPx = canvasHeight - yInch * pxPerInch;
                } else if (rot === 270) {
                    xPx = canvasWidth - yInch * pxPerInch;
                    yPx = xInch * pxPerInch;
                } else {
                    xPx = xInch * pxPerInch;
                    yPx = yInch * pxPerInch;
                }

                xs.push(xPx);
                ys.push(yPx);
            }
            const minX = Math.min.apply(null, xs);
            const minY = Math.min.apply(null, ys);
            const maxX = Math.max.apply(null, xs);
            const maxY = Math.max.apply(null, ys);

            const isHighlighted = box.fieldName === highlightedField;
            const div = document.createElement('div');
            div.className = 'bbox-overlay' + (isHighlighted ? ' bbox-highlighted' : '');
            div.style.left = minX + 'px';
            div.style.top = minY + 'px';
            div.style.width = (maxX - minX) + 'px';
            div.style.height = (maxY - minY) + 'px';
            div.title = box.fieldName + ': ' + (box.value || '');

            // Field name label (visible on hover or when highlighted)
            var label = document.createElement('span');
            label.className = 'bbox-label';
            label.textContent = box.fieldName;
            div.appendChild(label);

            // Click handler for bidirectional highlighting
            div.addEventListener('click', function () {
                if (dotnetRef) {
                    dotnetRef.invokeMethodAsync('OnBBoxClicked', box.fieldName);
                }
            });

            overlay.appendChild(div);
        });
    },

    /** Clear all bounding box overlays. */
    clearOverlays: function (overlayId) {
        const overlay = document.getElementById(overlayId);
        if (overlay) overlay.innerHTML = '';
    },

    /**
     * Scroll to and flash-highlight a field row in the results table.
     * @param {string} fieldName - the data-field attribute value to find
     */
    scrollToFieldRow: function (fieldName) {
        const row = document.querySelector('tr[data-field="' + fieldName + '"]');
        if (!row) return;
        row.scrollIntoView({ behavior: 'smooth', block: 'center' });
        row.classList.add('field-flash');
        setTimeout(function () { row.classList.remove('field-flash'); }, 1500);
    },

    /**
     * Render an image (JPG, PNG, etc.) into an <img> element and return dimensions.
     * @param {string} imgId - ID of the <img> element
     * @param {Uint8Array} imageData - image file bytes
     * @param {string} contentType - MIME type
     * @returns {{ width, height }}
     */
    renderImage: function (imgId, imageData, contentType) {
        const img = document.getElementById(imgId);
        if (!img) return null;

        const blob = new Blob([imageData], { type: contentType });
        const url = URL.createObjectURL(blob);
        img.src = url;

        return new Promise(function (resolve) {
            img.onload = function () {
                resolve({ width: img.naturalWidth, height: img.naturalHeight });
            };
        });
    }
};
