<div align="center">
  <h1>BitAndBeam Frontend</h1>
  <p>Intelligent Document Management System - Angular Web Interface</p>
</div>

![Angular](https://img.shields.io/badge/Angular-19.2.9-dd0031.svg)
![TypeScript](https://img.shields.io/badge/TypeScript-5.4.x-blue.svg)
![OpenAPI](https://img.shields.io/badge/OpenAPI-Generated_Client-green.svg)

## Overview

The BitAndBeam frontend provides a modern, responsive user interface for the document management system. Built with Angular, it offers an intuitive experience for managing building-related documents with features including:

- **Document Upload**: Drag-and-drop interface for adding new documents
- **Building Management**: Organize documents by building and location
- **Advanced Search**: Natural language search capabilities
- **Document Classification**: AI-assisted document categorization interface
- **User Authentication**: Secure login and role-based access control
- **Responsive Design**: Works seamlessly on desktop and mobile devices

## Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** v18.19.1 or newer - [Download](https://nodejs.org)
- **npm** (comes with Node.js)
- **Angular CLI** - For development tasks

Verify your installations:
```bash
node -v
npm -v
ng version
```

## Getting Started

### Development Setup

1. **Install Angular CLI** globally:
```bash
npm install -g @angular/cli
```

2. **Clone the repository**:
```bash
git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git
```

3. **Install project dependencies**:
```bash
cd amos2025ss02-building-documentation-management-system/BitAndBeam/frontend
npm install
```

4. **Start the development server**:
```bash
ng serve
```

5. **Access the application**:
Open your browser and navigate to [http://localhost:4200](http://localhost:4200)

## Project Architecture

### Directory Structure

```
/frontend
├── src/                   # Source code
│   ├── app/               # Application components
│   │   ├── components/    # Reusable UI components
│   │   ├── pages/         # Page-level components
│   │   ├── services/      # API and utility services
│   │   └── shared/        # Shared models and utilities
│   ├── assets/            # Static assets (images, icons)
│   ├── environments/      # Environment configuration
│   ├── api/               # Generated API client
│   └── styles/            # Global styles
├── angular.json           # Angular CLI configuration
├── Dockerfile             # Container definition
└── nginx.conf            # Web server configuration
```

### Key Components

- **Login & Authentication**: Secure JWT-based authentication
- **Building Dashboard**: Overview of buildings and related documents
- **Document Upload**: File upload with drag-and-drop and metadata editing
- **Document Browser**: Advanced filtering and searching capabilities
- **User Management**: Organization and user administration

### API Integration

The frontend uses an auto-generated OpenAPI client to communicate with the backend API. The client is regenerated automatically when the backend Swagger definition changes, ensuring type-safe API communication.

## Development Guide

### Available Commands

| Command | Description |
|---------|-------------|
| `ng serve` | Start development server at http://localhost:4200 |
| `ng build` | Build production-ready assets in `dist/` directory |
| `ng test` | Run unit tests with Karma |
| `ng lint` | Run linting checks |
| `npm run format` | Format code according to project standards |
| `ng generate component path/name` | Generate a new component |
| `ng generate service path/name` | Generate a new service |

### Creating New Features

When adding a new feature:

1. **Create a feature module** (optional for larger features):
   ```bash
   ng generate module features/feature-name --routing
   ```

2. **Generate components**:
   ```bash
   ng generate component features/feature-name/components/component-name
   ```

3. **Generate services** if needed:
   ```bash
   ng generate service features/feature-name/services/service-name
   ```

### Code Style and Best Practices

- Follow Angular style guide recommendations
- Use TypeScript interfaces for data models
- Implement lazy loading for feature modules
- Use reactive forms for complex form handling
- Maintain separation of concerns (components, services, models)

### Building for Production

```bash
ng build --configuration=production
```

Built files will be in the `dist/` directory, ready for deployment.

### Docker Deployment

To build and run the frontend in a Docker container:

```bash
# Build image
docker build -t bitandbeam-frontend .

# Run container
docker run -p 8080:80 bitandbeam-frontend
```

### Testing

```bash
# Unit tests
ng test

# Test coverage report
ng test --code-coverage

# Linting
ng lint
```

## Key Features Explained

### Document Upload

The document upload component supports:
- Drag-and-drop functionality
- Multi-file uploads
- Progress tracking
- Automatic metadata extraction
- Manual metadata editing before submission

### Document Processing

When a document is uploaded:
1. Frontend sends file to backend API
2. Backend processes the document (OCR, text extraction)
3. AI classification suggests document type
4. User can verify and correct classification if needed
5. Document becomes searchable in the system

### Search Capabilities

The search interface allows users to:
- Use natural language queries
- Filter by document attributes (type, date, building)
- Sort and group results
- Preview document content
- Export search results

## Configuration

### Environment Configuration

The application uses environment files for configuration:
- `environment.ts` - Development settings
- `environment.prod.ts` - Production settings

Key configuration options include:
- `apiUrl` - Backend API URL
- `authTokenKey` - LocalStorage key for auth token
- `defaultPageSize` - Default items per page

## Troubleshooting

### Common Issues

- **API Connection Errors**: Verify the backend server is running and `apiUrl` is correctly configured
- **CORS Issues**: Ensure the backend allows requests from the frontend origin
- **Authentication Failures**: Check that the JWT token is being sent correctly

## Resources

- [Angular Documentation](https://angular.dev/)
- [TypeScript Documentation](https://www.typescriptlang.org/docs/)
- [Angular Material](https://material.angular.io/) - UI component library used
- [RxJS Documentation](https://rxjs.dev/) - For reactive programming
