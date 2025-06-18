import { log } from 'console';
import { transform } from 'behaviour';

export let lastMinute = -1;
export let precentTime = -1;

export function onUpdate() {
    let time = Date.now();

    let dayInMilliseconds = 24 * 60 * 60 * 1000;
    precentTime = (time % dayInMilliseconds) / dayInMilliseconds;
    let vec = transform.localRotation.eulerAngles;

    transform.SetLocalPositionAndRotation(
        transform.localPosition,
        Quaternion.Euler(precentTime * 360 + 270, 0, 0)
    )

    let lm = Math.floor((time / 1000 / 60) % 60);
    if (lm !== lastMinute) {
        let hours = Math.floor((time / 1000 / 60 / 60) % 24);
        lastMinute = lm;
        log(`Time: ${hours.toString().padStart(2, '0')}:${lm.toString().padStart(2, '0')}`);
    }
}