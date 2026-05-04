import { transform, rigidbody } from 'behaviour';
import { log } from 'console';

let d = Date.now();
export let speed = 0.1;

export let exports = {
    target: null,
    distance: 10
};

export function onAwake() {
    log("Script Awake");
    reset();
}

function reset() {
    if (!exports?.target) return;
    transform.position = exports.target.position;
    if (!rigidbody) return;
    rigidbody.linearVelocity = new Vector3(0, 0, 0);
    rigidbody.angularVelocity = new Vector3(0, 0, 0);
}

export function onUpdate() {
    let delta = Date.now() - d;
    d = Date.now();
    transform.rotate(delta * -speed, delta * speed, delta * -speed);

    if (!exports?.target) return;
    const dis = transform.position.distance(exports.target.position);
    if (dis > exports.distance) reset();
}