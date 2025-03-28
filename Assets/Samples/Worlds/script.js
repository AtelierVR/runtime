import { log } from "logger";

let d = Date.now();

export function onUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    gameObject.transform.Rotate(delta * 0.1, delta * 0.1, delta * 0.1);
}

export function onPrepare() {
   // test(5_000_000);
}

async function test(n) {
    let li = [];
    for (let i = 0; i < 25; i++) {
        let t0 = Date.now();
        let i = 0;
        while (i < n) i++;
        let t1 = Date.now();
        li.push(t1 - t0);
        log(`${i} iterations took ${t1 - t0}ms`);
    }

    let sum = 0;
    for (let i = 0; i < li.length; i++)
        sum += li[i];
    log("Average time: " + (sum / li.length) + "ms");
}



