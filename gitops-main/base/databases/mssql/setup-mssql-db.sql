SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT 'Checking if database $(DB_NAME) exists...';

IF DB_ID('$(DB_NAME)') IS NULL
BEGIN
    PRINT 'Database $(DB_NAME) does not exist. Creating...';
    CREATE DATABASE [$(DB_NAME)];
    PRINT 'Database $(DB_NAME) created.';
END
ELSE
BEGIN
    PRINT 'Database $(DB_NAME) already exists.';
END
GO

BEGIN TRY
BEGIN TRANSACTION;

    PRINT 'Checking if login $(DB_USER) exists...';
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(DB_USER)')
    BEGIN
        CREATE LOGIN [$(DB_USER)] WITH PASSWORD = N'$(DB_PASSWORD)', DEFAULT_DATABASE = [$(DB_NAME)];
        PRINT 'Login $(DB_USER) created.';
    END
    ELSE
    BEGIN
        PRINT 'Login $(DB_USER) already exists.';
    END
COMMIT TRANSACTION;
PRINT 'Database and login setup completed.';
END TRY
BEGIN CATCH
    PRINT 'Error occurred in login setup. Rolling back.';
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

USE [$(DB_NAME)];
GO

BEGIN TRY
BEGIN TRANSACTION;
    PRINT 'Checking if user $(DB_USER) exists in database $(DB_NAME)...';
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(DB_USER)')
    BEGIN
        CREATE USER [$(DB_USER)] FOR LOGIN [$(DB_USER)];
        PRINT 'User $(DB_USER) created.';
    END
    ELSE
    BEGIN
        PRINT 'User $(DB_USER) already exists.';
    END
    PRINT 'Adding $(DB_USER) to db_datareader role...';
    ALTER ROLE db_datareader ADD MEMBER [$(DB_USER)];
    PRINT 'Adding $(DB_USER) to db_datawriter role...';
    ALTER ROLE db_datawriter ADD MEMBER [$(DB_USER)];
    PRINT 'Adding $(DB_USER) to db_ddladmin role...';
    ALTER ROLE db_ddladmin ADD MEMBER [$(DB_USER)];
COMMIT TRANSACTION;
PRINT 'User setup in $(DB_NAME) completed.';
END TRY
BEGIN CATCH
    PRINT 'Error occurred in user setup. Rolling back.';
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO
