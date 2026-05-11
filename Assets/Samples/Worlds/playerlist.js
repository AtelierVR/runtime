import console from 'console';
import { count, at } from 'players';

export let exports = {
  players: null,
};

let players = [];

export function onPlayerJoined(player) {
  console.log(`Player ${player.display} joined the game`);
  players.push(player.display);
  updatePlayerList();
}

export function onPlayerLeft(player) {
  console.log(`Player ${player.display} left the game`);
  players = players.filter(p => p !== player.display);
  updatePlayerList();
}

export function onAwake() {
  for(let i = 0; i < count; i++) {
    let p = at(i);
    players.push(p.display);
    console.log(`Player ${p.display} is in the game`);
  }
  
  console.log(players);
  updatePlayerList();
}

function updatePlayerList() {
  if (!exports || !exports.players) return;
  exports.players.text = players.join('\n');
}


