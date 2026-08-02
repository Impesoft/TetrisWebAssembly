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
