import { delay, randomBetween, randomDelay } from '../timing.js';
import { humanMouseMove } from './humanMouseMove.js';

export async function humanClick(page, selector, directMove = false) {
  try {
    await humanMouseMove(page, selector, directMove);

    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (rect) {
      const x = rect.x + randomBetween(5, rect.width - 5);
      const y = rect.y + randomBetween(5, rect.height - 5);

      await page.mouse.move(x, y);
      await delay(randomBetween(50, 150));
      await page.mouse.down();
      await delay(randomBetween(50, 150));
      await page.mouse.up();
      await randomDelay();
    }
  } catch (e) {}
}
