import { delay, randomBetween } from '../timing.js';

export async function simulateMistake(page) {
  if (Math.random() > 0.85) {
    try {
      const elements = await page.$$('a, button');
      if (elements.length > 0) {
        const randomEl = elements[randomBetween(0, Math.min(5, elements.length - 1))];
        await randomEl.click().catch(() => {});
        await delay(randomBetween(500, 1500));
        await page.goBack({ waitUntil: 'domcontentloaded' }).catch(() => {});
        await delay(randomBetween(1000, 2000));
      }
    } catch (e) {}
  }
}
