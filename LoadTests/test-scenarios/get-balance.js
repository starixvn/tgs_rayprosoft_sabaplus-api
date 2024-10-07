import { sleep, check } from "k6";
import { useOptions } from "../constant/test-option.js";
import { GetRandomOperatorCustId } from "../data-generator/users.js";
import getbalance from "../api/getbalance.js";

export const GetBalanceOption = useOptions("getBalanceTest");

export function getBalanceTest() {
  const operatorCustId = GetRandomOperatorCustId();
  const balanceResult = getbalance(operatorCustId);

  check(balanceResult.response, {
    "GetBalance/getBalance content error code is 0": (r) => r.ErrorCode == 0,
  });

  sleep(0.5);
}
