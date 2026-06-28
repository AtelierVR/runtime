import console from 'console';
import { all } from 'players';

export let exports = {
  players: null,
};

export function onPlayerJoined(player) {
  console.log(`Player ${player.display} joined the game`);
  updateList();
}

export function onPlayerLeft(player) {
  console.log(`Player ${player.display} left the game`);
  updateList();
}

export function onAwake() {
  all.forEach(p => console.log(`Player ${p.display} is in the game`));
  updateList();
}

function updateList() {
  if (!exports || !exports.players) return;
  exports.players.text = all.map(p => p.display).join('\n');
}


