# BitAndBeam Frontend Setup Instructions

## 💻 Developer Setup Instructions

Follow these steps after cloning the repository to get started:

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

## Initial Project Setup Instructions

These are the steps to initially set up a minimal Angular application in the `BitAndBeam/frontend` folder. Ignore if already done in master branch.

---

## 🛠 Step 1: Generate Angular Workspace
Run the following command inside the `BitAndBeam/frontend` directory to create a new Angular project:

```bash
npx @angular/cli new frontend --directory=. --routing --style=css
```

This will:
- Initialize an Angular app
- Enable routing
- Use CSS for styling

---

## 🧱 Step 2: Generate Components
Generate the required components using Angular CLI:

```bash
ng generate component pages/home
ng generate component components/upload-file
```

---