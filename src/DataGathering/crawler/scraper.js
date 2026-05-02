import puppeteer from 'puppeteer';
import fs         from 'fs';

import { selectSuggestions }                        from './ollama_helper.js';
import { delay, randomBetween, shuffleArray }        from './statelessfunctions/timing.js';
import { humanScroll }                               from './statelessfunctions/humanBehavior/humanScroll.js';
import { OUTPUT_DIR, PRODUCTS_DIR, persistRealtimeState, loadStateSets } from './persistence/stateStore.js';
import { initCSVFiles, logQuery, logSuggestion, loadSearchedQueries, loadSuggestions, normalizeQuery, saveCardData } from './persistence/csvLogger.js';
import { runQueryMachine }                           from './stateMachine/index.js';
import { logError, logWarn, logInfo, logDebug }      from './persistence/logger.js';

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

  let allQueries = [...new Set([...initialQueries, ...suggestionsSet].map(normalizeQuery).filter(Boolean))];
  allQueries = shuffleArray(allQueries);

  const browser = await puppeteer.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
    protocolTimeout: 120_000,  // 2 min — prevents mouse/wheel CDP timeouts under load
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
    logInfo('SIGINT received, stopping gracefully');
    persist({ lastEvent: 'sigint' });
    await browser.close();
    logInfo('Browser closed, shutdown complete');
    process.exit(0);
  });

  try {
    if (!page.url().includes('ebay.com')) {
      try {
        await page.goto('https://www.ebay.com', { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.evaluate(() => window.scrollTo(0, 0));
      } catch (navErr) {
        logError('Navigation to eBay failed', navErr, { url: 'https://www.ebay.com' });
        throw navErr;
      }
    }

    while (!exiting) {
      let currentIndex = 0;

      while (currentIndex < allQueries.length && !exiting) {
        const query = normalizeQuery(allQueries[currentIndex]);

        if (!query) {
          currentIndex++;
          continue;
        }

        logDebug(`Processing query`, { query, index: currentIndex, total: allQueries.length });

        // Build the state-machine context for this query
        const ctx = {
          page,
          query,
          searchedSet,
          suggestionsSet,
          csvLogger,
          persist,
          PRODUCTS_DIR,
        };

        try {
          const newSuggestions = await runQueryMachine(ctx);

          logInfo(`Query processed`, { query, suggestionsCount: newSuggestions.length });
          currentIndex++;

          if (currentIndex < allQueries.length && newSuggestions.length > 0) {
            logDebug(`Filtering suggestions with Ollama`, { query, suggestionCount: newSuggestions.length });
            const filtered = await selectSuggestions(query, newSuggestions);

            for (const s of filtered) {
              const nextQuery = normalizeQuery(s);
              if (nextQuery && !searchedSet.has(nextQuery) && !allQueries.includes(nextQuery)) {
                allQueries.push(nextQuery);
                logInfo(`Queued new search`, { query: nextQuery, fromQuery: query });
              }
            }
          }
        } catch (queryErr) {
          logError(`Query processing failed`, queryErr, { query, index: currentIndex });
          currentIndex++;
        }

        await humanScroll(page);
        await delay(randomBetween(500, 1500));
      }

      if (exiting) break;

      logInfo(`Cycle complete, restarting`, { queryCount: allQueries.length });
      allQueries = shuffleArray(allQueries);
    }
  } catch (err) {
    logError('Fatal error in main loop', err, { exiting, currentQueryCount: allQueries.length });
  } finally {
    persist({ lastEvent: 'shutdown' });
    if (!exiting) {
      await delay(2000);
      await browser.close();
      logInfo('Browser closed after normal shutdown');
    }
  }
}

main();

