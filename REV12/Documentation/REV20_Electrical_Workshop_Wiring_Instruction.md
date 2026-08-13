# REV20 AL1403 Electrical and Workshop Wiring Instruction

The authoritative construction allocation is `REV20_AL1403_Master_Port_Allocation.csv`.

- Label each IFM AL1403 master `AL100` through `AL104`.
- Label every field cable with its destination in the form `Device ID -> ALxxx/Px`.
- Preserve the port order and Device IDs exactly; do not substitute another port without revising the PLC mapping, HMI diagnostics, electrical drawing, and master allocation together.
- Do not move the machine A/B/Z motion encoder to an AL1403. Its deterministic Siemens counting/synchronism architecture remains unchanged.
- No PROFINET device name, IP address, process-image byte/bit, or channel address is approved by REV20. Those values remain a commissioning hold point.
- Before wiring AL104 customer-interface outputs, confirm PNP versus dry/potential-free requirements. Use interposing relays or isolation where required by the customer's interface specification.

## Drawing references

Electrical drawings shall show every `ALxxx/Px` allocation, Device ID, cable label, signal direction, and the unresolved physical-address hold point. `SPARE` means no device is to be installed. `RESERVED / NOT INSTALLED` means preserve the port for the named future function.
