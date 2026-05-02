import { States } from '../states.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';

async function smoothScrollToBottom(page) {
  await page.evaluate(async () => {
    await new Promise((resolve) => {
      const distance = 120;
      const baseDelay = 18;
      let scrolled = 0;
      const total = document.body.scrollHeight - window.innerHeight;

      function step() {
        const jitter = Math.round((Math.random() - 0.5) * 40);
        const amount = Math.min(distance + jitter, total - scrolled);
        if (amount <= 0) {
          // Overshoot: scroll a little past the bottom then bounce back
          const overshoot = Math.round(60 + Math.random() * 80);
          window.scrollBy(0, overshoot);
          setTimeout(() => {
            window.scrollBy(0, -overshoot);
            resolve();
          }, 180 + Math.round(Math.random() * 150));
          return;
        }
        window.scrollBy(0, amount);
        scrolled += amount;
        const pause = baseDelay + Math.round(Math.random() * 30);
        setTimeout(step, pause);
      }
      step();
    });
  });
}

export async function stateScrollToBottom(ctx) {
  const { page } = ctx;

  await smoothScrollToBottom(page);
  await delay(randomBetween(300, 700));
  ctx.readyForNextPage = true;

  return States.NEXT_PAGE;
}
