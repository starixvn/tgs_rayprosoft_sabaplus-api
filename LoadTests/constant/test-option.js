const Scenarios = {
  DEV: {
    per_vus_iterations_test: {
      executor: "per-vu-iterations",
      vus: 3, // 2 virtual user
      iterations: 2, // The user will send 2 requests
    }
  },
  LOCAL: {
    constant_rate_test: {
      executor: "constant-arrival-rate",
      rate: 90,
      timeUnit: "1m",
      duration: "5m",
      preAllocatedVUs: 10,
      maxVUs: 20,
      env: { MY_CROC_ID: '1' },
      tags: {
        test_type: "api_load_test",
      },
    },
    ramping_rate_test: {
      executor: "ramping-arrival-rate",
      startRate: 50,
      timeUnit: "1s",
      preAllocatedVUs: 50,
      maxVUs: 100,
      stages: [
        { target: 100, duration: "1m" },
        { target: 150, duration: "2m" },
        { target: 200, duration: "30s" },
        { target: 200, duration: "3m30s" },
        { target: 0, duration: "30s" },
      ],
    }
  }
}

export const useOptions = (execName, options = {}) => {
  const scenarios = useScenarios(execName);
  const thresholdsResult = {}; 

  const { thresholds } = options

  if (thresholds) {
    Object.keys(scenarios).forEach(scenario => {
      Object.keys(thresholds).forEach(threshold => {
        thresholdsResult[`${threshold}{scenario:${scenario}}`] = thresholds[threshold]
      })
    })
  } else { // default threshold
    Object.keys(scenarios).forEach(scenario => {
      thresholdsResult[`http_req_duration{scenario:${scenario}}`] = ["p(85)<250", "p(90)<350"]
      thresholdsResult[`http_req_failed{scenario:${scenario}}`] = ["rate<0.01"]
    })
  }

  return {
    scenarios: scenarios,
    thresholds: thresholdsResult,
  };
}

export const useScenarios = (execName) => {
  const scenarios = Scenarios[__ENV.RUN_MODE] ?? Scenarios.DEV;
  const result = {...scenarios}

  Object.keys(result).forEach((testType) => {
    const newTestType = `${execName}-${testType}`;
    
    // Rename the test type (copying the original content to the new key and deleting the old key)
    result[newTestType] = { ...result[testType] };
    result[newTestType]["exec"] = execName;

    delete result[testType];
  });

  return result;
}



