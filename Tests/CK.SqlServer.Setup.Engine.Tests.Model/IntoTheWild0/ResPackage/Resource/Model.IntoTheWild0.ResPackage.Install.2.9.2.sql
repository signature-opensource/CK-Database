-- CKCore.tSystem.DefaultLCID is French (12)
alter table CKCore.tSystem add 
	DefaultLCID int constraint DF_tSystem_DefaultLCID default(12);
