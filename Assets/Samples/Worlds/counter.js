import console from 'console';
import { from as bufferFrom, toString as bufferToString } from 'buffer';
import { getPublic, setPublic } from 'tables';

export let exports = {
    result: null,
};

let count = 0;
let lastClickTime = 0;
const SAVE_DELAY_MS = 5000;
let dirty = false;

function updateLabel() {
    if (exports?.result)
        exports.result.text = count.toString();
}

async function saveCount() {
    const raw = bufferFrom(count.toString(), 'utf8');
    await setPublic(raw);
    console.log(`Counter saved: ${count}`);
}

export async function onAwake() {
    const raw = await getPublic();
    if (!raw || raw.length === 0) {
        count = 0;
    } else {
        const str = bufferToString(raw, 'utf8');
        const parsed = parseInt(str, 10);
        count = isNaN(parsed) ? 0 : parsed;
    }
    updateLabel();
    console.log(`Counter loaded: ${count}`);
}

export async function onClick() {
    count++;
    updateLabel();
    lastClickTime = Date.now();
    dirty = true;
    console.log(`Counter: ${count}`);
}

export async function onUpdate() {
    if (dirty && Date.now() - lastClickTime >= SAVE_DELAY_MS) {
        dirty = false;
        await saveCount();
    }
}

export async function onDestroy() {
    if (dirty) {
        dirty = false;
        await saveCount();
    }
}

export async function onDisable() {
    if (dirty) {
        dirty = false;
        await saveCount();
    }
}
