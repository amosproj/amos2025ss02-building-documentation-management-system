## 🧬 OpenAPI SDK: Generate & Use

The frontend uses a **TypeScript SDK** auto-generated from the backend’s OpenAPI spec. This ensures **strong typing**, reduces manual errors, and keeps frontend-backend integration consistent.

---

### 📦 When to Regenerate the SDK

The SDK is automatically regenerated when:

- Backend API changes are pushed to main branch
- OpenAPI client generator configuration changes

You should manually regenerate the SDK (using docker compose) whenever:

- Manual regeneration is needed during local development

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

The SDK generation is fully automated in the CI/CD pipeline:

1. **Automatic Generation**
   - A GitHub Actions workflow monitors backend API changes
   - When changes are detected, it automatically regenerates the SDK
   - The workflow uses the same Docker-based approach as local development

2. **Review Process**
   - Generated SDK changes are submitted as a pull request
   - PR is automatically labeled with "automated" and "api-client"
   - Team can review the SDK changes before merging

3. **Version Control**
   - Generated SDK is tracked in version control (`frontend/src/api`)
   - This ensures consistent SDK versions across all environments
   - Frontend builds use the committed SDK version

4. **Production Deployment**
   - Backend continues serving the API in production
   - Frontend uses the validated, committed SDK version
   - No runtime SDK generation in production for stability

> ✅ This automated process ensures SDK consistency while maintaining code quality through review.

---

### ✅ Summary

| Task                    | How                                      |
|-------------------------|------------------------------------------|
| Generate SDK (dev)      | Run Docker container manually            |
| Generate SDK (prod)     | Automated via GitHub Actions             |
| Review Changes          | Through auto-generated pull requests     |
| Use in frontend         | Import from `@/api`                      |
| Production build        | Uses committed SDK version               |

---