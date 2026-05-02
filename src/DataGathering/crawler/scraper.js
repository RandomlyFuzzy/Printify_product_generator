import puppeteer from 'puppeteer';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { selectSuggestions } from './ollama_helper.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const OUTPUT_DIR = path.join(__dirname, 'output');
const PRODUCTS_DIR = path.join(OUTPUT_DIR, 'products');
const RESULTS_CSV = path.join(OUTPUT_DIR, 'results.csv');
const SUGGESTIONS_CSV = path.join(OUTPUT_DIR, 'search_suggestions.csv');
const QUERIES_CSV = path.join(OUTPUT_DIR, 'queries_searched.csv');
const STATE_FILE = path.join(OUTPUT_DIR, 'state.json');

fs.mkdirSync(OUTPUT_DIR, { recursive: true });
fs.mkdirSync(PRODUCTS_DIR, { recursive: true });

const delay = (ms) => new Promise(r => setTimeout(r, ms));
const randomBetween = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;
const randomDelay = () => delay(randomBetween(100, 500));

const CARD_WRITE_INTERVAL = 1000;
let resultsBuffer = [];
let cardsBuffer = [];
let flushTimer = null;

const startBufferFlushTimer = () => {
  if (flushTimer) return;
  flushTimer = setInterval(() => {
    flushBuffers();
  }, CARD_WRITE_INTERVAL);
};

const stopBufferFlushTimer = () => {
  if (flushTimer) {
    clearInterval(flushTimer);
    flushTimer = null;
  }
  flushBuffers();
};

const flushBuffers = () => {
  if (resultsBuffer.length > 0) {
    fs.appendFileSync(RESULTS_CSV, resultsBuffer.join(''));
    resultsBuffer = [];
  }
  if (cardsBuffer.length > 0) {
    cardsBuffer.forEach(({ content, filePath }) => {
      fs.writeFileSync(filePath, content);
    });
    cardsBuffer = [];
  }
};

const initCSVFiles = () => {
  if (!fs.existsSync(RESULTS_CSV)) {
    fs.writeFileSync(RESULTS_CSV, 'shop,query,product_id,utc_time,title\n');
  }
  if (!fs.existsSync(SUGGESTIONS_CSV)) {
    fs.writeFileSync(SUGGESTIONS_CSV, 'shop,partial_query,suggestion\n');
  }
  if (!fs.existsSync(QUERIES_CSV)) {
    fs.writeFileSync(QUERIES_CSV, 'shop,query,utc_time\n');
  }
};

const logQuery = (query) => {
  const utcTime = new Date().toISOString();
  fs.appendFileSync(QUERIES_CSV, `ebay,${query},${utcTime}\n`);
};

const logSuggestion = (partialQuery, suggestion) => {
  const clean = suggestion.replace(/\n.*/g, '').trim().toLowerCase();
  if (clean) {
    fs.appendFileSync(SUGGESTIONS_CSV, `ebay,${partialQuery},${clean}\n`);
  }
  return clean;
};

const saveCardData = (cardInfo, query) => {
  const utcTime = new Date().toISOString();
  const productId = extractProductId(cardInfo.url);
  const xmlContent = cardInfo.html || '';
  const xmlPath = path.join(PRODUCTS_DIR, `${productId}.xml`);

  resultsBuffer.push(`ebay,${query},${productId},${utcTime},${cardInfo.title}\n`);
  cardsBuffer.push({ content: `<xml>${xmlContent}</xml>\n`, filePath: xmlPath });

  console.log(`  Saved: ${cardInfo.title.substring(0, 50)}...`);
  return productId;
};

const loadSearchedQueries = () => {
  const searched = new Set();
  if (fs.existsSync(QUERIES_CSV)) {
    const lines = fs.readFileSync(QUERIES_CSV, 'utf8').split('\n').slice(1);
    lines.forEach(line => {
      const parts = line.split(',');
      if (parts[1]) searched.add(parts[1].trim());
    });
  }
  return searched;
};

const loadSuggestions = () => {
  const suggestions = new Set();
  if (fs.existsSync(SUGGESTIONS_CSV)) {
    const lines = fs.readFileSync(SUGGESTIONS_CSV, 'utf8').split('\n').slice(1);
    lines.forEach(line => {
      const parts = line.split(',');
      if (parts[2]) suggestions.add(parts[2].trim());
    });
  }
  return suggestions;
};

const shuffleArray = (arr) => {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = randomBetween(0, i);
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
};

const extractProductId = (url) => {
  const match = url.match(/\/itm\/(?:.*?\/)?(\d{12,})/);
  return match ? match[1] : Date.now().toString();
};

const humanMouseMove = async (page, selector, directMove = false) => {
  try {
    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (!rect) return;

    const targetX = rect.x + randomBetween(5, rect.width - 5);
    const targetY = rect.y + randomBetween(5, rect.height - 5);

    if (directMove) {
      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    } else {
      const startX = randomBetween(0, 100);
      const startY = randomBetween(0, 100);
      await page.mouse.move(startX, startY);
      await delay(randomBetween(100, 300));

      const midPoints = randomBetween(2, 4);
      for (let i = 0; i < midPoints; i++) {
        const midX = randomBetween(Math.min(startX, targetX), Math.max(startX, targetX));
        const midY = randomBetween(Math.min(startY, targetY), Math.max(startY, targetY));
        await page.mouse.move(midX, midY, { steps: randomBetween(3, 8) });
        await delay(randomBetween(100, 300));
      }

      await page.mouse.move(targetX, targetY, { steps: randomBetween(5, 15) });
    }

    await randomDelay();
  } catch (e) {}
};

const humanHover = async (page) => {
  try {
    const elements = await page.$$('a, button, .s-item, .srp-results li');
    if (elements.length > 0) {
      const randIdx = randomBetween(0, Math.min(10, elements.length - 1));
      const element = elements[randIdx];

      const rect = await element.boundingBox();
      if (rect) {
        const x = rect.x + randomBetween(5, rect.width - 5);
        const y = rect.y + randomBetween(5, rect.height - 5);
        await page.mouse.move(x, y, { steps: randomBetween(3, 8) });
        await delay(randomBetween(300, 800));
      }
    }
  } catch (e) {}
};

const humanClick = async (page, selector, directMove = false) => {
  try {
    await humanMouseMove(page, selector, directMove);

    const rect = await page.evaluate((sel) => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x, y: r.y, width: r.width, height: r.height };
    }, selector);

    if (rect) {
      const x = rect.x + randomBetween(5, rect.width - 5);
      const y = rect.y + randomBetween(5, rect.height - 5);

      await page.mouse.move(x, y);
      await delay(randomBetween(50, 150));
      await page.mouse.down();
      await delay(randomBetween(50, 150));
      await page.mouse.up();
      await randomDelay();
    }
  } catch (e) {}
};

const simulateReading = async (page) => {
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
};

const simulateMistake = async (page) => {
  if (Math.random() > 0.85) {
    try {
      const elements = await page.$$('a, button');
      if (elements.length > 0) {
        const randomEl = elements[randomBetween(0, Math.min(5, elements.length - 1))];
        await randomEl.click().catch(() => {});
        await delay(randomBetween(500, 1500));
        await page.goBack({ waitUntil: 'domcontentloaded' }).catch(() => {});
        await delay(randomBetween(1000, 2000));
      }
    } catch (e) {}
  }
};

const varyViewport = async (page) => {
  if (Math.random() > 0.9) {
    try {
      const width = 1920 + randomBetween(-50, 50);
      const height = 1080 + randomBetween(-30, 30);
      await page.setViewport({ width, height });
      await delay(randomBetween(500, 1000));
    } catch (e) {}
  }
};

const idlePause = async () => {
  if (Math.random() > 0.8) {
    await delay(randomBetween(5000, 15000));
  }
};

const detectCaptcha = async (page) => {
  try {
    const captchaDetected = await page.evaluate(() => {
      const pageText = document.body.innerText.toLowerCase();
      const hasCaptchaText = pageText.includes('captcha') ||
        pageText.includes('robot') ||
        pageText.includes('verify you are human') ||
        pageText.includes('automated requests');

      const hasCaptchaElement = document.querySelector('iframe[src*="captcha"], iframe[src*="recaptcha"], [class*="captcha"], [id*="captcha"], [class*="hcaptcha"]') !== null;

      return hasCaptchaText || hasCaptchaElement;
    });

    if (captchaDetected) {
      console.log('\n!!! CAPTCHA DETECTED !!!');
      console.log('Waiting 15 minutes before continuing...');
      await delay(15 * 60 * 1000);
      console.log('Resuming after CAPTCHA wait...\n');
      return true;
    }
    return false;
  } catch (e) {
    return false;
  }
};

const findSearchbar = async (page) => {
  const selectors = [
    '#gh-ac',
    '#srp-gh-ac',
    'input[type="search"]',
    'input[placeholder*="Search"]',
    '.gh-search-input',
    '[data-testid="search-input"]',
    'input[name="_nkw"]',
    '.yth-searchbox-input'
  ];

  for (const sel of selectors) {
    try {
      await page.waitForSelector(sel, { timeout: 3000, visible: true });
      return sel;
    } catch (e) {}
  }
  return null;
};

const humanType = async (page, selector, text, captureSuggestions, suggestionsSet, searchedSet) => {
  await humanClick(page, selector, true);
  await page.evaluate(sel => { document.querySelector(sel).value = ''; }, selector);
  await delay(randomBetween(300, 800));

  const newSuggestions = [];

  for (let i = 0; i < text.length; i++) {
    const char = text[i];
    await page.type(selector, char, { delay: randomBetween(80, 200) });
    await delay(randomBetween(200, 500));

    if (Math.random() > 0.9) {
      await page.keyboard.press('Backspace');
      await delay(randomBetween(200, 400));
      await page.type(selector, char, { delay: randomBetween(80, 200) });
    }

    if (captureSuggestions && i >= Math.floor(text.length / 2)) {
      try {
        const suggestions = await page.evaluate(() => {
          const items = document.querySelectorAll('.ui-autocomplete-category, [role="option"], .gh-sch-ajax__item');
          return [...items].map(el => el.innerText?.trim()).filter(t => t && t.length > 2 && !t.toLowerCase().includes('search'));
        });

        const partial = text.substring(0, i + 1).toLowerCase();
        for (const s of suggestions) {
          if (!suggestionsSet.has(s)) {
            suggestionsSet.add(s);
            logSuggestion(partial, s);
            if (!searchedSet.has(s.toLowerCase())) {
              console.log(`  + New suggestion: "${s.toLowerCase()}"`);
              newSuggestions.push(s.toLowerCase());
            }
          }
        }
      } catch (e) {}
    }
  }
  return newSuggestions;
};

const humanScroll = async (page) => {
  const scrolls = randomBetween(2, 5);
  const scrollAmount = 200;
  for (let i = 0; i < scrolls; i++) {
    await page.mouse.wheel({ deltaY: scrollAmount });
    await delay(randomBetween(20, 50));
  }
  await randomDelay();
};

const getCardLinks = async (page) => {
  try {
    return await page.evaluate(() => {
      const cardsInfo = [];
      const cardSelectors = ['.s-item', '.srp-results li', '[data-viewport-container] > div', '.s-item__wrapper'];

      const cards = new Set();
      cardSelectors.forEach(sel => {
        document.querySelectorAll(sel).forEach(el => cards.add(el));
      });

      cards.forEach(card => {
        const hasSvg = card.querySelector('svg') !== null;
        if (!hasSvg) return;

        const linkEl = card.querySelector('a[href*="/itm/"]');
        if (linkEl && linkEl.href) {
          const title = card.querySelector('[data-testid="x-item-title"], .s-item__title')?.innerText?.trim() || '';
          const shop = 'ebay';

          cardsInfo.push({
            url: linkEl.href,
            html: card.outerHTML,
            title,
            shop
          });
        }
      });

      return cardsInfo;
    });
  } catch (e) {
    return [];
  }
};

const goToNextPage = async (page) => {
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
};

const searchAndScrape = async (page, searchQuery, searchedSet, suggestionsSet) => {
  const query = searchQuery.toLowerCase();
  let newSuggestions = [];

  if (searchedSet.has(query)) {
    console.log(`Already searched: "${query}", skipping`);
    return [];
  }

  console.log(`\nSearching: "${query}"`);
  logQuery(query);
  searchedSet.add(query);

  try {
    const isHomepage = page.url() === 'https://www.ebay.com/' || page.url() === 'https://www.ebay.com';
    if (!isHomepage) {
      await page.evaluate(() => window.scrollTo(0, 0));
      await delay(randomBetween(500, 1000));
    }

    let searchSelector = await findSearchbar(page);

    if (!searchSelector) {
      await page.goto('https://www.ebay.com', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await randomDelay();
      await varyViewport(page);
      searchSelector = await findSearchbar(page);
    }

    if (!searchSelector) {
      throw new Error('Searchbar not found');
    }

    newSuggestions = await humanType(page, searchSelector, query, true, suggestionsSet, searchedSet);

    await randomDelay();
    await simulateMistake(page);
    await page.keyboard.press('Enter');

    await page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 });
    await detectCaptcha(page);
    await simulateReading(page);
    await idlePause();

    const totalPages = randomBetween(2,11);
    let cardsScraped = 0;

    for (let pageNum = 0; pageNum < totalPages; pageNum++) {
      console.log(`\nPage ${pageNum + 1}/${totalPages}`);
      await delay(randomBetween(500, 1000));
      await humanScroll(page);
      await humanHover(page);
      await simulateMistake(page);

      let links = await getCardLinks(page);
      console.log(`Found ${links.length} cards with SVG on page ${pageNum + 1}`);

      console.log(`Saving all ${links.length} cards on this page`);

      for (let i = 0; i < links.length; i++) {
        const cardInfo = links[i];
        console.log(`  [${i + 1}/${links.length}] Saving card...`);
        saveCardData(cardInfo, query);
        cardsScraped++;
        await randomDelay();
      }

      if (pageNum < totalPages - 1) {
        if (links.length > 0 && Math.random() > 0.5) {
          const randomCard = links[randomBetween(0, links.length - 1)];
          console.log(`  Visiting product: ${randomCard.title.substring(0, 40)}...`);

          try {
            await page.goto(randomCard.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
            await delay(randomBetween(1000, 3000));
            await simulateReading(page);

            const productId = extractProductId(page.url());
            const urlPath = page.url().split('/itm/')[1] || '';
            let productName = urlPath.split('?')[0].replace(/[^a-zA-Z0-9-_]/g, '_').substring(0, 100);
            if (!productName || productName === productId) {
              productName = page.url().split('/itm/')[1]?.split('/')[1]?.replace(/[^a-zA-Z0-9-_]/g, '_').substring(0, 100) || 'product';
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
        }

        await humanScroll(page);
        await delay(randomBetween(500, 1000));

        const hasNext = await goToNextPage(page);
        if (!hasNext) {
          console.log('No more pages available');
          break;
        }
        await simulateReading(page);
      }
    }

    console.log(`Total cards scraped for "${query}": ${cardsScraped}`);

  } catch (err) {
    console.error('Error during search:', err.message);
  }

  return newSuggestions || [];
};

const main = async () => {
  initCSVFiles();

  const searchedSet = loadSearchedQueries();
  const suggestionsSet = loadSuggestions();

  console.log(`Loaded ${searchedSet.size} previously searched queries`);
  console.log(`Loaded ${suggestionsSet.size} previous suggestions`);

  const initialQueries = ['men', 'women', 'unisex'];
  initialQueries.forEach(q => searchedSet.delete(q));

  let allQueries = [...new Set([...initialQueries, ...suggestionsSet])];
  allQueries = shuffleArray(allQueries);

  const browser = await puppeteer.launch({
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1920, height: 1080 });
  await page.setUserAgent('Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36');

  let exiting = false;
  process.on('SIGINT', async () => {
    if (exiting) return;
    exiting = true;
    console.log('\n\n=== Stopping gracefully... ===');
    stopBufferFlushTimer();
    await browser.close();
    console.log('Done!');
    process.exit(0);
  });

  startBufferFlushTimer();

  try {
    if (!page.url().includes('ebay.com')) {
      await page.goto('https://www.ebay.com', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.evaluate(() => window.scrollTo(0, 0));
    }

    while (!exiting) {
      let currentIndex = 0;

    while (currentIndex < allQueries.length && !exiting) {
      const query = allQueries[currentIndex];
      const newSuggestions = await searchAndScrape(page, query, searchedSet, suggestionsSet);

      const remaining = allQueries.length - currentIndex - 1;
      console.log(`  Queries remaining in queue: ${remaining}`);

      currentIndex++;

      if (currentIndex < allQueries.length) {
        // Process pending suggestions from previous search before next query
        if (newSuggestions.length > 0) {
          console.log(`  Asking Ollama to filter ${newSuggestions.length} suggestions...`);
          const filtered = await selectSuggestions(query, newSuggestions);

          for (const s of filtered) {
            if (!searchedSet.has(s) && !allQueries.includes(s)) {
              allQueries.push(s);
              console.log(`  + Queued new search: "${s}"`);
            }
          }
        }

        const remaining = allQueries.length - currentIndex;
        console.log(`  Queries remaining in queue: ${remaining}`);

        await humanScroll(page);
        await delay(randomBetween(500, 1500));
      }
    }

      if (exiting) break;

      console.log(`\n\n=== Cycle complete, restarting with ${allQueries.length} queries ===`);
      allQueries = shuffleArray(allQueries);
    }

  } catch (err) {
    console.error('Fatal error:', err);
  } finally {
    stopBufferFlushTimer();
    if (!exiting) {
      await delay(2000);
      await browser.close();
    }
  }
};

main();
