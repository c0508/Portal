
# ESG Data Collection Platform — Complete Application Blueprint

## 🎯 Objective
Build a secure, scalable, web-based platform that allows organizations (e.g., private equity firms, corporations) to launch ESG (Environmental, Social, Governance) data collection campaigns targeting multiple entities (e.g., portfolio companies, suppliers, business units). Each target can receive a customized questionnaire depending on attributes like sector, geography, or size.

---

## 👤 Core User Roles
- **Platform Admin**: Manages system-wide settings.
- **Org Admin**: Manages users and branding for their org.
- **Campaign Manager**: Creates questionnaires and launches campaigns.
- **Lead Responder**: Assigned per organization to manage questionnaire completion.
- **Responder**: Assigned to answer specific questions.
- **Reviewer**: Views and verifies submissions.

---

## 🔐 Key Functional Requirements

### ✅ Authentication
- Microsoft & Google SSO (OpenID Connect)
- Fallback email/password login

### ✅ Organizations & Users
- Organizations with branding (logo, color theme)
- Role-based user management (RBAC)
- Users belong to one organization

### ✅ Questionnaires
- Templates by sector (e.g., SASB-based)
- Question types: text, number, select, file upload, yes/no, etc.
- Versioning (immutable snapshots)
- Standard variable linkage (e.g., Scope 1 emissions)
- Conditional logic (if A = Yes, show B)

### ✅ Campaigns
- Target multiple organizations
- Each org receives a specific questionnaire version
- Assign a lead responder for each org
- Set deadlines, instructions
- Track progress and submission

### ✅ Delegation Workflow
- Lead responder can answer or assign questions to teammates
- Responses saved per user, with full audit trail
- Files uploaded per question (required/optional)
- Submission “locks” data unless a change is requested

### ✅ Dashboards
- Campaign manager dashboard for progress tracking
- Time series and cross-org KPI comparison
- Filters for org, campaign, KPI, sector

### ✅ Security
- Role-based access (API + views)
- Strict org-level data isolation
- Logged activity (who answered, delegated, edited)

### ✅ Reporting
- Razor-based charts (Chart.js)
- Normalized SQL schema for Power BI
- Future: PDF/Excel export

---

## 📦 Data Model Entities (Simplified Overview)
- `Organization`, `User`
- `Questionnaire`, `QuestionnaireVersion`, `Question`
- `Campaign`, `CampaignAssignment`
- `Response`, `ResponseChange`, `Delegation`
- `FileUpload`

Each campaign assignment points to a specific questionnaire version per organization.

---

# ✅ Project Task Breakdown

## Phase 1: Setup & Authentication
- [ ] Initialize .NET 9 MVC project with Razor & Bootstrap
- [ ] Configure EF Core & SQL Server
- [ ] Implement Microsoft and Google SSO
- [ ] Add fallback email/password login

## Phase 2: Models & Migrations
- [ ] Create all EF models (Organization, User, etc.)
- [ ] Define relationships and navigation properties
- [ ] Add DbContext and initial migration

## Phase 3: Org & User Management
- [ ] CRUD for organizations
- [ ] User invite & role management
- [ ] Role-based access enforcement

## Phase 4: Questionnaire Builder
- [ ] Create and edit questionnaire templates
- [ ] Add versioning logic
- [ ] Support all question types
- [ ] Conditional logic input

## Phase 5: Campaign Management
- [ ] Launch campaign with multiple orgs
- [ ] Assign questionnaire version per org
- [ ] Assign lead responders
- [ ] Add instructions and deadline

## Phase 6: Questionnaire Response
- [ ] Show questions to lead responder
- [ ] Support delegation per question
- [ ] Save answers, track changes
- [ ] Submit and lock logic

## Phase 7: Monitoring & Reminders
- [ ] Dashboard for campaign progress
- [ ] Manual, scheduled, and conditional reminders

## Phase 8: Dashboards & Charts
- [ ] Time series and cross-org views
- [ ] Filters for campaign, org, sector
- [ ] Chart rendering using Chart.js

## Phase 9: Branding & Polish
- [ ] Per-org logo and theme
- [ ] Polished Bootstrap UI

## Phase 10: Final QA
- [ ] Test full flow end-to-end
- [ ] Validate delegation, submission, reporting

---

This blueprint and task list should guide a developer or agent step-by-step with clarity on scope, priorities, and structure.
