import { delay, randomBetween, randomDelay } from '../timing.js';

export async function humanMouseMove(page, selector, directMove = false) {
  try {
    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (!rect) return;

    const targetX = rect.x + randomBetween(5, rect.width - 5);
    const targetY = rect.y + randomBetween(5, rect.height - 5);

    if (directMove) {
      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    } else {
      const startX = randomBetween(0, 100);
      const startY = randomBetween(0, 100);
      await page.mouse.move(startX, startY);
      await delay(randomBetween(100, 300));

      const midPoints = randomBetween(2, 4);
      for (let i = 0; i < midPoints; i++) {
        const midX = randomBetween(Math.min(startX, targetX), Math.max(startX, targetX));
        const midY = randomBetween(Math.min(startY, targetY), Math.max(startY, targetY));
        await page.mouse.move(midX, midY, { steps: randomBetween(3, 8) });
        await delay(randomBetween(100, 300));
      }

      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    }

    await randomDelay();
  } catch (e) {}
}