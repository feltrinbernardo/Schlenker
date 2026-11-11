# TIA V19 discovery verification — 2026-11-10

Operator confirmed this run is on the isolated workshop test network with no production route. Read-only Accessible devices discovery used PN/IE and Intel(R) I210 Gigabit Network Connection through the official Computer Use wrapper.

Result: Scan and information retrieval completed; 12 devices found.

| Observed name | Observed type | IPv4 |
| --- | --- | --- |
| plc_1.profinet interface_1 | CPU 1512C-1 PN | 192.168.10.1 |
| hmi_1.profinet interface_1 | MTP1200 Unified (remaining text truncated) | 192.168.10.2 |
| anybus-smc | Anybus Communicator (text truncated) | 192.168.10.20 |
| mes-profinet | PNOZ m ES Profinet (text truncated) | 192.168.10.21 |
| al100 | AL1403 | 192.168.10.30 |
| al101 | AL1403 | 192.168.10.31 |
| al102 | AL1403 | 192.168.10.32 |
| al103 | AL1403 | 192.168.10.33 |
| vsd-main-machine | G120C PN V4.7.13 | 192.168.10.40 |
| vsd-bottle-conveyor | G120C PN V4.7.13 | 192.168.10.41 |
| vsd-product-pump | G120C PN V4.7.13 | 192.168.10.42 |
| vsd-cap-distributor | G120C PN V4.7 | 192.168.10.43 |

No repeated IPv4 address appears among these 12 rows; this does not exclude undiscovered conflicting devices. AL104 was not discovered. Existing docs/PLC_IO_LINK_LIST.md describes five logical masters including required Customer CIP AL104, but this historical description does not establish today's approved physical scope. Absence does not prove failure: confirm the intended CIP architecture before deciding whether a fifth master is missing.

The physical HMI reports MTP1200 Unified; do not substitute a model from older documentation. The exact configured project IPs, subnet masks, port/process-data mapping, cyclic IO health and HMI tag communication were not verified. Network discovery proves reachability for discovery, not full application communication or FAT success.

## Computer Use result and boundaries

The adopted helper successfully captured TIA main/modal windows and accepted screenshot-coordinate clicks for discovery. Transient stale-cache and external-focus errors required refreshed observations; the repair does not eliminate every GUI error. No device address assignment, download, run/stop command, project edit or save was performed by this verification.

A disposable project in runs/2026-11-10-manual-valves-hmi-compile/latest-10-11-copy appeared open during the session. Its .ap19 hash read failed because the file was locked; source-hash invariance is therefore not asserted. No compile/logic-test verdict is issued.

Images were displayed by the official wrapper in the chat. No standalone evidence PNG was persisted; this report is a transcription of those observations, not a replacement for an exported screenshot. Local run record: runs/2026-11-10-tia-network-verification/report.md.
