# PersonalOS - Product, Scope, and Experience

**Version:** 1.3
**Status:** Milestone 3 daily operating system

## 1. Problem

Personal life is commonly managed through separate tools:

- calendar;
- notes;
- tasks;
- habits;
- calorie tracker;
- journal;
- reminders.

This fragmentation creates duplication, removes context, and makes it difficult to convert data into decisions.

## 2. Proposal

PersonalOS unifies:

```text
Capture -> Plan -> Execute -> Record -> Review -> Adjust
```

The product must be fast, calm, and explainable. It must not become a personal ERP, a punitive system, or a collection of charts without action.

## 3. Initial user

Jefferson:

- studies and works;
- develops software;
- wants to organize priorities;
- tracks habits;
- needs to manage calories;
- wants to record lessons learned;
- wants to improve every week;
- uses the project to gain practical Angular experience without abandoning .NET;
- needs to explain the architecture and security decisions during interviews.

## 4. Product principles

1. Action before decoration.
2. Fast capture.
3. Three priorities before twenty tasks.
4. Recovery before guilt.
5. Honest historical data.
6. Explainable recommendations.
7. Privacy by default.
8. Accessibility.
9. User control.
10. Incremental growth.

## 5. Modules

### Identity

- registration;
- login;
- logout;
- current user;
- preferences;
- time zone.

Email changes stay unavailable until confirmation and account-recovery flows exist. An editable
sign-in address without them is a way to lock a user out of their own account.

### Today

- local date decided by the server;
- a daily timeline of timed items, with untimed items grouped separately;
- routines that apply to the day, with their execution state;
- meals and the calorie total against the target;
- study recorded for the day;
- whether the reflection has been written;
- quick capture for a task, a meal, or a study session;
- a summary built only from counts the user produced.

Delivered in Milestone 3. There is deliberately no streak, score, or trend: none of them could be
derived honestly from a single day, and an invented number in a personal record is worse than no
number at all. Recommendations are deferred until real history exists.

### Planning

- tasks;
- events;
- dates;
- priorities;
- rescheduling;
- a calendar with a month view, a navigable daily agenda, a next-seven-days section, and a day
  planner with a 15-minute timeline.

Delivered in Milestone 3. Projects and goals are not implemented yet.

### Routines

- reusable ordered steps;
- recurrence: daily, weekly, chosen weekdays, monthly;
- execution recorded per day;
- workout steps with sets, repetitions, and weight;
- target beside actual;
- partial progress and completion;
- active and inactive state.

Delivered in Milestone 3. A habit is modelled as a routine with one step, so there is one
recurrence engine rather than two.

### Nutrition

- one daily calorie target chosen by the user;
- optional macronutrient targets;
- meals grouped by breakfast, lunch, dinner, snack, and other;
- free-text quantity;
- calories, with optional protein, carbohydrates, and fat;
- consumed against target for the day.

Delivered in Milestone 3. PersonalOS records the numbers the user enters and compares them. It
never proposes a target, never labels a value as healthy or unhealthy, and never gives advice.
Going over the target shows a negative remainder as a plain fact. There is no food database and no
external nutrition service. A versioned goal history is not implemented yet.

### Study

- subjects and learning projects;
- scheduled sessions with a duration;
- what was studied and where the user now stands;
- material as a title, a type, and an optional link;
- a Monday-to-Sunday week view with per-day and weekly totals.

Delivered in Milestone 3. Material is metadata only: a link must be `http` or `https`, the server
never fetches it, and no file is uploaded.

### Journal

- one entry per local day;
- what went well;
- what went poorly;
- why it happened;
- what was learned;
- what to adjust tomorrow;
- free-form notes.

Delivered in Milestone 3. Every section is optional, so a single sentence is a complete entry.
This is the most sensitive record in the product: it is never logged, never cached, never stored
in the browser, never placed in a URL, and never interpreted by PersonalOS.

### Settings

- profile summary and display name;
- read-only email until confirmation and recovery flows exist;
- saved IANA time zone;
- browser time-zone suggestion;
- Light, Dark, and System appearance preference.

Appearance is a non-sensitive browser preference. System follows the device, updates when the
operating-system theme changes, and applies before Angular renders so the page does not flash the
wrong theme.

### Weekly review

- plan versus execution;
- routines;
- nutrition;
- patterns;
- experiment for the next week.

Not implemented yet. It is deliberately deferred until enough real days exist for an aggregate to
mean something.

### Reminders

- date and time;
- recurrence;
- snooze;
- quiet hours;
- delivery state;
- channel.

## 6. Daily experience

### Start of day

- review the schedule;
- choose three priorities;
- view habits;
- review the calorie target;
- define an intention.

### During the day

- capture quickly;
- complete tasks;
- record habits;
- record meals;
- add notes.

### Daily close

- review outcomes;
- record what went well;
- record what was difficult;
- identify the cause;
- define an adjustment;
- prepare tomorrow.

### Weekly review

- compare;
- recognize patterns;
- choose one experiment;
- adjust goals.

## 7. User experience

Navigation delivered in Milestone 3:

```text
Today
Calendar
Routines
Nutrition
Study
Journal
Settings
```

A weekly Review destination is added once enough real history exists to aggregate.

Every flow must consider:

- loading;
- empty;
- success;
- validation;
- conflict;
- unauthorized;
- forbidden;
- rate limit;
- server error;
- offline, when applicable.

Experience requirements:

- keyboard navigation;
- explicit labels;
- visible focus;
- semantic HTML;
- no meaning communicated through color alone;
- mobile-first capture;
- desktop-friendly analysis;
- predictable navigation;
- no flash of private content while authentication is unresolved;
- no flash of the wrong appearance theme when a saved preference exists;
- clear distinction between unavailable features and empty data.

Angular implementation principles:

- standalone components;
- feature-oriented organization;
- lazy-loaded protected areas;
- typed reactive forms;
- signals for local and authentication state where appropriate;
- HttpClient for API communication;
- route guards for navigation only;
- server-side authorization for every protected operation;
- accessible loading and error feedback.

## 8. Gamification

Allowed:

- progress;
- consistency;
- recovery;
- levels;
- records;
- experiments.

Not allowed:

- humiliation;
- aggressive punishment;
- losing all progress because of one failure;
- rewarding dangerously low food intake;
- dark patterns;
- manipulative notifications;
- hiding privacy or account controls.

## 9. Privacy and trust

PersonalOS will process data that may reveal routines, health-related behavior, personal reflections, and future plans.

Product rules:

- collect only data needed by a defined feature;
- never expose authentication secrets to the Angular application;
- do not place sensitive information in URLs or query strings;
- do not persist current-user or private API data in browser storage by default;
- never show another user's data;
- do not use private content as analytics metadata;
- do not send sensitive notification content without user control;
- explain recommendations and require approval before changing user data;
- provide export and deletion paths before production maturity is claimed.

## 10. MVP

The complete MVP includes:

- secure account;
- Today;
- tasks;
- habits;
- nutrition;
- journal;
- review;
- trends;
- export;
- PWA.

The MVP is delivered incrementally. A navigation placeholder does not mean that a module is implemented.

## 11. Outside the MVP

- password manager or vault;
- medical advice;
- social network;
- marketplace;
- microservices;
- billing;
- multi-tenancy;
- native mobile application;
- AI that modifies data without approval.

A future vault is intentionally excluded because it requires a separate threat model, cryptographic design review, recovery strategy, and operational maturity.

## 12. Roadmap

### M1 - Secure walking skeleton

- ASP.NET Core Identity;
- server-managed authentication cookie;
- antiforgery;
- Angular authentication state;
- login;
- registration;
- current user;
- logout;
- protected Angular routes;
- application shell;
- Today empty state;
- Settings account summary;
- Problem Details;
- rate-limit handling;
- accessibility baseline;
- frontend and backend tests;
- CI;
- dependency auditing.

### M2 - Profile and time

- editable display name;
- read-only email, with the reason stated in the interface;
- persisted IANA time zone;
- browser time-zone suggestion offered as a suggestion only;
- server-side time-zone validation;
- local date on Today, rendered in English;
- abstract clock;
- profile preferences owned per account;
- deterministic time tests.

Delivered. The observable outcome is that a user can name themselves, choose their time zone, and
see the correct calendar day for that zone, with the values persisted in SQL Server and protected
by server-side ownership.

### M3 - Daily operating system

Delivered. This milestone builds the first honest version of every daily module at once, because a
day is only useful when it can be seen whole:

- calendar items of four kinds, timed or untimed, repeating or not, with idempotent per-day
  completion, reopening, and cancellation;
- a calendar page with a month grid, a daily agenda, a next-seven-days section, and an accessible
  day planner;
- recurring routines with calculated occurrences;
- workout recording: sets, repetitions, weight, target beside actual;
- meals, calories, and a user-chosen daily target;
- study projects, weekly sessions, and safe resource links;
- one daily reflection per day;
- an integrated Today screen that reports only what the user actually entered.

The observable outcome is that a user can plan a day, execute it, record what happened across
training, food, and study, reflect on it, and see all of it in one place, with every record owned
by exactly one account.

Habits as a separate module were folded into routines: a habit is a routine with one step, and a
second recurrence engine would have had to be tested twice to be trusted once.

### M4 - Weekly review

- aggregates across the modules delivered in M3;
- plan against execution;
- patterns;
- one experiment for the next week.

### M5 - Trends and export

- honest history once enough days exist;
- export;
- deletion path.

### M6 - Refinement

- delivered first slice: Light, Dark, and System themes with a Settings appearance control;
- recurrence exceptions if real use demands them;
- richer nutrition entry;
- study material organization.

### M7 - PWA and reminders

- installation;
- limited offline support;
- push;
- scheduler;
- private-data caching review;
- quiet hours.

### M8 - Hardening

- email confirmation;
- account recovery;
- MFA or passkeys;
- production Content Security Policy;
- Trusted Types evaluation;
- security headers;
- backup;
- restore test;
- staging;
- observability;
- abuse monitoring;
- production readiness review.

## 13. Future metrics

- active days;
- completed daily closes;
- completed tasks;
- recorded habits;
- weekly reviews;
- capture time;
- accepted recommendations;
- continued use.

Metrics must not become punishment. They must not expose private content, reward unhealthy behavior, or pressure the user into artificial engagement.

## 14. Milestone acceptance philosophy

A milestone is accepted only when:

- its observable user outcome works;
- private data remains protected;
- server-side authorization is present;
- loading, error, and empty states exist;
- accessibility is reviewed;
- negative security cases are tested;
- builds and audits pass;
- documentation describes the real implementation;
- unfinished work is named honestly.
