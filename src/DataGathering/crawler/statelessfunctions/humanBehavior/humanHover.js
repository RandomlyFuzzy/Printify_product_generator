import { delay, randomBetween } from '../timing.js';

export async function humanHover(page) {
  try {
    const elements = await page.$$('a, button, .s-item, .srp-results li');
    if (elements.length > 0) {
      const randIdx = randomBetween(0, Math.min(10, elements.length - 1));
      const element = elements[randIdx];

      const rect = await element.boundingBox();
      if (rect) {
        const x = rect.x + randomBetween(5, rect.width - 5);
        const y = rect.y + randomBetween(5, rect.height - 5);
        await page.mouse.move(x, y, { steps: randomBetween(3, 8) });
        await delay(randomBetween(300, 800));
      }
    }
  } catch (e) {}
}
