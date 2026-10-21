# ERD Logic Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure only an authenticated Admin can validate a Jukir deposit, preserving the `deposits.admin_id → admins.id` relationship in the ERD.

**Architecture:** Retain the existing normalized tables and lifecycle constraints. Add the missing role authorization at the service boundary before beginning the deposit-validation transaction; cover it with the disposable PostgreSQL integration check.

**Tech Stack:** .NET 10, C#, Npgsql, PostgreSQL 16.

**Spec:** `Parking_System.drawio` page `3 - ERD`.

## Global Constraints

- Preserve the existing schema version and the explicit bootstrap compatibility contract.
- A pending deposit has no `admin_id`; a validated deposit references the validating Admin.
- Tests use the isolated PostgreSQL fixture from `Check-Business.ps1`.

## Review Focus

- A Jukir must not turn any pending deposit into a validated deposit.
- An authenticated Admin still validates a pending deposit and becomes its `admin_id`.
- Rejected validation must leave the deposit pending.

---

### Task 1: Enforce deposit-validator role

**Files:**
- Modify: `ParkingSystem/ParkingSystem.Checks/TransactionChecks.cs`
- Modify: `ParkingSystem/ParkingSystem/Services/ParkingService.Deposits.cs`

**Interfaces:**
- Consumes: `ParkingService.ValidateDepositAsync(UserSession, int, string?, CancellationToken)`.
- Produces: Admin-only validation for `deposits.admin_id`.

- [ ] Add an integration assertion that a Jukir session cannot validate a pending deposit.
- [ ] Run `Check-Business.ps1`; observe the test fail because validation currently succeeds.
- [ ] Add `AuthorizeAsync(session, UserRole.Admin, ct)` at the beginning of `ValidateDepositAsync`.
- [ ] Run `Check-Business.ps1`; observe all checks pass.
- [ ] Update ERD labels to represent the existing valid schema: `Admins`, nullable `adminId` until validation, correct `amount*` spelling, `numeric(12,2)`, and `motorcycle`.
