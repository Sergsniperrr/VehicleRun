// Greedy player simulation of the GDD v1.0 economy (sections 4-10).
// Usage: node gdd_pace_sim.js <secondsPerAction> [priceMultiplier] [priceGrowth] [growthCap] [priceGrowthRank1]
const secondsPerAction = Number(process.argv[2] || 1.5);
const priceMultiplier = Number(process.argv[3] || 12);
const priceGrowth = Number(process.argv[4] || 1.04);
const growthCap = Number(process.argv[5] || 20);
const priceGrowthRank1 = Number(process.argv[6] || priceGrowth);

const MaxRank = 20;
const income = [0];
for (let r = 1; r <= MaxRank; r++) {
  income.push(Math.floor(Math.pow(2.2, r - 1) + 0.5));
}

const slotsFor = k => 6 + 3 * [3, 5, 7].filter(t => k >= t).length;
const trackFor = k => (k >= 12 ? 8 : k >= 8 ? 6 : k >= 4 ? 4 : 2);
const decorFor = k => 1 + 0.1 * Math.floor(Math.min(k, 20) / 4);
const boxRank = k => Math.max(1, k - 4);
const priceFor = (b, n) => Math.ceil(priceMultiplier * income[b] * Math.pow(b === 1 ? priceGrowthRank1 : priceGrowth, Math.min(n, growthCap)));

let k = 1;
let coins = 60;
let board = [{ kind: 'car', rank: 1 }, { kind: 'car', rank: 1 }];
let track = [];
let bought = {};
let freeTimer = 0;
let readyDelivery = false;
let time = 0;
const reached = { 1: 0 };
let boxesBought = 0;
let boxesFree = 0;
let idleActions = 0;

const freeSlots = () => slotsFor(k) - board.length;

function unlock(rank) {
  if (rank > k) {
    k = rank;
    reached[k] = time;
  }
}

function findPair() {
  const byRank = {};
  for (let i = 0; i < board.length; i++) {
    const item = board[i];
    if (item.kind === 'car' && item.rank < MaxRank) {
      if (byRank[item.rank] !== undefined) {
        return [byRank[item.rank], i];
      }
      byRank[item.rank] = i;
    }
  }
  return null;
}

function act() {
  const boxIndex = board.findIndex(item => item.kind === 'box');
  if (boxIndex >= 0) {
    board[boxIndex] = { kind: 'car', rank: board[boxIndex].rank };
    return;
  }

  const pair = findPair();
  if (pair) {
    const rank = board[pair[0]].rank + 1;
    board.splice(pair[1], 1);
    board[pair[0]] = { kind: 'car', rank };
    unlock(rank);
    return;
  }

  const cars = board.filter(item => item.kind === 'car').sort((a, b) => b.rank - a.rank);
  if (cars.length > 0 && track.length < trackFor(k)) {
    board.splice(board.indexOf(cars[0]), 1);
    track.push(cars[0].rank);
    return;
  }

  track.sort((a, b) => a - b);
  if (cars.length > 0 && track.length > 0 && cars[0].rank > track[0]) {
    const swapped = track[0];
    track[0] = cars[0].rank;
    cars[0].rank = swapped;
    return;
  }

  const b = boxRank(k);
  const price = priceFor(b, bought[b] || 0);
  if (freeSlots() > 0 && coins >= price) {
    coins -= price;
    bought[b] = (bought[b] || 0) + 1;
    boxesBought++;
    board.push({ kind: 'box', rank: b });
    return;
  }

  if (freeSlots() === 0 && cars.length > 0) {
    const lowest = cars[cars.length - 1];
    board.splice(board.indexOf(lowest), 1);
    return;
  }

  idleActions++;
}

function tick(dt) {
  const rate = decorFor(k) * track.reduce((sum, rank) => sum + income[rank], 0);
  coins += rate * dt;
  if (readyDelivery === false) {
    freeTimer += dt;
    if (freeTimer >= 20) {
      freeTimer = 0;
      readyDelivery = true;
    }
  }
  if (readyDelivery && freeSlots() > 0) {
    board.push({ kind: 'box', rank: boxRank(k) });
    boxesFree++;
    readyDelivery = false;
  }
  time += dt;
}

const Limit = 6 * 3600;
while (k < MaxRank && time < Limit) {
  act();
  tick(secondsPerAction);
}

const fmt = s => (s === undefined ? '-' : `${Math.floor(s / 60)}m${String(Math.round(s % 60)).padStart(2, '0')}s`);
console.log(`secondsPerAction=${secondsPerAction} priceMultiplier=${priceMultiplier} priceGrowth=${priceGrowth} growthCap=${growthCap} priceGrowthRank1=${priceGrowthRank1}`);
console.log([2, 4, 6, 8, 10, 12, 14, 16, 18, 20].map(r => `K${r}=${fmt(reached[r])}`).join('  '));
console.log(`bought=${boxesBought} free=${boxesFree} idleActions=${idleActions} coins=${Math.floor(coins)}`);
