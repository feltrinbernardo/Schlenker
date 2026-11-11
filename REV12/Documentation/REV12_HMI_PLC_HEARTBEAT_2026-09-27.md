# PLC/HMI communication heartbeat

## Implemented behaviour

- The Unified HMI increments `HMI_Heartbeat` every 500 ms from the common footer of the active screen.
- The PLC compares the received counter with its previous value.
- `DB_Global.Inp.HMILinkOK` becomes true only after at least one counter transition.
- If the counter does not change for 3 seconds, `HMILinkOK` becomes false.
- On every screen that uses the common footer (27 screens), the text remains white and a separate status lamp is green only while `Network_HMI_OK` has good quality and is true. During startup or after communication loss the lamp is amber.
- The lamp is a dedicated ellipse without a contrasting border; the live text no longer contains a coloured bullet.
- The single lamp and text are positioned inside the dark footer at `X/Y 859/766` and `875/754` respectively.
- The text object is recreated after the footer frame so its z-order is foreground on every common-footer screen; its single Text dynamization is restored unchanged and it has no events.
- The verified legacy connection/PLC lamp and text objects are absent, preventing duplicated PLC/HMI indications.

## Scope and safety

This heartbeat is a diagnostic indication only. It is not a safety signal, a machine permissive, or proof that PLC/HMI data are correct. It does not command valves, drives, pumps, or other outputs.

## Offline verification completed

- PLC source import and compilation: 0 errors, 0 warnings.
- Unified HMI update: 27 common footers updated.
- Unified HMI compilation: 0 errors, 0 warnings.
- HMI tag count after update: 464; used tags: 339.
- Final all-screen footer refinement: 27 screens updated; 78 verified legacy objects removed; final read-only audit found 0 legacy matches, 27 correctly positioned lamps and 27 correctly positioned text objects; 0 errors and 0 warnings after compilation.
- Runtime-photo follow-up: the initially reused text object was hidden behind the footer background. The final correction verified the text as the topmost item and the lamp immediately below it on all 27 screens; compilation again completed with 0 errors and 0 warnings.

## Physical acceptance still required

After the updated PLC and HMI software are downloaded to the approved bench targets:

1. Confirm the footer changes from amber to green within 3 seconds of Runtime communication becoming active.
2. Disconnect or stop HMI communication without commanding any output.
3. Confirm the footer returns to amber within 3 seconds.
4. Restore communication and confirm automatic recovery to green.

These physical checks remain pending until recorded on the isolated test bench.
