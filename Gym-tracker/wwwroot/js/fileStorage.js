window.gymTracker = (() => {
    let _db = null;
    let _handle = null;
    let _installPromptEvent = null;
    let _swRegistration = null;
    let _updateState = null;          // 'downloading' | 'ready' | null
    let _updateListener = null;       // .NET object reference

    // Capture install prompt as early as possible
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        _installPromptEvent = e;
    });

    async function openDb() {
        if (_db) return _db;
        return new Promise((resolve, reject) => {
            const req = indexedDB.open('gym_tracker_fs', 1);
            req.onupgradeneeded = e => e.target.result.createObjectStore('handles');
            req.onsuccess = e => { _db = e.target.result; resolve(_db); };
            req.onerror = e => reject(e);
        });
    }

    async function storeHandle(handle) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('handles', 'readwrite');
            tx.objectStore('handles').put(handle, 'workouts');
            tx.oncomplete = resolve;
            tx.onerror = reject;
        });
    }

    async function loadHandle() {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction('handles', 'readonly');
            const req = tx.objectStore('handles').get('workouts');
            req.onsuccess = e => resolve(e.target.result ?? null);
            req.onerror = reject;
        });
    }

    async function ensurePermission(handle) {
        // Only QUERY — never request. requestPermission() requires a user gesture
        // and is called explicitly via requestFilePermission() below.
        const perm = await handle.queryPermission({ mode: 'readwrite' });
        return perm === 'granted';
    }

    return {
        isSupported() {
            return 'showSaveFilePicker' in window;
        },

        async hasHandle() {
            try {
                const h = await loadHandle();
                return h != null;
            } catch {
                return false;
            }
        },

        async pickFile() {
            try {
                const handle = await window.showSaveFilePicker({
                    suggestedName: 'workouts.json',
                    types: [{ description: 'JSON', accept: { 'application/json': ['.json'] } }]
                });
                _handle = handle;
                await storeHandle(handle);
                return true;
            } catch {
                // User cancelled
                return false;
            }
        },

        async readFile() {
            try {
                if (!_handle) _handle = await loadHandle();
                if (!_handle) return null;
                if (!await ensurePermission(_handle)) return null;
                const file = await _handle.getFile();
                return await file.text();
            } catch {
                return null;
            }
        },

        async writeFile(content) {
            try {
                if (!_handle) _handle = await loadHandle();
                if (!_handle) return false;
                if (!await ensurePermission(_handle)) return false;
                const writable = await _handle.createWritable();
                await writable.write(content);
                await writable.close();
                return true;
            } catch {
                return false;
            }
        },

        async clearHandle() {
            _handle = null;
            const db = await openDb();
            return new Promise((resolve, reject) => {
                const tx = db.transaction('handles', 'readwrite');
                tx.objectStore('handles').delete('workouts');
                tx.oncomplete = resolve;
                tx.onerror = reject;
            });
        },

        // ── Permission helpers ─────────────────────────────────────────
        async checkFilePermission() {
            try {
                const h = _handle || await loadHandle();
                if (!h) return false;
                return (await h.queryPermission({ mode: 'readwrite' })) === 'granted';
            } catch { return false; }
        },

        async requestFilePermission() {
            try {
                const h = _handle || await loadHandle();
                if (!h) return false;
                const perm = await h.queryPermission({ mode: 'readwrite' });
                if (perm === 'granted') { _handle = h; return true; }
                const req = await h.requestPermission({ mode: 'readwrite' });
                if (req === 'granted') { _handle = h; return true; }
                return false;
            } catch { return false; }
        },

        playChime() {
            try {
                const ctx = new (window.AudioContext || window.webkitAudioContext)();
                [[880, 0], [1100, 0.18], [1320, 0.36]].forEach(([freq, start]) => {
                    const osc = ctx.createOscillator(), gain = ctx.createGain();
                    osc.connect(gain); gain.connect(ctx.destination);
                    osc.type = 'sine';
                    osc.frequency.setValueAtTime(freq, ctx.currentTime + start);
                    gain.gain.setValueAtTime(0.22, ctx.currentTime + start);
                    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + start + 0.38);
                    osc.start(ctx.currentTime + start);
                    osc.stop(ctx.currentTime  + start + 0.38);
                });
            } catch { /* Safari / no AudioContext */ }
        },

        // ── PWA install prompt ────────────────────────────────────────
        isInStandaloneMode() {
            return window.matchMedia('(display-mode: standalone)').matches
                || window.navigator.standalone === true;
        },

        isIos() {
            return /iPhone|iPad|iPod/i.test(navigator.userAgent) && !window.MSStream;
        },

        isInstallable() {
            return _installPromptEvent !== null && !this.isInStandaloneMode();
        },

        async showInstallPrompt() {
            if (!_installPromptEvent) return 'dismissed';
            _installPromptEvent.prompt();
            const { outcome } = await _installPromptEvent.userChoice;
            _installPromptEvent = null; // can only be used once
            return outcome; // 'accepted' | 'dismissed'
        },

        // ── Data export ───────────────────────────────────────────────
        downloadJson(content, filename) {
            const blob = new Blob([content], { type: 'application/json' });
            const url  = URL.createObjectURL(blob);
            const a    = document.createElement('a');
            a.href     = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        },

        // ── Utility ───────────────────────────────────────────────────
        clickById(id) {
            const el = document.getElementById(id);
            if (el) el.click();
        },

        // ── Service-worker update lifecycle ───────────────────────────
        // Called from index.html once the SW is registered. Watches for
        // a new worker installing and notifies the .NET listener.
        attachServiceWorker(reg) {
            if (!reg) return;
            _swRegistration = reg;

            const watchInstalling = (worker) => {
                if (!worker) return;
                _setUpdateState('downloading');
                worker.addEventListener('statechange', () => {
                    if (worker.state === 'installed') {
                        // Only counts as a real update when there is already
                        // an active controller (i.e. not the first install).
                        if (navigator.serviceWorker.controller) {
                            _setUpdateState('ready');
                        } else {
                            _setUpdateState(null);
                        }
                    }
                });
            };

            // A worker may already be waiting/installing when we attach
            if (reg.waiting && navigator.serviceWorker.controller) {
                _setUpdateState('ready');
            }
            if (reg.installing) {
                watchInstalling(reg.installing);
            }

            reg.addEventListener('updatefound', () => watchInstalling(reg.installing));
        },

        // The Razor banner registers itself here so we can call back into
        // .NET whenever the SW lifecycle progresses.
        registerUpdateListener(dotNetRef) {
            _updateListener = dotNetRef;
            // Replay current state so a late subscriber still gets the banner
            if (_updateState) {
                _notifyUpdate(_updateState);
            }
            // And surface the post-reload "just updated" flag once
            const justApplied = sessionStorage.getItem('gym_tracker_update_just_applied');
            if (justApplied === '1') {
                sessionStorage.removeItem('gym_tracker_update_just_applied');
                _notifyUpdate('activated');
            }
        },

        unregisterUpdateListener() {
            _updateListener = null;
        },

        // Apply the pending update: post SKIP_WAITING to the waiting worker
        // and let the controllerchange handler in index.html reload the page.
        applyUpdate() {
            if (!_swRegistration || !_swRegistration.waiting) return false;
            sessionStorage.setItem('gym_tracker_update_requested', '1');
            _swRegistration.waiting.postMessage({ type: 'SKIP_WAITING' });
            return true;
        }
    };

    function _setUpdateState(state) {
        _updateState = state;
        if (state) _notifyUpdate(state);
    }

    function _notifyUpdate(state) {
        if (!_updateListener) return;
        try {
            _updateListener.invokeMethodAsync('OnUpdateEvent', state);
        } catch { /* listener disposed */ }
    }
})();
