import http from "k6/http";
import { SabaPlusApiUrl } from "../constant/api.js";
import { SabaPlusGenerator } from "../data-generator/index.js";

export default function(operatorCustId) {
  const request = SabaPlusGenerator.getPlaceBetAndSettleSBPlusRequest(operatorCustId);
  let res = http.post(SabaPlusApiUrl.PlaceBetAndSettle,
    JSON.stringify(request),
    {
      headers: { "Content-Type": "application/json" },
    }
  );

  return { request: request, response: JSON.parse(res.body)}
}
