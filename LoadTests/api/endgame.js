import http from "k6/http";
import { SabaPlusApiUrl } from "../constant/api.js";
import { SabaPlusGenerator } from "../data-generator/index.js";

export default function(placebetRequest) {
  const request = SabaPlusGenerator.getEndGameSBPlusRequest(placebetRequest);
  let res = http.post(SabaPlusApiUrl.EndGame,
    JSON.stringify(request),
    {
      headers: { "Content-Type": "application/json" },
    }
  );

  return { request: request, response: JSON.parse(res.body)}
}
