# Internship Management System - Project Workflow

This document outlines the core functional workflow and logic of the Internship Management System.

## 1. System Roles & Access
The system is built with role-based access control (RBAC) featuring three primary roles:
- **Admin**: System overseer responsible for user management, assigning guides, and monitoring the overall progress.
- **Guide**: Faculty members or mentors who supervise students, assign tasks, and review student outputs.
- **Student**: Interns who perform tasks, submit progress logs, and upload required documentation.

---

## 2. Core Workflow Steps

### Phase 1: Administration & Setup (Admin)
- **User Creation**: The Admin creates Student and Guide accounts.
- **Guide Assignment**: The Admin assigns a specific Guide to a Student. This link is essential for the supervision flow.
- **System Management**: Admins have the authority to **View Details** of any student/guide for deep-dive monitoring and can **Delete** users if necessary.

### Phase 2: Internship Initiation (Student)
- **Profile Setup**: Students log in and enter their **Internship Details** (Company Name, Role, Technology, Duration, etc.).
- **Initial Review**: The assigned Guide can see the student's internship profile to understand their context.

### Phase 3: Progress & Documentation (Student)
- **Daily Diary**: Students submit a daily record of their work.
- **Document Submissions**: Students can upload critical project files (PPTs, DOCs, PDFs, ZIPs) to their **"My Documents"** section.
- **Weekly Reporting**: At the end of each week, students submit a comprehensive Weekly Report summarizing their learnings.

### Phase 4: Mentorship & Review (Guide)
- **Diary Approval**: The Guide reviews daily records and can either **Approve** or **Reject** them with comments.
- **Task Management**: Guides can assign specific **Internship Tasks** with deadlines. Students update these to "In Progress" or "Completed."
- **Document Review**: Guides have direct access to view and download all files uploaded by their assigned students for assessment.

---

## 3. Technical Architecture
- **Framework**: ASP.NET Core 8 with a Decoupled Service Layer for API Readiness.
- **Frontend Model**: Hybrid - Razor Views for current administrative operations with independent `React-Ready` API endpoints natively serving standard JSON payloads via stateless DTOs.
- **Database**: Microsoft SQL Server accessed via **Entity Framework Core**.
- **Authentication**: Secure **Cookie-based Authentication** scoped to support both standard Web Views and isolated React Web API calls natively.
- **Security & Robustness**: 
    - **Stateless Multi-Session Tab Support**: Unique URL-based session isolation (`sid`) allows multiple users in different tabs without collisions.
    - **Isolated Cookie Scoping**: Custom `ICookieManager` that prefixes authentication cookies with the session ID from the URL.
    - **CSRF Protection**: Comprehensive Anti-Forgery Token validation on all form submissions.
    - **Access Control**: Strict ownership verification for all user actions (Students can only manage their own data; Guides can only manage their assigned students).
    - **Input Validation**: Server-side uniqueness checks for Emails and Enrollment Numbers to ensure data integrity.
- **Modern UI/UX**:
    - **Zero-Refresh Updates**: Implemented global AJAX handlers with SweetAlert2 for an "app-like" experience.
    - **Dynamic Navigation**: Sidebar highlighting and automatic session context propagation.
    - **Design System**: Responsive layout using **Bootstrap 5** and custom CSS for a premium administrative experience.

---

## 4. Data Flow Overview
1.  **Admin** ➔ Registered **Students/Guides**.
2.  **Admin** ➔ Links **Student** to **Guide** (Assignment).
3.  **Student** ➔ Enters **InternshipDetails**.
4.  **Student** ➔ Records **DailyDiary** & **WeeklyReports**.
5.  **Student** ➔ Uploads **Documents** (PPT/DOC).
6.  **Guide** ➔ Reviews & **Approves** progress logs.
7.  **Guide** ➔ Assigns & Tracks **Tasks**.
8.  **Guide** ➔ Downloads & **Reviews** student documents.
