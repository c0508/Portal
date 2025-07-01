
# ESG Data Platform — Technology Stack and Architecture Overview

---

## 🎯 Purpose of This Document

This guide describes the technology stack and architecture used to build the ESG Data Collection Platform. It supports structured development, consistent decision-making, and smooth onboarding for developers.

---

## 🧰 Primary Technology Stack

### 🖥️ Backend
- **Framework:** .NET 9
- **Architecture:** ASP.NET MVC (Model-View-Controller)
- **Language:** C#
- **ORM:** Entity Framework Core (EF Core)
- **Authentication:** ASP.NET Identity + OpenID Connect for Microsoft & Google SSO

### 🗃️ Database
- **RDBMS:** Microsoft SQL Server
- **Access Layer:** EF Core models + LINQ queries
- **Schema:** Normalized relational schema with:
  - Versioned questionnaires
  - Per-organization campaign assignments
  - Per-question response storage and delegation
  - Audit/change logs

### 🌐 Frontend
- **View Engine:** Razor
- **UI Framework:** Bootstrap 5
- **Client-side:** Minimal JS; optionally Chart.js for dashboard graphs

### 🧪 Testing & DevOps
- **Testing Frameworks:** xUnit for backend; Selenium or Playwright (optional) for UI
- **Migrations:** EF Core Migrations
- **Environments:** Local dev, staging, production with environment-specific configs

---

## 🧱 Key Architectural Decisions

### 1. ASP.NET MVC
- Clear separation of concerns: Controller handles routing and logic; Views render UI; Models represent data
- Makes it easy to apply role-based security and form validation
- Suitable for enterprise-style internal and B2B applications

### 2. EF Core + SQL Server
- Strongly typed access to relational data
- Flexible for querying structured ESG responses, including audit logs and longitudinal (time-series) data
- Easy integration with Power BI via SQL views or direct DB connection

### 3. Razor + Bootstrap
- Razor offers server-side rendering with full .NET type safety
- Bootstrap provides a responsive layout and consistent UI components for forms, tables, and dashboards

---

## 🔒 Security and Access
- Use ASP.NET Identity for core user management
- OpenID Connect for seamless Microsoft and Google login
- Role-based access control (RBAC) implemented via `[Authorize]` attributes
- All data access scoped by organization and user role

---

## 📊 Reporting Integration
- Frontend charts using Chart.js or similar
- Backend: SQL views and stored procedures for export to BI tools
- Normalized schema ensures consistency across time and entities

---

## 🧩 Extensibility & Future Options
- Multitenancy support: isolated data per organization
- Azure AD B2C for enterprise identity federation (optional)
- Background job queue (e.g., Hangfire) for reminders or notifications

---

This technology architecture is designed to meet high security, usability, and extensibility requirements common in ESG, compliance, and B2B data platforms.
