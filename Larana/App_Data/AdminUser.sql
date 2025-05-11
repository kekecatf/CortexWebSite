-- First, ensure the admin role exists
IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Admin')
BEGIN
    INSERT INTO Roles (Name) VALUES ('Admin')
END

-- Create the admin user
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Password, Email, RoleId)
    SELECT 'admin', 'admin', 'admin@larana.com', Id
    FROM Roles
    WHERE Name = 'Admin'
END 