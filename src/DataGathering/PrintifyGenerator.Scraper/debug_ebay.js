// Debug script to inspect eBay listing structure
// Run this in browser console on eBay search results page

const cards = document.querySelectorAll('.s-card, li.s-item, [class*="item"]');
console.log('Found cards:', cards.length);

if (cards.length > 0) {
    const card = cards[0];
    console.log('Card HTML structure:', card.outerHTML.substring(0, 2000));
    
    // Try to find title
    const titleEl = card.querySelector('[class*="title"], h3, span[role="heading"]');
    console.log('Title element:', titleEl?.outerHTML);
    
    // Try to find price
    const priceEl = card.querySelector('[class*="price"]');
    console.log('Price element:', priceEl?.outerHTML);
    
    // Try to find link with item ID
    const link = card.querySelector('a[href*="itm"]');
    console.log('Link:', link?.href);
    
    // Try to find image
    const img = card.querySelector('img');
    console.log('Image:', img?.src);
    
    // Try to find shipping
    const shipping = card.querySelector('[class*="shipping"]');
    console.log('Shipping:', shipping?.textContent);
    
    // All classes on the card
    console.log('Card classes:', card.className);
}
