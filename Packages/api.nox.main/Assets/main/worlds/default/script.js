import { log } from "logger";

export function onPrepare() {
    log("onPrepare called");
}

export function onPlayerJoin(player) {
    log(`Player ${player.Display} joined the game`);
}

export function onPlayerLeave(player) {
    log(`Player ${player.name} left the game`);
}