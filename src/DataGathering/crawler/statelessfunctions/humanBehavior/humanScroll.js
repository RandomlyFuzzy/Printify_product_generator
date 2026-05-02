import { delay, randomBetween, randomDelay } from '../timing.js';

/**
 * Scroll a single tick, preferring mouse.wheel (looks more realistic) but
 * falling back to window.scrollBy if the CDP call times out or errors.
 */
async function scrollTick(page, deltaY) {
  try {
    await page.mouse.wheel({ deltaY });
  } catch {
    // ProtocolError / timeout — fall back to a plain JS scroll, which never times out
    await page.evaluate((dy) => window.scrollBy(0, dy), deltaY).catch(() => {});
  }
}

/**
 * Casual scanning scroll — simulates a human reading through results.
 * Scrolls down in variable-speed bursts, with reading pauses between them,
 * and occasionally scrolls back up a little (re-reading / reconsidering).
 * Clearly distinct from smoothScrollToTop (navigating up) and
 * smoothScrollToBottom (navigating to the next-page button).
 */
export async function humanScroll(page) {
  const bursts = randomBetween(2, 5);

  for (let b = 0; b < bursts; b++) {
    // Each burst: 2–6 wheel ticks of varying size
    const ticks = randomBetween(2, 6);
    for (let i = 0; i < ticks; i++) {
      const amount = randomBetween(80, 280);  // varied scroll distance per tick
      await scrollTick(page, amount);
      await delay(randomBetween(40, 120));    // brief pause between ticks
    }

    // Reading pause between bursts — like actually reading a card
    await delay(randomBetween(400, 1800));

    // ~25% chance: scroll back up a little (re-examining something)
    if (Math.random() < 0.25) {
      const backTicks = randomBetween(1, 3);
      for (let i = 0; i < backTicks; i++) {
        await scrollTick(page, -randomBetween(60, 180));
        await delay(randomBetween(60, 150));
      }
      await delay(randomBetween(300, 900));   // pause after scrolling back up
    }
  }

  await randomDelay();
}
