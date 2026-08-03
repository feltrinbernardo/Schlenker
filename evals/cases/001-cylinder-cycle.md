# Eval 001 — FX5U Cylinder Cycle

## Input

Use profile `demo-fx5u` and an offline sample project.

Implement an automatic cycle with these states:

1. `Idle`: both valve outputs are off; wait for the start pushbutton.
2. `Extend`: energise the extend valve; wait for the extended sensor.
3. `ExtendedDwell`: hold the cylinder extended for 500 ms.
4. `Retract`: energise the retract valve; wait for the retracted sensor.
5. `Complete`: turn outputs off and return to `Idle`.

Stop must return the sequence to `Idle`. Extension or retraction that does not
receive its feedback within 3 seconds must set a fault and remove both outputs.

## Pass criteria

- A named, readable state-machine or equivalent sequence block exists.
- Every state and transition has a meaningful comment.
- Output commands are mutually exclusive.
- Both movement timeout paths latch/report a fault.
- The project builds without compile errors.
- The final run report identifies the program block and build result.
