import console from 'console';
import { from as bufferFrom, toString as bufferToString } from 'buffer';
import { getPublic, setPublic } from 'tables';

export let exports = {
    result: null,
};

let count = 0;

function updateLabel() {
    if (exports?.result)
        exports.result.text = count.toString();
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
    const raw = bufferFrom(count.toString(), 'utf8');
    await setPublic(raw);
    console.log(`Counter: ${count}`);
}
