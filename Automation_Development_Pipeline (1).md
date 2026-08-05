# Automation Development Pipeline

## Project

**Schlenker Monoblock Refurbishment -- Siemens TIA Portal V19**

## Phase 1 -- Requirements

### Objective

Define all machine requirements before programming.

### Inputs

-   Mechanical drawings
-   Pneumatic drawings
-   Electrical schematics
-   I/O list
-   Safety requirements
-   Customer specifications
-   FAT requirements

### Deliverables

-   Functional Specification
-   Hardware Architecture
-   Software Architecture

## Phase 2 -- Hardware Configuration

### PLC

-   Siemens S7-1500 CPU 1512C-1 PN

### HMI

-   Siemens MTP1500 Unified Comfort

### Network

-   PROFINET

### Devices

-   SMC EX260 Valve Island
-   G120C Inverter
-   Pilz Safety Controller
-   Remote I/O
-   Sensors
-   Pneumatic valves
-   Product pump
-   Vacuum pump

Deliverable: - Complete TIA Hardware Configuration

## Phase 3 -- PLC Architecture

### Organization Blocks

-   OB1 Main Cycle
-   OB100 Startup
-   OB121 Programming Error
-   OB122 I/O Error

### Data Types (UDT)

-   Machine
-   Door Access
-   Commands
-   Status
-   Alarm
-   Recipe
-   Product
-   Valve
-   Pump
-   CIP
-   Diagnostics

### Global Data Blocks

-   DB_Global
-   DB_Settings
-   DB_Recipe
-   DB_Alarm
-   DB_HMI

### Function Blocks

-   Machine State Manager
-   Door Manager
-   Safety Manager
-   Valve Manager
-   Pump Manager
-   Vacuum Manager
-   Conveyor Manager
-   Accumulation Manager
-   Bottle Detection
-   Filler Control
-   Capper Control
-   CIP Manager
-   Alarm Manager
-   Diagnostics Manager
-   HMI Manager
-   Recipe Manager
-   Communication Manager

## Phase 4 -- Machine States

Idle • Ready • Automatic • Manual • Jog • CIP • Pause • Standby • Fault
• Emergency Stop • Recovery • Shutdown

## Phase 5 -- Safety

-   Emergency Stop
-   Guard Doors
-   Pressure Monitoring
-   Filter Monitoring
-   Safety Reset
-   Restart Sequence
-   Safety Permission Logic
-   Safe Outputs

## Phase 6 -- Pneumatic Control

-   SMC EX260
-   Valve Enable
-   Manual Override
-   Automatic Override
-   Safety Interlock
-   Output Validation
-   Valve Diagnostics
-   Timeout Detection

## Phase 7 -- Product Control

-   Pump Start/Stop
-   Minimum Speed
-   Maximum Speed
-   Level Control
-   Overflow Protection
-   Dry Run Protection

## Phase 8 -- Vacuum System

-   Vacuum Enable
-   Vacuum Monitor
-   Pressure Check
-   Timeout
-   Alarm Handling

## Phase 9 -- Conveyor

-   Bottle Detection
-   Infeed
-   Transfer
-   Accumulation
-   Jam Detection
-   Outfeed

## Phase 10 -- Filler

-   Bottle Present
-   Fill Permission
-   Height Mode
-   Fill Sequence
-   Completion Check

## Phase 11 -- Capper

-   Bottle Ready
-   Cap Detection
-   Torque Monitoring
-   Cap Complete
-   Fault Detection

## Phase 12 -- CIP

-   Start
-   Step Sequence
-   Timer Management
-   Valve Control
-   Pump Control
-   Completion
-   Abort
-   Recovery

## Phase 13 -- Alarm Management

-   Warning
-   Fault
-   Emergency
-   Safety
-   Maintenance
-   Communication
-   Diagnostics
-   Alarm History
-   Acknowledgement

## Phase 14 -- HMI Screens

-   Home
-   Overview
-   Automatic
-   Manual
-   Jog
-   Recipe
-   Alarms
-   Diagnostics
-   Maintenance
-   Settings
-   Service
-   Production
-   CIP
-   Statistics

## Phase 15 -- Diagnostics

-   PLC
-   PROFINET
-   Valve Island
-   Drives
-   Safety
-   Sensors
-   Outputs
-   Communication

## Phase 16 -- Communication

-   PLC ↔ HMI
-   PLC ↔ SMC
-   PLC ↔ G120C
-   PLC ↔ Pilz
-   PLC ↔ Remote I/O

## Phase 17 -- Software Verification

-   Static Analysis
-   Code Review
-   TIA Compile
-   Warning Resolution
-   Cross References
-   Memory Usage
-   Performance Review

## Phase 18 -- Simulation

-   Offline Simulation
-   I/O Simulation
-   Manual Test
-   Automatic Test
-   Alarm Test
-   Safety Test
-   Recovery Test

## Phase 19 -- FAT

-   Functional Test
-   Safety Test
-   Cycle Test
-   Alarm Test
-   Performance Test
-   Documentation Review
-   Customer Approval

## Phase 20 -- Commissioning

-   Download PLC
-   Download HMI
-   Parameter Verification
-   I/O Check
-   Safety Validation
-   Production Trial
-   Optimization
-   Final Acceptance

# Deliverables

-   Complete TIA Portal V19 Project
-   PLC Software
-   HMI Software
-   Hardware Configuration
-   PROFINET Configuration
-   Alarm Database
-   Recipe Database
-   I/O Mapping
-   Electrical Documentation
-   Functional Documentation
-   FAT Documentation
-   Commissioning Report
-   REV Release Package
