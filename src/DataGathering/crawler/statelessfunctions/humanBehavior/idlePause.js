import { delay, randomBetween } from '../timing.js';

export async function idlePause(page) {
  if (Math.random() > 0.8) {
    await delay(randomBetween(5000, 15000));
  }

  // ~20% chance: simulate switching away and back (tab blur/focus)
  if (page && Math.random() < 0.2) {
    await page.evaluate(() => {
      document.dispatchEvent(new Event('visibilitychange'));
      Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true });
    }).catch(() => {});
    await delay(randomBetween(2000, 8000));
    await page.evaluate(() => {
      Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true });
      document.dispatchEvent(new Event('visibilitychange'));
      window.dispatchEvent(new Event('focus'));
    }).catch(() => {});
    await delay(randomBetween(300, 700));
  }
}
