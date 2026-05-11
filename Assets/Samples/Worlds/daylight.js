import console from 'console';
import { transform } from 'behaviour';
import { Quaternion } from 'unity';

export let lastMinute = -1;
export let precentTime = -1;

export function onUpdate() {
    let time = Date.now();

    const dayInSeconds = 24 * 60 * 60;
    precentTime = (time % dayInSeconds) / dayInSeconds;

    transform.localRotation = Quaternion.Euler(precentTime * 360 + 270, 0, 0);

    const lm = Math.floor((time / 60) % 60);
    if (lm !== lastMinute) {
        const hours = Math.floor((time / 3600) % 24);
        lastMinute = lm;
        console.log(`Time: ${hours.toString().padStart(2, '0')}:${lm.toString().padStart(2, '0')}`);
    }
}