import { log, warn } from 'console';
import { from as bufferFrom, toString as bufferToString } from 'buffer';
import { getPublic, setPublic } from 'tables';

export let exports = {
    result: null,
};

let count = 0;

async function load() {
    const raw = await getPublic();
    if (!raw || raw.length === 0) {
        count = 0;
    } else {
        const str = bufferToString(raw, 'utf8');
        const parsed = parseInt(str, 10);
        count = isNaN(parsed) ? 0 : parsed;
    }
    updateLabel();
    log(`Counter loaded: ${count}`);
}

async function save() {
    const raw = bufferFrom(count.toString(), 'utf8');
    await setPublic(raw);
}

function updateLabel() {
    if (exports?.result) 
        exports.result.text = count.toString();
}

export async function onAwake() {
    await load();
}

export async function onClick() {
    count++;
    updateLabel();
    await save();
    log(`Counter: ${count}`);
}
