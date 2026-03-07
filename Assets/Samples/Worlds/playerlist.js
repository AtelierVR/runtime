import { log } from 'console';
import { getCount, getAt } from 'players';

export let exports = {
  players: null,
};

let players = [];

export function onPlayerJoined(player) {
  log(`Player ${player.Display} joined the game`);
  players.push(player.Display);
  updatePlayerList();
}

export function onPlayerLeft(player) {
  log(`Player ${player.Display} left the game`);
  players = players.filter(p => p !== player.Display);
  updatePlayerList();
}

export function onAwake() {
  for(var i = 0; i < getCount(); i++) {
    let p = getAt(i);
    players.push(p.Display);
    log(`Player ${p.Display} is in the game`);
  }
  
  log(players);
  updatePlayerList();
}

function updatePlayerList() {
  if (!exports || !exports.players) return;
  exports.players.text = players.join('\n');
}


