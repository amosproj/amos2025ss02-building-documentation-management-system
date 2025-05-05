# BitAndBeam Frontend Setup Instructions

## 💻 Developer Setup Instructions

Follow these steps after cloning the repository to get started (use administrator privileges, if necessary):

### 1. Install Node.js & npm

You need Node.js (v^18.19.1 or newer) and npm. Download and install from: [https://nodejs.org](https://nodejs.org)

Verify installation:
```bash
node -v
npm -v
```

### 2. Install Angular CLI (global)
```bash
npm install -g @angular/cli
```

### 3. Install Project Dependencies
Navigate to the frontend folder and install dependencies:
```bash
cd BitAndBeam/frontend
npm install
```

### 4. Start Development Server
```bash
ng serve
```
Visit [http://localhost:4200](http://localhost:4200) in your browser.

---

## Initial Project Setup Steps

These are the steps taken to initially set up a minimal Angular application in the `BitAndBeam/frontend` folder. Ignore if already done in master branch.

---

### 🛠 Step 1: Generate Angular Workspace
Run the following command inside the `BitAndBeam/frontend` directory to create a new Angular project (use administrator privileges, if necessary):

```bash
cd BitAndBeam/frontend
npx @angular/cli new frontend --directory=. --routing --style=css
```

This will:
- Initialize an Angular app
- Enable routing
- Use CSS for styling

---

### 🧱 Step 2: Generate Components
Generate the required components using Angular CLI:

```bash
ng generate component pages/home
ng generate component components/upload-file
```

---

# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.9.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

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
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Running linting and formatting

For linting and formatting, run:

```bash
cd BitAndBeam/frontend
npm run lint
npm run format
```

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
