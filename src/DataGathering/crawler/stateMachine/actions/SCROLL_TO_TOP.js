import { States } from '../states.js';
import { findSearchbar } from '../../statelessfunctions/humanBehavior/findSearchbar.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

async function smoothScrollToTop(page) {
  await page.evaluate(async () => {
    await new Promise((resolve) => {
      const distance = 150;
      const baseDelay = 16;

      function step() {
        if (window.scrollY <= 0) { resolve(); return; }
        const jitter = Math.round(Math.random() * 40);
        window.scrollBy(0, -(distance + jitter));
        const pause = baseDelay + Math.round(Math.random() * 25);
        setTimeout(step, pause);
      }
      step();
    });
  });
}

export async function stateScrollToTop(ctx) {
  const { page } = ctx;

  await smoothScrollToTop(page);
  await delay(randomBetween(300, 800));

  ctx.searchSelector = await findSearchbar(page);
  if (!ctx.searchSelector) {
    return States.ENSURE_HOME;
  }

  return States.SELECT_SEARCH;
}
