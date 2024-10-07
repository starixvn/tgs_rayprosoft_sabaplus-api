import { sleep } from "k6";

import { GetRandomSapaSportUserId } from "./data-generator/users.js";
import confirmbet from "./api/confirmbet.js";
import settle from "./api/settle.js";
import placebet from "./api/placebet.js";
import getBalance from "./api/get-balance.js";

export let options = {
  vus: 1, // 1 virtual user
  iterations: 5, // Run 1 iterations
  thresholds: {
    http_req_failed: [{ threshold: "rate<0.01", abortOnFail: true }],
    http_req_duration: ["p(95)<250", "p(99)<350"],
  },
};

export default function () {
  const userId = GetRandomSapaSportUserId();

  const getBalanceTest = getBalance(userId);
  sleep(1);

  const placebetTest = placebet(userId);
  sleep(0.5);

  const confirmBetTest = confirmbet(userId, placebetTest.response);
  sleep(3);

  const settledTest = settle(userId, confirmBetTest.request);
}
