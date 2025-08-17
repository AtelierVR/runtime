import { transform } from "behaviour";

let d = Date.now();
export let speed = 0.1;

export function onUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    transform.Rotate(delta * -speed, delta * speed, delta * -speed);
}