import { States } from '../states.js';

import { stateStartQuery }    from './START_QUERY.js';
import { stateEnsureHome }    from './ENSURE_HOME.js';
import { stateScrollToTop }   from './SCROLL_TO_TOP.js';
import { stateTypeSearch }    from './TYPE_SEARCH.js';
import { stateBrowsePage }    from './BROWSE_PAGE.js';
import { stateClickOnPage }   from './CLICK_ON_PAGE.js';
import { stateHoverCard }     from './HOVER_CARD.js';
import { stateSortChange }    from './SORT_CHANGE.js';
import { stateFilterClick }   from './FILTER_CLICK.js';
import { stateScrollExplore } from './SCROLL_EXPLORE.js';
import { stateScrollToBottom } from './SCROLL_TO_BOTTOM.js';
import { stateVisitProduct }  from './VISIT_PRODUCT.js';
import { stateNextPage }      from './NEXT_PAGE.js';
import { stateIdleWander }    from './IDLE_WANDER.js';
import { statePause }         from './PAUSE.js';

/**
 * Dispatch to the correct action function for the given state.
 * Returns the next state.
 * @param {string} state
 * @param {object} ctx
 * @returns {Promise<string>}
 */
export async function runState(state, ctx) {
  switch (state) {
    case States.START_QUERY:      return stateStartQuery(ctx);
    case States.ENSURE_HOME:      return stateEnsureHome(ctx);
    case States.SCROLL_TO_TOP:    return stateScrollToTop(ctx);
    case States.TYPE_SEARCH:      return stateTypeSearch(ctx);
    case States.BROWSE_PAGE:      return stateBrowsePage(ctx);
    case States.CLICK_ON_PAGE:    return stateClickOnPage(ctx);
    case States.HOVER_CARD:       return stateHoverCard(ctx);
    case States.SORT_CHANGE:      return stateSortChange(ctx);
    case States.FILTER_CLICK:     return stateFilterClick(ctx);
    case States.SCROLL_EXPLORE:   return stateScrollExplore(ctx);
    case States.SCROLL_TO_BOTTOM: return stateScrollToBottom(ctx);
    case States.VISIT_PRODUCT:    return stateVisitProduct(ctx);
    case States.NEXT_PAGE:        return stateNextPage(ctx);
    case States.IDLE_WANDER:      return stateIdleWander(ctx);
    case States.PAUSE:            return statePause(ctx);
    case States.QUERY_DONE:       return States.QUERY_DONE;
    default:
      console.error(`[StateMachine] Unknown state: ${state}`);
      return States.QUERY_DONE;
  }
}
