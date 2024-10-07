import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { getBalanceTest, GetBalanceOption } from "./test-scenarios/get-balance.js";
import {
  placebetEndgameTest,
  PlaceBetEndGameOption,
} from "./test-scenarios/placebet-endgame.js";
import {
  placebetSettleEndgameTest,
  PlaceBetSettleEndGameOption,
} from "./test-scenarios/placebetsettle-endgame.js";
import {
  placeFreeSpinEndgameTest,
  PlaceFreeSpinEndgameOption,
} from "./test-scenarios/placefreespin-endgame.js";

export const options = {
  scenarios: {
    ...GetBalanceOption.scenarios,
    ...PlaceBetEndGameOption.scenarios,
    ...PlaceBetSettleEndGameOption.scenarios,
    ...PlaceFreeSpinEndgameOption.scenarios,
  },
  thresholds: {
    ...GetBalanceOption.thresholds,
    ...PlaceBetEndGameOption.thresholds,
    ...PlaceBetSettleEndGameOption.thresholds,
    ...PlaceFreeSpinEndgameOption.thresholds,
  },
};

export function handleSummary(data) {
  return {
    [`output/summary.html`]: htmlReport(data),
  };
}

export {
  getBalanceTest,
  placebetEndgameTest,
  placebetSettleEndgameTest,
  placeFreeSpinEndgameTest,
};
