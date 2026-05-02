import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

export async function stateClickOnPage(ctx) {
  const { page } = ctx;

  console.log('  [click on page — intermediate in-page interaction]');

  try {
    const clickTarget = await page.evaluate(() => {
      const candidates = Array.from(document.querySelectorAll(
        '.s-item svg, .srp-results li svg, button, [role="button"], input[type="checkbox"]'
      ));
      if (candidates.length === 0) return null;

      const chosen = candidates[Math.floor(Math.random() * candidates.length)];
      const rect = chosen.getBoundingClientRect();
      if (!rect || rect.width <= 1 || rect.height <= 1) return null;

      return {
        x: rect.left + Math.min(12, Math.max(2, rect.width / 2)),
        y: rect.top + Math.min(12, Math.max(2, rect.height / 2)),
      };
    });

    if (clickTarget) {
      await page.mouse.move(clickTarget.x, clickTarget.y, { steps: randomBetween(4, 10) });
      await delay(randomBetween(80, 220));
      await page.mouse.click(clickTarget.x, clickTarget.y, { delay: randomBetween(30, 120) });
      await delay(randomBetween(300, 900));
    } else {
      const viewport = page.viewport() ?? { width: 1280, height: 720 };
      const x = randomBetween(100, Math.max(120, viewport.width - 100));
      const y = randomBetween(120, Math.max(140, viewport.height - 120));
      await page.mouse.move(x, y, { steps: randomBetween(3, 9) });
      await page.mouse.click(x, y, { delay: randomBetween(30, 120) });
      await delay(randomBetween(250, 700));
    }
  } catch (_) {
    // Non-critical action: continue state flow even if click fails.
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.CLICK_ON_PAGE]);
}
