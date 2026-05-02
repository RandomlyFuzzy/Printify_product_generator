import { delay, randomBetween } from '../timing.js';

export async function simulateReading(page) {
  const readTime = randomBetween(2000, 5000);
  const steps = randomBetween(2, 4);
  const stepTime = readTime / steps;

  for (let i = 0; i < steps; i++) {
    await delay(stepTime);
    if (Math.random() > 0.5) {
      const scrollAmount = randomBetween(50, 200);
      await page.evaluate((amt) => {
        window.scrollBy({ top: amt, behavior: 'smooth' });
      }, scrollAmount);
    }
  }
}
