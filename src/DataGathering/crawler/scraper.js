import puppeteer from 'puppeteer';
import fs         from 'fs';

import { selectSuggestions }                        from './ollama_helper.js';
import { delay, randomBetween, shuffleArray }        from './statelessfunctions/timing.js';
import { humanScroll }                               from './statelessfunctions/humanBehavior.js';
import { OUTPUT_DIR, PRODUCTS_DIR, persistRealtimeState, loadStateSets } from './persistence/stateStore.js';
import { initCSVFiles, logQuery, logSuggestion, loadSearchedQueries, loadSuggestions, saveCardData } from './persistence/csvLogger.js';
import { runQueryMachine }                           from './stateMachine/index.js';

fs.mkdirSync(OUTPUT_DIR,   { recursive: true });
fs.mkdirSync(PRODUCTS_DIR, { recursive: true });

async function main() {
  initCSVFiles();

  const { searched: searchedSet, suggestions: suggestionsSet } = loadStateSets();
  const persist = (extra = {}) => persistRealtimeState(searchedSet, suggestionsSet, extra);
  persist({ lastEvent: 'startup' });

  console.log(`Loaded ${searchedSet.size} previously searched queries`);
  console.log(`Loaded ${suggestionsSet.size} previous suggestions`);

  // Bundle CSV helpers into a single object passed through the machine context
  const csvLogger = { logQuery, logSuggestion, saveCardData };

  const initialQueries = ['men', 'women', 'unisex'];
  initialQueries.forEach(q => searchedSet.delete(q));

  let allQueries = [...new Set([...initialQueries, ...suggestionsSet])];
  allQueries = shuffleArray(allQueries);

  const browser = await puppeteer.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1920, height: 1080 });
  await page.setUserAgent(
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
  );

  let exiting = false;
  process.on('SIGINT', async () => {
    if (exiting) return;
    exiting = true;
    console.log('\n\n=== Stopping gracefully... ===');
    persist({ lastEvent: 'sigint' });
    await browser.close();
    console.log('Done!');
    process.exit(0);
  });

  try {
    if (!page.url().includes('ebay.com')) {
      await page.goto('https://www.ebay.com', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.evaluate(() => window.scrollTo(0, 0));
    }

    while (!exiting) {
      let currentIndex = 0;

      while (currentIndex < allQueries.length && !exiting) {
        const query = allQueries[currentIndex];

        // Build the state-machine context for this query
        const ctx = {
          page,
          query: query.toLowerCase(),
          searchedSet,
          suggestionsSet,
          csvLogger,
          persist,
          PRODUCTS_DIR,
          skipEnsureHome: currentIndex > 0,
        };

        const newSuggestions = await runQueryMachine(ctx);

        console.log(`  Queries remaining in queue: ${allQueries.length - currentIndex - 1}`);
        currentIndex++;

        if (currentIndex < allQueries.length && newSuggestions.length > 0) {
          console.log(`  Asking Ollama to filter ${newSuggestions.length} suggestions...`);
          const filtered = await selectSuggestions(query, newSuggestions);

          for (const s of filtered) {
            if (!searchedSet.has(s) && !allQueries.includes(s)) {
              allQueries.push(s);
              console.log(`  + Queued new search: "${s}"`);
            }
          }

          console.log(`  Queries remaining in queue: ${allQueries.length - currentIndex}`);
        }

        await humanScroll(page);
        await delay(randomBetween(500, 1500));
      }

      if (exiting) break;

      console.log(`\n\n=== Cycle complete, restarting with ${allQueries.length} queries ===`);
      allQueries = shuffleArray(allQueries);
    }
  } catch (err) {
    console.error('Fatal error:', err);
  } finally {
    persist({ lastEvent: 'shutdown' });
    if (!exiting) {
      await delay(2000);
      await browser.close();
    }
  }
}

main();

