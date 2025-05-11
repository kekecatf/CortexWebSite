@echo off
echo Database Fix Script
echo -----------------
echo.

echo 1. Checking SQL connection...
sqlcmd -S "(LocalDb)\MSSQLLocalDB" -Q "SELECT 'Connection successful!'" -b
if %ERRORLEVEL% NEQ 0 (
    echo SQL connection failed.
    goto error
)

echo 2. Checking database...
sqlcmd -S "(LocalDb)\MSSQLLocalDB" -Q "IF EXISTS (SELECT * FROM sys.databases WHERE name = 'LaranaDB') SELECT 'Database exists'; ELSE SELECT 'Database does not exist'" -b
if %ERRORLEVEL% NEQ 0 (
    echo Database check failed.
    goto error
)

echo 3. Running database fixes...
sqlcmd -S "(LocalDb)\MSSQLLocalDB" -d LaranaDB -i FixDatabaseSchema.sql -b
if %ERRORLEVEL% NEQ 0 (
    echo Fix script execution failed. Database might not exist or tables not created yet.
    goto error
)

echo.
echo Database fixes completed successfully!
goto end

:error
echo.
echo An error occurred during the database fix process.
echo.
echo The most likely issue is:
echo 1. Connection to SQL Server failed
echo 2. LaranaDB database does not exist yet
echo 3. Ratings table doesn't exist yet
echo.
echo Try running the application first to create the database, then run this script again.

:end
pause 