@echo off
echo Dropping and recreating the database...

sqlcmd -S "(LocalDb)\MSSQLLocalDB" -Q "IF EXISTS(SELECT * FROM sys.databases WHERE name = 'LaranaDB') DROP DATABASE [LaranaDB]"
if %ERRORLEVEL% NEQ 0 (
    echo Error dropping database.
    pause
    exit /b %ERRORLEVEL%
)

echo Database dropped successfully.
echo Starting the application to recreate the database with fixed constraints...

echo You can now run the application and the database will be recreated with the correct foreign key constraints.
pause 