-- ===================================================================
-- ESG Test Users Creation Script (Identity Framework Compatible)
-- ===================================================================
-- Creates test users with proper Identity framework fields
-- Run this AFTER create_esg_test_data.sql

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRANSACTION;

PRINT 'Creating ESG Test Users with Identity Framework...';

-- Get the platform organization ID
DECLARE @PlatformOrgId INT;
SELECT @PlatformOrgId = Id FROM Organizations WHERE Name = 'Sterling Capital Partners';

IF @PlatformOrgId IS NULL
BEGIN
    PRINT 'ERROR: Platform organization not found. Run create_esg_test_data.sql first.';
    ROLLBACK;
    RETURN;
END;

-- ===================================================================
-- 1. CREATE PLATFORM ADMIN USER
-- ===================================================================

DECLARE @PlatformUserId NVARCHAR(450) = NEWID();
DECLARE @SecurityStamp NVARCHAR(MAX) = UPPER(REPLACE(NEWID(), '-', ''));
DECLARE @ConcurrencyStamp NVARCHAR(MAX) = NEWID();

-- Create platform admin user with all Identity fields
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'sarah.mitchell@sterlingcp.com')
BEGIN
    INSERT INTO Users (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
        TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
        FirstName, LastName, OrganizationId, IsActive, CreatedAt
    )
    VALUES (
        @PlatformUserId, 
        'sarah.mitchell@sterlingcp.com', 
        'SARAH.MITCHELL@STERLINGCP.COM',
        'sarah.mitchell@sterlingcp.com',
        'SARAH.MITCHELL@STERLINGCP.COM',
        1, -- EmailConfirmed
        'AQAAAAIAAYagAAAAEKpJ8fCJTI8VEn8+fLj1HGxmPYPzXKhyVvGcUOlBVhF4jbsYyRJdKzXQ7wqFB1fMHw==', -- Password: Password123!
        @SecurityStamp,
        @ConcurrencyStamp,
        NULL, -- PhoneNumber
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        NULL, -- LockoutEnd
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Sarah',
        'Mitchell',
        @PlatformOrgId,
        1,
        GETUTCDATE()
    );
    
    PRINT 'Created platform admin user: Sarah Mitchell';
END
ELSE
BEGIN
    SELECT @PlatformUserId = Id FROM Users WHERE Email = 'sarah.mitchell@sterlingcp.com';
    PRINT 'Platform admin user already exists: Sarah Mitchell';
END;

-- ===================================================================
-- 2. CREATE USER ROLES
-- ===================================================================

-- Create roles if they don't exist
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'PlatformAdmin')
BEGIN
    INSERT INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'PlatformAdmin', 'PLATFORMADMIN', NEWID());
    PRINT 'Created PlatformAdmin role';
END;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'User')
BEGIN
    INSERT INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'User', 'USER', NEWID());
    PRINT 'Created User role';
END;

-- Assign platform admin role
DECLARE @PlatformAdminRoleId NVARCHAR(450);
SELECT @PlatformAdminRoleId = Id FROM Roles WHERE NormalizedName = 'PLATFORMADMIN';

IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @PlatformUserId AND RoleId = @PlatformAdminRoleId)
BEGIN
    INSERT INTO UserRoles (UserId, RoleId)
    VALUES (@PlatformUserId, @PlatformAdminRoleId);
    PRINT 'Assigned PlatformAdmin role to Sarah Mitchell';
END;

-- Create UserContext for platform admin
IF NOT EXISTS (SELECT 1 FROM UserContexts WHERE UserId = @PlatformUserId)
BEGIN
    INSERT INTO UserContexts (UserId, OrganizationId, LastSwitched, CreatedAt)
    VALUES (@PlatformUserId, @PlatformOrgId, GETUTCDATE(), GETUTCDATE());
    PRINT 'Created UserContext for platform admin';
END;

-- ===================================================================
-- 3. CREATE PORTFOLIO COMPANY USERS
-- ===================================================================

DECLARE @UserRoleId NVARCHAR(450);
SELECT @UserRoleId = Id FROM Roles WHERE NormalizedName = 'USER';

-- Create users for each portfolio company
DECLARE @CompanyId INT, @CompanyName VARCHAR(200);
DECLARE @UserId NVARCHAR(450), @Email VARCHAR(200), @UserName VARCHAR(200);
DECLARE @FirstName VARCHAR(50), @LastName VARCHAR(50);

-- User data for each company
DECLARE @Users TABLE (
    CompanyName VARCHAR(200),
    FirstName VARCHAR(50),
    LastName VARCHAR(50),
    Email VARCHAR(200)
);

INSERT INTO @Users VALUES 
    ('TechFlow Solutions', 'Michael', 'Chen', 'michael.chen@techflow.com'),
    ('GreenMotors Inc', 'Lisa', 'Rodriguez', 'lisa.rodriguez@greenmotors.com'),
    ('MediCore Technologies', 'David', 'Thompson', 'david.thompson@medicore.eu'),
    ('ShopSmart Digital', 'Emma', 'Johnson', 'emma.johnson@shopsmart.com'),
    ('FinanceForward', 'James', 'Wilson', 'james.wilson@financeforward.eu'),
    ('BioAdvance Labs', 'Sophie', 'Martinez', 'sophie.martinez@bioadvance.eu'),
    ('LogisTech Global', 'Robert', 'Anderson', 'robert.anderson@logistech.com'),
    ('StyleSustain Co', 'Anna', 'Schmidt', 'anna.schmidt@stylesustain.eu'),
    ('DataSecure Pro', 'Thomas', 'Brown', 'thomas.brown@datasecure.com'),
    ('EcoManufacture Ltd', 'Maria', 'Garcia', 'maria.garcia@ecomanufacture.eu');

DECLARE user_cursor CURSOR FOR
    SELECT o.Id, o.Name, u.FirstName, u.LastName, u.Email
    FROM Organizations o
    INNER JOIN @Users u ON o.Name = u.CompanyName
    WHERE o.Type = 2 -- Supplier organizations
    ORDER BY o.Name;

OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @CompanyId, @CompanyName, @FirstName, @LastName, @Email;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        SET @UserId = NEWID();
        SET @SecurityStamp = UPPER(REPLACE(NEWID(), '-', ''));
        SET @ConcurrencyStamp = NEWID();
        
        INSERT INTO Users (
            Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
            TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
            FirstName, LastName, OrganizationId, IsActive, CreatedAt
        )
        VALUES (
            @UserId,
            @Email,
            UPPER(@Email),
            @Email,
            UPPER(@Email),
            1, -- EmailConfirmed
            'AQAAAAIAAYagAAAAEKpJ8fCJTI8VEn8+fLj1HGxmPYPzXKhyVvGcUOlBVhF4jbsYyRJdKzXQ7wqFB1fMHw==', -- Password: Password123!
            @SecurityStamp,
            @ConcurrencyStamp,
            NULL, -- PhoneNumber
            0, -- PhoneNumberConfirmed
            0, -- TwoFactorEnabled
            NULL, -- LockoutEnd
            1, -- LockoutEnabled
            0, -- AccessFailedCount
            @FirstName,
            @LastName,
            @CompanyId,
            1,
            GETUTCDATE()
        );
        
        -- Assign User role
        INSERT INTO UserRoles (UserId, RoleId)
        VALUES (@UserId, @UserRoleId);
        
        -- Create UserContext
        INSERT INTO UserContexts (UserId, OrganizationId, LastSwitched, CreatedAt)
        VALUES (@UserId, @CompanyId, GETUTCDATE(), GETUTCDATE());
        
        PRINT 'Created user: ' + @FirstName + ' ' + @LastName + ' (' + @CompanyName + ')';
    END
    ELSE
    BEGIN
        PRINT 'User already exists: ' + @Email;
    END;
    
    FETCH NEXT FROM user_cursor INTO @CompanyId, @CompanyName, @FirstName, @LastName, @Email;
END;

CLOSE user_cursor;
DEALLOCATE user_cursor;

-- ===================================================================
-- SUMMARY
-- ===================================================================

DECLARE @UserCount INT, @CompanyCount INT;
SELECT @UserCount = COUNT(*) FROM Users;
SELECT @CompanyCount = COUNT(*) FROM Organizations WHERE Type = 2;

PRINT '=== ESG Test Users Creation Complete ===';
PRINT 'Total users created: ' + CAST(@UserCount AS VARCHAR);
PRINT 'Total portfolio companies: ' + CAST(@CompanyCount AS VARCHAR);
PRINT 'All users have password: Password123!';
PRINT 'Platform admin: sarah.mitchell@sterlingcp.com';

COMMIT; 