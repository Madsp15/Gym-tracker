window.gymTracker = (() => {
    let _db = null;
    let _handle = null;

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
        const perm = await handle.queryPermission({ mode: 'readwrite' });
        if (perm === 'granted') return true;
        const req = await handle.requestPermission({ mode: 'readwrite' });
        return req === 'granted';
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
        }
    };
})();

