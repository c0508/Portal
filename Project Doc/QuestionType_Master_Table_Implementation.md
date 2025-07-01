# QuestionType Master Table Implementation

## Overview

The ESG Platform has been enhanced to use a **QuestionType master table** approach instead of hardcoded enum values. This provides better data integrity, maintainability, and extensibility.

## What Was Changed

### 1. Database Schema
- **Added `QuestionTypes` table** with comprehensive metadata for each question type
- **Added `QuestionTypeMasterId` foreign key** to Questions table (nullable for backward compatibility)
- **Kept existing `QuestionType` enum** for backward compatibility
- **Migration populates master table** and links existing questions automatically

### 2. New QuestionType Master Table Structure
```sql
CREATE TABLE QuestionTypes (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(50) NOT NULL,              -- "Text", "LongText", etc.
    Name nvarchar(100) NOT NULL,             -- "Text Input", "Long Text Area"
    Description nvarchar(500),               -- Detailed description
    InputType nvarchar(50),                  -- "text", "textarea", "number", etc.
    RequiresOptions bit NOT NULL DEFAULT 0,  -- True for Select, Radio, Checkbox
    IsActive bit NOT NULL DEFAULT 1,         -- Enable/disable question types
    DisplayOrder int NOT NULL DEFAULT 0,     -- Ordering in dropdowns
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
);
```

### 3. Seeded Question Types
The migration automatically populates 10 question types:

| Code | Name | Description | Input Type | Requires Options |
|------|------|-------------|------------|------------------|
| Text | Text Input | Single line text input for short answers | text | No |
| LongText | Long Text Area | Multi-line text area for detailed responses | textarea | No |
| Number | Number Input | Numeric input for quantities, percentages, or calculations | number | No |
| Date | Date Picker | Date selection for deadlines, milestones, or time-based data | date | No |
| YesNo | Yes/No Choice | Simple binary choice for compliance or boolean questions | radio | No |
| Select | Dropdown List | Single selection from a predefined list of options | select | **Yes** |
| Radio | Radio Buttons | Single selection displayed as radio buttons | radio | **Yes** |
| MultiSelect | Multi-Select List | Multiple selections from a predefined list | multiselect | **Yes** |
| Checkbox | Checkbox Options | Multiple checkbox selections | checkbox | **Yes** |
| FileUpload | File Upload | Document or file attachment for evidence or supporting materials | file | No |

### 4. Service Layer
- **`QuestionTypeService`** provides abstraction for question type operations
- **Bridge between enum and master table** for smooth transition
- **Async methods** for database operations
- **Helper methods** for validation and display

### 5. Updated Views
- **Dynamic question type dropdowns** populated from database
- **Rich descriptions** instead of hardcoded labels
- **Data attributes** for JavaScript functionality
- **Automatic options visibility** based on `RequiresOptions` flag

## Benefits

### 1. **Data Integrity**
- ✅ Foreign key constraints ensure data consistency
- ✅ No orphaned question type references
- ✅ Centralized question type definitions

### 2. **Maintainability**
- ✅ Add new question types without code changes
- ✅ Update descriptions and metadata in database
- ✅ Single source of truth for question type information
- ✅ No more hardcoded values scattered across views

### 3. **Extensibility**
- ✅ Easy to add custom question types per organization
- ✅ Enable/disable question types dynamically
- ✅ Rich metadata support (descriptions, input types, validation rules)
- ✅ Future-proof for advanced features

### 4. **User Experience**
- ✅ Better question type descriptions in UI
- ✅ Consistent ordering across all dropdowns
- ✅ Dynamic form behavior based on metadata
- ✅ More intuitive question creation process

### 5. **Backward Compatibility**
- ✅ Existing code continues to work
- ✅ Gradual migration path
- ✅ No breaking changes to API
- ✅ Existing questions automatically linked

## Migration Details

The migration `20250623145408_AddQuestionTypeMasterTable` performs:

1. **Creates QuestionTypes table** with all metadata
2. **Adds QuestionTypeMasterId column** to Questions (nullable)
3. **Seeds all 10 question types** with proper descriptions
4. **Links existing questions** using enum-to-code mapping:
   ```sql
   UPDATE Questions 
   SET QuestionTypeMasterId = CASE QuestionType
       WHEN 0 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'Text')
       WHEN 1 THEN (SELECT Id FROM QuestionTypes WHERE Code = 'LongText')
       -- ... etc for all types
   END
   ```

## Usage Examples

### Service Usage
```csharp
// Get all active question types for dropdown
var questionTypes = await _questionTypeService.GetActiveQuestionTypesAsync();

// Check if a question type requires options
var requiresOptions = await _questionTypeService.RequiresOptionsAsync("Select");

// Get question type by enum (backward compatibility)
var questionType = await _questionTypeService.GetQuestionTypeByEnumAsync(QuestionType.Text);
```

### View Usage
```html
<!-- Dynamic dropdown from master table -->
<select asp-for="QuestionType" class="form-select">
    @foreach (var questionType in Model.AvailableQuestionTypes)
    {
        <option value="@questionType.Code" 
                data-requires-options="@questionType.RequiresOptions.ToString().ToLower()">
            @questionType.Name - @questionType.Description
        </option>
    }
</select>
```

## Future Enhancements

This foundation enables future features like:

- **Organization-specific question types**
- **Custom validation rules per question type**
- **Question type versioning**
- **Advanced input controls** (sliders, rating scales, etc.)
- **Conditional question logic**
- **Question type analytics and usage tracking**

## Testing

To verify the implementation:

1. **Check master table data:**
   ```sql
   SELECT * FROM QuestionTypes ORDER BY DisplayOrder;
   ```

2. **Verify question linking:**
   ```sql
   SELECT q.QuestionText, q.QuestionType, qt.Code, qt.Name 
   FROM Questions q 
   LEFT JOIN QuestionTypes qt ON q.QuestionTypeMasterId = qt.Id;
   ```

3. **Test question creation** - dropdowns should show rich descriptions
4. **Test options visibility** - should work automatically based on `RequiresOptions`

The implementation maintains full backward compatibility while providing a solid foundation for future enhancements. 