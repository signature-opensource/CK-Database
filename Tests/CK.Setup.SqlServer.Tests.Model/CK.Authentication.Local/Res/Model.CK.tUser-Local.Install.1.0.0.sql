
alter table [CK].[tUser] add 
	Pwd nvarchar(48) not null constraint DF_tUser_Pwd default(N''),
	CryptedPassword bit not null constraint DF_tUser_CryptedPassword default(0);