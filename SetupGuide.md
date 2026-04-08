# Internship Management System - Setup Guide

Follow these instructions to set up and run the Internship Management System on your local machine.

## 1. Prerequisites
Ensure you have the following installed:
- **.NET 8.0 SDK**
- **SQL Server** (LocalDB or SQLEXPRESS)
- **Visual Studio 2022** (preferred) or **VS Code** with C# Dev Kit.

---

## 2. Configuration & Database Setup

### Step 1: Update Connection String
Open `appsettings.json` and update the `DefaultConnection` to match your SQL Server instance.
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=InternshipDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
*Note: The project uses `TrustServerCertificate=True` for local development.*

### Step 2: Database Initialization
The project is configured with a `DbSeeder` that automatically creates the database and seeds initial data if it doesn't exist.
- Run the project once, and it will execute `context.Database.EnsureCreated()`.
- Alternatively, you can run migrations manually via Package Manager Console:
  ```powershell
  Update-Database
  ```

---

## 3. Running the Application

### Using Visual Studio:
1. Open the `.sln` or `.csproj` file.
2. Press `F5` or click the "Start" button (IIS Express or InternshipManagementSystem).

### Using Command Line:
1. Open terminal in the project root.
2. Run:
   ```bash
   dotnet run
   ```
3. Open your browser at the URL shown in the terminal (usually `https://localhost:7123` or `http://localhost:5234`).

---

## 4. Default Test Credentials
The `DbSeeder` creates the following default accounts for testing:

| Role | Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@example.com` | `admin` |
| **Student** | `student@example.com` | `password` |
| **Guide** | `guide@example.com` | `password` |

---

## 5. Project Structure Overview
- `Controllers/`: Contains the logic for Admin, Guide, and Student portals.
- `Models/`: Entity Framework models (User, Student, Guide, Task, etc.).
- `Views/`: Razor components for the UI.
- `Data/`: `ApplicationDbContext` and `DbSeeder`.
- `wwwroot/`: Static files (CSS, JS, Images).
