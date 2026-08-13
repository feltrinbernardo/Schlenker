# REV12.2 Acceptance Test Matrix

Use PLCSIM and a validated signal table before any hardware download.

| ID | Area | Test | Expected result | Offline status |
|---|---|---|---|---|
| P01 | Production | Select Production ON from stopped Ready | State 30; Production active only | Logic implemented; PLCSIM pending |
| P02 | Product valves | Run Production | 210/217/212 open; 213/247 closed | Logic implemented; physical map pending |
| P03 | Pump | Remove product presence | Pump cannot start; low tank inhibits start | Logic implemented; PLCSIM pending |
| P04 | Vacuum | Start with vacuum below -400 mbar | 5.5 s inhibit then stop/alarm | Logic implemented; PLCSIM pending |
| P05 | Vacuum | Reset first failure and retry | One new timed attempt permitted | Logic implemented; PLCSIM pending |
| P06 | Vacuum | Fail second attempt | Alarm remains until vacuum corrected/reset | Logic implemented; PLCSIM pending |
| P07 | Wash | RUN/count >0 then stop/count 0 | Valve closes after 2.5 s | Logic implemented; I/O pending |
| J01 | JOG | Jog with count 0 / count >0 | Wash closed / open while JOG held | Logic implemented; PLCSIM pending |
| G01 | Gate | Gate OFF | Gate forced closed; no admission | Logic implemented; PLCSIM pending |
| G02 | Gate | Gate ON Production | Reduce/prove speed, admit, close, restore | Logic implemented; handshake pending |
| G03 | Gate | Inject critical alarm in each step | Close immediately, abort, stop | Logic implemented; PLCSIM pending |
| R01 | Run Out | Enter from Production | State 95; gate open; low level bypassed | Logic implemented; PLCSIM pending |
| R02 | Run Out | Product presence clears | Product pump stops | Logic implemented; PLCSIM pending |
| R03 | Run Out | Tank reaches 3% | Gate closes | Logic implemented; PLCSIM pending |
| R04 | Run Out | Count reaches 0 | 20 s final timer then filler stops/completes | Logic implemented; PLCSIM pending |
| C01 | CIP interlock | Request CIP during Production | Request blocked; alarm 2101 | Logic implemented; PLCSIM pending |
| C02 | CIP gate | Run CIP and issue gate commands | Gate remains forced closed | Logic implemented; PLCSIM pending |
| C03 | CIP valves | Run CIP | 210/247 open; 217/213 follow path; 212 sequences | Logic implemented; physical map pending |
| C04 | CIP media | Lose presence briefly / continuously | Pump holds 3.5 s / stops and alarms 2102 | Logic implemented; PLCSIM pending |
| C05 | CIP vacuum | Tank full | 4 s, pump, 1 s, 212 open 7 s, post-run 10 s | Logic implemented; PLCSIM pending |
| C06 | CIP display | Change cycle/step/medium | Elapsed timer resets and runs | Final rebuild repeat required |
| H01 | HMI | Rebuild software | Zero errors and warnings | Passed: 0 / 0 |
| H02 | HMI | Audit navigation/commands/fields | All visible objects valid | Substantive checks passed; clean rerun pending |
| S01 | PLC | Rebuild software | Zero errors and warnings | Passed before final timer refinement; repeat pending |

