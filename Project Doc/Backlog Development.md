# ESG Platform Development Backlog

**Updated:** December 30, 2024  
**Last Sprint:** Enhanced Group Assignment Interface (Phase 3.1) ✅  
**Current Sprint:** Organizational Hierarchy Architecture Fix ✅

## **🚨 CRITICAL PRIORITY (P0)**

### ~~**1. Fix OrganizationRelationshipId Not Being Set (CRITICAL)**~~ ✅ **COMPLETED**
- ~~Create OrganizationRelationshipAttribute table~~ ✅ **DONE**
- ~~Fix assignment creation to set OrganizationRelationshipId~~ ✅ **DONE** 
- ~~Update bulk assignment logic for proper relationship context~~ ✅ **DONE**
- ~~Remove hardcoded organization attributes from Organization model~~ ✅ **DONE**
- ~~Migration strategy for existing data~~ ✅ **DONE**
- ~~Database schema updated to support relationship-specific attributes~~ ✅ **DONE**
- ~~Fix organization deletion foreign key constraint errors~~ ✅ **DONE**
- ~~Build comprehensive Organization Relationship Management UI~~ ✅ **DONE**
- ~~Auto-create relationships when Platform Org creates Supplier Org~~ ✅ **DONE**

**Status:** ✅ **COMPLETED** - All core infrastructure fixes implemented + full management UI
**Impact:** Critical data integrity issue resolved, proper relationship context now maintained, organization deletion properly handles dependencies, comprehensive relationship management system with automatic relationship creation

## Priority Classification
- **P0 (Critical)**: Core functionality issues
- **P1 (High)**: Essential features for MVP
- **P2 (Medium)**: Important enhancements  
- **P3 (Low)**: Advanced features and analytics

---

## Phase 1: Core Security & User Experience (P0-P1)

### 1.1 Security & Access Control (P0)
- [X] **Fix user rights system** 
  - Supplier organizations can only see questionnaires related to campaigns they are assigned
  - No campaigns from other organizations should be visible
  - *Estimated effort: 2-3 days*
  - *Dependencies: None*

### 1.2 UI/UX Improvements (P1)
- [X] **Implement vertical navigation**
  - Change navigation bars from horizontal to vertical
  - Use styling reference from `/Users/Charles.Kirby/Apps/DMApp`
  - *Estimated effort: 1-2 days*
  - *Dependencies: Complete security fixes first*

- [X] **Create delegation management views**
  - Dedicated delegation dashboard showing assigned and received delegations
  - Enhanced delegation workflow with better visibility and status tracking
  - Delegation history and timeline view
  - Bulk delegation actions and delegation templates
  - *Estimated effort: 2-3 days*
  - *Dependencies: Vertical navigation*

- [X] **Adapt navigation bar colors to corporate colors**
  - Use organization's primary and secondary colors for navigation bar styling
  - Dynamic color adaptation based on current organization context
  - Ensure proper contrast and accessibility standards
  - *Estimated effort: 1-2 days*
  - *Dependencies: Vertical navigation*

---

## Phase 2: Data Model Enhancements (P1-P2)

### 2.1 Organization Management (P1)
- [X] **Add custom organization attributes**
  - Add configurable custom fields (e.g., ABC segmentation, category)
  - Support for supplier classification
  - *Estimated effort: 2-3 days*
  - *Dependencies: None*

### 2.2 Question & Questionnaire Structure (P1)
- [X] **Add section field to questions**
  - Optional section value (string) for question classification/filtering
  - *Estimated effort: 1 day*
  - *Dependencies: None*

- [X] **Add reporting period to campaigns** (moved from questionnaires)
  - Start and end date fields for campaigns instead of questionnaires (better conceptual model)
  - Visible throughout campaign workflow and answer interface
  - *Estimated effort: 1 day*
  - *Dependencies: Question sections*

### 2.3 Question Assignment & Management (P1)
- [X] **Question assignment to users within questionnaires**
  - Assign questions by sections or individual questions to specific users
  - User-friendly interface for question assignment (not using delegation workflow)
  - Assignment overview and progress tracking per user
  - Email notifications for assigned questions
  - *Estimated effort: 3-4 days*
  - *Dependencies: Question sections*

- [X] **Question attributes master list management**
  - Administrative view to manage the master list of question attributes
  - CRUD operations for attribute types and values
  - Bulk operations for attribute management
  - *Estimated effort: 2-3 days*
  - *Dependencies: None*

- [X] **Question change tracking functionality** ✅
  - Implement controllers for existing question change tracking table
  - View to display question modification history
  - Audit trail for question updates and version control
  - *Estimated effort: 2-3 days*
  - *Dependencies: None*

---

## Phase 3: Campaign & Assignment Management (P2)

### 3.1 Bulk Assignment Features (P2)
- [X] **Enhanced group assignment interface** ✅
  - List of available companies (left panel)
  - List of selected companies (right panel)
  - "Add All" functionality for related organizations
  - Filter options (name, attributes, etc.)
  - *Estimated effort: 3-4 days*
  - *Dependencies: Organization custom attributes (✅ completed)*

---

## Phase 4: Response & Filtering Features (P2)

### 4.1 Question Filtering (P2)
- [ ] **Questionnaire response filtering**
  - Filter by: unanswered questions, question type, text search, section, question attributes
  - Real-time filtering during questionnaire completion
  - *Estimated effort: 2-3 days*
  - *Dependencies: Question sections, organization attributes*

### 4.2 Answer Pre-population (P2)
- [ ] **Previous period answer pre-population**
  - Copy answers from previous reporting periods (e.g., FY25 → FY26)
  - Mark pre-populated answers as "draft" until validated
  - Handle questionnaire version differences
  - Question matching algorithm for different questionnaire versions
  - *Estimated effort: 4-5 days*
  - *Dependencies: Reporting periods, question filtering*

---

## Phase 5: Analytics & Dashboards (P3)

### 5.1 Campaign Management Dashboard (P3)
- [ ] **Campaign progress tracking**
  - Open campaigns overview
  - Progress KPIs (completion rates, response times)
  - *Estimated effort: 3-4 days*
  - *Dependencies: All previous phases*

### 5.2 Analytics Suite (P3)
- [ ] **Global questionnaire analytics**
  - Number of companies, total answers, completion rates
  - *Estimated effort: 2-3 days*
  - *Dependencies: Campaign dashboard*

- [ ] **Question-level analytics**
  - Filter by question, attributes, sections
  - Different visualizations by question type
  - Aggregated view across all companies
  - Company-specific filtering
  - *Estimated effort: 4-5 days*
  - *Dependencies: Global analytics*

- [ ] **Comparative analytics**
  - One company vs. total comparison
  - Company vs. selection comparison
  - Benchmarking capabilities
  - *Estimated effort: 3-4 days*
  - *Dependencies: Question-level analytics*

### 5.3 Notifications & Communication (P3)
- [ ] **Email notifications for question assignments**
  - Email notifications when questions are assigned to users
  - Email templates for assignment notifications
  - Email settings and preferences management
  - Integration with existing question assignment system
  - *Estimated effort: 2-3 days*
  - *Dependencies: Question assignment system (✅ completed)*

### 5.4 Advanced Data Management (P3)
- [ ] **Cross-relationship answer reuse framework**
  - Smart answer templates based on previous responses
  - Cross-platform organization answer suggestions
  - Approval workflow for reusing answers across different platform relationships
  - Answer similarity detection and recommendation engine
  - Audit trail for answer reuse and modifications
  - Permission system for controlling answer sharing between relationships
  - *Estimated effort: 5-6 days*
  - *Dependencies: Fixed organizational hierarchy (P0), relationship-specific attributes*

---

## Development Timeline Estimate
- **Critical Fix**: 4-5 days (Week 1) - Organizational hierarchy
- **Phase 1**: 5-8 days (Weeks 1-2)
- **Phase 2**: 11-13 days (Weeks 2-4)  
- **Phase 3**: 3-4 days (Week 4)
- **Phase 4**: 6-8 days (Weeks 5-6)
- **Phase 5**: 17-22 days (Weeks 7-10) - includes answer reuse framework

**Total Estimated Duration**: 10-12 weeks

---

## Progress Tracking

### Completed ✅
- Custom login page with DMApp styling
- Authentication system improvements
- **User rights system fixed** (Phase 1.1) ✅
  - Supplier organizations now only see campaigns assigned to them
  - Platform organizations only see their own campaigns
  - Platform admins see everything
  - Access control properly enforced across all controllers
- Dashboard progress tracking fixes
- Question overview display fixes
- Clear answer functionality for questionnaires
- **Vertical navigation implemented** (Phase 1.2) ✅
  - Changed from horizontal to vertical navigation
  - Applied DMApp styling reference
- **Delegation management views created** (Phase 1.2) ✅
  - Enhanced delegation dashboard with statistics and progress tracking
  - Comprehensive delegation history with filtering and pagination
  - Detailed delegation view with timeline and action management
  - Bulk delegation interface for multiple question assignment
  - Integrated delegation management into main navigation
- **Navigation bar colors adapted to corporate colors** (Phase 1.2) ✅
  - Dynamic sidebar styling using organization's primary and secondary colors
  - Gradient background with branded color scheme for navigation
  - Enhanced accessibility with proper focus states and contrast
  - Automatic branding activation based on organization context
  - Mobile-responsive branded navigation styling
- **Add custom organization attributes** (Phase 2.1) ✅
  - Master data management system for organization attributes
  - Dynamic attribute types and values with full CRUD interface
  - Support for ABC segmentation, supplier classification, categories, industries, regions, size categories
  - Complete delete functionality with usage tracking and force delete options
  - Integration with organization create/edit forms using dynamic dropdowns
- **Section-based questionnaire navigation** (Phase 2.2) ✅
  - Questions grouped by section with "Other" section for ungrouped questions
  - Dynamic sidebar with accordion sections showing completion status and progress
  - Individual question navigation within sections with status indicators
  - Real-time progress tracking and completion percentage per section
- **Reporting period added to campaigns** (Phase 2.2) ✅
  - Moved reporting period from questionnaire level to campaign level (better design)
  - Campaign create/edit forms include optional reporting period fields
  - Reporting period displayed prominently in campaign views and answer interface
  - Clear indication to respondents of the time period for data collection
  - Expandable accordion navigation with section-level progress indicators
  - Smart status aggregation showing completion state per section
  - Real-time progress updates with visual feedback
  - Enhanced user experience with smooth scrolling and active highlighting
- **Add reporting period to questionnaires** (Phase 2.2) ✅
  - Start and end date fields for questionnaires with optional configuration
  - Database migration to add ReportingPeriodStart and ReportingPeriodEnd columns
  - Enhanced create/edit forms with date picker controls and clear labeling
  - Display integration in Details view showing formatted date ranges
  - Index view cards showing reporting periods with calendar icons
  - Copy functionality preserves reporting periods from source questionnaires
- **Question assignment to users within questionnaires** (Phase 2.3) ✅
  - Assign questions by sections or individual questions to specific users
  - User-friendly interface for question assignment (not using delegation workflow)
  - Assignment overview and progress tracking per user
  - *(Email notifications moved to separate backlog item)*
- **Question attributes master list management** (Phase 2.3) ✅
  - Administrative view to manage the master list of question attributes
  - CRUD operations for attribute types and values
  - Bulk operations for attribute management
- **Enhanced group assignment interface** (Phase 3.1) ✅
  - Dual-panel interface with available organizations (left) and selected organizations (right)
  - Real-time filtering by organization name and attributes
  - "Add All" functionality for bulk selection of filtered organizations
  - Visual indicators for organization details, user counts, and existing assignments
  - Bulk assignment creation with comprehensive error handling and validation
  - Integration into campaign management workflow with intuitive UI/UX
  - **Fixed attribute filtering** - Now properly loads relationship-specific attributes for accurate filtering
  - **Fixed platform organization attribute loading** - Platform orgs no longer show supplier attributes (attributes in relationships describe suppliers, not platforms)

### In Progress 🔄
- **Basic conditional questions system** ✅ **COMPLETED**
  - ✅ Data model for question dependencies (QuestionDependency entity)
  - ✅ Support for basic condition types (Equals, NotEquals, IsAnswered, IsNotAnswered)
  - ✅ Conditional question service with visibility logic
  - ✅ Admin interface for managing question dependencies
  - ✅ Real-time show/hide functionality in response forms
  - ✅ Integration with existing questionnaire workflow
  - ✅ AJAX-based conditional checking without page refreshes
  - ✅ Visual feedback with smooth animations
  - *Completed: 1 day implementation*

## **🔧 OUTSTANDING TODO ITEMS**

### Code TODOs Requiring Implementation (P2)
- **Email invitation system** (UserController.cs:181)
  - Send invitation email with temporary password for new users
  - Email templates for user invitations
  - SMTP configuration and email service integration
  - *Estimated effort: 2-3 days*

- **Relationship attribute usage tracking** (OrganizationAttributeController.cs:529,536)
  - Implement usage count for relationship-specific attributes
  - Usage check before allowing attribute deletion
  - Enhanced delete validation with dependency checking
  - *Estimated effort: 1-2 days*

### Completed ✅

- **Enhanced numeric question types with units and percentages** ✅ **COMPLETED**
  - ✅ Percentage handling for numeric questions (automatic % symbol, validation 0-100)
  - ✅ Unit specification system for numeric questions with predefined lists (30+ units across 8 categories)
  - ✅ Question-specific unit options (Energy: MWh, kWh, GWh; Distance: km, miles; Weight: kg, tons; Emissions: tCO2e, etc.)
  - ✅ Database schema updates for unit storage and percentage flags with comprehensive unit master table
  - ✅ UI enhancements for unit selection and display in questionnaire responses with input group styling
  - ✅ Excel import support for units and percentage questions with enhanced template
  - ✅ Frontend validation ensuring mutual exclusivity of percentage and unit options
  - ✅ Preview functionality showing percentage/unit configurations
  - ✅ Response display formatting with proper unit symbols and percentage display
  - *Completed: 1 day implementation*

### Next Up 📋

- **Excel import improvements** (P1)
  - ✅ Fixed multi-line options handling (Excel line breaks → newline format)
  - ✅ Enhanced template with clearer Alt+Enter instructions
  - Better error messages for option formatting
  - Support for percentage and unit questions in Excel import
  - *Estimated effort: 1 day*
  - *Dependencies: Enhanced numeric question types*

---

## Notes
- Each phase builds upon previous phases
- Security fixes (Phase 1.1) should be completed before any other work
- UI improvements can be done in parallel with data model changes
- Analytics features are lowest priority but highest complexity
- Consider breaking Phase 5 into smaller iterations for better delivery cadence
