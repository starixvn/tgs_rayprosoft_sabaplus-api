import { sleep, check } from "k6";
import { useOptions } from "../constant/test-option.js";
import { GetRandomOperatorCustId } from "../data-generator/users.js";
import voidgame from "../api/voidgame.js";
import placebetAndSettle from "../api/placebet-and-settle.js";

export const PlaceBetSettleEndGameOption = useOptions("placebetSettleEndgameTest");

export function placebetSettleEndgameTest() {
  const custId = GetRandomOperatorCustId();
  const placeBetResult = placebetAndSettle(custId)

  check(placeBetResult.response, {
    "PlacebetAndSettle/placebet error code is 0": (r) => r.ErrorCode === 0,
  });

  sleep(10);
  
  const voidGameResult = voidgame(placeBetResult.request)

  check(voidGameResult.response, {
    "PlacebetAndSettle/voidgame error code is 0": (r) => r.ErrorCode === 0,
  });
}
