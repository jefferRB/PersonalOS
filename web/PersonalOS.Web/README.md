# PersonalOS.Web

This is the Angular 22.1.0 web client for PersonalOS.

It implements the Milestone 1 authentication walking skeleton: login, registration, current-user state, protected routes, the application shell, Today, Settings, Problem Details handling, and antiforgery-aware auth requests.

## Development server

To start a local development server, run:

```bash
npm start
```

The project serves on `http://127.0.0.1:63901/`. The development proxy forwards relative `/api` and `/health` requests to `https://localhost:7268`, which is the HTTPS URL from the API launch profile.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
npm run build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Static analysis

The repository uses the Angular development build as the current static/template analysis gate:

```bash
npm run lint
```

## Running unit tests

To execute unit tests with Angular's configured [Vitest](https://vitest.dev/) runner, use:

```bash
npm run test -- --watch=false
```

## Running end-to-end tests

No end-to-end runner is configured in Milestone 1.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
