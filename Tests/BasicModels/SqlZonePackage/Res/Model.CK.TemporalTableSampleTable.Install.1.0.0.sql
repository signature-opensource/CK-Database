--[beginscript]

CREATE TABLE CK.[a Temporal Table Sample] (
 DepartmentID    int NOT NULL IDENTITY(1,1) PRIMARY KEY,
  DepartmentName  varchar(50) NOT NULL,
  ManagerID       int NULL,
  ValidFrom       datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
  ValidTo         datetime2 GENERATED ALWAYS AS ROW END   NOT NULL,
  PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = CK.[Temporal Table Sample History]))

--[endscript]
