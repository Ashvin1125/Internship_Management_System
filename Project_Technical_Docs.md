# Internship Management System - Technical Documentation

This document provides a deep-dive into the architectural and implementation details of the Internship Management System (IMS).

## 1. System Architecture
The application is built on **ASP.NET Core 8 MVC**, following a decoupled layered architecture for scalability and maintainability.

- **Frontend**: Razor Views, Bootstrap 5, and Custom Vanilla CSS for a premium administrative aesthetic.
- **Backend**: C# with .NET 8.
- **Data Layer**: Entity Framework Core (OR/M) with SQL Server.
- **Identity**: Custom Claims-based Authentication with Cookie-based persistence.

---

## 2. Stateless Multi-Session Tab Support
The system implements a rare and robust **URL-based Session Isolation** strategy, allowing users to log into different accounts in multiple browser tabs simultaneously.

### Core Implementation:
1.  **URL Prefixing**: Every request is prefixed with a unique session identifier (`sid`), e.g., `/s5a21/Admin/Index`.
2.  **`MultiSessionCookieManager`**: A custom implementation of `ICookieManager` that dynamically names the authentication cookie based on the `sid` in the request path (e.g., `.IMS.Auth.s5a21`).
3.  **Middleware Integration**: 
    - A custom middleware in `Program.cs` automatically detects missing SIDs and redirects users to a fresh, isolated context.
    - It ensures that visiting `/Account/Login` always generates a brand-new `sid`, effectively starting a fresh session.
4.  **Perspective Persistence**: JavaScript and Layout helpers ensure that the `sid` is propagated through all links (`<a>`), form submissions, and AJAX calls.

---

## 3. Zero-Refresh Interaction Model
Most data-modifying actions in the IMS are performed asynchronously to provide a "Single Page App" feel while retaining the SEO and structure of an MVC app.

- **Global AJAX Interceptor**: Located in `wwwroot/js/site.js`, this handler intercepts all forms marked with `data-ajax="true"`.
- **SweetAlert2 Integration**: Replaces standard browser alerts with high-fidelity, premium notifications for Success, Error, and Validation feedback.
- **Controller Versatility**: Action methods detect `X-Requested-With: XMLHttpRequest` headers and return specialized JSON payloads instead of full-page redirects.

---

## 4. Security Hardening
The system is protected against common web vulnerabilities through a "Hardening Layer" applied across all controllers.

- **CSRF (Cross-Site Request Forgery)**: Every POST action is enforced with `[ValidateAntiForgeryToken]` and matched against a hidden token in the client-side forms.
- **IDOR (Insecure Direct Object Reference) Prevention**: 
    - **Ownership Check**: Before a Student uploads or deletes a document, the server verifies the `StudentId` against the authenticated `UserId`.
    - **Guardian-only Access**: Guides can only view/download documents for students specifically assigned to them in the database.
- **Stateless Authentication**: No server-side session state is used for identity; all credentials reside within encrypted, path-scoped cookies.

---

## 5. Deployment Considerations
- **Environment**: Ensure the `appsettings.json` contains a valid `DefaultConnection` string for the SQL Server instance.
- **Uploads**: The application requires write permissions for the `wwwroot/uploads` directory to facilitate document management.
