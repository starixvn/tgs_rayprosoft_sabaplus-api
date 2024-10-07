import { sleep, check } from "k6";
import { useOptions } from "../constant/test-option.js";
import { GetRandomOperatorCustId } from "../data-generator/users.js";
import voidgame from "../api/voidgame.js";
import placeFreespin from "../api/place-freespin.js";
import endgame from "../api/endgame.js";

export const PlaceFreeSpinEndgameOption = useOptions("placeFreeSpinEndgameTest");

export function placeFreeSpinEndgameTest() {
  const custId = GetRandomOperatorCustId();
  const placeBetResult = placeFreespin(custId)

  check(placeBetResult.response, {
    "PlaceFreeSpin-Endgame/placefreespin error code is 0": (r) => r.ErrorCode === 0,
  });

  sleep(5);

  const endgameResult = endgame(placeBetResult.request)

  check(endgameResult.response, {
    "PlaceFreeSpin-Endgame/endgame error code is 0": (r) => r.ErrorCode === 0,
  });

  sleep(10);
  
  const voidGameResult = voidgame(placeBetResult.request)

  check(voidGameResult.response, {
    "PlaceFreeSpin-Endgame/voidgame error code is 0": (r) => r.ErrorCode === 0,
  });
}
