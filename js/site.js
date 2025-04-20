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
