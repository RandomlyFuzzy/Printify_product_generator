import { delay, randomBetween } from '../timing.js';

export async function goToNextPage(page) {
  try {
    const nextBtn = await page.$('a.pagination__next, a[aria-label*="Next"], a[rel="next"]');
    if (nextBtn) {
      const rect = await nextBtn.boundingBox();
      if (rect) {
        await page.mouse.move(rect.x + rect.width / 2, rect.y + rect.height / 2, { steps: randomBetween(5, 15) });
        await delay(randomBetween(50, 150));
        await page.mouse.down();
        await delay(randomBetween(50, 150));
        await page.mouse.up();
        await delay(randomBetween(3000, 5000));
        return true;
      }
    }
  } catch (e) {}
  return false;
}