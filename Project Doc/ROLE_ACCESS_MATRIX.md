# ESG Platform - Role-Based Access Matrix

## 🔐 Complete Access Control Matrix

This document defines the access permissions for each role in the ESG Platform system.

| Role | My Assignments | Questionnaires | Campaigns | Delegations | Review Mgmt | My Reviews | Admin | Question Assignments |
|------|----------------|----------------|-----------|-------------|-------------|------------|-------|---------------------|
| **Responder** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **LeadResponder** | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ |
| **Reviewer** | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| **CampaignManager** | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| **OrgAdmin** | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| **PlatformAdmin** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## 📋 Role Descriptions

### **Responder**
- **Purpose**: Answer assigned questionnaires
- **Access**: Can only view and respond to their assignments
- **Limitations**: Cannot delegate, create content, or manage others
- **Use Case**: Individual contributors completing ESG questionnaires

### **LeadResponder**  
- **Purpose**: Coordinate team responses and manage workflow
- **Access**: Can delegate questions, assign reviewers, submit final responses
- **Capabilities**: 
  - Delegate questions to team members
  - Assign reviewers to responses
  - Override delegated responses if needed
  - Submit completed assignments
- **Use Case**: Team leads managing ESG response processes

### **Reviewer**
- **Purpose**: Review and validate responses for quality control
- **Access**: Can review assigned responses, provide feedback, approve/reject
- **Capabilities**:
  - Review assigned questions, sections, or entire assignments
  - Add comments and feedback
  - Request changes or approve responses
  - Track review status and history
- **Use Case**: Quality control and compliance validation

### **CampaignManager**
- **Purpose**: Create and manage ESG campaigns
- **Access**: Full campaign lifecycle management
- **Capabilities**:
  - Create and edit questionnaires
  - Launch campaigns and assign to organizations
  - Monitor campaign progress
  - Assign reviewers and manage workflow
- **Use Case**: ESG program managers running assessment campaigns

### **OrgAdmin**
- **Purpose**: Manage organization-level ESG activities
- **Access**: Organization administration and campaign management
- **Capabilities**:
  - All CampaignManager capabilities
  - Manage organization users and roles
  - Configure organization settings
  - Internal question assignment management
- **Use Case**: ESG directors managing organizational compliance

### **PlatformAdmin**
- **Purpose**: System administration and cross-organization management
- **Access**: Full system access across all organizations
- **Capabilities**:
  - All lower role capabilities
  - Cross-organization data access
  - System configuration and user management
  - Platform-wide reporting and analytics
- **Use Case**: Platform operators and system administrators

## 🎯 Access Scenarios

### **Scenario 1: External Assignments (Platform → Supplier)**
```
PlatformAdmin/OrgAdmin creates campaign → assigns to supplier organization
↓
Supplier LeadResponder receives assignment
↓
LeadResponder can:
- Delegate questions to team Responders
- Assign Reviewers for quality control
- Submit final responses after review
```

### **Scenario 2: Internal Assignments (Platform → Own Organization)**
```
OrgAdmin/CampaignManager creates internal campaign
↓
Uses Question Assignment system to distribute work:
- Assign specific questions to team members
- Assign entire sections to departments
- Set up multi-level review workflow
↓
Reviewers validate responses before final submission
```

## 🛡️ Security Implementation

### **Multi-Tenant Isolation**
- Organizations can only access their own data
- Platform admins can access all organizations when needed
- Database-level query filters enforce isolation

### **Controller-Level Security**
```csharp
[Authorize(Policy = "LeadResponder")]  // Minimum role required
[Authorize(Policy = "OrgAdminOrHigher")]  // Multiple role authorization
```

### **Navigation Menu Security**
- Menus dynamically show/hide based on user roles
- No unauthorized functionality visible to users
- Role-based feature discovery

## 📊 Implementation Status

- ✅ **Database Schema**: Complete with all role relationships
- ✅ **Authentication**: ASP.NET Core Identity integration
- ✅ **Authorization**: Policy-based access control
- ✅ **Navigation**: Role-based menu visibility
- ✅ **Controllers**: Permission enforcement in all actions
- ✅ **Review Workflow**: Complete reviewer assignment and feedback system
- ✅ **Question Assignment**: Internal task distribution system
- ✅ **Audit Trail**: Complete logging of all role-based actions

## 🔄 Workflow Examples

### **Review Assignment Workflow**
1. **LeadResponder** accesses Review Management
2. Assigns **Reviewer** to specific questions/sections
3. **Reviewer** sees assignment in "My Reviews"
4. **Reviewer** provides feedback and approval
5. Complete audit trail maintained

### **Question Assignment Workflow**
1. **OrgAdmin** creates internal campaign
2. Uses Question Assignment to distribute questions
3. **Responders** see only their assigned questions
4. **LeadResponder** can assign **Reviewers** for validation
5. Final submission by **LeadResponder** only

---

**Last Updated**: December 30, 2024  
**Implementation Status**: ✅ Complete and Production Ready 