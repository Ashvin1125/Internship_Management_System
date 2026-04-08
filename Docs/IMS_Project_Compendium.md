# Internship Management System (IMS) - Comprehensive Project Details

The **Internship Management System** is a robust, role-based platform designed to streamline the administration, supervision, and reporting of engineering internships. It bridges the gap between students, faculty mentors (Guides), and department administrators.

---

## 🚀 1. Executive Summary
The IMS provides a digital ecosystem where students can track their daily progress, upload project documentation, and receive formal feedback. Administrators maintain the system structure by managing user accounts and assignments, while Guides provide professional oversight through task assignments and diary approvals.

---

## 🛠️ 2. Functional Modules

### 👑 Admin Module (The Architect)
*   **User Management**: Strategic creation and management of Guide and Student accounts.
*   **Security & Integrity**: Monitoring duplicate registrations (Email/Enrollment) and handling user deactivations/deletions.
*   **Relationship Mapping**: Formal assignment of Students to Guides to establish the mentorship chain.
*   **Global Overview**: High-level dashboard showing total student count, assigned vs. unassigned students, and active guides.

### 🎓 Guide Module (The Mentor)
*   **Student Supervision**: Access to detailed profiles of all assigned students.
*   **Progress Review**: Approving or rejecting Daily Diaries with constructive feedback and comments.
*   **Task Assignment**: Creating and tracking specific internship tasks with description and deadlines.
*   **Document Assessment**: Reviewing and downloading project-related files (PPTs/Reports) uploaded by students.

### 📝 Student Module (The Intern)
*   **Internship Profiling**: Documenting company details, tech stack, and internship duration.
*   **Progress Recording**: Submitting daily work diaries for formal approval.
*   **Task Management**: Tracking assigned tasks and updating their status (In Progress/Completed).
*   **Documentation Hub**: Uploading final presentations, reports, and code ZIPs (Supporting up to 50MB per file).

---

## 🎨 3. Technical Ecosystem

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Framework** | ASP.NET Core 8 | Dual presentation base (MVC & Web API) |
| **Business Logic** | Decoupled Service Layer | Injected operations independent of controllers |
| **Database** | SQL Server | Data Persistence |
| **ORM** | Entity Framework Core | Database Interaction |
| **Authentication** | Stateless Cookie-Based RBAC | Secure Access + Tab Isolation |
| **UX Model** | Zero-Refresh AJAX | Instant UI Feedback |
| **Tab Isolation** | URL-based SID | Stateless Multi-User Support |
| **Front-End API** | JSON Web API `/api/` | Stateless DTO responses ready for React clients |
| **UI Styling** | Bootstrap 5 + Vanilla CSS | Responsive, Premium Design |
| **Icons** | Bootstrap Icons | Visual Context |
| **Notifications** | SweetAlert2 | Premium User Feedback |

---

## 🛡️ 4. Security & Robustness Features

> [!IMPORTANT]
> **Anti-Forgery & Injection Proofing**
> The system utilizes ASP.NET Core's built-in Anti-Forgery tokens on every form submission to prevent CSRF attacks.

> [!NOTE]
> **Ownership Isolation**
> Rigid logic ensures that a Student cannot see or modify another student's diaries, and a Guide cannot access students they are not supervising.

*   **Stateless Navigation**: URL-based session isolation (`/{sid}/`) ensures each tab functions as an independent browser context.
*   **Validation Layer**: Server-side checks for unique Email and Enrollment numbers.
*   **File Size Security**: Explicit 50MB limit to prevent server overload.
*   **Premium Feedback**: Replaced standard reloading with AJAX-based Zero-Refresh updates and SweetAlert2 alerts.

---

## 📂 5. Core Data Entities
1.  **User**: Central authentication entity (Name, Email, Role).
2.  **Student**: Profile details (Enrollment No, Dept, Semester).
3.  **Guide**: Mentor details (Designation, Dept).
4.  **DailyDiary**: Work log records with Approval status.
5.  **InternshipTask**: Assignment tracking from Guide to Student.
6.  **Document**: Tracking physical file uploads in the `wwwroot/uploads` directory.

---

## 🛠️ 6. Quick Setup Requirements
- **.NET 8 SDK**
- **SQL Server / LocalDB**
- Update `ConnectionStrings` in `appsettings.json`.
- Run `dotnet ef database update` to initialize schema.
- Run `dotnet run` to launch the platform.
