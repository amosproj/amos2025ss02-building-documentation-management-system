## 🧬 OpenAPI SDK: Generate & Use

The frontend uses a **TypeScript SDK** auto-generated from the backend’s OpenAPI spec. This ensures **strong typing**, reduces manual errors, and keeps frontend-backend integration consistent.

---

### 📦 When to Regenerate the SDK

You should regenerate the SDK whenever:

- A backend route or controller changes  
- Request or response models are updated  
- New endpoints are added

---

### ⚙️ How to Generate the SDK (Dev Environment)

> 📍The SDK is generated inside a Docker container located in the `openapi-client/` folder.

#### ✅ Steps:

1. **Make sure the backend is running locally**, and its OpenAPI spec is available at:

   ```
   http://localhost:5000/swagger/v1/swagger.json
   ```

2. **Run the OpenAPI generator Docker container:**

   From the root of the project:

   ```bash
   cd BitAndBeam
   docker compose up --build openapi-client
   ```

   This will:
   - Build the OpenAPI generator image from `openapi-client/Dockerfile`
   - Generate the SDK inside the container
   - Mount it into your local `frontend/src/api` folder

> ✅ You should see TypeScript files appear in `frontend/src/api`.

> 💡 For Linux users: Replace `host.docker.internal` with `172.17.0.1` if needed, or pass the OpenAPI spec as a file.

---

### 🧑‍💻 How to Use the SDK in the Frontend

Once generated, you can import and use it like this:

```ts
import { DefaultApi, Configuration } from '@/api';

const api = new DefaultApi(new Configuration({ basePath: import.meta.env.VITE_API_URL }));

api.getSomeData().then(response => {
  console.log(response);
});
```

Make sure your `.env` or `Dockerfile` for the frontend includes:

```env
VITE_API_URL=http://backend:5000
```

---

### 🚀 How the SDK is Handled in Production

- The SDK is generated **only in development** and committed to version control.
- The **production build** (in GitHub Actions) uses the committed `frontend/src/api` code.
- The SDK is **not re-generated during production Docker builds** to ensure stability and repeatability.
- Backend should continue serving the actual API in production, as visible on /swagger/v1/swagger.json during development. Swagger UI or swagger.json are not necessary to be hosted during production.
- Frontend image should be rebuilt after SDK changes.

> ✅ This makes your production frontend always use a validated, tested version of the SDK.

---

### ✅ Summary

| Task                     | How                            |
|--------------------------|--------------------------------|
| Generate SDK (dev)       | Run Docker container manually  |
| Use in frontend          | Import from `@/api`            |
| Production build         | Uses committed SDK             |

---