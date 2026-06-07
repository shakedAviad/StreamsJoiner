# Stream Joiner — Coding Exercise

> **Interviewer:** Share this document with the candidate at the start of the session.

## Overview

You are building a **real-time stream joiner** for a call-center platform. Three independent event streams flow through Redis, each carrying a different aspect of a phone call's lifecycle. Your task is to **consume these streams, correlate events by `callId`, and emit unified call lifecycle events** to an output stream.

```
┌──────────────────┐
│  agent-events    │──┐
└──────────────────┘  │
┌──────────────────┐  │    ┌──────────────────────┐    ┌───────────────────────┐
│ customer-events  │──┼───>│  YOUR IMPLEMENTATION  │───>│  joined-call-events   │
└──────────────────┘  │    │                       │    │  (output stream)      │
┌──────────────────┐  │    └──────────────────────┘    └───────────────────────┘
│ business-data-   │──┘
│ events           │
└──────────────────┘
```

---

## Input Streams

All events share a common `callId` field that serves as the **join key**.

### `agent-events`

Events emitted when agents join or leave a call.

| Field       | Type   | Example              | Description            |
|-------------|--------|----------------------|------------------------|
| `callId`    | String | `call-a1b2c3d4`      | Unique call identifier |
| `eventType` | String | `AGENT_JOINED`       | `AGENT_JOINED` or `AGENT_LEFT` |
| `agentId`   | String | `agent-1`            | Agent identifier       |
| `agentName` | String | `Alice Johnson`      | Agent display name     |
| `timestamp` | String | `2026-03-10T14:00:05Z` | ISO-8601 timestamp   |

### `customer-events`

Events emitted when customers call in or hang up.

| Field        | Type   | Example              | Description            |
|--------------|--------|----------------------|------------------------|
| `callId`     | String | `call-a1b2c3d4`      | Unique call identifier |
| `eventType`  | String | `CUSTOMER_JOINED`    | `CUSTOMER_JOINED` or `CUSTOMER_LEFT` |
| `customerId` | String | `cust-101`           | Customer identifier    |
| `phoneNumber`| String | `+1-555-0101`        | Originating phone number |
| `timestamp`  | String | `2026-03-10T14:00:00Z` | ISO-8601 timestamp   |

### `business-data-events`

Business context updates related to a call. Each event carries an arbitrary set of key-value pairs alongside the `callId` and `timestamp`. Any field that is not `callId` or `timestamp` is treated as a business data key-value pair.

| Field       | Type   | Example              | Description            |
|-------------|--------|----------------------|------------------------|
| `callId`    | String | `call-a1b2c3d4`      | Unique call identifier |
| `timestamp` | String | `2026-03-10T14:00:01Z` | ISO-8601 timestamp   |
| *(any key)* | String | *(varies)*           | Arbitrary business data |

**Example events:**

```
callId=call-a1b2c3d4  skillName=billing-support  priority=HIGH         timestamp=...
callId=call-a1b2c3d4  language=en                queueId=queue-3       timestamp=...
callId=call-a1b2c3d4  sentiment=negative         callReason=cancellation  timestamp=...
```

Possible keys include (but are not limited to): `skillName`, `priority`, `language`, `queueId`, `sentiment`, `callReason`.

---

## Output Stream

Your joiner must write events to the `joined-call-events` stream.

| Field          | Type   | Description                                      |
|----------------|--------|--------------------------------------------------|
| `callId`       | String | The call identifier                              |
| `status`       | String | `STARTED`, `CONNECTED`, `DISCONNECTED`, or `ENDED` |
| `agentId`      | String | Current/last agent on the call (if known)        |
| `agentName`    | String | Current/last agent name (if known)               |
| `customerId`   | String | Customer identifier (if known)                   |
| `phoneNumber`  | String | Customer phone number (if known)                 |
| `data.*`       | String | All accumulated business data key-value pairs, prefixed with `data.` |
| `timestamp`    | String | Timestamp of the state change                    |

---

## Functional Requirements

### Rules

1. **STARTED** — Emit when the **first participant** (agent or customer) joins a call. This is the first event for any given `callId`. A call starts regardless of whether the first participant is an agent or a customer.

2. **CONNECTED** — Emit when at least **1 agent AND at least 1 customer** are present on the call.
   > **Important:** Two agents joining without any customer does **NOT** constitute a connected call.

3. **DISCONNECTED** — Emit when a call that was previously **CONNECTED** loses all participants of one type (e.g., the agent leaves but the customer is still on the line, or vice versa).

4. **ENDED** — Emit when **no participants** remain on the call (zero agents AND zero customers).

5. **Business Data Enrichment** — When business data events arrive for a `callId`, store all their key-value pairs and include them in any subsequent output events for that call. Multiple business data events may arrive over the lifetime of a call — accumulate all key-value pairs (later values for the same key overwrite earlier ones). Business data events do **not** trigger state transitions on their own.

6. **Agent Transfers** — A call may have multiple agents over its lifetime. When Agent A leaves and Agent B joins, this is a legitimate transfer scenario.

---

## Your Task

You are responsible for implementing **everything** between reading events from Redis and writing joined events back to Redis. This includes:

- **Redis Stream consumption** — Configure how your application reads from the three input streams
- **Event parsing** — Convert raw stream records into usable objects
- **State management & join logic** — Track call state and determine lifecycle transitions
- **Output publishing** — Write joined events to the `joined-call-events` output stream

### Provided For You

| Component | Purpose |
|-----------|---------|
| `StreamsJoinerApplication.java` | Spring Boot entry point |
| `CallJoinerService.java` | Empty starting point — implement your solution here or restructure as you see fit |
| Model POJOs | `AgentEvent`, `CustomerEvent`, `BusinessDataEvent`, `CallEvent` — data classes with the field definitions |
| `application.yml` | Redis connection configuration |
| `pom.xml` | Dependencies: `spring-boot-starter-data-redis`, `spring-boot-starter-web` |
| Docker Compose | Redis + RedisInsight + event generator + app |

You may create new classes, packages, and configuration files. You may restructure `CallJoinerService` however you like.

---

## Event Scenarios

The generator produces a mix of scenarios including:

- Normal calls (customer and agent join, then leave)
- Out-of-order arrivals (agent event before customer event)
- Agent transfers (agent A leaves, agent B joins same call)
- Calls with two agents but no customer
- Customers who hang up before an agent joins
- Late-arriving business data
