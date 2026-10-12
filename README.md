# AcxiomCRM - Enterprise Customer Relationship Management System

AcxiomCRM is a full-featured, role-based CRM application built with **ASP.NET Core 8.0 MVC**, **Entity Framework Core**, and **ASP.NET Core Identity**. The system enforces business workflows across the customer-sales lifecycle, role-scoped data authorization, audit compliance logging, dashboard analytics, and RESTful APIs.

---

## Key Features & Modules

1. **Authentication & Security**
   - Built on ASP.NET Core Identity with PBKDF2 adaptive password hashing.
   - Comprehensive password policy (min 8 characters, digit, uppercase, lowercase, non-alphanumeric).
   - Account lockout after 5 consecutive failed attempts (15-minute cooldown).
   - Anti-forgery validation (`[ValidateAntiForgeryToken]`) on state-changing requests.
   - Immutable audit logging on login, logout, and security events.

2. **Role-Based Authorization**
   - **Admin**: Full administrative control over users, roles, audit trails, and all CRM records.
   - **Manager**: Team-wide visibility over customers, leads, opportunities, follow-ups, and reports.
   - **Sales Executive**: Scoped visibility limited strictly to assigned accounts, leads, deals, and activities.

3. **Customer Management**
   - Complete CRUD operations with search, status filtering, and pagination.
   - Business validations: unique email and phone constraints to prevent duplicates.
   - Detailed customer view with tabs for related opportunities, follow-ups, and interaction history.

4. **Lead Management & Conversion**
   - Full lead lifecycle: `New` &rarr; `Contacted` &rarr; `Qualified` &rarr; `Converted` / `Lost`.
   - Single-click lead conversion into an active Customer account and optional sales Opportunity.

5. **Opportunity & Sales Pipeline**
   - Pipeline stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
   - Business validations: Amount > 0, Probability (0–100%), and future-dated Expected Close Date for active deals.
   - Real-time probability-weighted pipeline calculation: `(Amount × Probability) / 100`.

6. **Follow-Up & Activity Scheduling**
   - Schedule calls, meetings, emails, and demos against customers, leads, or deals.
   - Rule enforcement: planned follow-ups cannot be scheduled in the past.
   - Overdue highlighting and instant completion workflow.

7. **Dashboard & Analytics**
   - Role-scoped KPI cards (Total Customers, Total Leads, Open Leads, Pipeline Value, Won/Lost Deals).
   - Filter by date range: *Today*, *This Week*, *This Month*, *All Time*.
   - Interactive Chart.js visualizations:
     - Lead Status Distribution (Doughnut)
     - Opportunity Pipeline by Stage (Bar)
     - Monthly Sales & Outcomes (Line)

8. **Audit Trail**
   - Immutable journal recording: User, Action (Create, Update, Delete, Login, Security), Module, Record ID, and IP address.
   - Filterable by user, module, action, and date range.

9. **REST API**
   - Clean DTO-based endpoints returning standard HTTP status codes (200, 201, 400, 404, 409):
     - `GET /api/customers`, `POST /api/customers`, `GET /api/customers/{id}`, `PUT /api/customers/{id}`, `DELETE /api/customers/{id}`
     - `GET /api/leads`, `POST /api/leads`, `GET /api/leads/{id}`
     - `GET /api/opportunities`, `POST /api/opportunities`, `GET /api/opportunities/{id}`
     - `GET /api/reports/pipeline`

---

## Pre-Configured Demo Accounts

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@123` | Full system access, User admin, Audit logs |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123` | Team pipeline, Reports, all CRM records |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@123` | Scoped to assigned records |
| **Sales Executive** | `sales2@acxiomcrm.com` | `Sales@123` | Scoped to assigned records |

*(A 1-click credentials autofill panel is also provided directly on the Login page for convenience during evaluation).*

---

## How to Run

1. Open a terminal in the project directory:
   ```bash
   dotnet run
   ```
2. Navigate to `http://localhost:5000` (or the URL indicated in the console) in your browser.
3. Database (`AcxiomCRM.db`) and demo records across all stages and roles are created and seeded automatically on first launch.
