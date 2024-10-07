import { sleep, check } from "k6";
import { useOptions } from "../constant/test-option.js";
import { GetRandomOperatorCustId } from "../data-generator/users.js";
import placebet from "../api/placebet.js";
import endgame from "../api/endgame.js";
import voidgame from "../api/voidgame.js";

export const PlaceBetEndGameOption = useOptions("placebetEndgameTest");

export function placebetEndgameTest() {
  const custId = GetRandomOperatorCustId();
  const placeBetResult = placebet(custId)

  check(placeBetResult.response, {
    "Placebet-Endgame/placebet content status is 0": (r) => r.ErrorCode === 0,
  });

  sleep(5);

  const endgameResult = endgame(placeBetResult.request)

  check(endgameResult.response, {
    "Placebet-Endgame/endgame status is 0": (r) => r.ErrorCode === 0,
  });

  sleep(10);
  
  const voidGameResult = voidgame(placeBetResult.request)

  check(voidGameResult.response, {
    "Placebet-Endgame/voidgame error code is 0": (r) => r.ErrorCode === 0,
  });
}
