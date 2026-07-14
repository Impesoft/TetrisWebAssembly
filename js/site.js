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
