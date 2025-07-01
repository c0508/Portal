# ✅ InfoSec Compliance Checklist for .NET MVC Web App

This checklist helps ensure the application meets basic InfoSec requirements before submitting for internal security review or onboarding (e.g., BEAD process).

---

## 🔐 Authentication & Authorization
- [ ] ASP.NET Identity or external provider used (e.g., Azure AD, OAuth)
- [ ] Role-based or claim-based access control implemented
- [ ] MFA (if required) enabled
- [ ] Strong password policy and account lockout in place

## 🛡️ Data Protection
- [ ] Secrets and connection strings stored securely (User Secrets / Env Vars / Key Vault)
- [ ] HTTPS enforced via HSTS or `RequireHttpsAttribute`
- [ ] SQL Injection mitigated via EF Core (no raw SQL unless parameterized)
- [ ] Sensitive data encrypted at rest and in transit

## 🔍 Logging & Monitoring
- [ ] Logging implemented using ILogger / Serilog / NLog
- [ ] Login attempts and access to sensitive resources are logged
- [ ] Logs stored securely, with retention and rotation policies

## 🧪 Input Validation & Output Encoding
- [ ] Model validation using `[Required]`, `[StringLength]`, etc.
- [ ] Razor views escape HTML by default; no unsafe `@Html.Raw()` use
- [ ] Anti-XSS protection verified

## 🕵️ Session Management
- [ ] Session timeouts configured
- [ ] Anti-forgery tokens implemented (`@Html.AntiForgeryToken()` and `[ValidateAntiForgeryToken]`)

## 📁 Dependency & Patch Management
- [ ] All NuGet packages up to date (`dotnet list package --outdated`)
- [ ] Dependency vulnerability scanning (Dependabot / OWASP Dependency-Check)
- [ ] Regular patching schedule in place

## 🔒 Database Security
- [ ] Use least privilege DB credentials
- [ ] Migrations audited and reviewed
- [ ] No raw SQL or direct user input in queries

## 📄 Documentation & Diagrams
- [ ] Architecture diagram prepared (data flow, system components)
- [ ] Completed BEAD or equivalent InfoSec document
- [ ] Sample configs, sanitized logs, and dependency list included in submission

---

_Last reviewed: {{ update with date }}_
