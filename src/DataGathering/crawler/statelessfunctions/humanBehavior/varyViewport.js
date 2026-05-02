import { delay, randomBetween } from '../timing.js';

export async function varyViewport(page) {
  if (Math.random() > 0.9) {
    try {
      const width  = 1920 + randomBetween(-50, 50);
      const height = 1080 + randomBetween(-30, 30);
      // Vary deviceScaleFactor too — common values are 1, 1.25, 1.5, 2
      const scales = [1, 1, 1, 1.25, 1.25, 1.5, 2];
      const deviceScaleFactor = scales[Math.floor(Math.random() * scales.length)];
      await page.setViewport({ width, height, deviceScaleFactor });
      await delay(randomBetween(500, 1000));
    } catch (e) {}
  }
}
