import {log} from 'console';

export function onPrepare() {
    log("YantraJS Script is being prepared");
}

export function onStart() {
    log("YantraJS Script has started");
}

export function onUpdate() {
    log("YantraJS Script is updating");
}

export function onDestroy() {
    log("YantraJS Script is being destroyed");
}

