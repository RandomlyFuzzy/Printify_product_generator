const MIN_SLOWDOWN_MULTIPLIER = 2;
const MAX_SLOWDOWN_MULTIPLIER = 3;

export function scaleDuration(ms) {
  const duration = Number(ms);
  if (!Number.isFinite(duration) || duration <= 0) return 0;

  const multiplier = Math.random() * (MAX_SLOWDOWN_MULTIPLIER - MIN_SLOWDOWN_MULTIPLIER) + MIN_SLOWDOWN_MULTIPLIER;
  return Math.floor(duration * multiplier);
}

export function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, scaleDuration(ms)));
}

export function randomBetween(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

export function randomDelay() {
  return delay(randomBetween(100, 500));
}

export function shuffleArray(arr) {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = randomBetween(0, i);
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
}
