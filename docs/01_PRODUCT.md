# PersonalOS - Product, Scope, and Experience

**Version:** 1.1
**Status:** Milestone 1 Angular walking skeleton

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

### Today

- local date;
- upcoming events;
- three priorities;
- tasks;
- habits;
- calories;
- quick capture;
- recommendation;
- daily close.

### Planning

- tasks;
- events;
- projects;
- goals;
- dates;
- priorities;
- rescheduling.

### Habits

- frequency;
- days;
- completion check;
- quantity;
- duration;
- rest;
- reason for missing;
- consistency;
- recovery.

### Nutrition

- versioned goals;
- meals;
- foods;
- portions;
- calories;
- macronutrients;
- daily and weekly summaries.

### Journal

- free-form entry;
- morning reflection;
- evening close;
- win;
- problem;
- cause;
- lesson;
- adjustment.

### Weekly review

- plan versus execution;
- habits;
- nutrition;
- patterns;
- experiment for the next week.

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

Future navigation:

```text
Today
Plan
Habits
Nutrition
Journal
Review
Settings
```

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

- time zone;
- local date;
- abstract clock;
- profile preferences;
- deterministic time tests.

### M3 - Planning

- tasks;
- priorities;
- minimum Today workflow;
- server-side ownership;
- empty, conflict, and validation states.

### M4 - Habits

- definition;
- recording;
- consistency;
- recovery;
- historical integrity.

### M5 - Nutrition

- goals;
- meals;
- calories;
- macronutrients;
- safe boundaries and non-medical wording.

### M6 - Journal

- entries;
- daily close;
- privacy;
- deliberate logging and caching restrictions.

### M7 - Review

- aggregates;
- trends;
- explainable recommendations;
- weekly experiment.

### M8 - PWA and reminders

- installation;
- limited offline support;
- push;
- scheduler;
- private-data caching review;
- quiet hours.

### M9 - Hardening

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
