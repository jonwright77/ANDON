window.andonTheme = {
    get: function () {
        return localStorage.getItem('andon-theme') || 'light';
    },
    set: function (theme) {
        localStorage.setItem('andon-theme', theme);
        document.documentElement.classList.toggle('dark', theme === 'dark');
    },
    toggle: function () {
        this.set(this.get() === 'dark' ? 'light' : 'dark');
    }
};
