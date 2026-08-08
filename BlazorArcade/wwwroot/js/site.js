window.resizeHandler = {
    addEventListener: function (dotNetHelper) {
        window.addEventListener('resize', () => {
            dotNetHelper.invokeMethodAsync('OnWindowResize');
        });
    },
    removeEventListener: function () {
        window.removeEventListener('resize');
    }
};

window.audioContext = new (window.AudioContext || window.webkitAudioContext)();

window.playWaka = function () {
    if (window.audioContext.state === 'suspended') window.audioContext.resume();
    let osc = window.audioContext.createOscillator();
    let gain = window.audioContext.createGain();
    osc.type = 'triangle';
    osc.frequency.setValueAtTime(300, window.audioContext.currentTime);
    osc.frequency.exponentialRampToValueAtTime(600, window.audioContext.currentTime + 0.1);
    gain.gain.setValueAtTime(0.1, window.audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.1);
    osc.connect(gain);
    gain.connect(window.audioContext.destination);
    osc.start();
    osc.stop(window.audioContext.currentTime + 0.1);
}

window.playEatGhost = function () {
    if (window.audioContext.state === 'suspended') window.audioContext.resume();
    let osc = window.audioContext.createOscillator();
    let gain = window.audioContext.createGain();
    osc.type = 'square';
    osc.frequency.setValueAtTime(1000, window.audioContext.currentTime);
    osc.frequency.exponentialRampToValueAtTime(2000, window.audioContext.currentTime + 0.2);
    gain.gain.setValueAtTime(0.1, window.audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.2);
    osc.connect(gain);
    gain.connect(window.audioContext.destination);
    osc.start();
    osc.stop(window.audioContext.currentTime + 0.2);
}

window.playDie = function () {
    if (window.audioContext.state === 'suspended') window.audioContext.resume();
    let osc = window.audioContext.createOscillator();
    let gain = window.audioContext.createGain();
    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(300, window.audioContext.currentTime);
    osc.frequency.exponentialRampToValueAtTime(50, window.audioContext.currentTime + 0.5);
    gain.gain.setValueAtTime(0.1, window.audioContext.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.5);
    osc.connect(gain);
    gain.connect(window.audioContext.destination);
    osc.start();
    osc.stop(window.audioContext.currentTime + 0.5);
}

window.tangramInterop = {
    checkSolution: function(targets, pieces) {
        let canvas = document.createElement('canvas');
        canvas.width = 1100;
        canvas.height = 750;
        let ctx = canvas.getContext('2d', { willReadFrequently: true });
        ctx.imageSmoothingEnabled = false;

        ctx.fillStyle = 'black';
        ctx.fillRect(0, 0, 1100, 750);

        ctx.fillStyle = 'red';
        for (let poly of targets) {
            ctx.beginPath();
            ctx.moveTo(poly[0].x, poly[0].y);
            for (let i = 1; i < poly.length; i++) ctx.lineTo(poly[i].x, poly[i].y);
            ctx.closePath();
            ctx.fill();
        }

        ctx.globalCompositeOperation = 'source-over';
        ctx.fillStyle = 'green';
        ctx.strokeStyle = 'green';
        ctx.lineWidth = 1;
        for (let poly of pieces) {
            ctx.beginPath();
            ctx.moveTo(poly[0].x, poly[0].y);
            for (let i = 1; i < poly.length; i++) ctx.lineTo(poly[i].x, poly[i].y);
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
        }

        let data = ctx.getImageData(0, 0, 1100, 750).data;
        let redPixels = 0;
        for (let i = 0; i < data.length; i += 4) {
            if (data[i] > 128 && data[i+1] < 128) {
                redPixels++;
            }
        }
        
        return redPixels < 50;
    }
};

window.tangramInterop.getSvgScale = function() { let svg = document.querySelector('.tangram-svg'); return svg ? 1100.0 / svg.getBoundingClientRect().width : 1.0; };

window.blockBlastAudio = {
    playPlace: function() {
        if (!window.audioContext) return;
        if (window.audioContext.state === 'suspended') window.audioContext.resume();
        let osc = window.audioContext.createOscillator();
        let gain = window.audioContext.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(400, window.audioContext.currentTime);
        osc.frequency.exponentialRampToValueAtTime(800, window.audioContext.currentTime + 0.08);
        gain.gain.setValueAtTime(0.15, window.audioContext.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.08);
        osc.connect(gain);
        gain.connect(window.audioContext.destination);
        osc.start();
        osc.stop(window.audioContext.currentTime + 0.08);
    },
    playBlast: function() {
        if (!window.audioContext) return;
        if (window.audioContext.state === 'suspended') window.audioContext.resume();
        let osc = window.audioContext.createOscillator();
        let gain = window.audioContext.createGain();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(300, window.audioContext.currentTime);
        osc.frequency.exponentialRampToValueAtTime(1200, window.audioContext.currentTime + 0.2);
        gain.gain.setValueAtTime(0.25, window.audioContext.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.25);
        osc.connect(gain);
        gain.connect(window.audioContext.destination);
        osc.start();
        osc.stop(window.audioContext.currentTime + 0.25);
    },
    playCombo: function(streak) {
        if (!window.audioContext) return;
        if (window.audioContext.state === 'suspended') window.audioContext.resume();
        let baseFreq = 500 + (streak * 100);
        let osc = window.audioContext.createOscillator();
        let gain = window.audioContext.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(baseFreq, window.audioContext.currentTime);
        osc.frequency.exponentialRampToValueAtTime(baseFreq * 1.5, window.audioContext.currentTime + 0.3);
        gain.gain.setValueAtTime(0.2, window.audioContext.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.3);
        osc.connect(gain);
        gain.connect(window.audioContext.destination);
        osc.start();
        osc.stop(window.audioContext.currentTime + 0.3);
    },
    playGameOver: function() {
        if (!window.audioContext) return;
        if (window.audioContext.state === 'suspended') window.audioContext.resume();
        let osc = window.audioContext.createOscillator();
        let gain = window.audioContext.createGain();
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(400, window.audioContext.currentTime);
        osc.frequency.exponentialRampToValueAtTime(100, window.audioContext.currentTime + 0.6);
        gain.gain.setValueAtTime(0.2, window.audioContext.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.01, window.audioContext.currentTime + 0.6);
        osc.connect(gain);
        gain.connect(window.audioContext.destination);
        osc.start();
        osc.stop(window.audioContext.currentTime + 0.6);
    }
};

window.blockBlastStorage = {
    getHighScore: function() {
        return parseInt(localStorage.getItem('blockblast_highscore') || '0', 10);
    },
    saveHighScore: function(score) {
        localStorage.setItem('blockblast_highscore', score.toString());
    }
};

window.blockBlastInteraction = {
    dotNetHelper: null,
    isDragging: false,
    dragShapeId: null,
    dragClone: null,
    lastRow: -1,
    lastCol: -1,

    init: function(helper) {
        this.dotNetHelper = helper;
        
        this.pointerDownHandler = this.onPointerDown.bind(this);
        this.pointerMoveHandler = this.onPointerMove.bind(this);
        this.pointerUpHandler = this.onPointerUp.bind(this);
        
        document.addEventListener('pointerdown', this.pointerDownHandler, { passive: false });
        document.addEventListener('pointermove', this.pointerMoveHandler, { passive: false });
        document.addEventListener('pointerup', this.pointerUpHandler);
        document.addEventListener('pointercancel', this.pointerUpHandler);
    },

    cleanup: function() {
        document.removeEventListener('pointerdown', this.pointerDownHandler);
        document.removeEventListener('pointermove', this.pointerMoveHandler);
        document.removeEventListener('pointerup', this.pointerUpHandler);
        document.removeEventListener('pointercancel', this.pointerUpHandler);
        this.dotNetHelper = null;
    },

    onPointerDown: function(e) {
        let trayShape = e.target.closest('.tray-shape');
        if (trayShape) {
            let shapeId = parseInt(trayShape.getAttribute('data-shape-id'));
            if (!isNaN(shapeId)) {
                this.startDrag(e, trayShape, shapeId);
            }
        }
    },

    startDrag: function(e, shapeElement, shapeId) {
        if (this.isDragging) return;
        
        this.isDragging = true;
        this.dragShapeId = shapeId;
        
        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('OnShapeDragStart', shapeId);
        }

        this.dragClone = shapeElement.cloneNode(true);
        this.dragClone.classList.add('blockblast-drag-clone');
        this.dragClone.style.position = 'fixed';
        this.dragClone.style.pointerEvents = 'none';
        this.dragClone.style.zIndex = '9999';
        
        let isTouch = e.pointerType === 'touch';
        let yOffset = isTouch ? 60 : 0; 

        this.updateClonePosition(e.clientX, e.clientY - yOffset);
        document.body.appendChild(this.dragClone);
    },

    onPointerMove: function(e) {
        if (!this.isDragging || !this.dragClone) return;
        
        let isTouch = e.pointerType === 'touch';
        let yOffset = isTouch ? 60 : 0;

        let targetY = e.clientY - yOffset;
        this.updateClonePosition(e.clientX, targetY);

        let grid = document.querySelector('.grid-container');
        let r = -1;
        let c = -1;

        if (grid) {
            let rect = grid.getBoundingClientRect();
            // Check if we are within the general bounds of the grid
            if (e.clientX >= rect.left && e.clientX <= rect.right &&
                targetY >= rect.top && targetY <= rect.bottom) {
                
                let gridSizeStr = grid.style.getPropertyValue('--grid-size');
                let gridSize = gridSizeStr ? parseInt(gridSizeStr) : 8;
                
                // Divide the entire grid rect into equal cells, naturally splitting the gaps in half!
                let cellTotalWidth = rect.width / gridSize;
                let cellTotalHeight = rect.height / gridSize;
                
                c = Math.floor((e.clientX - rect.left) / cellTotalWidth);
                r = Math.floor((targetY - rect.top) / cellTotalHeight);
                
                c = Math.max(0, Math.min(gridSize - 1, c));
                r = Math.max(0, Math.min(gridSize - 1, r));
            }
        }

        if (r !== -1 && c !== -1) {
            if (r !== this.lastRow || c !== this.lastCol) {
                this.lastRow = r;
                this.lastCol = c;
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OnShapeHover', r, c);
                }
            }
        } else {
            if (this.lastRow !== -1) {
                this.lastRow = -1;
                this.lastCol = -1;
                if (this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('OnShapeHover', -1, -1);
                }
            }
        }
    },

    onPointerUp: function(e) {
        if (!this.isDragging) return;
        
        this.isDragging = false;
        if (this.dragClone && this.dragClone.parentNode) {
            this.dragClone.parentNode.removeChild(this.dragClone);
        }
        this.dragClone = null;
        
        let r = this.lastRow;
        let c = this.lastCol;
        
        this.lastRow = -1;
        this.lastCol = -1;

        if (this.dotNetHelper) {
            if (r !== -1 && c !== -1) {
                this.dotNetHelper.invokeMethodAsync('OnShapeDrop', r, c);
            } else {
                this.dotNetHelper.invokeMethodAsync('OnShapeDragCancel');
            }
        }
    },

    updateClonePosition: function(x, y) {
        if (this.dragClone) {
            let rect = this.dragClone.getBoundingClientRect();
            // Try to use the original rect width/height if it hasn't rendered yet
            let w = rect.width || 100;
            let h = rect.height || 100;
            this.dragClone.style.left = (x - w/2) + 'px';
            this.dragClone.style.top = (y - h/2) + 'px';
        }
    }
};

