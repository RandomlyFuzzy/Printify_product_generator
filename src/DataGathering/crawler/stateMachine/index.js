import { States }   from './states.js';
import { runState } from './actions/index.js';

/**
 * Run the state machine for a single search query.
 *
 * Starts at START_QUERY and drives states until QUERY_DONE is reached.
 * Any unexpected error inside a state action is caught here so the outer
 * query loop can continue rather than crashing entirely.
 *
 * @param {object} ctx  Machine context — see actions.js for the full shape.
 * @returns {Promise<string[]>}  New autocomplete suggestions discovered.
 */
export async function runQueryMachine(ctx) {
  let state = States.START_QUERY;

  while (state !== States.QUERY_DONE) {
    try {
      const nextState = await runState(state, ctx);
      console.log(`  [sm] ${state} → ${nextState}`);
      state = nextState;
    } catch (err) {
      console.error(`[StateMachine] Error in state "${state}": ${err.message}`);
      state = States.QUERY_DONE;
    }
  }

  console.log(`Total cards scraped for "${ctx.query}": ${ctx.cardsScraped ?? 0}`);
  return ctx.newSuggestions ?? [];
}
