import fs   from 'fs';
import path from 'path';

import { States } from '../states.js';
import { TRANSITIONS, pickNextState } from '../transitions.js';
import { simulateReading } from '../../statelessfunctions/humanBehavior/simulateReading.js';
import { humanScroll } from '../../statelessfunctions/humanBehavior/humanScroll.js';
import { delay, randomBetween } from '../../statelessfunctions/timing.js';
import { extractProductId } from '../../statelessfunctions/parsers.js';

export async function stateVisitProduct(ctx) {
  const { page, links, PRODUCTS_DIR } = ctx;

  if (!links || links.length === 0) return States.BROWSE_PAGE;

  const randomCard = links[randomBetween(0, links.length - 1)];
  const label = randomCard.title?.substring(0, 40) ?? randomCard.url;
  console.log(`  Visiting product: ${label}...`);

  try {
    await page.goto(randomCard.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await delay(randomBetween(1000, 3000));
    const visitBehavior = pickNextState([
      ['SCROLL_DEEPER', 80],
      ['GO_BACK_SOON', 20],
    ]);

    if (visitBehavior === 'SCROLL_DEEPER') {
      console.log('  Product behavior: scroll down before returning');
      await simulateReading(page);
      await humanScroll(page);
      if (Math.random() > 0.5) {
        await page.keyboard.press('PageDown');
        await delay(randomBetween(200, 600));
      }
    } else {
      console.log('  Product behavior: quick return to results');
      await delay(randomBetween(500, 1500));
    }

    const productId = extractProductId(page.url());
    const urlPath   = page.url().split('/itm/')[1] || '';
    let productName = urlPath.split('?')[0]
      .replace(/[^a-zA-Z0-9-_]/g, '_')
      .substring(0, 100);

    if (!productName || productName === productId) {
      productName = page.url().split('/itm/')[1]
        ?.split('/')[1]
        ?.replace(/[^a-zA-Z0-9-_]/g, '_')
        .substring(0, 100) ?? 'product';
    }

    const fileName = `${productId}_${productName}.html`;
    const filePath = path.join(PRODUCTS_DIR, fileName);
    const pageHTML = await page.content();
    fs.writeFileSync(filePath, pageHTML);
    console.log(`  Saved product page: ${fileName}`);

    await page.goBack({ waitUntil: 'domcontentloaded', timeout: 60000 });
    await delay(randomBetween(1000, 2000));
  } catch (e) {
    console.log('  Error visiting product, continuing...');
  }

  if (ctx.currentPageNum >= ctx.maxPages) return States.QUERY_DONE;

  return pickNextState(TRANSITIONS[States.VISIT_PRODUCT]);
}
